using GymBook.Services;

namespace GymBook.Controls;

/// <summary>
/// A day's calories on one bar, from 0 to a little past the most of eaten, burned and goal: eaten fills it in
/// <paramref name="fill"/>, turning <paramref name="surplus"/> past what was burned; a hatched band is the goal's
/// on-target range (±10%), a white tick what was burned, and a round <paramref name="marker"/> with a target the goal.
/// Fills in as it's revealed.
/// </summary>
public class CalorieBarDrawable(double eaten, double? burned, double? goal, Color fill, Color surplus, Color marker) : IDrawable, IRevealable
{
    static readonly Color Track = Color.FromArgb("#3A3F4B"), Tick = Color.FromArgb("#F4F6FB"), LabelColor = Color.FromArgb("#9AA3B5");
    const float BarHeight = 16, Knob = 30, LabelHeight = 20;

    /// <summary>The goal's on-target range: within a tenth of it either way.</summary>
    public const double Range = 0.1;

    public float Reveal { get; set; } = 1;

    /// <summary>Where the bar ends: past the biggest of what's on it, so the marker and fill have room.</summary>
    public static double Scale(double eaten, double? burned, double? goal) =>
        Math.Ceiling(Math.Max(Math.Max(eaten, burned ?? 0) * 1.08, (goal ?? 0) * 1.3) / 10) * 10 is var s && s > 0 ? s : 2000;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var scale = Scale(eaten, burned, goal);
        var pad = Knob / 2;
        var bar = new RectF(rect.X + pad, rect.Y + (Knob - BarHeight) / 2 + 2, rect.Width - pad * 2, BarHeight);
        float X(double kcal) => bar.Left + (float)Math.Clamp(kcal / scale, 0, 1) * bar.Width;
        var grow = 1 - MathF.Pow(1 - Math.Clamp(Reveal, 0, 1), 3);

        canvas.FillColor = Track;
        canvas.FillRoundedRectangle(bar, BarHeight / 2);

        // The goal's range, hatched.
        if (goal is > 0 && goal is { } g)
        {
            var from = X(g * (1 - Range));
            var to = X(g * (1 + Range));
            canvas.SaveState();
            canvas.ClipRectangle(from, bar.Top, to - from, bar.Height);
            canvas.StrokeColor = LabelColor.WithAlpha(0.8f);
            canvas.StrokeSize = 1.5f;
            for (var x = from - bar.Height; x < to; x += 5)
                canvas.DrawLine(x, bar.Bottom, x + bar.Height, bar.Top);
            canvas.RestoreState();
        }

        // Eaten: up to the burn in the fill colour, past it the surplus.
        var end = bar.Left + (X(eaten) - bar.Left) * grow;
        if (end > bar.Left + 1)
        {
            canvas.SaveState();
            canvas.ClipRectangle(bar.Left, bar.Top, end - bar.Left, bar.Height);
            canvas.FillColor = fill;
            canvas.FillRoundedRectangle(bar, BarHeight / 2);
            if (burned is { } b && eaten > b)
            {
                canvas.FillColor = surplus;
                canvas.FillRectangle(X(b), bar.Top, end - X(b), bar.Height);
            }
            canvas.RestoreState();
        }

        // Burned: a white tick through the bar.
        if (burned is > 0 && grow > 0.9f)
        {
            canvas.StrokeColor = Tick;
            canvas.StrokeSize = 3;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawLine(X(burned.Value), bar.Top - 5, X(burned.Value), bar.Bottom + 5);
        }

        // The goal: a round marker with a target in it.
        if (goal is > 0 && grow > 0.9f)
            LegendIconDrawable.Target(canvas, X(goal.Value), bar.Center.Y, Knob / 2, marker);

        canvas.FontColor = LabelColor;
        canvas.FontSize = 13;
        var labelTop = rect.Y + Knob + 4;
        canvas.DrawString("0", bar.Left - pad, labelTop, 60, LabelHeight, HorizontalAlignment.Left, VerticalAlignment.Top);
        canvas.DrawString(NutritionService.Kcal(scale), bar.Right + pad - 80, labelTop, 80, LabelHeight, HorizontalAlignment.Right, VerticalAlignment.Top);
    }
}

/// <summary>The little keys beside the calorie bar's figures: the goal's target, the hatched range, the burned tick.</summary>
public class LegendIconDrawable(LegendIcon icon, Color color) : IDrawable
{
    static readonly Color Hatch = Color.FromArgb("#9AA3B5");

