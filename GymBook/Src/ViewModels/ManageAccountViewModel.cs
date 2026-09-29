using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

public enum AccountChange { Password, Email }

/// <summary>Changes the signed-in account's password (or adds one to a Google account) or email. Opened from Profile.</summary>
public partial class ManageAccountViewModel(AccountService account, DialogService dialogs) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPassword), nameof(IsEmail), nameof(Title), nameof(Subtitle), nameof(SubmitText), nameof(ShowCurrentPassword), nameof(ShowCode))]
    AccountChange change;

    /// <summary>Email: the code went out to the new address and is asked for.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle), nameof(SubmitText), nameof(ShowCurrentPassword), nameof(ShowCode))]
    bool codeSent;

    /// <summary>The current password was forgotten: a reset code went to the account's email and replaces it.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Subtitle), nameof(ShowCurrentPassword), nameof(ShowCode), nameof(CodeReturnType))]
    bool resetting;

    [ObservableProperty] string currentPassword = "";
    [ObservableProperty] string newPassword = "";
    [ObservableProperty] string confirmPassword = "";
    [ObservableProperty] string newEmail = "";
    [ObservableProperty] string code = "";
    [ObservableProperty] string error = "";
    [ObservableProperty] bool isBusy;

    public bool IsPassword => Change == AccountChange.Password;
    public bool IsEmail => Change == AccountChange.Email;
    bool HasPassword => account.HasPassword;

    public string Title => Resetting ? "Reset password" : IsEmail ? "Change email" : HasPassword ? "Change password" : "Set a password";

    public string Subtitle => Change switch
    {
        _ when Resetting => $"We sent a code to {account.Email}. Enter it with your new password. Other devices will be signed out.",
        AccountChange.Email when CodeSent => $"We sent a code to {NewEmail.Trim()}. Enter it to switch your account to that address.",
        AccountChange.Email => $"You sign in with {account.Email}. We'll send a code to the new address to make sure it's yours.",
        _ when HasPassword => "You'll stay signed in here; other devices will be signed out.",
        _ => "Add a password to sign in with your email as well as with Google.",
    };

    public string SubmitText => IsEmail ? CodeSent ? "Change email" : "Send code" : "Save password";

    /// <summary>Proves it's the owner, not someone holding an unlocked phone. Google accounts have none to ask for.</summary>
    public bool ShowCurrentPassword => HasPassword && !Resetting && !(IsEmail && CodeSent);
    public bool ShowCode => Resetting || (IsEmail && CodeSent);
    // Resetting, the new password comes after the code; for an email change the code is the last field.
    public ReturnType CodeReturnType => Resetting ? ReturnType.Next : ReturnType.Go;
    public string PasswordHint => $"At least {AuthLimits.MinPasswordLength} characters";

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
            if (Resetting)
            {
                await account.ResetPasswordAsync(account.Email!, Code, NewPassword);
                await Close();
                await dialogs.Alert("Password saved", "Use your new password next time you sign in.");
            }
            else if (IsPassword)
            {
                await account.ChangePasswordAsync(CurrentPassword, NewPassword);
                await Close();
                await dialogs.Alert("Password saved", "Use your new password next time you sign in.");
            }
            else if (!CodeSent)
            {
                await account.SendEmailChangeCodeAsync(NewEmail, CurrentPassword);
                CodeSent = true;
            }
            else
            {
                await account.ConfirmEmailChangeAsync(NewEmail, Code);
                await Close();
                await dialogs.Alert("Email changed", $"Sign in with {account.Email} from now on.");
            }
        });
    }

    string? Validate()
    {
        if (ShowCurrentPassword && string.IsNullOrEmpty(CurrentPassword))
            return "Enter your current password.";
        if (IsPassword && NewPassword.Length < AuthLimits.MinPasswordLength)
            return $"Use at least {AuthLimits.MinPasswordLength} characters for your password.";
        if (IsPassword && NewPassword != ConfirmPassword)
            return "The passwords don't match.";
        if (IsEmail && !NewEmail.Contains('@'))
            return "Enter the new email address.";
        if (ShowCode && string.IsNullOrWhiteSpace(Code))
            return "Enter the code from the email.";
        return null;
    }

    /// <summary>
    /// The current password is forgotten: emails a reset code to the account's address and asks for it instead.
    /// From the email change too, since that needs the password first.
    /// </summary>
    [RelayCommand]
    Task ForgotPassword()
    {
        Error = "";
        return RunAsync(async () =>
        {
            await account.SendPasswordResetCodeAsync(account.Email!);
            Change = AccountChange.Password;
            CodeSent = false;
            CurrentPassword = Code = "";
            Resetting = true;
        });
    }

    [RelayCommand]
    Task ResendCode()
    {
        Error = "";
        return RunAsync(() => account.SendPasswordResetCodeAsync(account.Email!));
    }

    /// <summary>Back to the address field, e.g. after a typo in it.</summary>
    [RelayCommand]
    void EditEmail()
    {
        CodeSent = false;
        Code = Error = "";
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

    [RelayCommand]
    // The page is a sheet: it slides down before it closes.
    static Task Close() => Application.Current!.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() is Views.SheetPage sheet
        ? sheet.CloseAsync()
        : Application.Current.Windows[0].Page!.Navigation.PopModalAsync();
}
