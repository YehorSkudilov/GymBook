using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>
/// The badges at the far right of each tab's title, the same on every tab: bound to the app's one
/// <see cref="StatusBadgesViewModel"/>, so the page they're on needn't know about them.
/// </summary>
public partial class StatusBadges : ContentView
{
    public StatusBadges()
    {
        InitializeComponent();
        if (IPlatformApplication.Current?.Services.GetService<StatusBadgesViewModel>() is { } badges)
        {
            BindingContext = badges;
            Loaded += (_, _) => badges.Start();
        }
    }
}
