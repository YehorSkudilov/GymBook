using Microsoft.Maui.Controls.Shapes;

namespace GymBook.Controls;

/// <summary>
/// "Online" or "Offline" with a coloured dot, keeping itself up to date while it's on screen. In each tab's badges
/// (StatusBadges), and only there, so no page shows it twice.
/// </summary>
public class ConnectionPill : ContentView
{
    static readonly Color OnlineColor = Color.FromArgb("#2ED47A"), OfflineColor = Color.FromArgb("#FFB020");

    // A notch smaller than the other badges so it reads as a status, not an action: the dot centred in a 12 px slot,
    // 11 pt text, 6,3 padding.
    readonly Ellipse _dot = new() { WidthRequest = 7, HeightRequest = 7, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    readonly Label _text = new() { FontSize = 11, FontFamily = "OpenSansSemibold", VerticalOptions = LayoutOptions.Center };

    public ConnectionPill()
    {
        Content = new Border
        {
            Style = (Style)Application.Current!.Resources["Pill"],
            BackgroundColor = (Color)Application.Current.Resources["Surface2"],
            Padding = new Thickness(6, 3),
            Content = new HorizontalStackLayout
            {
                Spacing = 2,
                Children = { new Grid { WidthRequest = 12, HeightRequest = 12, VerticalOptions = LayoutOptions.Center, Children = { _dot } }, _text },
            },
        };
        VerticalOptions = LayoutOptions.Center;
        Loaded += (_, _) =>
        {
            Connectivity.Current.ConnectivityChanged -= OnChanged;
            Connectivity.Current.ConnectivityChanged += OnChanged;
            Show();
        };
        Unloaded += (_, _) => Connectivity.Current.ConnectivityChanged -= OnChanged;
    }

    void OnChanged(object? sender, ConnectivityChangedEventArgs e) => MainThread.BeginInvokeOnMainThread(Show);

    void Show()
    {
        var online = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        _text.Text = online ? "Online" : "Offline";
        _dot.Fill = online ? OnlineColor : OfflineColor;
    }
}
