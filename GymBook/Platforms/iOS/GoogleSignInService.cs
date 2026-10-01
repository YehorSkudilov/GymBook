using System.Security.Cryptography;
using System.Text.Json;
using GymBook.Services.Sync;

namespace GymBook;

/// <summary>
/// Google sign-in in the system's sign-in sheet (ASWebAuthenticationSession, through <see cref="WebAuthenticator"/>), the
/// OAuth flow for installed apps: the iOS OAuth client with PKCE and no secret. Its ID token is addressed to the iOS
/// client, which the API accepts alongside the Web one (Google__IosClientId).
/// </summary>
public class GoogleSignInService : IGoogleSignIn
{
    const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    const string TokenUrl = "https://oauth2.googleapis.com/token";

    static readonly HttpClient Http = new();

    public bool IsAvailable => !string.IsNullOrWhiteSpace(GoogleConfig.IosClientId);

    // Google's redirect for iOS clients: the client ID reversed, as a URL scheme the sign-in sheet hands back to.
    static string RedirectUri
    {
        get
        {
            var id = GoogleConfig.IosClientId;
            const string suffix = ".apps.googleusercontent.com";
            var prefix = id.EndsWith(suffix, StringComparison.Ordinal) ? id[..^suffix.Length] : id;
            return $"com.googleusercontent.apps.{prefix}:/oauth2redirect";
        }
    }

    public async Task<string?> SignInAsync()
    {
        if (!IsAvailable)
            throw new GoogleSignInException("Google sign-in isn't available on this device.");

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));

        var url = AuthorizeUrl + "?" + Query(new()
        {
            ["client_id"] = GoogleConfig.IosClientId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            // The sheet shares Safari's Google session; always ask which account, like the Android picker.
            ["prompt"] = "select_account",
        });

        WebAuthenticatorResult result;
        try
        {
            result = await WebAuthenticator.Default.AuthenticateAsync(new Uri(url), new Uri(RedirectUri));
        }
        catch (TaskCanceledException)
        {
            return null;
        }

        if (result.Properties.TryGetValue("error", out var error))
            throw error == "access_denied" ? new GoogleSignInException("Google sign-in was cancelled.") : new GoogleSignInException($"Google sign-in failed ({error}). Please try again.");
        if (!result.Properties.TryGetValue("state", out var returned) || returned != state || !result.Properties.TryGetValue("code", out var code))
            throw new GoogleSignInException("Google didn't send back a sign-in code. Please try again.");

        try
        {
            using var response = await Http.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = GoogleConfig.IosClientId,
                ["code"] = code,
                ["code_verifier"] = verifier,
                ["redirect_uri"] = RedirectUri,
                ["grant_type"] = "authorization_code",
            }));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (response.IsSuccessStatusCode && json.RootElement.TryGetProperty("id_token", out var token) && token.GetString() is { Length: > 0 } idToken)
                return idToken;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException)
        {
            throw new GoogleSignInException("Couldn't reach Google. Check your connection and try again.");
        }
        throw new GoogleSignInException("Google didn't send back a sign-in token. Please try again.");
    }

    // Nothing kept on the device; the next sign-in asks for the account again anyway (prompt=select_account).
    public Task SignOutAsync() => Task.CompletedTask;

    static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    static string Query(Dictionary<string, string> values) =>
        string.Join("&", values.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
}
