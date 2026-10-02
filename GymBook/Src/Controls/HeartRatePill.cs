using GymBook.Services;
using Microsoft.Maui.Controls.Shapes;

namespace GymBook.Controls;

/// <summary>
/// "♥ 128": the heart rate the Wear OS app is reading, live while it's on screen. Hidden when there's none (no watch,
/// or none for a while).
/// </summary>
public class HeartRatePill : ContentView
{
    readonly Label _text = new()
    {
        FontSize = 11,
        FontFamily = "OpenSansSemibold",
        TextColor = Color.FromArgb("#FF4D5E"),
        VerticalOptions = LayoutOptions.Center,
    };
    WatchLink? _watch;
    IDispatcherTimer? _timer;

    public HeartRatePill()
    {
        Content = new Border
        {
            Style = (Style)Application.Current!.Resources["Pill"],
            BackgroundColor = (Color)Application.Current.Resources["Surface2"],
            Padding = new Thickness(8, 3),
            Content = _text,
        };
        VerticalOptions = LayoutOptions.Center;
        IsVisible = false;
        Loaded += (_, _) =>
        {
            _watch ??= IPlatformApplication.Current?.Services.GetService<WatchLink>();
            if (_watch == null)
                return;
            _watch.HeartRateChanged -= Show;
            _watch.HeartRateChanged += Show;
            // Hides itself once the watch stops sending.
            _timer ??= Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick -= OnTick;
            _timer.Tick += OnTick;
            _timer.Start();
            Show();
        };
        Unloaded += (_, _) =>
        {
            if (_watch != null)
                _watch.HeartRateChanged -= Show;
            _timer?.Stop();
        };
    }

    void OnTick(object? sender, EventArgs e) => Show();

    void Show()
    {
        var bpm = _watch?.HeartRate;
        IsVisible = bpm != null;
        if (bpm != null)
            _text.Text = $"♥ {bpm}";
    }
}
