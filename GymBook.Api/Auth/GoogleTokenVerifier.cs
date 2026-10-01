using Google.Apis.Auth;

namespace GymBook.Api.Auth;

public class GoogleOptions
{
    /// <summary>
    /// The Web application OAuth client ID from Google Cloud Console. The app asks Google for ID tokens addressed to it
    /// (even on Android), so it's what the tokens' audience is checked against. Empty turns Google sign-in off.
    /// </summary>
    public string ClientId { get; set; } = "";

    /// <summary>
    /// The iOS OAuth client ID. The iOS app signs in with it directly (no Google SDK to re-address the token to the Web
    /// client), so its tokens' audience is this one. Optional: empty means only the Web client's tokens are accepted.
    /// </summary>
    public string IosClientId { get; set; } = "";
}

/// <summary>Checks ID tokens from Google sign-in on the device: signed by Google, for this app, not expired.</summary>
public class GoogleTokenVerifier(GoogleOptions options)
{
    public const string Provider = "Google";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.ClientId);

    /// <summary>The token's claims, or null when it's invalid or Google sign-in is off.</summary>
    public async Task<GoogleJsonWebSignature.Payload?> VerifyAsync(string idToken)
    {
        if (!IsConfigured)
            return null;
        try
        {
            string[] audience = string.IsNullOrWhiteSpace(options.IosClientId) ? [options.ClientId] : [options.ClientId, options.IosClientId];
            return await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = audience });
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
