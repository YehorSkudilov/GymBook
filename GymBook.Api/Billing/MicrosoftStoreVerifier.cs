using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GymBook.Api.Billing;

/// <summary>
/// Checks Microsoft Store subscriptions with the Microsoft Store purchase API (recurrences/query). The Windows app makes
/// a purchase ID key with <see cref="ServiceTicketAsync"/>'s token (StoreContext.GetCustomerPurchaseIdAsync), valid for
/// 90 days, and the subscriptions it finds are those the user bought for this app.
/// </summary>
public class MicrosoftStoreVerifier(HttpClient http, MicrosoftStoreOptions options, TimeProvider time, ILogger<MicrosoftStoreVerifier> log)
{
    const string ApiAudience = "https://onestore.microsoft.com";
    const string PurchaseKeyAudience = "https://onestore.microsoft.com/b2b/keys/create/purchase";

    readonly Dictionary<string, (string Token, DateTimeOffset Expires)> _tokens = [];
    readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsConfigured => options.IsConfigured;

    /// <summary>The token the app makes a purchase ID key with.</summary>
    public Task<string> ServiceTicketAsync(CancellationToken ct) => TokenAsync(PurchaseKeyAudience, ct);

    public async Task<StorePurchaseInfo> CheckAsync(string purchaseIdKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://purchase.mp.microsoft.com/v8.0/b2b/recurrences/query")
        {
            Content = JsonContent.Create(new Dictionary<string, string> { ["b2bKey"] = purchaseIdKey }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(ApiAudience, ct));
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            log.LogError("The Microsoft Store answered {Status}: {Body}", (int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
            throw new BillingException("Couldn't check the purchase with the Microsoft Store. Try again from the app.");
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        StorePurchaseInfo? best = null;
        foreach (var item in json.RootElement.GetProperty("items").EnumerateArray())
        {
            var state = item.TryGetProperty("recurrenceState", out var s) ? s.GetString() : null;
            var expires = item.TryGetProperty("expirationTime", out var e) && DateTimeOffset.TryParse(e.GetString(), out var at) ? at : DateTimeOffset.MinValue;
            // "Active" runs to its expiration; "Canceled" too, until then. "Lapsed", "Failed" and the rest don't count.
            if (state is not ("Active" or "Canceled") && expires > time.GetUtcNow())
                expires = time.GetUtcNow();
            var info = new StorePurchaseInfo(
                PurchaseKey: item.GetProperty("id").GetString()!,
                ProductId: item.TryGetProperty("productId", out var p) ? p.GetString() ?? "" : "",
                ExpiresAt: expires,
                AutoRenewing: item.TryGetProperty("autoRenew", out var auto) && auto.GetBoolean() && state == "Active",
                InTrial: item.TryGetProperty("isTrial", out var trial) && trial.ValueKind == JsonValueKind.True,
                Revoked: state == "Refunded",
                RefreshToken: purchaseIdKey);
            if (best == null || info.ExpiresAt > best.ExpiresAt)
                best = info;
        }
        return best ?? throw new BillingException("The Microsoft Store has no subscription for this account.");
    }

    /// <summary>An Azure AD app token for <paramref name="audience"/>, kept until shortly before it expires.</summary>
    async Task<string> TokenAsync(string audience, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_tokens.TryGetValue(audience, out var cached) && cached.Expires > time.GetUtcNow().AddMinutes(5))
                return cached.Token;
            using var response = await http.PostAsync($"https://login.microsoftonline.com/{Uri.EscapeDataString(options.TenantId)}/oauth2/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = options.ClientId,
                    ["client_secret"] = options.ClientSecret,
                    ["resource"] = audience,
                }), ct);
            if (!response.IsSuccessStatusCode)
            {
                log.LogError("Azure AD refused a Microsoft Store token: {Body}", await response.Content.ReadAsStringAsync(ct));
                throw new HttpRequestException("Couldn't sign in to the Microsoft Store.");
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            var token = json.RootElement.GetProperty("access_token").GetString()!;
            var seconds = json.RootElement.TryGetProperty("expires_in", out var e) && long.TryParse(e.ToString(), out var s) ? s : 3600;
            _tokens[audience] = (token, time.GetUtcNow().AddSeconds(seconds));
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }
}
