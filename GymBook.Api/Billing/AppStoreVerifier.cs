using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GymBook.Api.Billing;

/// <summary>
/// Checks App Store subscriptions with the App Store Server API (Get All Subscription Statuses). Apple's answer comes
/// over TLS from Apple with our own key's token, so its signed transactions are read without checking their
/// certificate chain. Production first, then the sandbox (TestFlight and test accounts).
/// </summary>
public class AppStoreVerifier(HttpClient http, AppStoreOptions options, TimeProvider time)
{
    public const string Production = "Production", Sandbox = "Sandbox";

    ECDsa? _key;

    public bool IsConfigured => options.IsConfigured;

    /// <param name="transactionId">Any transaction of the subscription: the purchase's, or its original one.</param>
    /// <param name="environment">Where it was found before, to look there first.</param>
    public async Task<StorePurchaseInfo> CheckAsync(string transactionId, string? environment, CancellationToken ct)
    {
        var order = environment == Sandbox ? new[] { Sandbox, Production } : [Production, Sandbox];
        foreach (var env in order)
        {
            var host = env == Production ? "https://api.storekit.itunes.apple.com" : "https://api.storekit-sandbox.itunes.apple.com";
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{host}/inApps/v1/subscriptions/{Uri.EscapeDataString(transactionId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token());
            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
                continue;
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Couldn't check the purchase with the App Store ({(int)response.StatusCode}).");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            return Read(json.RootElement, env);
        }
        throw new BillingException("The App Store doesn't know that purchase.");
    }

    /// <summary>The subscription's latest transaction among those returned (one per subscription group).</summary>
    StorePurchaseInfo Read(JsonElement root, string env)
    {
        StorePurchaseInfo? best = null;
        foreach (var group in root.GetProperty("data").EnumerateArray())
            foreach (var last in group.GetProperty("lastTransactions").EnumerateArray())
            {
                // 1 active, 2 expired, 3 billing retry, 4 grace period, 5 revoked.
                var status = last.GetProperty("status").GetInt32();
                var tx = Payload(last.GetProperty("signedTransactionInfo").GetString()!);
                var renewal = last.TryGetProperty("signedRenewalInfo", out var r) && r.GetString() is { } signed ? Payload(signed) : default;
                var expires = tx.TryGetProperty("expiresDate", out var e) ? DateTimeOffset.FromUnixTimeMilliseconds(e.GetInt64()) : DateTimeOffset.MinValue;
                // Billing retry gives no access; grace period does, up to the grace period's end.
                if (status == 3 && expires > time.GetUtcNow())
                    expires = time.GetUtcNow();
                if (status == 4 && renewal.ValueKind == JsonValueKind.Object && renewal.TryGetProperty("gracePeriodExpiresDate", out var grace))
                    expires = DateTimeOffset.FromUnixTimeMilliseconds(grace.GetInt64());
                var info = new StorePurchaseInfo(
                    PurchaseKey: tx.GetProperty("originalTransactionId").GetString()!,
                    ProductId: tx.GetProperty("productId").GetString() ?? "",
                    ExpiresAt: expires,
                    AutoRenewing: renewal.ValueKind == JsonValueKind.Object && renewal.TryGetProperty("autoRenewStatus", out var auto) && auto.GetInt32() == 1,
                    // offerType 1: an introductory offer, which is how the free trial is set up.
                    InTrial: tx.TryGetProperty("offerType", out var offer) && offer.GetInt32() == 1 && status == 1,
                    Revoked: status == 5 || tx.TryGetProperty("revocationDate", out _),
                    AccountToken: tx.TryGetProperty("appAccountToken", out var account) ? account.GetString() : null,
                    Environment: env);
                if (tx.TryGetProperty("bundleId", out var bundle) && bundle.GetString() != options.BundleId)
                    continue;
                if (best == null || info.ExpiresAt > best.ExpiresAt)
                    best = info;
            }
        return best ?? throw new BillingException("The App Store has no subscription for that purchase.");
    }

    /// <summary>The payload of a JWS from Apple.</summary>
    static JsonElement Payload(string jws)
    {
        var part = jws.Split('.')[1].Replace('-', '+').Replace('_', '/');
        part = part.PadRight(part.Length + (4 - part.Length % 4) % 4, '=');
        return JsonDocument.Parse(Convert.FromBase64String(part)).RootElement.Clone();
    }

    /// <summary>A token for the App Store Server API: ES256 with the in-app purchase key, valid for a few minutes.</summary>
    string Token()
    {
        if (_key == null)
        {
            var key = ECDsa.Create();
            key.ImportFromPem(BillingIds.Decode(options.PrivateKey));
            _key = key;
        }
        var now = time.GetUtcNow().ToUnixTimeSeconds();
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object> { ["alg"] = "ES256", ["kid"] = options.KeyId, ["typ"] = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["iss"] = options.IssuerId,
            ["iat"] = now,
            ["exp"] = now + 600,
            ["aud"] = "appstoreconnect-v1",
            ["bid"] = options.BundleId,
        }));
        // ES256 is the raw r‖s signature, which is .NET's default format.
        var signature = Base64Url(_key.SignData(Encoding.ASCII.GetBytes($"{header}.{payload}"), HashAlgorithmName.SHA256));
        return $"{header}.{payload}.{signature}";
    }

    static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
