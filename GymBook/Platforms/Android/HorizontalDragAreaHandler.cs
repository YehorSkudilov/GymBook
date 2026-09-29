using Android.Content;
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace GymBook;

/// <summary>
/// <see cref="Controls.HorizontalDragArea"/> on Android: a touch inside it belongs to the strip from the moment it
/// lands, so the parents (the swiping tabs, a page's scroll view) can't take a sideways drag, however quick. Only once
/// the drag turns out to be mostly up or down is it handed back, so the page still scrolls.
/// </summary>
public class HorizontalDragAreaHandler : ContentViewHandler
{
    protected override ContentViewGroup CreatePlatformView() => new DragAreaViewGroup(Context) { CrossPlatformLayout = VirtualView };
}

sealed class DragAreaViewGroup(Context context) : ContentViewGroup(context)
{
    readonly float _slop = (ViewConfiguration.Get(context)?.ScaledTouchSlop ?? 16) / 2f;
    float _downX, _downY;
    bool _decided;

    // Only watches: the touch still goes to the strip and its chips.
    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e != null)
            Watch(e);
        return base.DispatchTouchEvent(e);
    }

    void Watch(MotionEvent e)
    {
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                (_downX, _downY, _decided) = (e.RawX, e.RawY, false);
                // Claimed straight away: the tabs would otherwise take a fast sideways flick before it's measured.
                Parent?.RequestDisallowInterceptTouchEvent(true);
                break;
            case MotionEventActions.Move when !_decided:
                var dx = Math.Abs(e.RawX - _downX);
                var dy = Math.Abs(e.RawY - _downY);
                if (dx > _slop || dy > _slop)
                {
                    _decided = true;
                    // Up or down: the page scrolls after all.
                    if (dy > dx)
                        Parent?.RequestDisallowInterceptTouchEvent(false);
                }
                break;
        }
    }
}
