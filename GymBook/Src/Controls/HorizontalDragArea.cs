namespace GymBook.Controls;

/// <summary>
/// A container for something that scrolls sideways (a strip of chips) on a tab. The tabs swipe sideways too
/// (AppSkeleton's CView takes over any mostly-horizontal drag), which would steal the drag from the strip. Inside this,
/// a drag that's clearly sideways keeps it: on Android it tells the parents to leave the gesture alone (see
/// HorizontalDragAreaHandler). Vertical drags still scroll the page. Elsewhere it's a plain ContentView.
/// </summary>
public class HorizontalDragArea : ContentView;
