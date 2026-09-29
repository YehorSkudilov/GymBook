using GymBook.Services;

namespace GymBook.Controls;

/// <summary>A chart that can draw itself in: <see cref="Reveal"/> runs from 0 (nothing yet) to 1 (all of it). See <see cref="ChartReveal"/>.</summary>
public interface IRevealable
{
    float Reveal { get; set; }
}

public abstract class ChartDrawable(IReadOnlyList<ChartPoint> points, Color color, Func<double, string> format) : IDrawable, IRevealable
{
    public float Reveal { get; set; } = 1;

    protected static readonly Color Grid = Color.FromArgb("#2C3240");
    protected static readonly Color LabelColor = Color.FromArgb("#626B7E");
    protected static readonly Color ValueColor = Color.FromArgb("#F4F6FB");

    protected IReadOnlyList<ChartPoint> Points { get; } = points;
    protected Color Color { get; } = color;
    protected Func<double, string> Format { get; } = format;

    protected const float LabelHeight = 18, ValueHeight = 18;

    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.FontSize = 10;
        if (Points.Count == 0 || Points.All(p => p.Value <= 0))
        {
            canvas.FontColor = LabelColor;
            canvas.FontSize = 13;
            canvas.DrawString("No data yet", rect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        var plot = new RectF(rect.X, rect.Y + ValueHeight, rect.Width, rect.Height - LabelHeight - ValueHeight);
        canvas.StrokeColor = Grid;
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = [3, 4];
        for (var i = 0; i <= 2; i++)
        {
            var y = plot.Top + plot.Height * i / 2f;
            canvas.DrawLine(plot.Left, y, plot.Right, y);
        }
        canvas.StrokeDashPattern = null;
        DrawSeries(canvas, plot);
    }

    protected static float EaseOut(float x) => 1 - MathF.Pow(1 - Math.Clamp(x, 0, 1), 3);

    protected abstract void DrawSeries(ICanvas canvas, RectF plot);

    protected void DrawLabel(ICanvas canvas, int index, float centerX, float width, RectF plot)
    {
        // Skip labels when crowded so they never overlap.
        var every = Math.Max(1, (int)Math.Ceiling(Points.Count * 44 / Math.Max(1, plot.Width)));
        if (index % every != 0 && index != Points.Count - 1)
            return;
        canvas.FontColor = LabelColor;
        canvas.DrawString(Points[index].Label, centerX - 30, plot.Bottom + 3, 60, LabelHeight - 3, HorizontalAlignment.Center, VerticalAlignment.Top);
    }
}

public class BarChartDrawable(IReadOnlyList<ChartPoint> points, Color color, Func<double, string> format)
    : ChartDrawable(points, color, format)
{
    protected override void DrawSeries(ICanvas canvas, RectF plot)
    {
        var max = Points.Max(p => p.Value);
        var slot = plot.Width / Points.Count;
        var barW = Math.Min(28, slot * 0.6f);
        for (var i = 0; i < Points.Count; i++)
        {
            var cx = plot.Left + slot * (i + 0.5f);
            // Each bar grows in a little after the one before it.
            var grow = EaseOut((Reveal - 0.45f * i / Points.Count) / 0.55f);
            var h = (float)(Points[i].Value / max) * plot.Height * grow;
            var last = i == Points.Count - 1;
            canvas.FillColor = last ? Color : Color.WithAlpha(0.45f);
            if (h > 0)
                canvas.FillRoundedRectangle(cx - barW / 2, plot.Bottom - Math.Max(h, 4), barW, Math.Max(h, 4), 5);
            if ((last || Points[i].Value == max) && grow > 0.95f)
            {
                canvas.FontColor = ValueColor;
                canvas.DrawString(Format(Points[i].Value), cx - 40, plot.Bottom - h - ValueHeight, 80, ValueHeight - 2, HorizontalAlignment.Center, VerticalAlignment.Bottom);
            }
            DrawLabel(canvas, i, cx, slot, plot);
        }
    }
}

public class LineChartDrawable(IReadOnlyList<ChartPoint> points, Color color, Func<double, string> format)
    : ChartDrawable(points, color, format)
{
    protected override void DrawSeries(ICanvas canvas, RectF plot)
    {
        var min = Points.Min(p => p.Value);
        var max = Points.Max(p => p.Value);
        var pad = Math.Max((max - min) * 0.15, max * 0.02 + 0.5);
        min -= pad;
        max += pad;

        PointF At(int i) => new(
            Points.Count == 1 ? plot.Center.X : plot.Left + 8 + (plot.Width - 16) * i / (Points.Count - 1),
            plot.Bottom - (float)((Points[i].Value - min) / (max - min)) * plot.Height);

        var line = new PathF();
        var area = new PathF();
        area.MoveTo(At(0).X, plot.Bottom);
        for (var i = 0; i < Points.Count; i++)
        {
            var p = At(i);
            if (i == 0) line.MoveTo(p); else line.LineTo(p);
            area.LineTo(p);
        }
        area.LineTo(At(Points.Count - 1).X, plot.Bottom);
        area.Close();

        // The line sweeps in from left to right; the labels under it are there from the start.
        for (var i = 0; i < Points.Count; i++)
            DrawLabel(canvas, i, At(i).X, 0, plot);
        canvas.SaveState();
        canvas.ClipRectangle(plot.Left - 10, plot.Top - ValueHeight - 10, (plot.Width + 20) * EaseOut(Reveal), plot.Height + ValueHeight + LabelHeight + 20);

        canvas.SetFillPaint(new LinearGradientPaint([new PaintGradientStop(0, Color.WithAlpha(0.35f)), new PaintGradientStop(1, Color.WithAlpha(0f))], new Point(0, 0), new Point(0, 1)), plot);
        canvas.FillPath(area);

        canvas.StrokeColor = Color;
        canvas.StrokeSize = 2.5f;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(line);

        for (var i = 0; i < Points.Count; i++)
        {
            var p = At(i);
            var last = i == Points.Count - 1;
            canvas.FillColor = last ? ValueColor : Color;
            canvas.FillCircle(p, last ? 4.5f : 3f);
        }
        canvas.RestoreState();
        if (Reveal > 0.95f)
        {
            var p = At(Points.Count - 1);
            canvas.FontColor = ValueColor;
            canvas.DrawString(Format(Points[^1].Value), p.X - 60, p.Y - ValueHeight - 2, 64, ValueHeight, HorizontalAlignment.Right, VerticalAlignment.Bottom);
        }
    }
}

/// <summary>
/// Training days as a grid of squares, a column per week (oldest on the left) and a row per weekday: the more work
/// that day, the brighter its square. Today is outlined. The columns pop in from the left as it's revealed.
/// </summary>
public class HeatmapDrawable(DateTime firstMonday, IReadOnlyList<double> days, Color color) : IDrawable, IRevealable
{
    static readonly Color Empty = Color.FromArgb("#1D212C"), LabelColor = Color.FromArgb("#626B7E"), TodayRing = Color.FromArgb("#F4F6FB");
    static readonly string[] Weekdays = ["M", "", "W", "", "F", "", "S"];

