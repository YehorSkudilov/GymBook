using Microsoft.Maui.Controls.Shapes;

namespace GymBook.Controls;

/// <summary>
/// "Online" or "Offline" with a coloured dot, the same pill as on Home, keeping itself up to date while it's on screen.
/// </summary>
public class ConnectionPill : ContentView
{
    static readonly Color OnlineColor = Color.FromArgb("#2ED47A"), OfflineColor = Color.FromArgb("#FFB020");

    readonly Ellipse _dot = new() { WidthRequest = 7, HeightRequest = 7, VerticalOptions = LayoutOptions.Center };
    readonly Label _text = new() { FontSize = 11, FontFamily = "OpenSansSemibold", VerticalOptions = LayoutOptions.Center };

    public ConnectionPill()
    {
        _text.TextColor = (Color)Application.Current!.Resources["TextSecondary"];
        Content = new Border
        {
            Style = (Style)Application.Current.Resources["Pill"],
            BackgroundColor = (Color)Application.Current.Resources["Surface2"],
            Padding = new Thickness(8, 3),
            Content = new HorizontalStackLayout { Spacing = 5, Children = { _dot, _text } },
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
