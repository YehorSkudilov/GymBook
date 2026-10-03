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

    /// <summary>Signed out here on purpose: no signing in through the phone without asking until signed in again by hand.</summary>
    const string NoAutoSignInKey = "watch.no_auto_sign_in";

    Task<bool>? _autoSignIn;

    /// <summary>
    /// Signs in through the phone without asking when the watch isn't signed in, the phone is in reach and signed in, and
    /// the user didn't sign out here: so the watch gets the phone's account (and with it the plans and history) as soon
    /// as it's connected. True when it signed in; any failure is quiet (the Settings page still offers it by hand).
    /// </summary>
    public Task<bool> TrySignInWithPhoneAsync() => _autoSignIn is { IsCompleted: false } running ? running : _autoSignIn = Attempt();

    async Task<bool> Attempt()
    {
        try
        {
            await session.EnsureLoadedAsync();
            if (session.IsSignedIn || Preferences.Default.Get(NoAutoSignInKey, false))
                return false;
            await SignInWithPhoneAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    async Task CompleteSignInAsync(AuthResponse auth)
    {
        var owner = store.Local.AccountId;
        // Data left behind by a different account must never be uploaded into this one.
        if (owner != null && owner != auth.UserId)
            store.WipeDevice();
        if (owner != auth.UserId)
            store.Local.AttachToAccount(auth.UserId);
        await session.SetAsync(auth);
        Preferences.Default.Remove(NoAutoSignInKey);
        await sync.SyncNowAsync();
    }

    /// <summary>Ends the watch's session and removes the account's data from the watch. Unsynced workouts would be lost, so the caller warns first.</summary>
    public async Task SignOutAsync()
    {
        await api.LogoutAsync();
        session.Clear();
        store.WipeDevice();
        Preferences.Default.Set(NoAutoSignInKey, true);
    }
}
