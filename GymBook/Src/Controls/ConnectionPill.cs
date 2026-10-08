using Microsoft.Maui.Controls.Shapes;

namespace GymBook.Controls;

/// <summary>
/// A coloured dot, green online and amber offline, keeping itself up to date while it's on screen. In each tab's badges
/// (StatusBadges), and only there, so no page shows it twice.
/// </summary>
public class ConnectionPill : ContentView
{
    static readonly Color OnlineColor = Color.FromArgb("#2ED47A"), OfflineColor = Color.FromArgb("#FFB020");

    // Just the dot, no pill or text; screen readers still hear "Online" or "Offline".
    readonly Ellipse _dot = new() { WidthRequest = 9, HeightRequest = 9, VerticalOptions = LayoutOptions.Center };

    public ConnectionPill()
    {
        Content = _dot;
        VerticalOptions = LayoutOptions.Center;
        Margin = new Thickness(4, 0);
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
        SemanticProperties.SetDescription(this, online ? "Online" : "Offline");
        _dot.Fill = online ? OnlineColor : OfflineColor;
    }
}
