using Android.Content;
using Android.Views;
using GymBook.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace GymBook;

/// <summary>
/// <see cref="ReorderItem"/> on Android: holding the item (or its grip, if it has one; never a text box in it) starts the
/// drag, so a swipe that starts on it still scrolls. Until then the touch belongs to whatever is under the finger (a tap
/// opens an exercise, a swipe scrolls the list or the day strip); once it starts, the item takes the touch over and the
/// scrolling parents are told to leave it alone until the finger lifts.
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

    // The touch once dragging, or when no child wanted it: kept only while it may become a drag, so a touch that won't
    // (e.g. on an item with a grip, away from it) goes on to scroll the list.
    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e != null)
            Track(e);
        return _pending || _dragging;
    }

    /// <summary>A text box (shown) under the screen point <paramref name="x"/>, <paramref name="y"/> inside <paramref name="view"/>.</summary>
    static bool OnTextField(Android.Views.View view, int x, int y)
    {
        var bounds = new Android.Graphics.Rect();
        if (view.Visibility != ViewStates.Visible || !view.GetGlobalVisibleRect(bounds) || !bounds.Contains(x, y))
            return false;
        if (view is Android.Widget.EditText)
            return true;
        if (view is ViewGroup group)
            for (var i = 0; i < group.ChildCount; i++)
                if (group.GetChildAt(i) is { } child && OnTextField(child, x, y))
                    return true;
        return false;
    }

    void Track(MotionEvent e)
    {
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                // Raw (screen) positions: the item itself moves under the finger while dragging.
                (_downX, _downY) = (e.RawX, e.RawY);
                _dragging = false;
                _pending = false;
                RemoveCallbacks(_begin);
                // Held to drag (a long-press), so a swipe still scrolls: on an item with a grip, from the grip only; on one
                // without, from anywhere on it but a text box, where holding selects text.
                var handle = _item()?.FindHandle()?.Handler?.PlatformView as Android.Views.View;
                var hit = new Android.Graphics.Rect();
                if (handle == null
                        ? !OnTextField(this, (int)e.RawX, (int)e.RawY)
                        : handle.GetGlobalVisibleRect(hit) && hit.Contains((int)e.RawX, (int)e.RawY))
                {
                    _pending = true;
                    PostDelayed(_begin, LongPressMs);
                }
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
