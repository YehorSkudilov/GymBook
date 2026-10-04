using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GymBook.Services.Sync;

namespace GymBook;

/// <summary>
/// Google sign-in in the default browser, the OAuth flow for desktop apps (WebAuthenticator doesn't work on Windows):
/// the Desktop OAuth client with PKCE, and Google's answer caught on a loopback address (http://127.0.0.1:port), which
/// Desktop clients accept on any port. Its ID token is addressed to the Desktop client, which the API accepts alongside
/// the Web one (Google__DesktopClientId).
/// </summary>
public class GoogleSignInService : IGoogleSignIn
{
    const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    const string TokenUrl = "https://oauth2.googleapis.com/token";

    /// <summary>How long the browser has to come back before the sign-in is given up as abandoned.</summary>
    static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    static readonly HttpClient Http = new();

    // The sign-in waiting on the browser; a new tap gives it up.
    CancellationTokenSource? _pending;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(GoogleConfig.DesktopClientId);

    public bool WaitsInBrowser => true;

    public void CancelPending() => _pending?.Cancel();

    public async Task<string?> SignInAsync()
    {
        if (!IsAvailable)
            throw new GoogleSignInException("Google sign-in isn't available on this device.");

        _pending?.Cancel();
        using var cts = _pending = new CancellationTokenSource(Timeout);

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));

        // Any free port: Google takes any for a Desktop client's loopback redirect.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var redirectUri = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";

        var url = AuthorizeUrl + "?" + Query(new()
        {
            ["client_id"] = GoogleConfig.DesktopClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            // The browser may be signed in to several accounts; always ask which, like the Android picker.
            ["prompt"] = "select_account",
        });
        await Browser.Default.OpenAsync(new Uri(url), BrowserLaunchMode.External);

        Dictionary<string, string> result;
        try
        {
            result = await ReceiveAsync(listener, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Tapped again, or the browser never came back.
            return null;
        }
        finally
        {
            if (_pending == cts)
                _pending = null;
        }
        BringAppForward();

        if (result.TryGetValue("error", out var error))
            return error == "access_denied" ? null : throw new GoogleSignInException($"Google sign-in failed ({error}). Please try again.");
        if (!result.TryGetValue("state", out var returned) || returned != state || !result.TryGetValue("code", out var code))
            throw new GoogleSignInException("Google didn't send back a sign-in code. Please try again.");

        try
        {
            using var response = await Http.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = GoogleConfig.DesktopClientId,
                // Desktop clients have a secret, which Google doesn't treat as confidential: it ships in the app.
                ["client_secret"] = GoogleConfig.DesktopClientSecret,
                ["code"] = code,
                ["code_verifier"] = verifier,
                ["redirect_uri"] = redirectUri,
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

    /// <summary>
    /// Waits for the browser's redirect, answers it with a page saying to go back to the app, and returns its query.
    /// Anything else that knocks (a favicon request, say) gets a 404 and the wait goes on.
    /// </summary>
    static async Task<Dictionary<string, string>> ReceiveAsync(TcpListener listener, CancellationToken ct)
    {
        while (true)
        {
            using var client = await listener.AcceptTcpClientAsync(ct);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            // "GET /?code=...&state=... HTTP/1.1"; the headers after it don't matter.
            var requestLine = await reader.ReadLineAsync(ct) ?? "";
            var parts = requestLine.Split(' ');
            var target = parts.Length >= 2 ? parts[1] : "";
            var query = target.StartsWith("/?", StringComparison.Ordinal) ? ParseQuery(target[2..]) : null;

            var found = query != null && (query.ContainsKey("code") || query.ContainsKey("error"));
            var body = found
                ? "<!doctype html><meta charset=\"utf-8\"><title>Gym Book</title>"
                  + "<body style=\"font-family:system-ui,sans-serif;text-align:center;padding-top:20vh\">"
                  + "<h2>You can close this tab</h2><p>Go back to Gym Book to carry on.</p></body>"
                : "";
            var bytes = Encoding.UTF8.GetBytes(body);
            var head = $"HTTP/1.1 {(found ? "200 OK" : "404 Not Found")}\r\nContent-Type: text/html; charset=utf-8\r\n"
                + $"Content-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(head), ct);
            await stream.WriteAsync(bytes, ct);
            if (found)
                return query!;
        }
    }

    // The browser has the focus after signing in; the app comes back in front for the rest.
    static void BringAppForward()
    {
        try
        {
            if (Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView is Microsoft.UI.Xaml.Window window)
                window.Activate();
        }
        catch (Exception)
        {
            // Only saves a click.
        }
    }

    static Dictionary<string, string> ParseQuery(string query) =>
        query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .GroupBy(kv => Uri.UnescapeDataString(kv[0]))
            .ToDictionary(g => g.Key, g => g.First() is { Length: 2 } kv ? Uri.UnescapeDataString(kv[1].Replace('+', ' ')) : "");

    static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    static string Query(Dictionary<string, string> values) =>
        string.Join("&", values.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
}
