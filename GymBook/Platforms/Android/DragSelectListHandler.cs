using Android.Content;
using Android.Views;
using GymBook.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace GymBook;

/// <summary>
/// <see cref="DragSelectList"/> on Android: a touch on a row's checkbox (the first <see cref="DragSelectList.CheckColumnWidth"/>)
/// starts selecting at once, anywhere else a long-press does. Until then the touch belongs to whatever is under the
/// finger (a tap on a row, a swipe that scrolls); once selecting, the list takes the touch over and the scrolling parents
/// are told to leave it alone until the finger lifts. Held near the top or bottom of the scrolling parent, the list
/// scrolls on by itself and keeps selecting. Like ReorderItemHandler.
/// </summary>
public class DragSelectListHandler : ContentViewHandler
{
    protected override ContentViewGroup CreatePlatformView() =>
        new DragSelectViewGroup(Context, () => VirtualView as DragSelectList) { CrossPlatformLayout = VirtualView };
}

sealed class DragSelectViewGroup : ContentViewGroup
{
    static readonly int LongPressMs = ViewConfiguration.LongPressTimeout;
    const int EdgeDp = 72, MaxScrollDp = 18, FrameMs = 16;

    readonly Func<DragSelectList?> _list;
    readonly int _slop;
    readonly Java.Lang.Runnable _begin, _scroll;
    float _downX, _downY, _lastRawY;
    bool _pending, _dragging;

    public DragSelectViewGroup(Context context, Func<DragSelectList?> list) : base(context)
    {
        _list = list;
        _slop = ViewConfiguration.Get(context)?.ScaledTouchSlop ?? 16;
        _begin = new Java.Lang.Runnable(Begin);
        _scroll = new Java.Lang.Runnable(AutoScroll);
    }

    /// <summary>Where the finger is, from the top of the list, in device-independent units.</summary>
    double LocalY(float rawY)
    {
        var at = new int[2];
        GetLocationOnScreen(at);
        return Context.FromPixels(rawY - at[1]);
    }

    void Begin()
    {
        if (!_pending)
            return;
        _pending = false;
        _dragging = true;
        Parent?.RequestDisallowInterceptTouchEvent(true);
        PerformHapticFeedback(FeedbackConstants.LongPress);
        _list()?.OnDragStarted(LocalY(_downY));
        PostDelayed(_scroll, FrameMs);
    }

    void End()
    {
        _pending = false;
        RemoveCallbacks(_begin);
        RemoveCallbacks(_scroll);
        if (!_dragging)
            return;
        _dragging = false;
        _list()?.OnDragEnded();
    }

    /// <summary>The nearest parent that scrolls up and down (MAUI's ScrollView is a NestedScrollView).</summary>
    Android.Views.View? Scroller()
    {
        for (var p = Parent; p != null; p = p.Parent)
            if (p is AndroidX.Core.Widget.NestedScrollView or Android.Widget.ScrollView)
                return (Android.Views.View)p;
        return null;
    }

    // While selecting, held near an edge of the scrolling parent: scroll that way, faster the closer, and select on.
    void AutoScroll()
    {
        if (!_dragging)
            return;
        if (Scroller() is { } scroller)
        {
            var at = new int[2];
            scroller.GetLocationOnScreen(at);
            var (top, bottom) = (at[1], at[1] + scroller.Height);
            var edge = Context.ToPixels(EdgeDp);
            var max = Context.ToPixels(MaxScrollDp);
            var step = _lastRawY < top + edge ? -max * (float)Math.Min(1, (top + edge - _lastRawY) / edge)
                : _lastRawY > bottom - edge ? max * (float)Math.Min(1, (_lastRawY - (bottom - edge)) / edge)
                : 0;
            if (step != 0)
            {
                scroller.ScrollBy(0, (int)step);
                _list()?.OnDragMoved(LocalY(_lastRawY));
            }
        }
        PostDelayed(_scroll, FrameMs);
    }

    public override bool OnInterceptTouchEvent(MotionEvent? e)
    {
        if (e != null)
            Track(e);
        return _dragging;
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e != null)
            Track(e);
        return _pending || _dragging;
    }

    void Track(MotionEvent e)
    {
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                End();
                (_downX, _downY, _lastRawY) = (e.RawX, e.RawY, e.RawY);
                _pending = true;
                var at = new int[2];
                GetLocationOnScreen(at);
                // The checkbox: at once. Anywhere else: held, so a quick swipe still scrolls and a tap still taps.
                if (Context.FromPixels(e.RawX - at[0]) <= (_list()?.CheckColumnWidth ?? 0))
                    Begin();
                else
                    PostDelayed(_begin, LongPressMs);
                break;
            case MotionEventActions.Move:
                _lastRawY = e.RawY;
                if (_dragging)
                    _list()?.OnDragMoved(LocalY(e.RawY));
                else if (_pending && (Math.Abs(e.RawX - _downX) > _slop / 2f || Math.Abs(e.RawY - _downY) > _slop / 2f))
                {
                    _pending = false;
                    RemoveCallbacks(_begin);
                }
                break;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                End();
                break;
        }
    }
}
