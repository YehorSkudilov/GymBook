using System.Runtime.CompilerServices;
using Android.Content;
using Android.Views;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Canvas = Android.Graphics.Canvas;

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

sealed class PassiveGraphicsView : PlatformTouchGraphicsView
{
    readonly PlatformCanvas _canvas;
    readonly ScalingCanvas _scalingCanvas;

    public PassiveGraphicsView(Context context) : base(context)
    {
        _canvas = new PlatformCanvas(context);
        _scalingCanvas = new ScalingCanvas(_canvas);
    }

    public override bool OnTouchEvent(MotionEvent? e) => false;

    // MAUI's own drawing resets its canvas three times a draw, and each reset makes six new Android Paints through
    // JNI (and frees as many): most of what an animated decoration cost, 30 times a second. Here the canvas is given
    // each frame's Android canvas without a reset and the drawing is wrapped in one save and restore, which puts the
    // state back the same way for three Paints. The drawables keep their saves and restores paired, so nothing builds up.
    public override void Draw(Canvas? canvas)
    {
        var drawable = Drawable;
        int width = Width, height = Height;
        if (canvas == null || drawable == null || width <= 0 || height <= 0)
            return;

        int count = canvas.Save();
        if (LayoutDirection == Android.Views.LayoutDirection.Rtl)
        {
            canvas.Translate(width, 0);
            canvas.Scale(-1, 1);
        }
        canvas.ClipRect(0, 0, width, height);

        PlatformCanvasField(_canvas) = canvas;
        _scalingCanvas.SaveState();
        try
        {
            if (BackgroundColor != null)
            {
                _scalingCanvas.FillColor = BackgroundColor;
                _scalingCanvas.FillRectangle(0, 0, width, height);
            }
            float density = Resources?.DisplayMetrics?.Density ?? 1;
            _scalingCanvas.Scale(density, density);
            drawable.Draw(_scalingCanvas, new RectF(0, 0, width / density, height / density));
        }
        finally
        {
            _scalingCanvas.RestoreState();
            PlatformCanvasField(_canvas) = null;
            canvas.RestoreToCount(count);
        }
    }

    // PlatformCanvas.Canvas's setter resets the state, which is what this avoids.
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_canvas")]
    static extern ref Canvas? PlatformCanvasField(PlatformCanvas canvas);
}
