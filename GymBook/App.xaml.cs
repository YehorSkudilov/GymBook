using GymBook.Services;
using GymBook.Services.Sync;
using GymBook.Views;

namespace GymBook;

public partial class App : Application
{
    readonly DataStore _store;
    readonly SyncService _sync;
    readonly IServiceProvider _services;

    public App(DataStore store, SyncService sync, AuthSession session, IServiceProvider services)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _store = store;
        _sync = sync;
        _services = services;
        // An unverified email blocks the app until it's verified, whenever that's found out (sign-in, token renewal).
        session.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            // The AI's plan reviews belong to the account that asked for them.
            if (!session.IsSignedIn)
                _services.GetRequiredService<AiPlanService>().ClearReviews();
            _ = VerifyEmailPage.ShowIfNeededAsync(_services);
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page root = _store.Profile.OnboardingDone
            ? new AppShell()
            : _services.GetRequiredService<PlanWizardPage>().ForOnboarding();
        var window = new Window(root) { Title = "Gym Book" };
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
            _ = VerifyEmailPage.ShowIfNeededAsync(_services);
            // The weekly AI look at the active plan; does nothing most of the time.
            _ = _services.GetRequiredService<AiPlanService>().CheckActivePlanAsync();
        };
        window.Resumed += (_, _) =>
        {
            _sync.Schedule(TimeSpan.Zero);
            _ = VerifyEmailPage.ShowIfNeededAsync(_services);
            _ = _services.GetRequiredService<AiPlanService>().CheckActivePlanAsync();
        };
        return window;
    }

    /// <summary>Called when onboarding finishes to swap in the main shell.</summary>
    public static void ShowMainShell()
    {
        var window = Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new AppShell();
    }

    /// <summary>
    /// Still on the first-run screens but the profile is already finished (it came down from the account, e.g. after
    /// signing in or verifying the email): skip the wizard. Does nothing otherwise, so it's safe to call more than once.
    /// </summary>
    public static void ShowMainShellIfOnboarded()
    {
        var window = Current?.Windows.FirstOrDefault();
        // Not while a sheet is still up over the welcome screen: swapping the root page would strand it.
        if (Current is App app && app._store.Profile.OnboardingDone && Shell.Current == null && window?.Page?.Navigation.ModalStack.Count == 0)
            ShowMainShell();
    }

    /// <summary>Back to the first-run welcome screen, e.g. after signing out.</summary>
    public static void ShowOnboarding()
    {
        var window = Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = Current!.Handler!.MauiContext!.Services.GetRequiredService<PlanWizardPage>().ForOnboarding();
    }
}
