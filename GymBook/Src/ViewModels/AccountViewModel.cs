using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

public enum AccountMode { SignIn, Register, ResetPassword }

/// <summary>
/// Sign in, create an account, or reset a forgotten password with a code sent by email. Shown modally from Profile
/// and from the first-run welcome screen.
/// </summary>
public partial class AccountViewModel(AccountService account, IServiceProvider services) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSignIn), nameof(IsRegister), nameof(IsReset), nameof(Title), nameof(Subtitle), nameof(SubmitText),
        nameof(ToggleText), nameof(PasswordLabel), nameof(ShowPassword), nameof(ShowConfirm), nameof(ShowCode),
        nameof(ShowGoogle), nameof(PasswordReturnType))]
    AccountMode mode;

    /// <summary>In <see cref="AccountMode.ResetPassword"/>: the code went out, so it and the new password are asked for.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle), nameof(SubmitText), nameof(ShowPassword), nameof(ShowConfirm), nameof(ShowCode), nameof(PasswordReturnType))]
    bool codeSent;

    [ObservableProperty] string email = "";
    [ObservableProperty] string password = "";
    [ObservableProperty] string confirmPassword = "";
    [ObservableProperty] string code = "";
    [ObservableProperty] string error = "";
    [ObservableProperty] bool isBusy;

    public bool IsSignIn => Mode == AccountMode.SignIn;
    public bool IsRegister => Mode == AccountMode.Register;
    public bool IsReset => Mode == AccountMode.ResetPassword;

    public string Title => Mode switch
    {
        AccountMode.Register => "Create account",
        AccountMode.ResetPassword => "Reset password",
        _ => "Sign in",
    };

    public string Subtitle => Mode switch
    {
        AccountMode.ResetPassword when CodeSent => $"If {Email.Trim()} has an account, we sent a code to it. Enter it with your new password.",
        AccountMode.ResetPassword => "Enter your account's email and we'll send you a code to set a new password.",
        _ => "Back up your workouts and pick up where you left off on any device.",
    };

    public string SubmitText => Mode switch
    {
        AccountMode.Register => "Create account",
        AccountMode.ResetPassword => CodeSent ? "Set new password" : "Send code",
        _ => "Sign in",
    };

    public string ToggleText => Mode switch
    {
        AccountMode.Register => "Already have an account? Sign in",
        AccountMode.ResetPassword => "Back to sign in",
        _ => "New here? Create an account",
    };

    public string PasswordLabel => IsReset ? "NEW PASSWORD" : "PASSWORD";
    public bool ShowPassword => !IsReset || CodeSent;
    public bool ShowConfirm => IsRegister || (IsReset && CodeSent);
    public bool ShowCode => IsReset && CodeSent;
    public ReturnType PasswordReturnType => ShowConfirm ? ReturnType.Next : ReturnType.Go;
    public bool ShowGoogle => account.CanUseGoogle && !IsReset;
    public string PasswordHint => $"At least {AuthLimits.MinPasswordLength} characters";

    [RelayCommand]
    void Toggle() => SwitchTo(Mode == AccountMode.SignIn ? AccountMode.Register : AccountMode.SignIn);

    [RelayCommand]
    void ForgotPassword() => SwitchTo(AccountMode.ResetPassword);

    void SwitchTo(AccountMode next)
    {
        Mode = next;
        CodeSent = false;
        Password = ConfirmPassword = Code = Error = "";
    }

    [RelayCommand]
    async Task Submit()
    {
        Error = "";
        if (Validate() is { } problem)
        {
            Error = problem;
            return;
        }

        await RunAsync(async () =>
        {
            switch (Mode)
            {
                case AccountMode.Register:
                    await account.RegisterAsync(Email, Password);
                    break;
                case AccountMode.ResetPassword when !CodeSent:
                    await account.SendPasswordResetCodeAsync(Email);
                    CodeSent = true;
                    return;
                case AccountMode.ResetPassword:
                    await account.ResetPasswordAsync(Email, Code, Password);
                    break;
                default:
                    await account.SignInAsync(Email, Password);
                    break;
            }
            await SignedInAsync();
        });
    }

    string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return "Enter your email.";
        if (ShowCode && string.IsNullOrWhiteSpace(Code))
            return "Enter the code from the email.";
        if (ShowPassword && string.IsNullOrEmpty(Password))
            return IsReset ? "Enter a new password." : "Enter your email and password.";
        if (ShowConfirm && Password.Length < AuthLimits.MinPasswordLength)
            return $"Use at least {AuthLimits.MinPasswordLength} characters for your password.";
        if (ShowConfirm && Password != ConfirmPassword)
            return "The passwords don't match.";
        return null;
    }

    [RelayCommand]
    Task ResendCode()
    {
        Error = "";
        return RunAsync(() => account.SendPasswordResetCodeAsync(Email));
    }

    [RelayCommand]
    Task Google()
    {
        Error = "";
        return RunAsync(async () =>
        {
            if (await account.SignInWithGoogleAsync())
                await SignedInAsync();
        });
    }

    async Task SignedInAsync()
    {
        Password = ConfirmPassword = Code = "";
        await Close();
        // Signing in on a fresh device can bring back a finished profile: skip the first-run wizard.
        App.ShowMainShellIfOnboarded();
        // A new account (or one never verified) verifies its email before going on.
        await Views.VerifyEmailPage.ShowIfNeededAsync(services);
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
        catch (Exception e) when (e is ApiException or GoogleSignInException)
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
    // The page is a sheet: it slides down before it closes.
    static Task Close() => Application.Current!.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() is Views.SheetPage sheet
        ? sheet.CloseAsync()
        : Application.Current.Windows[0].Page!.Navigation.PopModalAsync();
}
