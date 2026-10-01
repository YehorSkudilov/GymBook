using System.Net.Security;
using System.Reflection;

namespace GymBook.Admin.Services;

/// <summary>Where the app finds the GymBook API.</summary>
public static class ApiConfig
{
    // Baked in at build time from ApiBaseUrl / API_BASE_URL / .env via [AssemblyMetadata] - see GymBook.Admin.csproj.
    public static Uri BaseAddress { get; } = new(
        typeof(ApiConfig).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "ApiBaseUrl")?.Value
        ?? throw new InvalidOperationException("ApiBaseUrl assembly metadata is missing - this build should have failed at compile time."));

    public static HttpClient CreateClient()
    {
        if (BaseAddress.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The API must be reached over HTTPS.");

        var handler = new HttpClientHandler();
#if DEBUG
        // Debug builds only: accept the local API's ASP.NET Core development certificate (localhost, or 10.0.2.2 from
        // the Android emulator). Release builds keep full validation.
        handler.ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
            errors == SslPolicyErrors.None || request.RequestUri?.Host is "localhost" or "10.0.2.2";
#endif
        return new HttpClient(handler) { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(60) };
    }
}
