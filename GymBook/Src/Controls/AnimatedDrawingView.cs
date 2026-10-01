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
    public void Restart() => _clock.Restart();

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
        // A one-second animation that only redraws, repeated for as long as it should run.
        new Animation(_ => Invalidate()).Commit(this, FramesAnimation, rate: 16, length: 1000,
            repeat: () => IsRunning && IsLoaded);
    }

    sealed class Frame(AnimatedDrawingView view) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect) => view.DrawFrame(canvas, rect, (float)view._clock.Elapsed.TotalSeconds);
    }
}
