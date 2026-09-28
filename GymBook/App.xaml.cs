using GymBook.Services;
using GymBook.Services.Sync;
using GymBook.Views;

namespace GymBook;

public partial class App : Application
{
    readonly DataStore _store;
    readonly SyncService _sync;
    readonly IServiceProvider _services;

    public App(DataStore store, SyncService sync, IServiceProvider services)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _store = store;
        _sync = sync;
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page root = _store.Profile.OnboardingDone
            ? new AppShell()
            : _services.GetRequiredService<PlanWizardPage>().ForOnboarding();
        var window = new Window(root) { Title = "GymBook" };
#if WINDOWS
        // The layout is designed for phones; open desktop builds at a phone-like size.
        window.Width = 440;
        window.Height = 760;
#endif
        // Catch up with changes made on other devices.
        window.Created += (_, _) =>
        {
            window.AddOverlay(new Controls.SyncToast(window, _sync));
            _sync.Schedule(TimeSpan.Zero);
        };
        window.Resumed += (_, _) => _sync.Schedule(TimeSpan.Zero);
        return window;
    }

    /// <summary>Called when onboarding finishes to swap in the main shell.</summary>
    public static void ShowMainShell()
    {
        var window = Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new AppShell();
    }
}
