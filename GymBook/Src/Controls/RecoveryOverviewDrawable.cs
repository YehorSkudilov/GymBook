using GymBook.Services;

namespace GymBook.Controls;

/// <summary>
/// Every muscle group at a moment as a strip of segments, one each, coloured by how recovered it is and most tired first;
/// under it, how many are fatigued, recovering, almost recovered and fresh. The one graphic under every recovery map.
/// </summary>
public class RecoveryOverviewDrawable(IReadOnlyList<double> recovery) : IDrawable
{
    // The four states, most tired first, at the thresholds RecoveryService colours and names them by.
    static readonly (string Name, double Below)[] States = [("Fatigued", 0.35), ("Recovering", 0.6), ("Almost", 0.9), ("Fresh", double.MaxValue)];
    static readonly Color LabelColor = Color.FromArgb("#9AA3B5");

    const float StripHeight = 12, Gap = 3, LegendTop = StripHeight + 10;

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (recovery.Count == 0)
            return;
        var sorted = recovery.OrderBy(r => r).ToList();
        var width = (rect.Width - Gap * (sorted.Count - 1)) / sorted.Count;
        for (var i = 0; i < sorted.Count; i++)
        {
            canvas.FillColor = RecoveryService.ColorFor(sorted[i]);
            canvas.FillRoundedRectangle(rect.X + i * (width + Gap), rect.Y, width, StripHeight, 3);
        }

        // A column per state, all four always, so the strip reads the same everywhere.
        var column = rect.Width / States.Length;
        var lower = double.MinValue;
        for (var s = 0; s < States.Length; s++)
        {
            var (name, below) = States[s];
            var count = sorted.Count(r => r >= lower && r < below);
            lower = below;
            var x = rect.X + s * column;
            var y = rect.Y + LegendTop;
            canvas.FillColor = RecoveryService.ColorFor(s switch { 0 => 0, 1 => 0.5, 2 => 0.75, _ => 1 });
            canvas.FillCircle(x + 5, y + 7, 4);
            canvas.FontSize = 11;
            canvas.FontColor = count > 0 ? Color.FromArgb("#F4F6FB") : LabelColor;
            canvas.DrawString($"{count} {name}", x + 13, y, column - 13, 14, HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }
}
