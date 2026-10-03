using GymBook.Contracts;

namespace GymBook.Services.Billing;

/// <summary>A Gym Book Pro plan as the store sells it, with the store's own localized price.</summary>
/// <param name="Period">"month" or "year".</param>
/// <param name="Trial">The free trial the store offers on it ("7-day free trial"), or null when there's none for this user.</param>
public record StoreOffer(string ProductId, string Price, string Period, string? Trial);

/// <summary>A purchase for the API to check (see VerifyPurchaseRequest.Token).</summary>
public record StorePurchase(string ProductId, string Token);

/// <summary>The store was unavailable or refused; the message can be shown.</summary>
public class StoreBillingException(string message) : Exception(message);

/// <summary>
/// Buying Gym Book Pro through the platform's store: Google Play on Android (Platforms/Android/PlayBilling.cs), the App
/// Store on iOS (Platforms/iOS/AppStoreBilling.cs), the Microsoft Store on Windows
/// (Platforms/Windows/MicrosoftStoreBilling.cs). The API checks each purchase with the store (SubscriptionService).
/// </summary>
public interface IStoreBilling
{
    /// <summary>Null on a platform without store purchases here (macOS for now).</summary>
    SubscriptionStore? Store { get; }

    /// <summary>The plans of <see cref="SubscriptionProducts.All"/> the store has, in that order.</summary>
    Task<IReadOnlyList<StoreOffer>> GetOffersAsync();

    /// <summary>
    /// Buys <paramref name="productId"/> for the account <paramref name="accountToken"/> stands for (see
    /// SubscriptionStatusResponse.AccountToken). Null when the user cancelled. Microsoft Store: the token is made with
    /// <paramref name="microsoftTicket"/>.
    /// </summary>
    Task<StorePurchase?> PurchaseAsync(string productId, string accountToken, Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket);

    /// <summary>The subscriptions the store says this store account owns, to record against the Gym Book account again.</summary>
    Task<IReadOnlyList<StorePurchase>> RestoreAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket);

    /// <summary>
    /// What the store says is owned, without asking the user anything (Restore on iOS can ask for the Apple ID password,
    /// so there it's only purchases that finished while nothing was waiting for them). For keeping the API up to date.
    /// </summary>
    Task<IReadOnlyList<StorePurchase>> OwnedQuietlyAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket);

    /// <summary>
    /// A purchase that completed on its own, outside <see cref="PurchaseAsync"/>: a pending payment going through, Ask
    /// to Buy approved, a purchase finished after the app was closed.
    /// </summary>
    event EventHandler<StorePurchase>? PurchaseArrived;

    /// <summary>The store's own page for managing (cancelling) subscriptions.</summary>
    Uri ManageUrl { get; }
}

public class NoStoreBilling : IStoreBilling
{
    public SubscriptionStore? Store => null;

    public Task<IReadOnlyList<StoreOffer>> GetOffersAsync() => Task.FromResult<IReadOnlyList<StoreOffer>>([]);

    public Task<StorePurchase?> PurchaseAsync(string productId, string accountToken, Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket) =>
        throw new StoreBillingException($"{SubscriptionProducts.Name} can't be bought on this device yet. Subscribe on your phone, and it works here too.");

    public Task<IReadOnlyList<StorePurchase>> RestoreAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket) =>
        Task.FromResult<IReadOnlyList<StorePurchase>>([]);

    public Task<IReadOnlyList<StorePurchase>> OwnedQuietlyAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket) =>
        Task.FromResult<IReadOnlyList<StorePurchase>>([]);

    public event EventHandler<StorePurchase>? PurchaseArrived { add { } remove { } }

    public Uri ManageUrl => new("https://gymbook.app/support");
}
