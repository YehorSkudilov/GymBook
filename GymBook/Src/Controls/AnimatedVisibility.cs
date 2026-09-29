namespace GymBook.Controls;

/// <summary>
/// IsVisible with an animation: set <c>controls:AnimatedVisibility.IsShown</c> instead, and the view grows open and
/// fades in, or fades out and collapses, rather than popping. Views not on screen yet just take the state, so a page
/// doesn't animate while it's being built.
/// </summary>
public static class AnimatedVisibility
{
    const uint Length = 220;
    const string Name = "AnimatedVisibility";

    // Each view's own minimum height, kept while an animation zeroes it (an interrupted one would otherwise lose it).
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<View, object> Minimums = new();

    static double OwnMinimum(View view) => (double)Minimums.GetValue(view, v => v.MinimumHeightRequest);

    public static readonly BindableProperty IsShownProperty = BindableProperty.CreateAttached(
        "IsShown", typeof(bool), typeof(AnimatedVisibility), true, propertyChanged: OnIsShownChanged);

    public static bool GetIsShown(BindableObject view) => (bool)view.GetValue(IsShownProperty);

    public static void SetIsShown(BindableObject view, bool value) => view.SetValue(IsShownProperty, value);

    static void OnIsShownChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not View view)
            return;
        var show = (bool)newValue;
        if (view.Handler == null || view.Parent is not VisualElement { Width: > 0 } parent || show == view.IsVisible && !view.AnimationIsRunning(Name))
        {
            view.AbortAnimation(Name);
            view.IsVisible = show;
            return;
        }
        if (show)
            Expand(view, parent.Width);
        else
            Collapse(view);
    }

    static void Expand(View view, double width)
    {
        var minimum = OwnMinimum(view);
        view.AbortAnimation(Name);
        // Measured once visible (a hidden view measures as nothing), while still transparent.
        view.Opacity = 0;
        view.IsVisible = true;
        view.HeightRequest = -1;
        var target = Math.Max(minimum, view.Measure(width, double.PositiveInfinity).Height);
        // The minimum height would stop it starting from nothing; it comes back when the animation ends.
        view.MinimumHeightRequest = 0;
        view.HeightRequest = 0;

        var animation = new Animation();
        animation.Add(0, 1, new Animation(v => view.HeightRequest = v, 0, target, Easing.CubicOut));
        animation.Add(0.3, 1, new Animation(v => view.Opacity = v, 0, 1));
        animation.Commit(view, Name, length: Length, finished: (_, _) => Restore(view, minimum));
    }

    static void Collapse(View view)
    {
        var minimum = OwnMinimum(view);
        view.AbortAnimation(Name);
        view.MinimumHeightRequest = 0;

        var animation = new Animation();
        animation.Add(0, 1, new Animation(v => view.HeightRequest = v, view.Height, 0, Easing.CubicIn));
        animation.Add(0, 0.7, new Animation(v => view.Opacity = v, 1, 0));
        animation.Commit(view, Name, length: Length, finished: (_, _) =>
        {
            // Only hide if nothing asked for it to show again meanwhile.
            if (!GetIsShown(view))
                view.IsVisible = false;
            Restore(view, minimum);
        });
    }

    static void Restore(View view, double minimum)
    {
        view.HeightRequest = -1;
        view.MinimumHeightRequest = minimum;
        view.Opacity = 1;
    }
}
