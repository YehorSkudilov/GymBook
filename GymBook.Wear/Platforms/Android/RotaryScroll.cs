using Android.Views;
using Microsoft.Maui.Handlers;

namespace GymBook.Wear;

/// <summary>
/// Scrolling with the watch's crown or rotating bezel: Wear OS sends their turns as scroll motion events to the
/// focused view, so every ScrollView takes focus when shown and scrolls by the turn (Galaxy Watch bezels included).
/// </summary>
public static class RotaryScroll
{
    public static void Register() =>
        ScrollViewHandler.Mapper.AppendToMapping("RotaryScroll", (handler, _) =>
        {
            var view = handler.PlatformView;
            view.Focusable = true;
            view.FocusableInTouchMode = true;
            view.ViewAttachedToWindow -= OnAttached;
            view.ViewAttachedToWindow += OnAttached;
            view.GenericMotion -= OnGenericMotion;
            view.GenericMotion += OnGenericMotion;
        });

    // Focus on showing, so the crown scrolls this page straight away.
    static void OnAttached(object? sender, Android.Views.View.ViewAttachedToWindowEventArgs e) => (sender as Android.Views.View)?.RequestFocus();

    static void OnGenericMotion(object? sender, Android.Views.View.GenericMotionEventArgs e)
    {
        if (sender is not Android.Views.View view || e.Event is not { } motion
            || motion.Action != MotionEventActions.Scroll || !motion.IsFromSource(InputSourceType.RotaryEncoder))
        {
            e.Handled = false;
            return;
        }
        // Turning clockwise gives a negative value and should move down the page.
        var factor = ViewConfiguration.Get(view.Context)?.ScaledVerticalScrollFactor ?? 64f;
        view.ScrollBy(0, (int)Math.Round(-motion.GetAxisValue(Axis.Scroll) * factor));
        e.Handled = true;
    }
}
