using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>Sign in or create an account. Shown modally from Profile and from the first-run welcome screen.</summary>
public partial class AccountViewModel(AccountService account, DataStore store) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(SubmitText), nameof(ToggleText), nameof(PasswordHint))]
    bool isRegister;

    [ObservableProperty] string email = "";
    [ObservableProperty] string password = "";
    [ObservableProperty] string error = "";
    [ObservableProperty] bool isBusy;

    public string Title => IsRegister ? "Create account" : "Sign in";
    public string SubmitText => IsRegister ? "Create account" : "Sign in";
    public string ToggleText => IsRegister ? "Already have an account? Sign in" : "New here? Create an account";
    public string PasswordHint => IsRegister ? $"At least {AuthLimits.MinPasswordLength} characters" : "";

    [RelayCommand]
    void Toggle()
    {
        IsRegister = !IsRegister;
        Error = "";
    }

    [RelayCommand]
    async Task Submit()
    {
        Error = "";
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Error = "Enter your email and password.";
            return;
        }
        if (IsRegister && Password.Length < AuthLimits.MinPasswordLength)
        {
            Error = $"Use at least {AuthLimits.MinPasswordLength} characters for your password.";
            return;
        }

        IsBusy = true;
        try
        {
            if (IsRegister)
                await account.RegisterAsync(Email, Password);
            else
                await account.SignInAsync(Email, Password);
            Password = "";
            await Close();
            // Signing in on a fresh device can bring back a finished profile: skip the first-run wizard.
            if (store.Profile.OnboardingDone && Shell.Current == null)
                App.ShowMainShell();
        }
        catch (ApiException e)
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

    [RelayCommand]
    static Task Close() => Application.Current!.Windows[0].Page!.Navigation.PopModalAsync();
}
