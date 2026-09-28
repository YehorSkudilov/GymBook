#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
#endif

namespace GymBook.Views;

/// <summary>
/// Lets a horizontal list scroll with a mouse on Windows, where only touch pans it: the wheel scrolls it sideways,
/// and dragging with the button held pans it. Does nothing on other platforms.
/// </summary>
public static class HorizontalMouseScroll
{
    public static void Attach(VisualElement view)
    {
#if WINDOWS
        view.HandlerChanged += (_, _) =>
        {
            if (view.Handler?.PlatformView is FrameworkElement platform)
                Hook(platform);
        };
        if (view.Handler?.PlatformView is FrameworkElement current)
            Hook(current);
#endif
    }

#if WINDOWS
    const double DragThreshold = 6;

    static void Hook(FrameworkElement platform)
    {
        double? pressX = null;
        double startOffset = 0;
        var dragging = false;

        // The list's ScrollViewer marks wheel and pointer events handled, so listen to handled ones too.
        platform.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler((_, e) =>
        {
            if (FindScrollViewer(platform) is not { } scroll)
                return;
            var delta = e.GetCurrentPoint(platform).Properties.MouseWheelDelta;
            scroll.ChangeView(scroll.HorizontalOffset - delta, null, null, false);
            e.Handled = true;
        }), true);

        platform.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, e) =>
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse || FindScrollViewer(platform) is not { } scroll)
                return;
            pressX = e.GetCurrentPoint(platform).Position.X;
            startOffset = scroll.HorizontalOffset;
            dragging = false;
        }), true);

        platform.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((_, e) =>
        {
            if (pressX is not { } x || FindScrollViewer(platform) is not { } scroll)
                return;
            if (!e.GetCurrentPoint(platform).Properties.IsLeftButtonPressed)
            {
                pressX = null;
                return;
            }
            var dx = e.GetCurrentPoint(platform).Position.X - x;
            if (!dragging && Math.Abs(dx) < DragThreshold)
                return;
            dragging = true;
            platform.CapturePointer(e.Pointer);
            scroll.ChangeView(startOffset - dx, null, null, true);
            e.Handled = true;
        }), true);

        void End(object? sender, PointerRoutedEventArgs e)
        {
            // A drag isn't a tap: swallow the release so the tile under the pointer isn't selected.
            if (dragging)
                e.Handled = true;
            pressX = null;
            dragging = false;
            platform.ReleasePointerCapture(e.Pointer);
        }
        platform.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(End), true);
        platform.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((_, _) => { pressX = null; dragging = false; }), true);
    }

    static WScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is WScrollViewer scroll)
            return scroll;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (FindScrollViewer(VisualTreeHelper.GetChild(root, i)) is { } found)
                return found;
        return null;
    }
#endif
}
