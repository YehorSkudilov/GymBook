namespace GymBook.Controls;

/// <summary>
/// A small drawing that animates itself, for decorations drawn in code (no images). Subclasses draw one frame for a
/// given time; this runs about 30 frames a second while it's loaded and <see cref="IsRunning"/> is on.
/// </summary>
public abstract class AnimatedDrawingView : GraphicsView
{
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

    DateTime _started = DateTime.Now;
    IDispatcherTimer? _timer;

    protected AnimatedDrawingView()
    {
        Drawable = new Frame(this);
        InputTransparent = true;
        Loaded += (_, _) => OnRunningChanged(IsRunning);
        Unloaded += (_, _) => _timer?.Stop();
    }

    /// <summary>Plays the animation again from its start (e.g. another burst of confetti).</summary>
    public void Restart() => _started = DateTime.Now;

    /// <summary>Draws the picture as it is <paramref name="seconds"/> into the animation.</summary>
    protected abstract void DrawFrame(ICanvas canvas, RectF rect, float seconds);

    void OnRunningChanged(bool running)
    {
        if (!running || !IsLoaded)
        {
            _timer?.Stop();
            return;
        }
        if (_timer == null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(33);
            _timer.Tick += (_, _) => Invalidate();
        }
        if (!_timer.IsRunning)
            _timer.Start();
    }

    sealed class Frame(AnimatedDrawingView view) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect) => view.DrawFrame(canvas, rect, (float)(DateTime.Now - view._started).TotalSeconds);
    }
}
