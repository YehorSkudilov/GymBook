using Microsoft.Maui.Controls.Shapes;

namespace GymBook.Controls;

/// <summary>
/// "Online" or "Offline" with a coloured dot, keeping itself up to date while it's on screen. In each tab's badges
/// (StatusBadges), and only there, so no page shows it twice.
/// </summary>
public class ConnectionPill : ContentView
{
    static readonly Color OnlineColor = Color.FromArgb("#2ED47A"), OfflineColor = Color.FromArgb("#FFB020");

    // Sized like the Nutrition tab's streak pill, as every badge is: the dot centred where its 16 px icon would be, 13 pt
    // text, 8,4 padding.
    readonly Ellipse _dot = new() { WidthRequest = 9, HeightRequest = 9, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    readonly Label _text = new() { FontSize = 13, FontFamily = "OpenSansSemibold", VerticalOptions = LayoutOptions.Center };

    public ConnectionPill()
    {
        Content = new Border
        {
            Style = (Style)Application.Current!.Resources["Pill"],
            BackgroundColor = (Color)Application.Current.Resources["Surface2"],
            Padding = new Thickness(8, 4),
            Content = new HorizontalStackLayout
            {
                Spacing = 3,
                Children = { new Grid { WidthRequest = 16, HeightRequest = 16, VerticalOptions = LayoutOptions.Center, Children = { _dot } }, _text },
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
