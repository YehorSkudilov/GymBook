using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace GymBook.Api.Billing;

/// <summary>
/// Checks Google Play subscriptions with the Play Developer API (purchases.subscriptionsv2), and acknowledges new ones:
/// Play refunds a purchase that isn't acknowledged within three days.
/// </summary>
public class GooglePlayVerifier(HttpClient http, GooglePlayOptions options, ILogger<GooglePlayVerifier> log)
{
    const string Scope = "https://www.googleapis.com/auth/androidpublisher";
    const string Api = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/";

    ITokenAccess? _credential;

    public bool IsConfigured => options.IsConfigured;

    public async Task<StorePurchaseInfo> CheckAsync(string purchaseToken, CancellationToken ct)
    {
        var url = $"{Api}{Uri.EscapeDataString(options.PackageName)}/purchases/subscriptionsv2/tokens/{Uri.EscapeDataString(purchaseToken)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(ct));
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Gone)
            throw new BillingException("Google Play doesn't know that purchase.");
        if (!response.IsSuccessStatusCode)
        {
            log.LogError("Google Play answered {Status}: {Body}", (int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
            throw new HttpRequestException("Couldn't check the purchase with Google Play.");
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var root = json.RootElement;
        var state = Text(root, "subscriptionState") ?? "";
        // One line item per product bought; ours have one. The latest expiry is the one that counts.
        var item = root.GetProperty("lineItems").EnumerateArray()
            .OrderByDescending(i => Time(i, "expiryTime") ?? DateTimeOffset.MinValue)
            .First();
        var productId = Text(item, "productId") ?? "";
        var expires = Time(item, "expiryTime") ?? DateTimeOffset.MinValue;
        var renewing = item.TryGetProperty("autoRenewingPlan", out var plan) && plan.TryGetProperty("autoRenewEnabled", out var auto) && auto.GetBoolean();
        // An offer tagged "trial" in Play Console (the free trial offer) marks the trial phase.
        var trial = item.TryGetProperty("offerDetails", out var offer) && offer.TryGetProperty("offerTags", out var tags)
            && tags.EnumerateArray().Any(t => t.GetString()?.Contains("trial", StringComparison.OrdinalIgnoreCase) == true)
            && state == "SUBSCRIPTION_STATE_ACTIVE";
        var account = root.TryGetProperty("externalAccountIdentifiers", out var ids) ? Text(ids, "obfuscatedExternalAccountId") : null;
        // Cancelled still runs to its expiry; on hold, paused, pending or expired doesn't count, whatever the date.
        if (state is not ("SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" or "SUBSCRIPTION_STATE_CANCELED") && expires > DateTimeOffset.UtcNow)
            expires = DateTimeOffset.UtcNow;

        if (Text(root, "acknowledgementState") == "ACKNOWLEDGEMENT_STATE_PENDING" && state is "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD")
            await AcknowledgeAsync(productId, purchaseToken, ct);

        return new StorePurchaseInfo(purchaseToken, productId, expires, renewing, trial, Revoked: false, account,
            LinkedKey: Text(root, "linkedPurchaseToken"));
    }

    async Task AcknowledgeAsync(string productId, string token, CancellationToken ct)
    {
        var url = $"{Api}{Uri.EscapeDataString(options.PackageName)}/purchases/subscriptions/{Uri.EscapeDataString(productId)}/tokens/{Uri.EscapeDataString(token)}:acknowledge";
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(ct));
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            // Checked again at the next refresh, still within Play's three days.
            log.LogWarning("Couldn't acknowledge a Google Play purchase: {Status}", (int)response.StatusCode);
    }

    Task<string> TokenAsync(CancellationToken ct)
    {
        if (_credential == null)
        {
#pragma warning disable CS0618 // FromJson: the key is our own service account's, from our own settings.
            _credential = GoogleCredential.FromJson(BillingIds.Decode(options.ServiceAccountJson)).CreateScoped(Scope);
#pragma warning restore CS0618
        }
        return _credential.GetAccessTokenForRequestAsync(cancellationToken: ct);
    }

    static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static DateTimeOffset? Time(JsonElement e, string name) => Text(e, name) is { } s && DateTimeOffset.TryParse(s, out var t) ? t : null;
}