    public void Draw(ICanvas canvas, RectF rect)
    {
        var r = Math.Min(rect.Width, rect.Height) / 2;
        switch (icon)
        {
            case LegendIcon.Target:
                Target(canvas, rect.Center.X, rect.Center.Y, r, color);
                break;
            case LegendIcon.Range:
                canvas.SaveState();
                var path = new PathF();
                path.AppendCircle(rect.Center.X, rect.Center.Y, r);
                canvas.ClipPath(path);
                canvas.FillColor = Hatch.WithAlpha(0.25f);
                canvas.FillCircle(rect.Center.X, rect.Center.Y, r);
                canvas.StrokeColor = Hatch;
                canvas.StrokeSize = 1.5f;
                for (var x = rect.Left - rect.Height; x < rect.Right; x += 4)
                    canvas.DrawLine(x, rect.Bottom, x + rect.Height, rect.Top);
                canvas.RestoreState();
                break;
            default:
                canvas.StrokeColor = color;
                canvas.StrokeSize = 3;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawLine(rect.Center.X, rect.Center.Y - r * 0.8f, rect.Center.X, rect.Center.Y + r * 0.8f);
                break;
        }
    }

    /// <summary>A filled circle with a white target (two rings and a dot) in it.</summary>
    public static void Target(ICanvas canvas, float cx, float cy, float r, Color color)
    {
        canvas.FillColor = color;
        canvas.FillCircle(cx, cy, r);
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = Math.Max(1.5f, r * 0.11f);
        canvas.DrawCircle(cx, cy, r * 0.62f);
        canvas.DrawCircle(cx, cy, r * 0.32f);
        canvas.FillColor = Colors.White;
        canvas.FillCircle(cx, cy, r * 0.1f);
    }
}

public enum LegendIcon { Target, Range, Tick }

/// <summary>A bar in <see cref="MacroStackChartDrawable"/>: its label and the calories from carbs, fat and protein.</summary>
public record MacroBar(string Label, double CarbsKcal, double FatKcal, double ProteinKcal)
{
    public double Total => CarbsKcal + FatKcal + ProteinKcal;
}

/// <summary>
/// The macro split over time: a bar per day (or week), each split top to bottom into carbs, fat and protein by their
/// share of its calories, all the same height; days with nothing logged leave a gap. A dashed line across marks the
/// split aimed for between each part. Bars grow up as it's revealed.
/// </summary>
public class MacroStackChartDrawable(IReadOnlyList<MacroBar> bars, IReadOnlyList<double> target, IReadOnlyList<Color> colors) : IDrawable, IRevealable
{
    static readonly Color Empty = Color.FromArgb("#1D212C"), LabelColor = Color.FromArgb("#626B7E");
    const float LabelHeight = 18;

