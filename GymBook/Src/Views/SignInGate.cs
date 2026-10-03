using GymBook.Services.Sync;
using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>
/// Gym Book needs an account: without a session on this device, the sign-in sheet goes up over whatever is showing
/// and can't be closed until it signs in. Not over the first-run welcome screen, though: that comes first, and its
/// buttons ask for the account (see <see cref="PlanWizardViewModel"/>). A session is kept on the device (see <see cref="AuthSession"/>), so once
/// signed in the app never needs a connection to open, and losing the connection never signs anyone out.
/// Data already on the device (e.g. from before an account was required) is kept and moves into the account.
/// </summary>
public static class SignInGate
{
    static bool _checking;

    // Set by "Continue offline": someone with data here and no connection carries on with it until they're back online.
    static bool _skipped;

    /// <summary>For this run of the app, or until it comes back to the foreground with a connection.</summary>
    public static void SkipUntilOnline() => _skipped = true;

    /// <summary>
    /// From the welcome screen's buttons: the required sheet straight away, on creating an account if
    /// <paramref name="register"/>. Not through <see cref="ShowIfNeededAsync"/>, whose checks (one already running, an
    /// offline skip) could swallow a tap. Does nothing when signed in or a sign-in sheet is already up.
    /// </summary>
    public static async Task AskAsync(IServiceProvider services, bool register)
    {
        var account = services.GetRequiredService<AccountService>();
        await account.EnsureLoadedAsync();
        if (account.IsSignedIn || Application.Current?.Windows.FirstOrDefault()?.Page is not { } root
            || root.Navigation.ModalStack.Any(p => p is AccountPage))
            return;
        await AccountPage.ShowAsync(services, register, required: true);
    }

    /// <summary>
    /// Shows the required sign-in sheet when there's no session, unless it's already up. Waits while another sheet or
    /// dialog is open, so it lands on top. <paramref name="resumed"/>: back in the foreground (or just opened), when an
    /// offline skip ends if there's a connection again.
    /// </summary>
    public static async Task ShowIfNeededAsync(IServiceProvider services, bool resumed = false)
    {
        if (resumed && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            _skipped = false;
        if (_checking)
            return;
        _checking = true;
        try
        {
            var account = services.GetRequiredService<AccountService>();
            await account.EnsureLoadedAsync();
            // Let a root page swap that's under way (signing out goes back to the welcome screen) land first.
            await Task.Delay(100);
            while (!account.IsSignedIn && !_skipped && Application.Current?.Windows.FirstOrDefault()?.Page is { } root)
            {
                // A first look at the app before being asked for anything.
                if (root is PlanWizardPage { BindingContext: PlanWizardViewModel { IsOnboarding: true, IsWelcome: true } })
                    return;
                var modals = root.Navigation.ModalStack;
                if (modals.Any(p => p is AccountPage { BindingContext: AccountViewModel { IsRequired: true } }))
                    return;
                if (modals.Count == 0)
                {
                    await AccountPage.ShowAsync(services, required: true);
                    return;
                }
                await Task.Delay(500);
            }
        }
        finally
        {
            _checking = false;
        }
    }
}
