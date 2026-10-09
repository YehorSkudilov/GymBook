using GymBook.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace GymBook;

/// <summary>
/// <see cref="ReorderItem"/> on Windows: pressed and moved, it's dragged. Not MAUI's pan gesture, which measures the
/// movement against the item itself; the item moving under the pointer then throws every reading off, so it jitters
/// between two places (a "phantom" copy). Here it's measured against the window, as Android does against the screen.
/// </summary>
public class ReorderItemHandler : ContentViewHandler
{
    // Moved less than this, it's still a tap (on the photo, to show the exercise).
    const double Slop = 4;

    Windows.Foundation.Point _down;
    Pointer? _pointer;
    bool _pressed, _dragging;
    PointerEventHandler? _onPressed, _onMoved, _onReleased, _onLost;

    ReorderItem? Item => VirtualView as ReorderItem;

    protected override void ConnectHandler(ContentPanel platformView)
    {
        base.ConnectHandler(platformView);
        // Handled ones too: the item's tap marks them handled.
        platformView.AddHandler(UIElement.PointerPressedEvent, _onPressed = OnPressed, true);
        platformView.AddHandler(UIElement.PointerMovedEvent, _onMoved = OnMoved, true);
        platformView.AddHandler(UIElement.PointerReleasedEvent, _onReleased = OnReleased, true);
        platformView.AddHandler(UIElement.PointerCanceledEvent, _onLost = OnLost, true);
        platformView.AddHandler(UIElement.PointerCaptureLostEvent, _onLost, true);
    }

    protected override void DisconnectHandler(ContentPanel platformView)
    {
        platformView.RemoveHandler(UIElement.PointerPressedEvent, _onPressed);
        platformView.RemoveHandler(UIElement.PointerMovedEvent, _onMoved);
        platformView.RemoveHandler(UIElement.PointerReleasedEvent, _onReleased);
        platformView.RemoveHandler(UIElement.PointerCanceledEvent, _onLost);
        platformView.RemoveHandler(UIElement.PointerCaptureLostEvent, _onLost);
        base.DisconnectHandler(platformView);
    }

    void OnPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(null);
        // The left mouse button, a finger or a pen; pressing in a text box selects text instead.
        if (point.PointerDeviceType == PointerDeviceType.Mouse && !point.Properties.IsLeftButtonPressed
                || OnTextBox(e.OriginalSource as DependencyObject))
            return;
        // Nothing in it starts Windows' own drag and drop (a see-through copy of it following the pointer).
        NoSystemDrag(PlatformView);
        // Window positions: the item itself moves under the pointer while dragging.
        _down = point.Position;
        _pointer = e.Pointer;
        _pressed = true;
        _dragging = false;
    }

    void OnMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_pressed || _pointer == null || e.Pointer.PointerId != _pointer.PointerId)
            return;
        var at = e.GetCurrentPoint(null).Position;
        var (dx, dy) = (at.X - _down.X, at.Y - _down.Y);
        if (!_dragging)
        {
            if (Math.Abs(dx) <= Slop && Math.Abs(dy) <= Slop)
                return;
            _dragging = true;
            PlatformView.CapturePointer(_pointer);
            // Drawn above the photos it passes over. Natively, not MAUI's ZIndex, which re-adds the view and drops the capture.
            Canvas.SetZIndex(PlatformView, 1);
            Item?.OnReorderStarted();
        }
        Item?.OnReorderMoved(dx, dy);
        e.Handled = true;
    }

    void OnReleased(object sender, PointerRoutedEventArgs e)
    {
        // A drag isn't a tap: the photo under the pointer isn't selected.
        if (_dragging)
            e.Handled = true;
        End(false);
    }

    void OnLost(object sender, PointerRoutedEventArgs e) => End(true);

    void End(bool canceled)
    {
        var dragging = _dragging;
        _pressed = _dragging = false;
        if (!dragging)
            return;
        if (_pointer != null)
            PlatformView.ReleasePointerCapture(_pointer);
        Canvas.SetZIndex(PlatformView, 0);
        Item?.OnReorderEnded(canceled);
    }

    static void NoSystemDrag(DependencyObject view)
    {
        if (view is UIElement element)
        {
            element.CanDrag = false;
            element.DragStarting -= CancelDrag;
            element.DragStarting += CancelDrag;
        }
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(view); i++)
            NoSystemDrag(VisualTreeHelper.GetChild(view, i));
    }

    static void CancelDrag(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs e) => e.Cancel = true;

    bool OnTextBox(DependencyObject? from)
    {
        for (var o = from; o != null && o != PlatformView; o = VisualTreeHelper.GetParent(o))
            if (o is TextBox or RichEditBox or PasswordBox)
                return true;
        return false;
    }
}
