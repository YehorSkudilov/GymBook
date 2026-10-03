using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.Wear;

/// <summary>A page that runs things while on screen (the clock, the heart rate sensor), paused while the app isn't.</summary>
public interface ILivePage
{
    Task ResumeAsync();
    void Pause();
}

public partial class App : Application
{
    readonly IServiceProvider _services;
    readonly PhoneLink _phone;
    readonly SyncService _sync;

    public App(IServiceProvider services, PhoneLink phone, SyncService sync, DataStore store)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _services = services;
        _phone = phone;
        _sync = sync;
        // The Tile shows the workout in progress or the next one: redraw it when either changes. The Ongoing Activity
        // (the icon on the watch face) follows the workouts in progress, the watch's own and the phone's.
        store.Changed += (_, _) =>
        {
            WorkoutTileService.RequestUpdate();
            WatchOngoing.Refresh(phone.Workout);
        };
        phone.WorkoutChanged += WatchOngoing.Refresh;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Pages are made here rather than taken in the constructor: their XAML needs App.xaml's resources, which only
        // exist once InitializeComponent above has run. Back (swipe right, or the button) pops a page off the home.
        var navigation = new NavigationPage(_services.GetRequiredService<HomePage>());
        var window = new Window(navigation);
        // Leaving the app (or the screen turning off) doesn't make the page disappear, so follow the window: nothing
        // runs while the app isn't on screen, and coming back picks up whatever changed on the phone or the account.
        var live = _services.GetRequiredService<LiveSync>();
        window.Stopped += (_, _) =>
        {
            _phone.Stop();
            // The live sync connection too: the battery. Coming back reconnects and catches up.
            _ = live.StopAsync();
            (navigation.CurrentPage as ILivePage)?.Pause();
        };
        window.Resumed += async (_, _) =>
        {
            await StartPhoneLinkAsync();
            // Not signed in yet: through the phone, without asking, so its data comes over.
            await _services.GetRequiredService<WatchAccount>().TrySignInWithPhoneAsync();
            _sync.Schedule(TimeSpan.Zero);
            _ = live.StartAsync();
            if (navigation.CurrentPage is ILivePage page)
                await page.ResumeAsync();
        };
        _ = StartPhoneLinkAsync();
        _ = _services.GetRequiredService<WatchAccount>().TrySignInWithPhoneAsync();
        // Changes from the phone, the website or another watch the moment they're made, while the app is open.
        _ = live.StartAsync();
        return window;
    }

    // Following the phone's workout. Without a phone (or Google Play services) the watch still works on its own.
    async Task StartPhoneLinkAsync()
    {
        try
        {
            // Whether the phone is in reach decides companion or on its own (see WatchOwnership.IsWatchs).
            await _phone.CheckConnectedAsync();
            await _phone.StartAsync();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"GymBook.Wear: phone link failed: {e}");
        }
    }
}
