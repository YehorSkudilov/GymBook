using System.Security.Cryptography;
using System.Text;

namespace GymBook.Api.Billing;

/// <summary>Google Play (GooglePlay__*): the app's package and a service account with access to its financial data.</summary>
public class GooglePlayOptions
{
    public string PackageName { get; set; } = "";
    /// <summary>The service account's JSON key, as it is or base64-encoded.</summary>
    public string ServiceAccountJson { get; set; } = "";
    /// <summary>
    /// Real-time developer notifications: the token at the end of the Pub/Sub push endpoint
    /// (…/api/store-notifications/google?token=…). Empty: notifications are refused.
    /// </summary>
    public string NotificationToken { get; set; } = "";

    public bool IsConfigured => PackageName.Length > 0 && ServiceAccountJson.Length > 0;
}

/// <summary>App Store (AppStore__*): an App Store Connect in-app purchase key, for the App Store Server API.</summary>
public class AppStoreOptions
{
    public string BundleId { get; set; } = "";
    public string IssuerId { get; set; } = "";
    public string KeyId { get; set; } = "";
    /// <summary>The .p8 key's contents (PEM), as they are, with \n for line breaks, or base64-encoded.</summary>
    public string PrivateKey { get; set; } = "";
    /// <summary>App Store Server Notifications: the token at the end of their URL (…/api/store-notifications/apple?token=…). Empty: refused.</summary>
    public string NotificationToken { get; set; } = "";

    public bool IsConfigured => BundleId.Length > 0 && IssuerId.Length > 0 && KeyId.Length > 0 && PrivateKey.Length > 0;
}

/// <summary>
/// Microsoft Store (MicrosoftStore__*): an Azure AD (Entra) app registered for the app in Partner Center, for the
/// Microsoft Store collection and purchase APIs.
/// </summary>
public class MicrosoftStoreOptions
{
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    public bool IsConfigured => TenantId.Length > 0 && ClientId.Length > 0 && ClientSecret.Length > 0;
}

/// <summary>A purchase as its store reports it.</summary>
/// <param name="PurchaseKey">What the purchase is checked again with (see Data.StoreSubscription.PurchaseKey).</param>
/// <param name="AccountToken">The account the purchase was made for (see <see cref="BillingIds.AccountToken"/>), when the store keeps it.</param>
/// <param name="LinkedKey">Google Play: the purchase this one replaced (a switch between monthly and yearly, or a resubscribe), whose account it continues.</param>
public record StorePurchaseInfo(
    string PurchaseKey,
    string ProductId,
    DateTimeOffset ExpiresAt,
    bool AutoRenewing,
    bool InTrial,
    bool Revoked,
    string? AccountToken = null,
    string? Environment = null,
    string? RefreshToken = null,
    string? LinkedKey = null);

/// <summary>The store refused or didn't know the purchase; the message can be shown.</summary>
public class BillingException(string message) : Exception(message);

public static class BillingIds
{
    /// <summary>
    /// The account's id given to the stores with each purchase (Google Play's obfuscated account id, Apple's app account
    /// token, which has to be a UUID; the Microsoft Store's publisher user id): derived from the user id, so it means
    /// nothing outside Gym Book, and always the same for the account.
    /// </summary>
    public static string AccountToken(string userId) =>
        new Guid(SHA256.HashData(Encoding.UTF8.GetBytes("gymbook-billing:" + userId)).AsSpan(0, 16)).ToString();

    /// <summary>A setting that may be base64-encoded (a key or JSON that doesn't fit an env var as it is).</summary>
    public static string Decode(string value)
    {
        value = value.Trim();
        if (value.StartsWith('{') || value.StartsWith("-----", StringComparison.Ordinal))
            return value.Replace("\\n", "\n");
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException)
        {
            return value;
        }
    }
}
