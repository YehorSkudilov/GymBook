using System.Text;
using System.Text.Json;
using GymBook.Contracts;

namespace GymBook.Admin.Services;

/// <summary>
/// The signed-in admin. The refresh token lives in the platform's secure storage (DPAPI / Keystore); the access token,
/// and the role read from it, only in memory.
/// </summary>
public class AdminSession
{
    public const string AdminRole = "Admin";
    public const string SuperAdminRole = "SuperAdmin";

    const string RefreshTokenKey = "admin.refresh_token";
    const string UserIdKey = "admin.user_id";
    const string EmailKey = "admin.email";

    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? AccessToken { get; private set; }
    public DateTimeOffset AccessTokenExpiresAt { get; private set; }

    /// <summary>Admin or SuperAdmin, from the access token; null before the first one or for a regular account.</summary>
    public string? Role { get; private set; }

    public bool IsSuperAdmin => Role == SuperAdminRole;

    public async Task LoadAsync()
    {
        try
        {
            RefreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            UserId = await SecureStorage.Default.GetAsync(UserIdKey);
            Email = await SecureStorage.Default.GetAsync(EmailKey);
        }
        catch (Exception)
        {
            // Unreadable secure storage (reset keystore, copied profile): start signed out.
            Clear();
        }
    }

    public async Task SetAsync(AuthResponse auth)
    {
        await SecureStorage.Default.SetAsync(RefreshTokenKey, auth.RefreshToken);
        await SecureStorage.Default.SetAsync(UserIdKey, auth.UserId);
        await SecureStorage.Default.SetAsync(EmailKey, auth.Email);
        (RefreshToken, UserId, Email) = (auth.RefreshToken, auth.UserId, auth.Email);
        (AccessToken, AccessTokenExpiresAt, Role) = (auth.AccessToken, auth.AccessTokenExpiresAt, RoleOf(auth.AccessToken));
    }

    public void Clear()
    {
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(EmailKey);
        RefreshToken = UserId = Email = AccessToken = Role = null;
    }

    /// <summary>The access token's "role" claim (see the API's TokenService). Read only to shape the UI; the API checks it.</summary>
    public static string? RoleOf(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
            return null;
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return json.RootElement.TryGetProperty("role", out var role) && role.GetString() is AdminRole or SuperAdminRole ? role.GetString() : null;
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return null;
        }
    }
}
