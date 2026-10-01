using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.Wear;

/// <summary>
/// The watch's own sign-in, so it can sync workouts started on it: "Sign in with phone" (the phone app asks the API for
/// a session for the watch) or email and password. The same rules for data on the device as the phone's AccountService.
/// </summary>
public class WatchAccount(ApiClient api, AuthSession session, DataStore store, SyncService sync, PhoneLink phone)
{
    public bool IsSignedIn => session.IsSignedIn;
    public string? Email => session.Email;

    public Task EnsureLoadedAsync() => session.EnsureLoadedAsync();

    /// <summary>
    /// Signs in through the phone app. When the phone can't (not reachable, not signed in), throws an
    /// <see cref="InvalidOperationException"/> whose message says why, ready to show.
    /// </summary>
    public async Task SignInWithPhoneAsync()
    {
        var reply = await phone.RequestSessionAsync();
        if (reply.Auth is not { } auth)
            throw new InvalidOperationException(reply.Error ?? "Couldn't sign in through your phone.");
        await CompleteSignInAsync(auth);
    }

    public async Task SignInAsync(string email, string password) => await CompleteSignInAsync(await api.LoginAsync(email.Trim(), password));

    async Task CompleteSignInAsync(AuthResponse auth)
    {
        var owner = store.Local.AccountId;
        // Data left behind by a different account must never be uploaded into this one.
        if (owner != null && owner != auth.UserId)
            store.WipeDevice();
        if (owner != auth.UserId)
            store.Local.AttachToAccount(auth.UserId);
        await session.SetAsync(auth);
        await sync.SyncNowAsync();
    }

    /// <summary>Ends the watch's session and removes the account's data from the watch. Unsynced workouts would be lost, so the caller warns first.</summary>
    public async Task SignOutAsync()
    {
        await api.LogoutAsync();
        session.Clear();
        store.WipeDevice();
    }
}
