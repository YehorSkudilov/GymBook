using System.ComponentModel.DataAnnotations;

namespace GymBook.Contracts;

/// <summary>Where a subscription was bought; each store's purchase is checked with that store by the API.</summary>
public enum SubscriptionStore { GooglePlay, AppStore, MicrosoftStore }

/// <summary>
/// Gym Book Pro: the subscription that unlocks the AI features (plans, imports, the plan chat and reviews). The same
/// product ids in Google Play, App Store Connect and, as each add-on's product id (its in-app offer token), Partner
/// Center. A free trial is a store offer on them (Play's free trial phase, Apple's introductory offer, Partner
/// Center's free trial), so the stores run it.
/// </summary>
public static class SubscriptionProducts
{
    public const string Monthly = "gymbook_pro_monthly";
    public const string Yearly = "gymbook_pro_yearly";

    public static readonly string[] All = [Monthly, Yearly];

    public const string Name = "Gym Book Pro";
}

/// <summary>The signed-in user's subscription, and what the app needs to tie a new purchase to the account.</summary>
public class SubscriptionStatusResponse
{
    public bool Active { get; set; }
    public SubscriptionStore? Store { get; set; }
    public string? ProductId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    /// <summary>Renews at <see cref="ExpiresAt"/>; false once cancelled (it still runs until then).</summary>
    public bool AutoRenewing { get; set; }
    /// <summary>In the store's free trial.</summary>
    public bool InTrial { get; set; }
    /// <summary>
    /// The account's id for purchases: Google Play's obfuscated account id and Apple's app account token (a UUID), so
    /// a purchase can only be claimed by the account that made it.
    /// </summary>
    public string AccountToken { get; set; } = "";
}

/// <summary>A purchase for the API to check with its store and record against the account.</summary>
public class VerifyPurchaseRequest
{
    public SubscriptionStore Store { get; set; }
    [MaxLength(64)]
    public string ProductId { get; set; } = "";
    /// <summary>
    /// Google Play: the purchase token. App Store: the transaction id. Microsoft Store: a purchase ID key from
    /// StoreContext.GetCustomerPurchaseIdAsync, made with <see cref="MicrosoftStoreTicketResponse"/>.
    /// </summary>
    [Required, MaxLength(8192)]
    public string Token { get; set; } = "";
}

/// <summary>What the Windows app needs to make a Microsoft Store purchase ID key the API can check purchases with.</summary>
public class MicrosoftStoreTicketResponse
{
    /// <summary>An Azure AD token for StoreContext.GetCustomerPurchaseIdAsync.</summary>
    public string ServiceTicket { get; set; } = "";
    public string PublisherUserId { get; set; } = "";
}
