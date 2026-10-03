using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GymBook.Api.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

/// <summary>
/// The stores telling us a subscription changed (renewed, cancelled, refunded, revoked, switched plan), so it's
/// checked again within seconds rather than at its expiry. No account: the stores call these, with the token from
/// our settings in the URL. What a notification says is never trusted; it only names a purchase, which is then
/// checked with the store's API (see <see cref="Subscriptions"/>). The Microsoft Store has no such notifications:
/// <see cref="SubscriptionRefresher"/> covers it.
/// </summary>
[ApiController]
[Route("api/store-notifications")]
[AllowAnonymous]
[EnableRateLimiting(RateLimits.Foods)]
public class StoreNotificationsController(Subscriptions subscriptions, GooglePlayOptions google, AppStoreOptions apple,
    ILogger<StoreNotificationsController> log) : ControllerBase
{
    /// <summary>
    /// Google Play real-time developer notifications, pushed by Pub/Sub. Answering with an error makes Pub/Sub try
    /// again later, so that's only for a store that couldn't be reached.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> Google([FromQuery] string? token, [FromBody] JsonElement body, CancellationToken ct)
    {
        if (!Matches(token, google.NotificationToken))
            return Unauthorized();
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("message", out var message) || !message.TryGetProperty("data", out var data) || data.GetString() is not { } encoded)
            return NoContent();

        JsonDocument notification;
        try
        {
            notification = JsonDocument.Parse(Convert.FromBase64String(encoded));
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return NoContent();
        }
        using var _ = notification;
        var root = notification.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return NoContent();
        if (root.TryGetProperty("packageName", out var package) && package.GetString() != google.PackageName)
            return NoContent();
        // A subscription's renewal, cancellation, hold, recovery, revocation…; or a refund ("voided purchase").
        var purchaseToken =
            root.TryGetProperty("subscriptionNotification", out var sub) && sub.TryGetProperty("purchaseToken", out var t1) ? t1.GetString()
            : root.TryGetProperty("voidedPurchaseNotification", out var voided) && voided.TryGetProperty("purchaseToken", out var t2) ? t2.GetString()
            : null;
        if (purchaseToken == null)
            // The console's test notification, or one about something else.
            return NoContent();

        try
        {
            await subscriptions.OnGoogleNotificationAsync(purchaseToken, ct);
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "Couldn't check a notified Google Play purchase");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        return NoContent();
    }

    /// <summary>
    /// App Store Server Notifications (version 2): a signed payload whose transaction names the subscription. Apple
    /// retries a notification that isn't answered with success.
    /// </summary>
    [HttpPost("apple")]
    public async Task<IActionResult> Apple([FromQuery] string? token, [FromBody] JsonElement body, CancellationToken ct)
    {
        if (!Matches(token, apple.NotificationToken))
            return Unauthorized();
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("signedPayload", out var signed) || signed.GetString() is not { } payload)
            return NoContent();

        var notification = Payload(payload);
        if (notification.ValueKind != JsonValueKind.Object || !notification.TryGetProperty("data", out var data))
            // A summary or test notification: nothing about one subscription.
            return NoContent();
        if (data.TryGetProperty("bundleId", out var bundle) && bundle.GetString() != apple.BundleId)
            return NoContent();
        if (!data.TryGetProperty("signedTransactionInfo", out var tx) || tx.GetString() is not { } transaction
            || Payload(transaction) is not { ValueKind: JsonValueKind.Object } info
            || !info.TryGetProperty("originalTransactionId", out var original) || original.GetString() is not { } originalId)
            return NoContent();

        try
        {
            await subscriptions.OnAppleNotificationAsync(originalId, ct);
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "Couldn't check a notified App Store subscription");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (BillingException)
        {
            // Apple doesn't know it (anymore): nothing to do, and retrying won't change that.
        }
        return NoContent();
    }

    /// <summary>The token in the URL is ours. An unset token turns the endpoint off.</summary>
    static bool Matches(string? given, string expected) =>
        expected.Length > 0 && given != null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));

    /// <summary>The payload of a JWS (read, not trusted: see the class remarks).</summary>
    static JsonElement Payload(string jws)
    {
        var parts = jws.Split('.');
        if (parts.Length < 2)
            return default;
        var part = parts[1].Replace('-', '+').Replace('_', '/');
        part = part.PadRight(part.Length + (4 - part.Length % 4) % 4, '=');
        try
        {
            return JsonDocument.Parse(Convert.FromBase64String(part)).RootElement.Clone();
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return default;
        }
    }
}
