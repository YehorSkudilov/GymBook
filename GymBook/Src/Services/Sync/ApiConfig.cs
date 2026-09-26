using System.Net.Security;
using System.Reflection;

namespace GymBook.Services.Sync;

/// <summary>Where the app finds the GymBook API.</summary>
public static class ApiConfig
{
#if DEBUG
    // The local API from GymBook.Api (https profile). The Android emulator reaches the host machine at 10.0.2.2.
    public static Uri BaseAddress { get; } = new(DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:7292/" : "https://localhost:7292/");
#else
    // Baked in at build time from the ApiBaseUrl MSBuild property (see GymBook.csproj).
    public static Uri BaseAddress { get; } = new(
        typeof(ApiConfig).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(a => a.Key == "ApiBaseUrl").Value!);
#endif

    public static HttpClient CreateClient()
    {
        if (BaseAddress.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The API must be reached over HTTPS.");

        var handler = new HttpClientHandler();
#if DEBUG
        // Debug builds only: accept the ASP.NET Core development certificate of the local API, which the
        // emulator doesn't trust and whose name doesn't match 10.0.2.2. Release builds keep full validation.
        handler.ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
            errors == SslPolicyErrors.None || request.RequestUri?.Host is "localhost" or "10.0.2.2";
#endif
        return new HttpClient(handler) { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
    }
}
