using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

public enum AccountMode { SignIn, Register, ResetPassword }

/// <summary>
/// Sign in, create an account, or reset a forgotten password with a code sent by email. Shown modally from Profile,
/// by <see cref="Views.SignInGate"/> as the required sign-in that can't be closed, and as a step of the first-run
/// wizard (<see cref="IsInline"/>).
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
    /// <summary>Picked when creating an account: the name others see (ranks, friends, shared plans).</summary>
    [ObservableProperty] string username = "";
    [ObservableProperty] string password = "";
    [ObservableProperty] string confirmPassword = "";
    [ObservableProperty] string code = "";
    [ObservableProperty] string error = "";
    [ObservableProperty] bool isBusy;

    /// <summary>Opened by <see cref="Views.SignInGate"/>: the app can't be used until it signs in, so there's no closing it.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle), nameof(CanClose), nameof(CanContinueOffline))]
    bool isRequired;

    /// <summary>The server couldn't be reached (or there's no connection at all).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinueOffline))]
    bool isOffline = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

    // Workouts, plans or a finished profile on this device, e.g. from before signing in was required.
    readonly bool _hasLocalData = services.GetRequiredService<DataStore>() is var store
        && (store.Profile.OnboardingDone || store.Data.Plans.Count > 0 || store.Data.Sessions.Count > 0);

    public bool CanClose => !IsRequired && !IsInline;

    /// <summary>
    /// A step of the first-run wizard rather than a sheet: nothing to close, and <see cref="SignedIn"/> moves the wizard on.
    /// </summary>
    public bool IsInline { get; init; }

    /// <summary>Signed in (or the account was made); raised after the sheet, if this is one, has closed.</summary>
    public event EventHandler? SignedIn;

    /// <summary>
    /// Someone with data on this device and no connection isn't locked out of it: they carry on offline and are
    /// asked again once they're back online. A new install has nothing to carry on with, so it waits for a connection.
    /// </summary>
    public bool CanContinueOffline => IsRequired && !IsInline && IsOffline && _hasLocalData;

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
        _ when IsInline => "Your workouts and plans are kept in your account, so you can pick up where you left off on any device. Gym Book works offline too.",
        _ when IsRequired && _hasLocalData =>
            "Sign in or create an account to keep using Gym Book. Your workouts on this device move into your account, and the app still works offline.",
        _ when IsRequired => "Sign in or create an account to get started. After that, Gym Book works offline too.",
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
                    await account.RegisterAsync(Email, Password, Username);
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
        if (IsRegister && UsernameProblem(Username.Trim().TrimStart('@')) is { } usernameProblem)
            return usernameProblem;
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

    // The same rules the server applies (it also turns away taken and reserved names).
    static string? UsernameProblem(string name) =>
        name.Length == 0 ? "Pick a username."
        : name.Length is < SocialLimits.UsernameMinLength or > SocialLimits.UsernameMaxLength
            ? $"A username is {SocialLimits.UsernameMinLength} to {SocialLimits.UsernameMaxLength} characters."
        : !System.Text.RegularExpressions.Regex.IsMatch(name, SocialLimits.UsernamePattern)
            ? "A username can only have letters, numbers, dots and underscores."
        : null;

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
            IsWaitingForGoogle = account.GoogleWaitsInBrowser;
            try
            {
                if (!await account.SignInWithGoogleAsync())
                    return;
            }
            finally
            {
                IsWaitingForGoogle = false;
            }
            await SignedInAsync();
        });
    }

    /// <summary>Google sign-in is going on in the browser (Windows): the form says so and offers to cancel it.</summary>
    [ObservableProperty] bool isWaitingForGoogle;

    /// <summary>Left the browser without finishing: back to the form.</summary>
    [RelayCommand]
    void CancelGoogle() => account.CancelGoogleSignIn();

    async Task SignedInAsync()
    {
        Password = ConfirmPassword = Code = "";
        if (!IsInline)
            await Close();
        // Signing in on a fresh device can bring back a finished profile: skip the first-run wizard.
        App.ShowMainShellIfOnboarded();
        SignedIn?.Invoke(this, EventArgs.Empty);
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
            IsOffline = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>No connection: use the app on this device for now; <see cref="Views.SignInGate"/> asks again later.</summary>
    [RelayCommand]
    Task ContinueOffline()
    {
        Views.SignInGate.SkipUntilOnline();
        return Close();
    }

    [RelayCommand]
    // The page is a sheet: it slides down before it closes.
    static Task Close() => Application.Current!.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() is Views.SheetPage sheet
        ? sheet.CloseAsync()
        : Application.Current.Windows[0].Page!.Navigation.PopModalAsync();
}
