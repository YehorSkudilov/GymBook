using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using GymBook.Contracts;
using GymBook.Serialization;

namespace GymBook.Admin.Services;

/// <summary>The API refused the request; <see cref="Exception.Message"/> is safe to show.</summary>
public class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>The session can't be renewed, or the account lost its admin role: sign in again.</summary>
public class SessionExpiredException(string message = "Your session has expired. Please sign in again.") : Exception(message);

/// <summary>Typed calls to the API's /api/admin endpoints, renewing the access token as needed.</summary>
public class AdminApi(HttpClient http, AdminSession session)
{
    readonly SemaphoreSlim _refreshGate = new(1, 1);

    /// <summary>Raised when the session was cleared and the app should go back to sign-in.</summary>
    public event EventHandler? SessionExpired;

    /// <summary>Signs in; refuses (and revokes the new session of) an account that isn't an admin.</summary>
    public async Task SignInAsync(string email, string password, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = email, Password = password }, TypeInfo<LoginRequest>(), ct);
        var auth = await ReadAsync<AuthResponse>(response);
        if (AdminSession.RoleOf(auth.AccessToken) == null)
        {
            await RevokeAsync(auth.RefreshToken);
            throw new ApiException(HttpStatusCode.Forbidden, "This account doesn't have admin access.");
        }
        if (!auth.EmailVerified)
        {
            await RevokeAsync(auth.RefreshToken);
            throw new ApiException(HttpStatusCode.Forbidden, "Verify this account's email in the Gym Book app first.");
        }
        await session.SetAsync(auth);
    }

    /// <summary>Revokes the session on the server (best effort) and forgets it here.</summary>
    public async Task SignOutAsync()
    {
        if (session.RefreshToken is { } token)
            await RevokeAsync(token);
        session.Clear();
    }

    /// <summary>Gets a fresh access token, e.g. at startup to find out whether the saved session still works.</summary>
    public Task EnsureSignedInAsync(CancellationToken ct = default) => GetAccessTokenAsync(forceRefresh: true, ct);

    public Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default) => GetAsync<DashboardResponse>("api/admin/dashboard", ct);

    public Task<List<UserRow>> GetUsersAsync(string? query, string? filter, CancellationToken ct = default)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query))
            parameters.Add("q=" + Uri.EscapeDataString(query.Trim()));
        if (!string.IsNullOrEmpty(filter))
            parameters.Add("filter=" + Uri.EscapeDataString(filter));
        return GetAsync<List<UserRow>>("api/admin/users" + (parameters.Count > 0 ? "?" + string.Join("&", parameters) : ""), ct);
    }

    public Task<UserDetail> GetUserAsync(string id, CancellationToken ct = default) => GetAsync<UserDetail>($"api/admin/users/{Uri.EscapeDataString(id)}", ct);

    public Task<AiUsageResponse> GetAiUsageAsync(int days, CancellationToken ct = default) => GetAsync<AiUsageResponse>($"api/admin/ai-usage?days={days}", ct);

    public Task SetDisabledAsync(string id, bool disabled) => PostAsync(UserPath(id, "disabled"), new SetDisabledRequest(disabled));

    public Task SetEmailVerifiedAsync(string id, bool verified) => PostAsync(UserPath(id, "email-verified"), new SetEmailVerifiedRequest(verified));

    public Task SignOutEverywhereAsync(string id) => SendAsync(() => new HttpRequestMessage(HttpMethod.Post, UserPath(id, "sign-out")));

    public Task ResetAiQuotaAsync(string id) => SendAsync(() => new HttpRequestMessage(HttpMethod.Post, UserPath(id, "ai-quota/reset")));

    public Task SendPasswordResetAsync(string id) => SendAsync(() => new HttpRequestMessage(HttpMethod.Post, UserPath(id, "password-reset-email")));

    /// <summary>SuperAdmin only. Null makes the account a regular user.</summary>
    public Task SetRoleAsync(string id, string? role) => PostAsync(UserPath(id, "role"), new SetRoleRequest(role));

    /// <summary>SuperAdmin only. Deletes the account and all its data.</summary>
    public Task DeleteUserAsync(string id) => SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/admin/users/{Uri.EscapeDataString(id)}"));

    public Task<List<RankReportRow>> GetRankReportsAsync(bool all, CancellationToken ct = default) =>
        GetAsync<List<RankReportRow>>($"api/admin/ranks/reports?all={(all ? "true" : "false")}", ct);

    /// <summary>Closes every open report about the reported user.</summary>
    public Task ResolveRankReportAsync(Guid id) => SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"api/admin/ranks/reports/{id}/resolve"));

    public Task SetRankHiddenAsync(string userId, bool hidden) =>
        PostAsync($"api/admin/ranks/users/{Uri.EscapeDataString(userId)}/hidden", new SetRankHiddenRequest(hidden));

    /// <summary>Clears the user's public bio, home gym and picture.</summary>
    public Task ClearPublicProfileAsync(string userId) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"api/admin/ranks/users/{Uri.EscapeDataString(userId)}/clear-profile"));

    static string UserPath(string id, string action) => $"api/admin/users/{Uri.EscapeDataString(id)}/{action}";

    async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Get, path), ct);
        return await ReadAsync<T>(response);
    }

    Task PostAsync<TRequest>(string path, TRequest body) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, TypeInfo<TRequest>()) });

    async Task SendAsync(Func<HttpRequestMessage> create)
    {
        using var response = await SendWithTokenAsync(create, CancellationToken.None);
        await EnsureSuccessAsync(response);
    }

    async Task<HttpResponseMessage> SendWithTokenAsync(Func<HttpRequestMessage> create, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(forceRefresh: false, ct);
        var response = await SendAsync(create(), token, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Rejected (expired early, revoked): renew once and retry.
        response.Dispose();
        token = await GetAccessTokenAsync(forceRefresh: true, ct);
        response = await SendAsync(create(), token, ct);
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

            using var response = await http.PostAsJsonAsync("api/auth/refresh", new RefreshRequest { RefreshToken = refreshToken }, TypeInfo<RefreshRequest>(), ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
                Expire();
            var auth = await ReadAsync<AuthResponse>(response);
            await session.SetAsync(auth);
            // The role is read from the database at every refresh, so a removed role shows up here.
            if (session.Role == null)
            {
                await RevokeAsync(auth.RefreshToken);
                Expire("This account no longer has admin access.");
            }
            return auth.AccessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    void Expire(string? message = null)
    {
        session.Clear();
        SessionExpired?.Invoke(this, EventArgs.Empty);
        throw message == null ? new SessionExpiredException() : new SessionExpiredException(message);
    }

    async Task RevokeAsync(string refreshToken)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var _ = await http.PostAsJsonAsync("api/auth/logout", new RefreshRequest { RefreshToken = refreshToken }, TypeInfo<RefreshRequest>(), cts.Token);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
        }
    }

    async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string token, CancellationToken ct)
    {
        request.Headers.Authorization = new("Bearer", token);
        return await http.SendAsync(request, ct);
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
            ?? response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => "Too many attempts. Please wait a minute and try again.",
                // Authorization refuses without a body: not an admin (any more), or the action is SuperAdmin-only.
                HttpStatusCode.Forbidden => "You don't have permission to do that.",
                _ => "Something went wrong. Please try again.",
            };
        throw new ApiException(response.StatusCode, message);
    }

    static JsonTypeInfo<T> TypeInfo<T>() => (JsonTypeInfo<T>)GymBookJson.Options.GetTypeInfo(typeof(T));
}
