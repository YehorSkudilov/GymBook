using GymBook.Services;

namespace GymBook.Controls;

/// <summary>
/// Calories eaten against the day's goal as a ring that fills clockwise from the top; past the goal, the extra wraps
/// round again in <paramref name="overColor"/>. The middle shows what's left (or over). Sweeps in as it's revealed.
/// </summary>
public class CalorieRingDrawable(double eaten, double? goal, Color color, Color overColor, string centerValue, string centerLabel)
    : IDrawable, IRevealable
{
    static readonly Color Track = Color.FromArgb("#1D212C"), ValueColor = Color.FromArgb("#F4F6FB"), LabelColor = Color.FromArgb("#9AA3B5");

    public float Reveal { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height);
        var thickness = size * 0.1f;
        var r = size / 2 - thickness / 2 - 2;
        var box = new RectF(rect.Center.X - r, rect.Center.Y - r, r * 2, r * 2);
        canvas.StrokeSize = thickness;
        canvas.StrokeColor = Track;
        canvas.DrawEllipse(box);

        // Without a goal the ring just shows something was eaten.
        var share = goal is > 0 ? eaten / goal.Value : eaten > 0 ? 1 : 0;
        var reveal = 1 - MathF.Pow(1 - Math.Clamp(Reveal, 0, 1), 3);
        canvas.StrokeLineCap = LineCap.Round;
        var first = (float)Math.Min(share, 1) * 360 * reveal;
        if (first > 0.5f)
        {
            canvas.StrokeColor = color;
            DrawSweep(canvas, box, first);
        }
        var over = (float)Math.Min(Math.Max(share - 1, 0), 1) * 360 * reveal;
        if (over > 0.5f)
        {
            canvas.StrokeColor = overColor;
            DrawSweep(canvas, box, over);
        }

        canvas.FontColor = ValueColor;
        canvas.FontSize = size * 0.17f;
        canvas.DrawString(centerValue, rect.Left, rect.Center.Y - size * 0.15f, rect.Width, size * 0.22f, HorizontalAlignment.Center, VerticalAlignment.Center);
        canvas.FontColor = LabelColor;
        canvas.FontSize = size * 0.08f;
        canvas.DrawString(centerLabel, rect.Left, rect.Center.Y + size * 0.07f, rect.Width, size * 0.12f, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    // Degrees counter-clockwise from 3 o'clock: from the top, going clockwise. A whole turn is drawn as a circle.
    static void DrawSweep(ICanvas canvas, RectF box, float degrees)
    {
        if (degrees >= 359.5f)
            canvas.DrawEllipse(box);
        else
            canvas.DrawArc(box, 90, 90 - degrees, true, false);
    }
}

/// <summary>
/// Each day's energy balance as a bar up (surplus) or down (deficit) from a zero line, with a dashed line at the daily
/// target if there is one. Days without a balance leave a gap. Bars grow from the line as it's revealed.
/// </summary>
public class BalanceChartDrawable(IReadOnlyList<ChartPoint> points, double? target, Color surplus, Color deficit) : IDrawable, IRevealable
{
    static readonly Color Grid = Color.FromArgb("#2C3240"), LabelColor = Color.FromArgb("#626B7E"), ValueColor = Color.FromArgb("#F4F6FB");
    const float LabelHeight = 18, ValueHeight = 16;

    public float Reveal { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.FontSize = 10;
        if (points.Count == 0 || points.All(p => p.Value == 0))
        {
            canvas.FontColor = LabelColor;
            canvas.FontSize = 13;
            canvas.DrawString("Log food to see your balance", rect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        var plot = new RectF(rect.X, rect.Y + ValueHeight, rect.Width, rect.Height - LabelHeight - ValueHeight * 2);
        var max = Math.Max(points.Max(p => p.Value), Math.Max(target ?? 0, 0));
        var min = Math.Min(points.Min(p => p.Value), Math.Min(target ?? 0, 0));
        // Room on both sides, so a lone surplus or deficit still has a visible zero line.
        var span = Math.Max(max - min, 1);
        max += span * 0.08;
        min -= span * 0.08;
        float Y(double v) => plot.Top + (float)((max - v) / (max - min)) * plot.Height;

        canvas.StrokeColor = Grid;
        canvas.StrokeSize = 1;
        canvas.DrawLine(plot.Left, Y(0), plot.Right, Y(0));
        if (target is { } t && t != 0)
        {
            canvas.StrokeColor = (t > 0 ? surplus : deficit).WithAlpha(0.7f);
            canvas.StrokeDashPattern = [4, 4];
            canvas.DrawLine(plot.Left, Y(t), plot.Right, Y(t));
            canvas.StrokeDashPattern = null;
        }

        var slot = plot.Width / points.Count;
        var barW = Math.Min(22, slot * 0.6f);
        var grow = 1 - MathF.Pow(1 - Math.Clamp(Reveal, 0, 1), 3);
        var every = Math.Max(1, (int)Math.Ceiling(points.Count * 40 / Math.Max(1, plot.Width)));
        for (var i = 0; i < points.Count; i++)
        {
            var cx = plot.Left + slot * (i + 0.5f);
            var v = points[i].Value;
            var last = i == points.Count - 1;
            if (v != 0)
            {
                var zero = Y(0);
                var end = zero + (Y(v) - zero) * grow;
                var color = v > 0 ? surplus : deficit;
                canvas.FillColor = last ? color : color.WithAlpha(0.55f);
                var top = Math.Min(zero, end);
                var h = Math.Max(Math.Abs(end - zero), 3);
                canvas.FillRoundedRectangle(cx - barW / 2, v > 0 ? zero - h : zero, barW, h, 4);
                if (last && grow > 0.95f)
                {
                    canvas.FontColor = ValueColor;
                    var y = v > 0 ? top - ValueHeight : top + h;
                    canvas.DrawString(NutritionService.Signed(v), cx - 40, y, 80, ValueHeight - 2, HorizontalAlignment.Center,
                        v > 0 ? VerticalAlignment.Bottom : VerticalAlignment.Top);
                }
            }
            if (i % every == 0 || last)
            {
                canvas.FontColor = LabelColor;
                canvas.DrawString(points[i].Label, cx - 30, rect.Bottom - LabelHeight + 3, 60, LabelHeight - 3, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }
    }
}
