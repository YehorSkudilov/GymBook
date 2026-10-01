namespace GymBook.Admin.Controls;

/// <summary>
/// A plain bar chart: one bar per value, and optionally a second series stacked on top in the second accent colour
/// (e.g. AI plans with chats above them). Scaled to the tallest bar; the numbers themselves are shown next to it.
/// </summary>
public class BarChart : GraphicsView, IDrawable
{
    public static readonly BindableProperty ValuesProperty =
        BindableProperty.Create(nameof(Values), typeof(IReadOnlyList<int>), typeof(BarChart), null, propertyChanged: Redraw);

    public static readonly BindableProperty StackedValuesProperty =
        BindableProperty.Create(nameof(StackedValues), typeof(IReadOnlyList<int>), typeof(BarChart), null, propertyChanged: Redraw);

    static readonly Color BarColor = Color.FromArgb("#3F7DFF"), StackedColor = Color.FromArgb("#7C5CFF"), EmptyColor = Color.FromArgb("#1D212C");

    public BarChart()
    {
        Drawable = this;
        HeightRequest = 120;
    }

    public IReadOnlyList<int>? Values
    {
        get => (IReadOnlyList<int>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IReadOnlyList<int>? StackedValues
    {
        get => (IReadOnlyList<int>?)GetValue(StackedValuesProperty);
        set => SetValue(StackedValuesProperty, value);
    }

    static void Redraw(BindableObject bindable, object oldValue, object newValue) => ((BarChart)bindable).Invalidate();

    public void Draw(ICanvas canvas, RectF rect)
    {
        var values = Values;
        if (values is not { Count: > 0 })
            return;
        var stacked = StackedValues;
        int Top(int i) => values[i] + (stacked != null && i < stacked.Count ? stacked[i] : 0);

        var max = Math.Max(1, Enumerable.Range(0, values.Count).Max(Top));
        var slot = rect.Width / values.Count;
        var gap = Math.Min(4f, slot * 0.25f);
        var width = Math.Max(1f, slot - gap);
        for (var i = 0; i < values.Count; i++)
        {
            var x = rect.Left + i * slot + gap / 2;
            if (Top(i) == 0)
            {
                // A sliver, so empty days still read as days.
                canvas.FillColor = EmptyColor;
                canvas.FillRoundedRectangle(x, rect.Bottom - 3, width, 3, 1.5f);
                continue;
            }
            var height = rect.Height * values[i] / max;
            canvas.FillColor = BarColor;
            canvas.FillRoundedRectangle(x, rect.Bottom - height, width, height, Math.Min(3, width / 2));
            if (stacked != null && i < stacked.Count && stacked[i] > 0)
            {
                var extra = rect.Height * stacked[i] / max;
                canvas.FillColor = StackedColor;
                canvas.FillRoundedRectangle(x, rect.Bottom - height - extra, width, extra, Math.Min(3, width / 2));
            }
        }
    }
}
