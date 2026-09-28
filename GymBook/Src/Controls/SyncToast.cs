using GymBook.Services.Sync;

namespace GymBook.Controls;

/// <summary>
/// A small "Syncing…" pill drawn over the whole window, so it shows on every page and popup alike, and only
/// while a sync is running. It waits a moment before appearing, so quick syncs don't flash it, and stays up
/// long enough to read once shown. Touches pass straight through to the page underneath.
/// </summary>
public sealed class SyncToast : WindowOverlay, IWindowOverlayElement
{
    static readonly TimeSpan ShowAfter = TimeSpan.FromMilliseconds(300), MinVisible = TimeSpan.FromMilliseconds(800);
    static readonly Color Fill = Color.FromArgb("#272C39").WithAlpha(0.96f);
    static readonly Color Outline = Color.FromArgb("#353B49");
    static readonly Color TextColor = Color.FromArgb("#F4F6FB");
    static readonly Color Accent = Color.FromArgb("#3F7DFF");
    const string Caption = "Syncing…";
    const float FontSize = 13, Height = 32, Spinner = 12;

    readonly SyncService _sync;
    readonly IDispatcherTimer _timer;
    DateTime _syncStarted, _shownAt;
    float _angle;

    public SyncToast(IWindow window, SyncService sync) : base(window)
    {
        _sync = sync;
        EnableDrawableTouchHandling = false;
        IsVisible = false;
        AddWindowElement(this);

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(40);
        _timer.Tick += (_, _) => Tick();
        sync.StatusChanged += (_, _) => MainThread.BeginInvokeOnMainThread(OnStatusChanged);
    }

    void OnStatusChanged()
    {
        if (_sync.State == SyncState.Syncing && !_timer.IsRunning)
        {
            _syncStarted = DateTime.Now;
            _timer.Start();
        }
    }

    void Tick()
    {
        var now = DateTime.Now;
        var syncing = _sync.State == SyncState.Syncing;
        if (!IsVisible && syncing && now - _syncStarted >= ShowAfter)
        {
            IsVisible = true;
            _shownAt = now;
        }
        else if (IsVisible && !syncing && now - _shownAt >= MinVisible)
        {
            IsVisible = false;
        }
        if (!IsVisible && !syncing)
        {
            _timer.Stop();
            Invalidate();
            return;
        }
        _angle = (_angle + 18) % 360;
        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!IsVisible)
            return;
        var font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.Font = font;
        canvas.FontSize = FontSize;
        var textWidth = canvas.GetStringSize(Caption, font, FontSize).Width;
        var width = 14 + Spinner + 8 + textWidth + 16;
        // Below the status bar on phones, near the top edge on desktop.
        var top = DeviceInfo.Idiom == DeviceIdiom.Desktop ? 12f : 52f;
        var left = dirtyRect.Center.X - width / 2;

        canvas.FillColor = Fill;
        canvas.FillRoundedRectangle(left, top, width, Height, Height / 2);
        canvas.StrokeColor = Outline;
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(left, top, width, Height, Height / 2);

        // A spinning quarter-arc.
        var sx = left + 14;
        var sy = top + (Height - Spinner) / 2;
        canvas.StrokeColor = Accent.WithAlpha(0.25f);
        canvas.StrokeSize = 2;
        canvas.DrawEllipse(sx, sy, Spinner, Spinner);
        canvas.StrokeColor = Accent;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawArc(sx, sy, Spinner, Spinner, _angle, _angle + 100, false, false);

        canvas.FontColor = TextColor;
        canvas.DrawString(Caption, sx + Spinner + 8, top, textWidth + 4, Height, HorizontalAlignment.Left, VerticalAlignment.Center);
    }

    public bool Contains(Point point) => false;
}