    public float Reveal { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var weeks = (days.Count + 6) / 7;
        if (weeks == 0)
            return;
        const float labelW = 16, headerH = 16;
        var gap = 3f;
        var cell = Math.Min((rect.Width - labelW - gap * (weeks - 1)) / weeks, (rect.Height - headerH - gap * 6) / 7);
        var left = rect.Left + labelW + (rect.Width - labelW - (cell * weeks + gap * (weeks - 1))) / 2;
        var top = rect.Top + headerH;
        var max = days.DefaultIfEmpty(0).Max();

        canvas.FontSize = 9;
        canvas.FontColor = LabelColor;
        for (var d = 0; d < 7; d++)
            canvas.DrawString(Weekdays[d], rect.Left, top + d * (cell + gap), labelW - 4, cell, HorizontalAlignment.Left, VerticalAlignment.Center);

        var todayIndex = (DateTime.Today - firstMonday).Days;
        for (var w = 0; w < weeks; w++)
        {
            var x = left + w * (cell + gap);
            // The month's name over the first week of each month.
            var monday = firstMonday.AddDays(7 * w);
            if (w == 0 || monday.Month != monday.AddDays(-7).Month)
            {
                canvas.FontColor = LabelColor;
                canvas.DrawString(monday.ToString("MMM"), x, rect.Top, 40, headerH - 3, HorizontalAlignment.Left, VerticalAlignment.Top);
            }
            // The grid is always there; the training days light up in it, a week at a time from the left.
            var pop = Math.Clamp((Reveal - 0.6f * w / weeks) / 0.4f, 0, 1);
            for (var d = 0; d < 7; d++)
            {
                var i = w * 7 + d;
                if (i >= days.Count)
                    break;
                var y = top + d * (cell + gap);
                canvas.FillColor = Empty;
                canvas.FillRoundedRectangle(x, y, cell, cell, cell * 0.25f);
                var level = max <= 0 || days[i] <= 0 ? 0 : 0.35f + 0.65f * (float)(days[i] / max);
                if (level > 0 && pop > 0)
                {
                    var size = cell * (0.4f + 0.6f * pop);
                    canvas.FillColor = color.WithAlpha(level * pop);
                    canvas.FillRoundedRectangle(x + (cell - size) / 2, y + (cell - size) / 2, size, size, size * 0.25f);
                }
                if (i == todayIndex)
                {
                    canvas.StrokeColor = TodayRing.WithAlpha(0.8f);
                    canvas.StrokeSize = 1.5f;
                    canvas.DrawRoundedRectangle(x + 0.75f, y + 0.75f, cell - 1.5f, cell - 1.5f, cell * 0.25f);
                }
            }
        }
    }
}

/// <summary>One slice of a <see cref="DonutDrawable"/>.</summary>
public record DonutSlice(string Label, double Value, Color Color);

/// <summary>
/// Shares of a whole as a ring, the biggest slice from the top going clockwise, with the total in the middle. The ring
/// sweeps round as it's revealed.
/// </summary>
public class DonutDrawable(IReadOnlyList<DonutSlice> slices, string centerValue, string centerLabel) : IDrawable, IRevealable
{
    static readonly Color Track = Color.FromArgb("#1D212C"), ValueColor = Color.FromArgb("#F4F6FB"), LabelColor = Color.FromArgb("#9AA3B5");

