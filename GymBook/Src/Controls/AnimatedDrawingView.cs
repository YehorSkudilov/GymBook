using System.Diagnostics;

namespace GymBook.Controls;

/// <summary>
/// A small drawing that animates itself, for decorations drawn in code (no images). Subclasses draw one frame for a
/// given time; this redraws every display frame while it's loaded and <see cref="IsRunning"/> is on.
/// </summary>
public abstract class AnimatedDrawingView : GraphicsView
{
    // Frames come from MAUI's animation ticker, which follows the display's refresh: a fixed timer (it was 33 ms) lands
    // unevenly against a 60/90/120 Hz screen, which shows as jitter in slow, large movement like a card's drifting glow.
    const string FramesAnimation = "AnimatedDrawingView.Frames";

    public static readonly BindableProperty IsRunningProperty =
        BindableProperty.Create(nameof(IsRunning), typeof(bool), typeof(AnimatedDrawingView), true,
            propertyChanged: (b, _, value) => ((AnimatedDrawingView)b).OnRunningChanged((bool)value));

    /// <summary>
    /// Off while its tab isn't showing: the tabs all stay loaded, so being on screen isn't enough to go by.
    /// </summary>
    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    // A monotonic clock, so frame times are evenly spaced (DateTime.Now can step).
    readonly Stopwatch _clock = Stopwatch.StartNew();

    protected AnimatedDrawingView()
    {
        Drawable = new Frame(this);
        InputTransparent = true;
        Loaded += (_, _) => OnRunningChanged(IsRunning);
        Unloaded += (_, _) => this.AbortAnimation(FramesAnimation);
    }

    /// <summary>Plays the animation again from its start (e.g. another burst of confetti).</summary>
    public void Restart()
    {
        _clock.Restart();
        _lastFrame = double.NegativeInfinity;
    }

    /// <summary>Draws the picture as it is <paramref name="seconds"/> into the animation.</summary>
    protected abstract void DrawFrame(ICanvas canvas, RectF rect, float seconds);

    void OnRunningChanged(bool running)
    {
        if (!running || !IsLoaded)
        {
            this.AbortAnimation(FramesAnimation);
            return;
        }
        if (this.AnimationIsRunning(FramesAnimation))
            return;
        // A one-second animation that only redraws, repeated for as long as it should run. Frames aren't drawn while
        // the view can't be seen (scrolled away, under another page, or inside something hidden); the picture goes by
        // the clock, so it carries on just where it would have been once it's back.
        new Animation(_ =>
        {
            // Every few display frames for a slow decoration (still on a frame, so evenly spaced): each redraw makes the
            // whole window draw again, and Home has several of these going at once.
            var now = _clock.Elapsed.TotalMilliseconds;
            if (now - _lastFrame < MinFrameInterval || !IsOnScreen())
                return;
            _lastFrame = now;
            Invalidate();
        }).Commit(this, FramesAnimation, rate: 16, length: 1000, repeat: () => IsRunning && IsLoaded);
    }

    double _lastFrame = double.NegativeInfinity;

    /// <summary>
    /// The least time between frames, in ms: about 30 a second by default (a little under 33, so a 60 Hz screen's every
    /// other frame makes it). 0 for every display frame, for quick movement like confetti.
    /// </summary>
    protected virtual double MinFrameInterval => 30;

#if ANDROID
    Android.Graphics.Rect? _visibleRect;
#endif

    bool IsOnScreen()
    {
#if ANDROID
        // Shown: it and every view above it are visible and in a window. A visible rect: some of it is inside the
        // screen and its scrolling parents.
        if (Handler?.PlatformView is Android.Views.View view)
            return view.IsShown && view.GetGlobalVisibleRect(_visibleRect ??= new Android.Graphics.Rect());
#endif
        for (Element? e = this; e != null; e = e.Parent)
            if (e is VisualElement { IsVisible: false })
                return false;
        return true;
    }

    sealed class Frame(AnimatedDrawingView view) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect) => view.DrawFrame(canvas, rect, (float)view._clock.Elapsed.TotalSeconds);
    }
}
