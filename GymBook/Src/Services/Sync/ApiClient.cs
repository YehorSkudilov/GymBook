using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using GymBook.Contracts;
using GymBook.Serialization;

namespace GymBook.Services.Sync;

/// <summary>The API refused the request; <see cref="Exception.Message"/> is safe to show.</summary>
public class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>The session can't be renewed; the user has to sign in again.</summary>
public class SessionExpiredException() : Exception("Your session has expired. Please sign in again.");

/// <summary>Typed calls to the GymBook API. Renews the access token transparently.</summary>
public class ApiClient(HttpClient http, AuthSession session)
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30), PlanGenerationTimeout = TimeSpan.FromSeconds(120);

    readonly SemaphoreSlim _refreshGate = new(1, 1);

    /// <summary>Raised when the refresh token is rejected and the session was cleared.</summary>
    public event EventHandler? SessionExpired;

    public Task<AuthResponse> RegisterAsync(string email, string password, CancellationToken ct = default) =>
        SendAsync<RegisterRequest, AuthResponse>("api/auth/register", new() { Email = email, Password = password }, null, ct);

    public Task<AuthResponse> LoginAsync(string email, string password, CancellationToken ct = default) =>
        SendAsync<LoginRequest, AuthResponse>("api/auth/login", new() { Email = email, Password = password }, null, ct);

    /// <summary>Revokes the session on the server. Best effort: signing out locally must work offline too.</summary>
    public async Task LogoutAsync()
    {
        if (session.RefreshToken is not { } token)
            return;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var _ = await PostAsync("api/auth/logout", new RefreshRequest { RefreshToken = token }, null, cts.Token);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
        }
    }

    public Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<SyncRequest, SyncResponse>("api/sync", request, ct);

    /// <summary>An AI-made plan for the wizard's answers. Signed-in users only; it can take the model a minute.</summary>
    public Task<GeneratePlanResponse> GeneratePlanAsync(GeneratePlanRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<GeneratePlanRequest, GeneratePlanResponse>("api/plans/generate", request, ct, PlanGenerationTimeout);

    public async Task DeleteAccountAsync(string password, CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync("api/account/delete", new DeleteAccountRequest { Password = password }, ct);
        await EnsureSuccessAsync(response);
    }

    async Task<TResponse> SendAuthorizedAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct, TimeSpan? timeout = null)
    {
        using var response = await SendWithTokenAsync(path, body, ct, timeout);
        return await ReadAsync<TResponse>(response);
    }

    async Task<HttpResponseMessage> SendWithTokenAsync<TRequest>(string path, TRequest body, CancellationToken ct, TimeSpan? timeout = null)
    {
        var token = await GetAccessTokenAsync(forceRefresh: false, ct);
        var response = await PostAsync(path, body, token, ct, timeout);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // The access token was rejected (expired early, revoked, account gone): renew once and retry.
        response.Dispose();
        token = await GetAccessTokenAsync(forceRefresh: true, ct);
        response = await PostAsync(path, body, token, ct, timeout);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            Expire();
        }
        return response;
    }

    async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            if (!forceRefresh && session.AccessToken != null && session.AccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
                return session.AccessToken;
            if (session.RefreshToken is not { } refreshToken)
                throw new SessionExpiredException();

            using var response = await PostAsync("api/auth/refresh", new RefreshRequest { RefreshToken = refreshToken }, null, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
                Expire();
            var auth = await ReadAsync<AuthResponse>(response);
            await session.SetAsync(auth);
            return auth.AccessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    void Expire()
    {
        session.Clear();
        SessionExpired?.Invoke(this, EventArgs.Empty);
        throw new SessionExpiredException();
    }

    async Task<TResponse> SendAsync<TRequest, TResponse>(string path, TRequest body, string? token, CancellationToken ct)
    {
        using var response = await PostAsync(path, body, token, ct);
        return await ReadAsync<TResponse>(response);
    }

    // Each call has its own timeout (the HttpClient's is only the upper bound). The response body is buffered before
    // SendAsync returns, so it can still be read after the timeout is disposed.
    async Task<HttpResponseMessage> PostAsync<TRequest>(string path, TRequest body, string? token, CancellationToken ct, TimeSpan? timeout = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, TypeInfo<TRequest>()) };
        if (token != null)
            request.Headers.Authorization = new("Bearer", token);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout ?? DefaultTimeout);
        return await http.SendAsync(request, cts.Token);
    }

    static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync(TypeInfo<T>()) ?? throw new ApiException(response.StatusCode, "The server sent an empty response.");
    }

    static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;
        ApiProblem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync(TypeInfo<ApiProblem>());
        }
        catch (Exception)
        {
            // Not a problem-details body (e.g. a proxy error page); fall back to a generic message.
        }
        var message = problem?.Errors?.Values.SelectMany(e => e).FirstOrDefault()
            ?? problem?.Title
            ?? (response.StatusCode == HttpStatusCode.TooManyRequests ? "Too many attempts. Please wait a minute and try again." : "Something went wrong. Please try again.");
        throw new ApiException(response.StatusCode, message);
    }

    static JsonTypeInfo<T> TypeInfo<T>() => (JsonTypeInfo<T>)GymBookJson.Options.GetTypeInfo(typeof(T));
}
