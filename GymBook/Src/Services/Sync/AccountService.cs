using GymBook.Contracts;

namespace GymBook.Services.Sync;

/// <summary>Sign-in, sign-out and account deletion, and what each means for the data on this device.</summary>
public class AccountService(ApiClient api, AuthSession session, DataStore store, SyncService sync)
{
    public bool IsSignedIn => session.IsSignedIn;
    public string? Email => session.Email;

    public async Task SignInAsync(string email, string password) => await CompleteSignInAsync(await api.LoginAsync(email.Trim(), password));

    public async Task RegisterAsync(string email, string password) => await CompleteSignInAsync(await api.RegisterAsync(email.Trim(), password));

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
    }

    /// <summary>Deletes the account and all of its data on the server, then clears this device.</summary>
    public async Task DeleteAccountAsync(string password)
    {
        await api.DeleteAccountAsync(password);
        session.Clear();
        store.WipeDevice();
    }
}
