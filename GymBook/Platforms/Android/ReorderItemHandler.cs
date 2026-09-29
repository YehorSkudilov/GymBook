using Android.Content;
using Android.Views;
using GymBook.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace GymBook;

/// <summary>
/// <see cref="ReorderItem"/> on Android: a long-press starts the drag. Until then the touch belongs to whatever is
/// under the finger (a tap opens an exercise, a quick swipe scrolls the list or the day strip); once it fires, the item
/// takes the touch over and the scrolling parents are told to leave it alone until the finger lifts.
/// </summary>
public class ReorderItemHandler : ContentViewHandler
{
    protected override ContentViewGroup CreatePlatformView() =>
        new ReorderViewGroup(Context, () => VirtualView as ReorderItem) { CrossPlatformLayout = VirtualView };
}

sealed class ReorderViewGroup : ContentViewGroup
{
    // Android's own long-press time, so it feels like every other hold in the system.
    static readonly int LongPressMs = ViewConfiguration.LongPressTimeout;

    readonly Func<ReorderItem?> _item;
    readonly int _slop;
    readonly Java.Lang.Runnable _begin;
    float _downX, _downY;
    bool _pending, _dragging;

    public ReorderViewGroup(Context context, Func<ReorderItem?> item) : base(context)
    {
        _item = item;
        _slop = ViewConfiguration.Get(context)?.ScaledTouchSlop ?? 16;
        _begin = new Java.Lang.Runnable(Begin);
    }

    // The long-press fired with the finger still where it went down: the drag starts.
    void Begin()
    {
        if (!_pending)
            return;
        _pending = false;
        _dragging = true;
        Parent?.RequestDisallowInterceptTouchEvent(true);
        PerformHapticFeedback(FeedbackConstants.LongPress);
        // Drawn above the rows it passes over. Elevation, not MAUI's ZIndex, which re-adds the view and cancels the touch.
        TranslationZ = Context.ToPixels(8);
        _item()?.OnReorderStarted();
    }

    void EndDrag(bool canceled)
    {
        _dragging = false;
        TranslationZ = 0;
        _item()?.OnReorderEnded(canceled);
    }

    // Sees the touch while a child (e.g. the tap to open an exercise) has it; takes it over once dragging.
    public override bool OnInterceptTouchEvent(MotionEvent? e)
    {
        if (e != null)
            Track(e);
        return _dragging;
    }

    // The touch once dragging, or when no child wanted it (still worth watching for a long-press).
    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e != null)
            Track(e);
        return true;
    }

    void Track(MotionEvent e)
    {
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                // Raw (screen) positions: the item itself moves under the finger while dragging.
                (_downX, _downY) = (e.RawX, e.RawY);
                _dragging = false;
                _pending = true;
                RemoveCallbacks(_begin);
                PostDelayed(_begin, LongPressMs);
                break;
            case MotionEventActions.Move:
                var dx = e.RawX - _downX;
                var dy = e.RawY - _downY;
                if (_dragging)
                    _item()?.OnReorderMoved(Context.FromPixels(dx), Context.FromPixels(dy));
                // Any real movement before the long-press is a scroll or a swipe: it gets the touch, not the drag.
                else if (_pending && (Math.Abs(dx) > _slop / 2f || Math.Abs(dy) > _slop / 2f))
                {
                    _pending = false;
                    RemoveCallbacks(_begin);
                }
                break;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                _pending = false;
                RemoveCallbacks(_begin);
                if (_dragging)
                    EndDrag(e.ActionMasked == MotionEventActions.Cancel);
                break;
        }
    }
}
