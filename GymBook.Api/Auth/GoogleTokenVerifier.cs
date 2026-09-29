using Google.Apis.Auth;

namespace GymBook.Api.Auth;

public class GoogleOptions
{
    /// <summary>
    /// The Web application OAuth client ID from Google Cloud Console. The app asks Google for ID tokens addressed to it
    /// (even on Android), so it's what the tokens' audience is checked against. Empty turns Google sign-in off.
    /// </summary>
    public string ClientId { get; set; } = "";
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
            return await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = [options.ClientId] });
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
