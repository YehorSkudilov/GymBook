using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using GymBook.Contracts;
using GymBook.Models;
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

    public Task<AuthResponse> GoogleSignInAsync(string idToken, CancellationToken ct = default) =>
        SendAsync<GoogleSignInRequest, AuthResponse>("api/auth/google", new() { IdToken = idToken }, null, ct);

    /// <summary>Asks for a password reset code by email.</summary>
    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        using var response = await PostAsync("api/auth/password/forgot", new ForgotPasswordRequest { Email = email }, null, ct);
        await EnsureSuccessAsync(response);
    }

    public Task<AuthResponse> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default) =>
        SendAsync<ResetPasswordRequest, AuthResponse>("api/auth/password/reset", new() { Email = email, Code = code, NewPassword = newPassword }, null, ct);

    public async Task<AccountResponse> GetAccountAsync(CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Get, "api/account"), ct);
        return await ReadAsync<AccountResponse>(response);
    }

    /// <summary>Changes (or adds) the password. Other devices are signed out; the result is this device's new session.</summary>
    public Task<AuthResponse> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default) =>
        SendAuthorizedAsync<ChangePasswordRequest, AuthResponse>("api/account/password", new() { CurrentPassword = currentPassword, NewPassword = newPassword }, ct);

    /// <summary>Emails a code to <paramref name="newEmail"/> for <see cref="ConfirmEmailChangeAsync"/>.</summary>
    public async Task ChangeEmailAsync(string newEmail, string password, CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync("api/account/email", new ChangeEmailRequest { NewEmail = newEmail, Password = password }, ct);
        await EnsureSuccessAsync(response);
    }

    /// <summary>Emails a code for <see cref="VerifyEmailAsync"/>.</summary>
    public async Task SendVerificationCodeAsync(CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/account/verify-email/send"), ct);
        await EnsureSuccessAsync(response);
    }

    public Task<AccountResponse> VerifyEmailAsync(string code, CancellationToken ct = default) =>
        SendAuthorizedAsync<VerifyEmailRequest, AccountResponse>("api/account/verify-email", new() { Code = code }, ct);

    /// <summary>
    /// Renews the session now, e.g. once the email is verified: the access token says whether it is, so the old one
    /// would still be refused by sync.
    /// </summary>
    public Task RefreshSessionAsync(CancellationToken ct = default) => GetAccessTokenAsync(forceRefresh: true, ct);

    public Task<AccountResponse> ConfirmEmailChangeAsync(string newEmail, string code, CancellationToken ct = default) =>
        SendAuthorizedAsync<ConfirmEmailChangeRequest, AccountResponse>("api/account/email/confirm", new() { NewEmail = newEmail, Code = code }, ct);

    /// <summary>A separate session for another of the user's devices (the Wear OS app's "Sign in with phone").</summary>
    public async Task<AuthResponse> CreateDeviceSessionAsync(CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/account/device-session"), ct);
        return await ReadAsync<AuthResponse>(response);
    }

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

    /// <summary>
    /// Pushes and pulls changes. <paramref name="liveConnection"/>: this app's live sync connection (see LiveSync), so the
    /// server tells only the user's other devices about these changes, not this one.
    /// </summary>
    public async Task<SyncResponse> SyncAsync(SyncRequest request, string? liveConnection = null, CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync(() =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "api/sync") { Content = JsonContent.Create(request, TypeInfo<SyncRequest>()) };
            if (liveConnection != null)
                message.Headers.Add(LiveSyncConnectionHeader, liveConnection);
            return message;
        }, ct);
        return await ReadAsync<SyncResponse>(response);
    }

    /// <summary>The API's SyncHub.ConnectionHeader.</summary>
    const string LiveSyncConnectionHeader = "X-Sync-Connection";

    /// <summary>A current access token (renewed when it's about to expire), for the live sync connection.</summary>
    public Task<string> AccessTokenAsync(CancellationToken ct = default) => GetAccessTokenAsync(forceRefresh: false, ct);

    /// <summary>An AI-made plan for the wizard's answers. Signed-in users only; it can take the model a minute.</summary>
    public Task<GeneratePlanResponse> GeneratePlanAsync(GeneratePlanRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<GeneratePlanRequest, GeneratePlanResponse>("api/plans/generate", request, ct, PlanGenerationTimeout);

    /// <summary>The AI's follow-up questions for the wizard's answers.</summary>
    public Task<PlanQuestionsResponse> GetPlanQuestionsAsync(PlanAnswers answers, CancellationToken ct = default) =>
        SendAuthorizedAsync<PlanAnswers, PlanQuestionsResponse>("api/plans/questions", answers, ct, PlanGenerationTimeout);

    /// <summary>A plan read by the AI from text, an image or a file.</summary>
    public Task<GeneratePlanResponse> ImportPlanAsync(ImportPlanRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<ImportPlanRequest, GeneratePlanResponse>("api/plans/import", request, ct, PlanGenerationTimeout);

    /// <summary>The app's exercises for names from another app's export, matched by the AI.</summary>
    public Task<MatchExercisesResponse> MatchExercisesAsync(MatchExercisesRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<MatchExercisesRequest, MatchExercisesResponse>("api/plans/match-exercises", request, ct, PlanGenerationTimeout);

    /// <summary>A message to the AI about a plan; the reply may carry a changed plan.</summary>
    public Task<PlanChatResponse> ChatAboutPlanAsync(PlanChatRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<PlanChatRequest, PlanChatResponse>("api/plans/chat", request, ct, PlanGenerationTimeout);

    /// <summary>Suggestions to improve a plan from how the user has been doing on it.</summary>
    public Task<PlanReviewResponse> ReviewPlanAsync(PlanReviewRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<PlanReviewRequest, PlanReviewResponse>("api/plans/review", request, ct, PlanGenerationTimeout);

    /// <summary>How many AI plans the user has left.</summary>
    public async Task<PlanQuotaResponse> GetPlanQuotaAsync(CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Get, "api/plans/quota"), ct);
        return await ReadAsync<PlanQuotaResponse>(response);
    }

    /// <summary>Foods from the databases the API searches (USDA branded foods, FatSecret). Works signed out too.</summary>
    public async Task<FoodSearchResponse> SearchFoodsAsync(string query, CancellationToken ct = default)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/foods/search?q=" + Uri.EscapeDataString(query)), null, ct,
            TimeSpan.FromSeconds(15));
        return await ReadAsync<FoodSearchResponse>(response);
    }

    /// <summary>The packaged food with this barcode in the API's databases; null when none is known.</summary>
    public async Task<FoodInfo?> FoodByBarcodeAsync(string code, CancellationToken ct = default)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/foods/barcode/" + Uri.EscapeDataString(code)), null, ct,
            TimeSpan.FromSeconds(15));
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        return await ReadAsync<FoodInfo>(response);
    }

    /// <summary>Confirmed with the password, or for an account without one, a fresh Google ID token.</summary>
    public async Task DeleteAccountAsync(string password, string? googleIdToken = null, CancellationToken ct = default)
    {
        using var response = await SendWithTokenAsync("api/account/delete", new DeleteAccountRequest { Password = password, GoogleIdToken = googleIdToken }, ct);
        await EnsureSuccessAsync(response);
    }

    // ---- Gym Book Pro (the API's SubscriptionsController) ----

    public Task<SubscriptionStatusResponse> GetSubscriptionAsync(CancellationToken ct = default) =>
        GetAuthorizedAsync<SubscriptionStatusResponse>("api/subscriptions", ct);

    /// <summary>A store purchase for the API to check with its store and record for the account.</summary>
    public Task<SubscriptionStatusResponse> VerifyPurchaseAsync(VerifyPurchaseRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<VerifyPurchaseRequest, SubscriptionStatusResponse>("api/subscriptions/verify", request, ct);

    public Task<MicrosoftStoreTicketResponse> GetMicrosoftStoreTicketAsync(CancellationToken ct = default) =>
        GetAuthorizedAsync<MicrosoftStoreTicketResponse>("api/subscriptions/microsoft-ticket", ct);

    // ---- Social: public profile, ranks, friends (the API's SocialController) ----

    public Task<MyPublicProfile> GetMyPublicProfileAsync(CancellationToken ct = default) => GetAuthorizedAsync<MyPublicProfile>("api/social/me", ct);

    public Task<MyPublicProfile> SavePublicProfileAsync(SavePublicProfileRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<SavePublicProfileRequest, MyPublicProfile>("api/social/me", request, ct);

    public Task<MyPublicProfile> SetAvatarAsync(byte[] image, CancellationToken ct = default) =>
        SendAuthorizedAsync<SetAvatarRequest, MyPublicProfile>("api/social/me/avatar", new() { ImageBase64 = Convert.ToBase64String(image) }, ct);

    public Task<MyPublicProfile> RemoveAvatarAsync(CancellationToken ct = default) => PostAuthorizedAsync<MyPublicProfile>("api/social/me/avatar/remove", ct);

    public Task<ProfileCard> GetUserAsync(string username, CancellationToken ct = default) =>
        GetAuthorizedAsync<ProfileCard>("api/social/users/" + Uri.EscapeDataString(username), ct);

    public Task<RankStatusResponse> GetRanksAsync(CancellationToken ct = default) => GetAuthorizedAsync<RankStatusResponse>("api/social/ranks", ct);

    public Task<RankStatusResponse> JoinRanksAsync(Sex sex, CancellationToken ct = default) =>
        SendAuthorizedAsync<JoinRanksRequest, RankStatusResponse>("api/social/ranks/join", new() { Sex = sex }, ct);

    public Task LeaveRanksAsync(CancellationToken ct = default) => SendEmptyAsync("api/social/ranks/leave", ct);

    /// <summary><paramref name="friendsOnly"/>: the user and their friends; <paramref name="lift"/>: a lift's board, null for overall.</summary>
    public Task<LeaderboardResponse> GetLeaderboardAsync(bool friendsOnly, RankLift? lift, CancellationToken ct = default) =>
        GetAuthorizedAsync<LeaderboardResponse>($"api/social/leaderboard?scope={(friendsOnly ? "friends" : "global")}" + (lift is { } l ? $"&lift={l}" : ""), ct);

    public Task<List<ProfileCard>> GetFriendsAsync(CancellationToken ct = default) => GetAuthorizedAsync<List<ProfileCard>>("api/social/friends", ct);

    /// <summary>Adds a friend by their friend code or username.</summary>
    public Task<ProfileCard> AddFriendAsync(string code, CancellationToken ct = default) =>
        SendAuthorizedAsync<AddFriendRequest, ProfileCard>("api/social/friends", new() { Code = code }, ct);

    public Task RemoveFriendAsync(string username, CancellationToken ct = default) =>
        SendEmptyAsync("api/social/friends/remove", new UsernameRequest { Username = username }, ct);

    public Task ReportAsync(ReportRequest request, CancellationToken ct = default) => SendEmptyAsync("api/social/report", request, ct);

    /// <summary>A picture's full address, from a path the API gave (ProfileCard.AvatarPath and the like).</summary>
    public static string? AvatarUrl(string? path) => path == null ? null : new Uri(ApiConfig.BaseAddress, path).ToString();

    // ---- Shared plans (the API's PlanSharesController) ----

    /// <summary>Shares the plan (or returns its share, when it already is). It must have synced first.</summary>
    public Task<PlanShareResponse> SharePlanAsync(string planId, CancellationToken ct = default) =>
        SendAuthorizedAsync<CreatePlanShareRequest, PlanShareResponse>("api/plan-shares", new() { PlanId = planId }, ct);

    public Task<PlanShareResponse> GetPlanShareAsync(string shareId, CancellationToken ct = default) =>
        GetAuthorizedAsync<PlanShareResponse>(SharePath(shareId), ct);

    public Task<PlanShareResponse> UpdatePlanShareAsync(string shareId, UpdatePlanShareRequest request, CancellationToken ct = default) =>
        SendAuthorizedAsync<UpdatePlanShareRequest, PlanShareResponse>(SharePath(shareId, "settings"), request, ct);

    public Task<PlanShareResponse> InviteToPlanAsync(string shareId, string username, PlanShareRole role, CancellationToken ct = default) =>
        SendAuthorizedAsync<ShareInviteRequest, PlanShareResponse>(SharePath(shareId, "invite"), new() { Username = username, Role = role }, ct);

    public Task<PlanShareResponse> SetPlanMemberRoleAsync(string shareId, string username, PlanShareRole role, CancellationToken ct = default) =>
        SendAuthorizedAsync<ShareInviteRequest, PlanShareResponse>(SharePath(shareId, "members/role"), new() { Username = username, Role = role }, ct);

    public Task<PlanShareResponse> RemovePlanMemberAsync(string shareId, string username, CancellationToken ct = default) =>
        SendAuthorizedAsync<UsernameRequest, PlanShareResponse>(SharePath(shareId, "members/remove"), new() { Username = username }, ct);

    public Task StopSharingPlanAsync(string shareId, CancellationToken ct = default) => SendEmptyAsync(SharePath(shareId, "stop"), ct);

    public Task<List<PlanShareInvite>> GetPlanInvitesAsync(CancellationToken ct = default) =>
        GetAuthorizedAsync<List<PlanShareInvite>>("api/plan-shares/incoming", ct);

    public Task<PlanShareResponse> AcceptPlanInviteAsync(string shareId, CancellationToken ct = default) =>
        PostAuthorizedAsync<PlanShareResponse>(SharePath(shareId, "accept"), ct);

    /// <summary>Declines an invite, or leaves a shared plan (its copy stays, as the user's own).</summary>
    public Task LeavePlanShareAsync(string shareId, CancellationToken ct = default) => SendEmptyAsync(SharePath(shareId, "leave"), ct);

    public Task<SharedPlanView> ViewSharedPlanAsync(string shareId, CancellationToken ct = default) =>
        GetAuthorizedAsync<SharedPlanView>(SharePath(shareId, "view"), ct);

    /// <summary>Saves the user's own copy of a shared plan; it arrives with the next sync.</summary>
    public Task<CopyPlanResponse> CopySharedPlanAsync(string shareId, CancellationToken ct = default) =>
        PostAuthorizedAsync<CopyPlanResponse>(SharePath(shareId, "copy"), ct);

    static string SharePath(string shareId, string? action = null) =>
        "api/plan-shares/" + Uri.EscapeDataString(shareId) + (action == null ? "" : "/" + action);

    async Task<T> GetAuthorizedAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Get, path), ct);
        return await ReadAsync<T>(response);
    }

    async Task<T> PostAuthorizedAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Post, path), ct);
        return await ReadAsync<T>(response);
    }

    async Task SendEmptyAsync(string path, CancellationToken ct)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Post, path), ct);
        await EnsureSuccessAsync(response);
    }

    async Task SendEmptyAsync<TRequest>(string path, TRequest body, CancellationToken ct)
    {
        using var response = await SendWithTokenAsync(path, body, ct);
        await EnsureSuccessAsync(response);
    }

    async Task<TResponse> SendAuthorizedAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct, TimeSpan? timeout = null)
    {
        using var response = await SendWithTokenAsync(path, body, ct, timeout);
        return await ReadAsync<TResponse>(response);
    }

    Task<HttpResponseMessage> SendWithTokenAsync<TRequest>(string path, TRequest body, CancellationToken ct, TimeSpan? timeout = null) =>
        SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, TypeInfo<TRequest>()) }, ct, timeout);

    async Task<HttpResponseMessage> SendWithTokenAsync(Func<HttpRequestMessage> create, CancellationToken ct, TimeSpan? timeout = null)
    {
        var token = await GetAccessTokenAsync(forceRefresh: false, ct);
        var response = await SendAsync(create(), token, ct, timeout);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // The access token was rejected (expired early, revoked, account gone): renew once and retry.
        response.Dispose();
        token = await GetAccessTokenAsync(forceRefresh: true, ct);
        response = await SendAsync(create(), token, ct, timeout);
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
    Task<HttpResponseMessage> PostAsync<TRequest>(string path, TRequest body, string? token, CancellationToken ct, TimeSpan? timeout = null) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, TypeInfo<TRequest>()) }, token, ct, timeout);

    async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string? token, CancellationToken ct, TimeSpan? timeout = null)
    {
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
            ?? response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => "Too many attempts. Please wait a minute and try again.",
                // Authorization refuses without a body; for a signed-in user that means the email isn't verified yet.
                HttpStatusCode.Forbidden => "Verify your email to sync and use AI plans.",
                _ => "Something went wrong. Please try again.",
            };
        throw new ApiException(response.StatusCode, message);
    }

    static JsonTypeInfo<T> TypeInfo<T>() => (JsonTypeInfo<T>)GymBookJson.Options.GetTypeInfo(typeof(T));
}
