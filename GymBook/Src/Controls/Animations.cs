namespace GymBook.Controls;

/// <summary>
/// A little bounce when something becomes true, e.g. a set's tick as it's done: <c>controls:Pop.When="{Binding IsCompleted}"</c>.
/// <see cref="AmountProperty"/> sets how big it gets at its peak (1.2 = 20% bigger).
/// </summary>
public static class Pop
{
    public static readonly BindableProperty WhenProperty =
        BindableProperty.CreateAttached("When", typeof(bool), typeof(Pop), false, propertyChanged: OnWhenChanged);

    public static readonly BindableProperty AmountProperty =
        BindableProperty.CreateAttached("Amount", typeof(double), typeof(Pop), 1.2);

    public static bool GetWhen(BindableObject view) => (bool)view.GetValue(WhenProperty);
    public static void SetWhen(BindableObject view, bool value) => view.SetValue(WhenProperty, value);
    public static double GetAmount(BindableObject view) => (double)view.GetValue(AmountProperty);
    public static void SetAmount(BindableObject view, double value) => view.SetValue(AmountProperty, value);

    static void OnWhenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Only on the change itself, not when a row is first shown already done.
        if (bindable is not VisualElement view || newValue is not true || oldValue is not false || !view.IsLoaded)
            return;
        view.AbortAnimation("pop");
        var peak = GetAmount(view);
        var pop = new Animation
        {
            { 0, 0.45, new Animation(v => view.Scale = v, 0.85, peak, Easing.CubicOut) },
            { 0.45, 1, new Animation(v => view.Scale = v, peak, 1, Easing.SpringOut) },
        };
        pop.Commit(view, "pop", length: 420, finished: (_, _) => view.Scale = 1);
    }
}

/// <summary>
/// A progress bar that glides to its value instead of jumping: <c>controls:SmoothProgress.Value="{Binding Progress}"</c>
/// in place of Progress.
/// </summary>
public static class SmoothProgress
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.CreateAttached("Value", typeof(double), typeof(SmoothProgress), 0.0, propertyChanged: OnValueChanged);

    public static readonly BindableProperty LengthProperty =
        BindableProperty.CreateAttached("Length", typeof(int), typeof(SmoothProgress), 450);

    public static double GetValue(BindableObject view) => (double)view.GetValue(ValueProperty);
    public static void SetValue(BindableObject view, double value) => view.SetValue(ValueProperty, value);
    public static int GetLength(BindableObject view) => (int)view.GetValue(LengthProperty);
    public static void SetLength(BindableObject view, int value) => view.SetValue(LengthProperty, value);

    static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ProgressBar bar)
            return;
        var value = Math.Clamp((double)newValue, 0, 1);
        if (!bar.IsLoaded)
        {
            bar.Progress = value;
            return;
        }
        bar.AbortAnimation("Progress");
        _ = bar.ProgressTo(value, (uint)GetLength(bar), Easing.CubicOut);
    }
}
