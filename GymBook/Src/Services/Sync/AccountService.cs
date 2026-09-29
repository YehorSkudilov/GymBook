using GymBook.Contracts;

namespace GymBook.Services.Sync;

/// <summary>Sign-in, sign-out, account changes and deletion, and what each means for the data on this device.</summary>
public class AccountService(ApiClient api, AuthSession session, DataStore store, SyncService sync, IGoogleSignIn google)
{
    public bool IsSignedIn => session.IsSignedIn;
    public string? Email => session.Email;
    public bool HasPassword => session.HasPassword;
    public bool CanUseGoogle => google.IsAvailable;

    /// <summary>Signed in with an email that hasn't been verified yet: the app asks for the code before anything else.</summary>
    public bool NeedsEmailVerification => session.IsSignedIn && !session.EmailVerified;

    public Task EnsureLoadedAsync() => session.EnsureLoadedAsync();

    /// <summary>Signed in or out, or the email or password changed.</summary>
    public event EventHandler? Changed
    {
        add => session.Changed += value;
        remove => session.Changed -= value;
    }

    public async Task SignInAsync(string email, string password) => await CompleteSignInAsync(await api.LoginAsync(email.Trim(), password));

    public async Task RegisterAsync(string email, string password) => await CompleteSignInAsync(await api.RegisterAsync(email.Trim(), password));

    /// <summary>Signs in (or up) with the Google account the user picks. False when they backed out of the picker.</summary>
    public async Task<bool> SignInWithGoogleAsync()
    {
        if (await google.SignInAsync() is not { } idToken)
            return false;
        await CompleteSignInAsync(await api.GoogleSignInAsync(idToken));
        return true;
    }

    public Task SendPasswordResetCodeAsync(string email) => api.ForgotPasswordAsync(email.Trim());

    /// <summary>Sets the new password with the emailed code and signs in with it.</summary>
    public async Task ResetPasswordAsync(string email, string code, string newPassword) =>
        await CompleteSignInAsync(await api.ResetPasswordAsync(email.Trim(), code.Trim(), newPassword));

    async Task CompleteSignInAsync(AuthResponse auth)
    {
        var owner = store.Local.AccountId;
        // Data left behind by a different account must never be uploaded into this one.
        if (owner != null && owner != auth.UserId)
            store.WipeDevice();
        // Data created before signing in (or after a wipe) now belongs to this account and is uploaded.
        if (owner != auth.UserId)
            store.Local.AttachToAccount(auth.UserId);
        await session.SetAsync(auth);
        await sync.SyncNowAsync();
    }

    /// <summary>Changes the password, or adds one to a Google account. Other devices are signed out; this one stays in.</summary>
    public async Task ChangePasswordAsync(string currentPassword, string newPassword) =>
        await session.SetAsync(await api.ChangePasswordAsync(currentPassword, newPassword));

    /// <summary>Emails a code to the new address; <see cref="ConfirmEmailChangeAsync"/> finishes the change.</summary>
    public Task SendEmailChangeCodeAsync(string newEmail, string password) => api.ChangeEmailAsync(newEmail.Trim(), password);

    public async Task ConfirmEmailChangeAsync(string newEmail, string code)
    {
        var account = await api.ConfirmEmailChangeAsync(newEmail.Trim(), code.Trim());
        await session.SetEmailAsync(account.Email);
        // The code also proved the new address, which may be what makes the account verified.
        if (!session.EmailVerified && account.EmailVerified)
            await ActivateVerifiedAsync();
    }

    public Task SendVerificationCodeAsync() => api.SendVerificationCodeAsync();

    /// <summary>Verifies the email with the emailed code, then syncs what was held back.</summary>
    public async Task VerifyEmailAsync(string code)
    {
        await api.VerifyEmailAsync(code.Trim());
        await ActivateVerifiedAsync();
    }

    // The access token says whether the email is verified: renew it so sync is let through, then catch up.
    async Task ActivateVerifiedAsync()
    {
        await api.RefreshSessionAsync();
        await sync.SyncNowAsync();
    }

    /// <summary>Whether signing out now would lose changes that haven't reached the server.</summary>
    public async Task<bool> HasUnsyncedChangesAsync()
    {
        await sync.SyncNowAsync();
        return store.Local.HasPendingChanges;
    }

    /// <summary>Ends the session and removes the account's data from this device. It stays in the account.</summary>
    public async Task SignOutAsync()
    {
        await api.LogoutAsync();
        session.Clear();
        store.WipeDevice();
        await ForgetGoogleAccountAsync();
    }

    /// <summary>Deletes the account and all of its data on the server, then clears this device.</summary>
    public async Task DeleteAccountAsync(string password)
    {
        await api.DeleteAccountAsync(password);
        await FinishDeleteAsync();
    }

    /// <summary>For an account without a password: picking the linked Google account again confirms. False when the user backed out.</summary>
    public async Task<bool> DeleteAccountWithGoogleAsync()
    {
        // Show the picker rather than silently reusing the account picked last time.
        await ForgetGoogleAccountAsync();
        if (await google.SignInAsync() is not { } idToken)
            return false;
        await api.DeleteAccountAsync("", idToken);
        await FinishDeleteAsync();
        return true;
    }

    async Task FinishDeleteAsync()
    {
        session.Clear();
        store.WipeDevice();
        await ForgetGoogleAccountAsync();
    }

    // So the next Google sign-in asks which account instead of reusing this one.
    async Task ForgetGoogleAccountAsync()
    {
        try
        {
            await google.SignOutAsync();
        }
        catch (Exception)
        {
            // Only affects which account the picker suggests next time.
        }
    }
}