    public float Reveal { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (bars.All(b => b.Total <= 0))
        {
            canvas.FontColor = LabelColor;
            canvas.FontSize = 13;
            canvas.DrawString("Log food to see the split", rect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }
        var plot = new RectF(rect.X, rect.Y, rect.Width, rect.Height - LabelHeight);
        var slot = plot.Width / bars.Count;
        var barW = Math.Min(22, slot * 0.62f);
        var grow = 1 - MathF.Pow(1 - Math.Clamp(Reveal, 0, 1), 3);
        var every = Math.Max(1, (int)Math.Ceiling(bars.Count * 40 / Math.Max(1, plot.Width)));
        canvas.FontSize = 10;
        for (var i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            var cx = plot.Left + slot * (i + 0.5f);
            var x = cx - barW / 2;
            if (bar.Total <= 0)
            {
                canvas.FillColor = Empty;
                canvas.FillRoundedRectangle(x, plot.Bottom - 4, barW, 4, 2);
            }
            else
            {
                // Clipped to the rounded bar, then filled part by part from the bottom: protein, fat, carbs on top.
                var height = plot.Height * grow;
                canvas.SaveState();
                var path = new PathF();
                path.AppendRoundedRectangle(x, plot.Bottom - height, barW, height, Math.Min(6, barW / 2));
                canvas.ClipPath(path);
                var y = plot.Bottom;
                foreach (var (kcal, color) in new[] { (bar.ProteinKcal, colors[2]), (bar.FatKcal, colors[1]), (bar.CarbsKcal, colors[0]) })
                {
                    var h = (float)(kcal / bar.Total) * height;
                    canvas.FillColor = color;
                    canvas.FillRectangle(x, y - h, barW, h + 0.5f);
                    y -= h;
                }
                canvas.RestoreState();
            }
            if (i % every == 0 || i == bars.Count - 1)
            {
                canvas.FontColor = LabelColor;
                canvas.DrawString(bar.Label, cx - 30, plot.Bottom + 3, 60, LabelHeight - 3, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        // The split aimed for: where protein would end and where fat would, dashed across.
        var total = target.Sum();
        if (total > 0 && grow > 0.95f)
        {
            canvas.StrokeColor = Colors.White.WithAlpha(0.55f);
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = [3, 3];
            var protein = (float)(target[2] / total);
            var fat = (float)(target[1] / total);
            foreach (var share in new[] { protein, protein + fat })
                canvas.DrawLine(plot.Left, plot.Bottom - share * plot.Height, plot.Right, plot.Bottom - share * plot.Height);
            canvas.StrokeDashPattern = null;
        }
    }
}

/// <summary>
/// How the day's calories split between carbs, fat and protein: a bar of the three in proportion with their shares
/// above, and under it thin bars of the split aimed for (the goals, or a usual one) with theirs below.
/// </summary>
public class MacroSplitDrawable(IReadOnlyList<double> actual, IReadOnlyList<double> target, IReadOnlyList<Color> colors) : IDrawable, IRevealable
{
    static readonly Color Well = Color.FromArgb("#1D212C"), LabelColor = Color.FromArgb("#626B7E");
    const float LabelHeight = 20, BarHeight = 30, Gap = 4, TargetHeight = 4;

    public float Reveal { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var total = actual.Sum();
        var grow = 1 - MathF.Pow(1 - Math.Clamp(Reveal, 0, 1), 3);
        var well = new RectF(rect.X, rect.Y + LabelHeight, rect.Width, BarHeight + TargetHeight + 10);
        canvas.FillColor = Well;
        canvas.FillRoundedRectangle(well, 10);

        var inner = new RectF(well.X + 4, well.Y + 4, well.Width - 8, BarHeight);
        if (total <= 0)
        {
            canvas.FontColor = LabelColor;
            canvas.FontSize = 12;
            canvas.DrawString("Log food to see the split", inner, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
        else
            Segments(canvas, actual, inner, BarHeight, 6, grow, rect.Y, LabelHeight, VerticalAlignment.Bottom, 1);

        var thin = new RectF(inner.X, inner.Bottom + 4, inner.Width, TargetHeight);
        Segments(canvas, target, thin, TargetHeight, 2, 1, thin.Bottom + 6, LabelHeight, VerticalAlignment.Top, 0.6f);
    }

    /// <summary>The shares as segments across <paramref name="area"/>, with each one's percentage at its start (the last at its end).</summary>
    void Segments(ICanvas canvas, IReadOnlyList<double> values, RectF area, float height, float corner, float grow,
        float labelTop, float labelHeight, VerticalAlignment align, float alpha)
    {
        var total = values.Sum();
        if (total <= 0)
            return;
        var width = area.Width - Gap * (values.Count - 1);
        var x = area.X;
        canvas.FontSize = 15;
        for (var i = 0; i < values.Count; i++)
        {
            var share = values[i] / total;
            var w = (float)share * width;
            if (w > 0.5f)
            {
                canvas.FillColor = colors[i].WithAlpha(alpha);
                canvas.FillRoundedRectangle(x, area.Y, Math.Max(w * grow, Math.Min(w, corner * 2)), height, corner);
            }
            canvas.FontColor = colors[i];
            var label = $"{share * 100:0}%";
            var last = i == values.Count - 1;
            canvas.DrawString(label, last ? area.Right - 80 : x, labelTop, 80, labelHeight,
                last ? HorizontalAlignment.Right : HorizontalAlignment.Left, align);
            x += w + Gap;
        }
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

/// <summary>A thin bar of a food's calories split into carbs, fat and protein, for a search result's preview.</summary>
public class MacroMiniBarDrawable(double carbsKcal, double fatKcal, double proteinKcal) : IDrawable
{
    static readonly Color Carbs = Color.FromArgb("#C58CFF"), Fat = Color.FromArgb("#FF7B72"), Protein = Color.FromArgb("#FFD84D"),
        Track = Color.FromArgb("#2C3240");

    public void Draw(ICanvas canvas, RectF rect)
    {
        var h = Math.Min(rect.Height, 4);
        var bar = new RectF(rect.X, rect.Center.Y - h / 2, rect.Width, h);
        var total = carbsKcal + fatKcal + proteinKcal;
        canvas.SaveState();
        var path = new PathF();
        path.AppendRoundedRectangle(bar, h / 2);
        canvas.ClipPath(path);
        canvas.FillColor = Track;
        canvas.FillRectangle(bar);
        if (total > 0)
        {
            var x = bar.Left;
            foreach (var (kcal, color) in new[] { (carbsKcal, Carbs), (fatKcal, Fat), (proteinKcal, Protein) })
            {
                var w = (float)(kcal / total) * bar.Width;
                canvas.FillColor = color;
                canvas.FillRectangle(x, bar.Top, w, bar.Height);
                x += w;
            }
        }
        canvas.RestoreState();
    }
}
