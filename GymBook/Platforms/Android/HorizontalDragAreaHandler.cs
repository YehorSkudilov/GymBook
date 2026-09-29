using Android.Content;
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace GymBook;

/// <summary>
/// <see cref="Controls.HorizontalDragArea"/> on Android: once a drag inside it is clearly sideways, the parents (the
/// swiping tabs, a page's scroll view) are told not to take it, so the strip inside scrolls. It decides at half the
/// usual touch slop, before the tabs would claim the drag at the full slop.
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
    public override bool OnInterceptTouchEvent(MotionEvent? e)
    {
        if (e == null)
            return false;
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                (_downX, _downY, _decided) = (e.RawX, e.RawY, false);
                break;
            case MotionEventActions.Move when !_decided:
                var dx = Math.Abs(e.RawX - _downX);
                var dy = Math.Abs(e.RawY - _downY);
                if (dx > _slop || dy > _slop)
                {
                    _decided = true;
                    if (dx > dy)
                        Parent?.RequestDisallowInterceptTouchEvent(true);
                }
                break;
        }
        return false;
    }
}
