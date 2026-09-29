using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services;
using GymBook.Services.Sync;
using GymBook.Views;

namespace GymBook.ViewModels;

/// <summary>
/// Asks for the code emailed to a new account's address. It can't be dismissed: the only ways off it are verifying,
/// changing to an address that gets verified, or signing out.
/// </summary>
public partial class VerifyEmailViewModel(AccountService account, DialogService dialogs, IServiceProvider services) : BaseViewModel
{
    bool _codeRequested;

    [ObservableProperty] string email = "";
    [ObservableProperty] string code = "";
    [ObservableProperty] string error = "";
    [ObservableProperty] string info = "";
    [ObservableProperty] bool isBusy;

    /// <summary>Raised once verification is no longer needed (verified, however that happened, or signed out); the page closes.</summary>
    public event EventHandler? Done;

    public override async Task OnAppearingAsync()
    {
        account.Changed += OnAccountChanged;
        Email = account.Email ?? "";
        if (!account.NeedsEmailVerification)
        {
            Done?.Invoke(this, EventArgs.Empty);
            return;
        }
        // A code for this address (registration sends one): send it now unless one already went out a moment ago.
        if (!_codeRequested)
        {
            _codeRequested = true;
            await SendCodeAsync(quiet: true);
        }
    }

    public override void OnDisappearing() => account.Changed -= OnAccountChanged;

    // E.g. the email was changed from here, which verifies the new address, or another device verified it.
    void OnAccountChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
    {
        Email = account.Email ?? "";
        if (!account.NeedsEmailVerification)
            Done?.Invoke(this, EventArgs.Empty);
    });

    [RelayCommand]
    async Task Verify()
    {
        Error = Info = "";
        if (string.IsNullOrWhiteSpace(Code))
        {
            Error = "Enter the code from the email.";
            return;
        }
        await RunAsync(async () =>
        {
            // Also syncs, which brings down the account's profile; sync was refused until now.
            await account.VerifyEmailAsync(Code);
            Done?.Invoke(this, EventArgs.Empty);
            // Signed in from the welcome screen to an account that's already set up: go to the app, not the wizard.
            // The sheet may still be sliding away, so give it a moment first.
            for (var i = 0; i < 20 && Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation.ModalStack.Any(p => p is VerifyEmailPage) == true; i++)
                await Task.Delay(150);
            App.ShowMainShellIfOnboarded();
        });
    }

    [RelayCommand]
    Task ResendCode() => SendCodeAsync(quiet: false);

    /// <summary><paramref name="quiet"/>: sent automatically, so "a code was just sent" isn't worth reporting.</summary>
    async Task SendCodeAsync(bool quiet)
    {
        Error = Info = "";
        await RunAsync(async () =>
        {
            try
            {
                await account.SendVerificationCodeAsync();
                Info = $"We sent a code to {Email}.";
            }
            catch (ApiException e) when (quiet && e.Status == System.Net.HttpStatusCode.TooManyRequests)
            {
                Info = $"We sent a code to {Email}.";
            }
        });
    }

    /// <summary>A typo in the address: switch to the right one, which the code sent there verifies.</summary>
    [RelayCommand]
    Task ChangeEmail() => ManageAccountPage.ShowAsync(services, AccountChange.Email);

    [RelayCommand]
    async Task SignOut()
    {
        if (!await dialogs.Confirm("Sign out?",
                "Nothing has synced to this account yet, so workouts and plans on this device are removed when you sign out.", "Sign out"))
            return;
        if (Application.Current!.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() is SheetPage sheet)
            await sheet.CloseAsync();
        await account.SignOutAsync();
        App.ShowOnboarding();
    }

    async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception e) when (e is ApiException or SessionExpiredException)
        {
            Error = e.Message;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Error = "Couldn't reach the server. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
