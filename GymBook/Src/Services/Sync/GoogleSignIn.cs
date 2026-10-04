using System.Reflection;

namespace GymBook.Services.Sync;

/// <summary>The platform's Google account picker. Hands back an ID token for the API to check.</summary>
public interface IGoogleSignIn
{
    /// <summary>Whether this platform and build can sign in with Google; the button is hidden otherwise.</summary>
    bool IsAvailable { get; }

    /// <summary>An ID token for the picked account, or null when the user backed out. Throws <see cref="GoogleSignInException"/> on failure.</summary>
    Task<string?> SignInAsync();

    /// <summary>Forgets the picked account, so the next sign-in shows the picker again instead of reusing it.</summary>
    Task SignOutAsync();

    /// <summary>
    /// Signing in happens in the browser, outside the app, which can't tell when it's abandoned: the app offers to cancel
    /// while it waits. Elsewhere the platform's picker has its own way back.
    /// </summary>
    bool WaitsInBrowser => false;

    /// <summary>Gives up the sign-in waiting on the browser; its <see cref="SignInAsync"/> returns null.</summary>
    void CancelPending() { }
}

public class GoogleSignInException(string message) : Exception(message);

public static class GoogleConfig
{
    // Baked in at build time from GoogleWebClientId / GOOGLE_WEB_CLIENT_ID / GymBook/.env - see GymBook.csproj.
    public static string WebClientId { get; } =
        Metadata("GoogleWebClientId");

    // The iOS OAuth client, from GoogleIosClientId / GOOGLE_IOS_CLIENT_ID / GymBook/.env. iOS signs in with it directly.
    public static string IosClientId { get; } = Metadata("GoogleIosClientId");

    // The Desktop OAuth client and its secret (not confidential for Desktop clients), from GoogleDesktopClientId /
    // GoogleDesktopClientSecret, GOOGLE_DESKTOP_CLIENT_ID / GOOGLE_DESKTOP_CLIENT_SECRET or GymBook/.env. Windows signs in with it.
    public static string DesktopClientId { get; } = Metadata("GoogleDesktopClientId");
    public static string DesktopClientSecret { get; } = Metadata("GoogleDesktopClientSecret");

    static string Metadata(string key) =>
        typeof(GoogleConfig).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value ?? "";
}

/// <summary>Platforms without Google sign-in (yet).</summary>
public class NoGoogleSignIn : IGoogleSignIn
{
    public bool IsAvailable => false;
    public Task<string?> SignInAsync() => throw new GoogleSignInException("Google sign-in isn't available on this device.");
    public Task SignOutAsync() => Task.CompletedTask;
}
