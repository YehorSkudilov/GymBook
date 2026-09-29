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
            // Not on screen yet (e.g. a new row of a list): it fills in from empty once it is.
            bar.Progress = 0;
            bar.Loaded -= OnLoaded;
            bar.Loaded += OnLoaded;
            return;
        }
        bar.AbortAnimation("Progress");
        _ = bar.ProgressTo(value, (uint)GetLength(bar), Easing.CubicOut);
    }

    static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is not ProgressBar bar)
            return;
        bar.Loaded -= OnLoaded;
        _ = bar.ProgressTo(Math.Clamp(GetValue(bar), 0, 1), (uint)Math.Max(GetLength(bar), 700), Easing.CubicOut);
    }
}

/// <summary>
/// A gentle, endless heartbeat while something is true, e.g. the badge of the workout in progress:
/// <c>controls:Pulse.While="{Binding IsRunning}"</c>.
/// </summary>
public static class Pulse
{
    public static readonly BindableProperty WhileProperty =
        BindableProperty.CreateAttached("While", typeof(bool), typeof(Pulse), false, propertyChanged: OnWhileChanged);

    public static bool GetWhile(BindableObject view) => (bool)view.GetValue(WhileProperty);
    public static void SetWhile(BindableObject view, bool value) => view.SetValue(WhileProperty, value);

    static void OnWhileChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not VisualElement view)
            return;
        view.Loaded -= OnLoaded;
        view.Loaded += OnLoaded;
        Update(view);
    }

    static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
            Update(view);
    }

    static void Update(VisualElement view)
    {
        view.AbortAnimation("pulse");
        view.Scale = 1;
        if (!GetWhile(view) || !view.IsLoaded)
            return;
        var beat = new Animation
        {
            { 0, 0.5, new Animation(v => view.Scale = v, 1, 1.14, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => view.Scale = v, 1.14, 1, Easing.SinInOut) },
        };
        beat.Commit(view, "pulse", length: 1300, repeat: () => GetWhile(view) && view.IsLoaded, finished: (_, _) => view.Scale = 1);
    }
}

/// <summary>
/// A label whose number counts up to it: <c>controls:CountUp.Text="{Binding TotalVolume}"</c> in place of Text. The
/// first number in the text counts (with the same decimals), and what's around it stays, e.g. "12.5k kg".
/// <see cref="ReplayProperty"/> counts again each time it turns true (e.g. the tab coming into view).
/// </summary>
public static class CountUp
{
    static readonly System.Text.RegularExpressions.Regex Number = new(@"^(\D*?)(\d[\d,]*(?:\.\d+)?)(.*)$", System.Text.RegularExpressions.RegexOptions.Singleline);

    public static readonly BindableProperty TextProperty =
        BindableProperty.CreateAttached("Text", typeof(string), typeof(CountUp), "", propertyChanged: (b, _, _) => Play(b));

    public static readonly BindableProperty ReplayProperty =
        BindableProperty.CreateAttached("Replay", typeof(bool), typeof(CountUp), false, propertyChanged: (b, _, v) => { if (v is true) Play(b); });

    public static string GetText(BindableObject view) => (string)view.GetValue(TextProperty);
    public static void SetText(BindableObject view, string value) => view.SetValue(TextProperty, value);
    public static bool GetReplay(BindableObject view) => (bool)view.GetValue(ReplayProperty);
    public static void SetReplay(BindableObject view, bool value) => view.SetValue(ReplayProperty, value);

    static void Play(BindableObject bindable)
    {
        if (bindable is not Label label)
            return;
        var text = GetText(label) ?? "";
        label.AbortAnimation("count");
        var match = Number.Match(text);
        if (!match.Success || !double.TryParse(match.Groups[2].Value.Replace(",", ""), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var target) || target <= 0)
        {
            label.Text = text;
            return;
        }
        var (before, digits, after) = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
        var decimals = digits.Contains('.') ? digits.Length - digits.IndexOf('.') - 1 : 0;
        var grouped = digits.Contains(',');
        string Show(double v) => before + v.ToString((grouped ? "#,0" : "0") + (decimals > 0 ? "." + new string('0', decimals) : ""),
            System.Globalization.CultureInfo.InvariantCulture) + after;
        label.Text = Show(0);
        new Animation(v => label.Text = Show(target * v)).Commit(label, "count", 16, 900, Easing.CubicOut, (_, _) => label.Text = text);
    }
}

/// <summary>
/// Fades and rises into place each time <c>When</c> turns true, e.g. a tab's cards as it comes into view, one after
/// another by <see cref="DelayProperty"/> (ms).
/// </summary>
public static class Rise
{
    public static readonly BindableProperty WhenProperty =
        BindableProperty.CreateAttached("When", typeof(bool), typeof(Rise), false, propertyChanged: OnWhenChanged);

    public static readonly BindableProperty DelayProperty =
        BindableProperty.CreateAttached("Delay", typeof(int), typeof(Rise), 0);

    public static bool GetWhen(BindableObject view) => (bool)view.GetValue(WhenProperty);
    public static void SetWhen(BindableObject view, bool value) => view.SetValue(WhenProperty, value);
    public static int GetDelay(BindableObject view) => (int)view.GetValue(DelayProperty);
    public static void SetDelay(BindableObject view, int value) => view.SetValue(DelayProperty, value);

    static async void OnWhenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not VisualElement view || newValue is not true)
            return;
        view.CancelAnimations();
        view.Opacity = 0;
        view.TranslationY = 22;
        if (GetDelay(view) is > 0 and var delay)
            await Task.Delay(delay);
        if (!GetWhen(view))
        {
            view.Opacity = 1;
            view.TranslationY = 0;
            return;
        }
        await Task.WhenAll(view.FadeTo(1, 300, Easing.CubicOut), view.TranslateTo(0, 0, 420, Easing.CubicOut));
    }
}