    public float Reveal { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height);
        var thickness = size * 0.14f;
        var r = size / 2 - thickness / 2 - 2;
        var box = new RectF(rect.Center.X - r, rect.Center.Y - r, r * 2, r * 2);
        canvas.StrokeSize = thickness;
        canvas.StrokeColor = Track;
        canvas.DrawEllipse(box);

        var total = slices.Sum(s => s.Value);
        var sweep = 360 * (1 - MathF.Pow(1 - Math.Clamp(Reveal, 0, 1), 3));
        if (total > 0)
        {
            canvas.StrokeLineCap = LineCap.Butt;
            var start = 0f;
            foreach (var slice in slices)
            {
                var angle = (float)(slice.Value / total * 360);
                var end = Math.Min(start + angle, sweep);
                // A hair of space between slices.
                if (end - start > 1.5f)
                {
                    canvas.StrokeColor = slice.Color;
                    // Degrees counter-clockwise from 3 o'clock: from the top, going clockwise.
                    canvas.DrawArc(box, 90 - start - 0.75f, 90 - end + 0.75f, true, false);
                }
                start += angle;
                if (start >= sweep)
                    break;
            }
        }

        canvas.FontColor = ValueColor;
        canvas.FontSize = size * 0.16f;
        canvas.DrawString(centerValue, rect.Left, rect.Center.Y - size * 0.14f, rect.Width, size * 0.2f, HorizontalAlignment.Center, VerticalAlignment.Center);
        canvas.FontColor = LabelColor;
        canvas.FontSize = size * 0.08f;
        canvas.DrawString(centerLabel, rect.Left, rect.Center.Y + size * 0.06f, rect.Width, size * 0.12f, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}

/// <summary>
/// Draws a chart in (see <see cref="IRevealable"/>): <c>controls:ChartReveal.When="{Binding IsShowing}"</c> on its
/// GraphicsView plays it each time that turns true, and whenever the chart is replaced while it's true.
/// </summary>
public static class ChartReveal
{
    public static readonly BindableProperty WhenProperty =
        BindableProperty.CreateAttached("When", typeof(bool), typeof(ChartReveal), false, propertyChanged: OnWhenChanged);

    static readonly BindableProperty HookedProperty =
        BindableProperty.CreateAttached("Hooked", typeof(bool), typeof(ChartReveal), false);

    public static bool GetWhen(BindableObject view) => (bool)view.GetValue(WhenProperty);
    public static void SetWhen(BindableObject view, bool value) => view.SetValue(WhenProperty, value);

    static void OnWhenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not GraphicsView view)
            return;
        if (!(bool)view.GetValue(HookedProperty))
        {
            view.SetValue(HookedProperty, true);
            view.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(GraphicsView.Drawable) && GetWhen(view))
                    Play(view);
            };
        }
        if (newValue is true)
            Play(view);
    }

    static void Play(GraphicsView view)
    {
        if (view.Drawable is not IRevealable chart)
            return;
        view.AbortAnimation("reveal");
        chart.Reveal = 0;
        view.Invalidate();
        new Animation(v =>
        {
            chart.Reveal = (float)v;
            view.Invalidate();
        }).Commit(view, "reveal", 16, 1000, Easing.Linear, (_, _) =>
        {
            chart.Reveal = 1;
            view.Invalidate();
        });
    }
}
