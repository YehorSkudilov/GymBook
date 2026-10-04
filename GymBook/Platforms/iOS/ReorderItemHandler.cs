using CoreGraphics;
using GymBook.Controls;
using Microsoft.Maui.Handlers;
using UIKit;

namespace GymBook;

/// <summary>
/// <see cref="ReorderItem"/> on iOS, as on Android: holding the item (or its grip, if it has one; never a text box in it)
/// starts the drag, so a swipe that starts on it still scrolls. Until then the touch belongs to whatever is under the
/// finger (a tap opens an exercise, a swipe scrolls the list or the day strip); once it starts, the scroll views around
/// it stay put until the finger lifts.
/// </summary>
public class ReorderItemHandler : ContentViewHandler
{
    UILongPressGestureRecognizer? _hold;
    CGPoint _down;
    bool _dragging;
    readonly List<UIScrollView> _frozen = [];

    ReorderItem? Item => VirtualView as ReorderItem;

    protected override void ConnectHandler(Microsoft.Maui.Platform.ContentView platformView)
    {
        base.ConnectHandler(platformView);
        // The system's own hold time and slack (any real movement before it is a scroll), like every other hold.
        _hold = new UILongPressGestureRecognizer(OnHold) { ShouldReceiveTouch = (_, touch) => MayDrag(touch) };
        platformView.AddGestureRecognizer(_hold);
    }

    protected override void DisconnectHandler(Microsoft.Maui.Platform.ContentView platformView)
    {
        if (_hold != null)
        {
            platformView.RemoveGestureRecognizer(_hold);
            _hold.Dispose();
            _hold = null;
        }
        Unfreeze();
        base.DisconnectHandler(platformView);
    }

    // On an item with a grip, from the grip only; on one without, from anywhere on it but a text box, where holding
    // selects text.
    bool MayDrag(UITouch touch)
    {
        if (Item?.FindHandle()?.Handler?.PlatformView is UIView handle)
            return handle.PointInside(touch.LocationInView(handle), null);
        for (var view = touch.View; view != null && view != PlatformView; view = view.Superview)
            if (view is UITextField or UITextView)
                return false;
        return true;
    }

    void OnHold(UILongPressGestureRecognizer hold)
    {
        // Window positions: the item itself moves under the finger while dragging. Points are already MAUI's units.
        var at = hold.LocationInView(null);
        switch (hold.State)
        {
            case UIGestureRecognizerState.Began:
                _down = at;
                _dragging = true;
                Freeze();
                new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Medium).ImpactOccurred();
                // Drawn above the rows it passes over. The layer's z, not MAUI's ZIndex, which reorders the subviews.
                PlatformView.Layer.ZPosition = 1;
                Item?.OnReorderStarted();
                break;
            case UIGestureRecognizerState.Changed when _dragging:
                Item?.OnReorderMoved(at.X - _down.X, at.Y - _down.Y);
                break;
            case UIGestureRecognizerState.Ended or UIGestureRecognizerState.Cancelled or UIGestureRecognizerState.Failed when _dragging:
                _dragging = false;
                Unfreeze();
                PlatformView.Layer.ZPosition = 0;
                Item?.OnReorderEnded(hold.State != UIGestureRecognizerState.Ended);
                break;
        }
    }

    // The scroll views around the item (the page, the day strip) don't scroll while it's being dragged.
    void Freeze()
    {
        for (var view = PlatformView.Superview; view != null; view = view.Superview)
            if (view is UIScrollView { ScrollEnabled: true } scroll)
            {
                scroll.ScrollEnabled = false;
                _frozen.Add(scroll);
            }
    }

    void Unfreeze()
    {
        foreach (var scroll in _frozen)
            scroll.ScrollEnabled = true;
        _frozen.Clear();
    }
}
