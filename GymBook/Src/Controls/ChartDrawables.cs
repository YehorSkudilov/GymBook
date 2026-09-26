using GymBook.Services;

namespace GymBook.Controls;

public abstract class ChartDrawable(IReadOnlyList<ChartPoint> points, Color color, Func<double, string> format) : IDrawable
{
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
            var h = (float)(Points[i].Value / max) * plot.Height;
            var last = i == Points.Count - 1;
            canvas.FillColor = last ? Color : Color.WithAlpha(0.45f);
            if (h > 0)
                canvas.FillRoundedRectangle(cx - barW / 2, plot.Bottom - Math.Max(h, 4), barW, Math.Max(h, 4), 5);
            if (last || Points[i].Value == max)
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
            if (last)
            {
                canvas.FontColor = ValueColor;
                canvas.DrawString(Format(Points[i].Value), p.X - 60, p.Y - ValueHeight - 2, 64, ValueHeight, HorizontalAlignment.Right, VerticalAlignment.Bottom);
            }
            DrawLabel(canvas, i, p.X, 0, plot);
        }
    }
}
