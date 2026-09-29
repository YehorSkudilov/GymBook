using Android.Content;
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace GymBook;

/// <summary>
/// Every GraphicsView on Android (muscle maps, charts, the animated decorations): they only draw, so they don't keep a
/// touch that lands on them. MAUI's own view claims every touch for drawing interactions the app never uses, so a
/// swipe that starts on a map or a chart doesn't scroll the page. Taps still work: gesture recognizers on the view, or
/// on anything around it, see the touch as before.
/// </summary>
public class PassiveGraphicsViewHandler : GraphicsViewHandler
{
    protected override PlatformTouchGraphicsView CreatePlatformView() => new PassiveGraphicsView(Context);
}

sealed class PassiveGraphicsView(Context context) : PlatformTouchGraphicsView(context)
{
    public override bool OnTouchEvent(MotionEvent? e) => false;
}
