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
}

public class GoogleSignInException(string message) : Exception(message);

public static class GoogleConfig
{
    // Baked in at build time from GoogleWebClientId / GOOGLE_WEB_CLIENT_ID / GymBook/.env - see GymBook.csproj.
    public static string WebClientId { get; } =
        typeof(GoogleConfig).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "GoogleWebClientId")?.Value ?? "";
}

/// <summary>Platforms without Google sign-in (yet).</summary>
public class NoGoogleSignIn : IGoogleSignIn
{
    public bool IsAvailable => false;
    public Task<string?> SignInAsync() => throw new GoogleSignInException("Google sign-in isn't available on this device.");
    public Task SignOutAsync() => Task.CompletedTask;
}
