using GymBook.Contracts;

namespace GymBook.Services.Sync;

/// <summary>
/// The signed-in account. The refresh token lives in the platform's secure storage (Keychain / Keystore /
/// DPAPI); the access token is short-lived and kept in memory only.
/// </summary>
public class AuthSession
{
    const string RefreshTokenKey = "auth.refresh_token";
    const string UserIdKey = "auth.user_id";
    const string EmailKey = "auth.email";
    const string HasPasswordKey = "auth.has_password";

    readonly Lazy<Task> _load;

    public AuthSession() => _load = new(LoadAsync);

    public string? UserId { get; private set; }
    public string? Email { get; private set; }

    /// <summary>False for a Google account without a password: it confirms with Google where others type the password.</summary>
    public bool HasPassword { get; private set; } = true;
    public string? RefreshToken { get; private set; }
    public string? AccessToken { get; private set; }
    public DateTimeOffset AccessTokenExpiresAt { get; private set; }

    public bool IsSignedIn => RefreshToken != null;

    public event EventHandler? Changed;

    public Task EnsureLoadedAsync() => _load.Value;

    async Task LoadAsync()
    {
        try
        {
            RefreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            UserId = await SecureStorage.Default.GetAsync(UserIdKey);
            Email = await SecureStorage.Default.GetAsync(EmailKey);
            // Sessions from before Google sign-in existed have no entry, and they all have a password.
            HasPassword = await SecureStorage.Default.GetAsync(HasPasswordKey) != "false";
        }
        catch (Exception)
        {
            // Secure storage can become unreadable (e.g. restored backup, reset keystore): start signed out.
            SecureStorage.Default.RemoveAll();
            RefreshToken = UserId = Email = null;
        }
    }

    public async Task SetAsync(AuthResponse auth)
    {
        await SecureStorage.Default.SetAsync(RefreshTokenKey, auth.RefreshToken);
        await SecureStorage.Default.SetAsync(UserIdKey, auth.UserId);
        await SecureStorage.Default.SetAsync(EmailKey, auth.Email);
        await SecureStorage.Default.SetAsync(HasPasswordKey, auth.HasPassword ? "true" : "false");
        (RefreshToken, UserId, Email, HasPassword) = (auth.RefreshToken, auth.UserId, auth.Email, auth.HasPassword);
        (AccessToken, AccessTokenExpiresAt) = (auth.AccessToken, auth.AccessTokenExpiresAt);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>After the account moved to another address; the tokens stay valid.</summary>
    public async Task SetEmailAsync(string email)
    {
        await SecureStorage.Default.SetAsync(EmailKey, email);
        Email = email;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(HasPasswordKey);
        RefreshToken = UserId = Email = AccessToken = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
