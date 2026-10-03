using GymBook.Contracts;
using GymBook.Services.Billing;
using Windows.Services.Store;

namespace GymBook.WinUI;

/// <summary>
/// Gym Book Pro through the Microsoft Store: subscription add-ons in Partner Center whose product ids (in-app offer
/// tokens) are those of <see cref="SubscriptionProducts"/>, with their free trial set there. The API checks purchases
/// with a purchase ID key made from its service ticket, which carries the account's id (the publisher user id).
/// </summary>
public class MicrosoftStoreBilling : IStoreBilling
{
    StoreContext? _context;
    readonly Dictionary<string, StoreProduct> _products = [];

    public SubscriptionStore? Store => SubscriptionStore.MicrosoftStore;

    public Uri ManageUrl => new("https://account.microsoft.com/services");

    // The Microsoft Store has no purchases that complete on their own later.
    public event EventHandler<StorePurchase>? PurchaseArrived { add { } remove { } }

    /// <summary>
    /// A fresh purchase ID key while a subscription is active, asking the user nothing: the API's key lasts 90 days, so
    /// this keeps it able to check the subscription.
    /// </summary>
    public Task<IReadOnlyList<StorePurchase>> OwnedQuietlyAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket) => RestoreAsync(microsoftTicket);

    /// <summary>The store context, tied to the app's window so the Store's purchase dialog can show over it.</summary>
    StoreContext Context()
    {
        if (_context != null)
            return _context;
        _context = StoreContext.GetDefault();
        if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView is Microsoft.UI.Xaml.Window window)
            WinRT.Interop.InitializeWithWindow.Initialize(_context, WinRT.Interop.WindowNative.GetWindowHandle(window));
        return _context;
    }

    public async Task<IReadOnlyList<StoreOffer>> GetOffersAsync()
    {
        var result = await Context().GetAssociatedStoreProductsAsync(["Durable"]);
        if (result.ExtendedError is { } error)
            throw new StoreBillingException($"Couldn't load the plans from the Microsoft Store. {error.Message}");
        var offers = new List<StoreOffer>();
        foreach (var id in SubscriptionProducts.All)
        {
            if (result.Products.Values.FirstOrDefault(p => p.InAppOfferToken == id) is not { } product)
                continue;
            _products[id] = product;
            var info = product.Skus.FirstOrDefault(s => s.IsSubscription)?.SubscriptionInfo;
            var period = info?.BillingPeriodUnit == StoreDurationUnit.Year ? "year" : "month";
            var trial = info is { HasTrialPeriod: true } ? $"{info.TrialPeriod}-{Unit(info.TrialPeriodUnit)} free trial" : null;
            var price = string.IsNullOrEmpty(product.Price.FormattedRecurrencePrice) ? product.Price.FormattedPrice : product.Price.FormattedRecurrencePrice;
            offers.Add(new StoreOffer(id, price, period, trial));
        }
        return offers;
    }

    public async Task<StorePurchase?> PurchaseAsync(string productId, string accountToken, Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket)
    {
        if (!_products.ContainsKey(productId))
            await GetOffersAsync();
        if (!_products.TryGetValue(productId, out var product))
            throw new StoreBillingException("That plan isn't available in the Microsoft Store right now.");
        var result = await Context().RequestPurchaseAsync(product.StoreId);
        switch (result.Status)
        {
            case StorePurchaseStatus.Succeeded or StorePurchaseStatus.AlreadyPurchased:
                return new StorePurchase(productId, await PurchaseKeyAsync(microsoftTicket));
            case StorePurchaseStatus.NotPurchased:
                return null;
            case StorePurchaseStatus.NetworkError:
                throw new StoreBillingException("Couldn't reach the Microsoft Store. Check your connection and try again.");
            default:
                throw new StoreBillingException($"The purchase didn't go through.{(result.ExtendedError is { } e ? " " + e.Message : "")}");
        }
    }

    public async Task<IReadOnlyList<StorePurchase>> RestoreAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket)
    {
        var license = await Context().GetAppLicenseAsync();
        var owned = license.AddOnLicenses.Values.Where(l => l.IsActive && SubscriptionProducts.All.Contains(l.InAppOfferToken)).ToList();
        if (owned.Count == 0)
            return [];
        // One key covers every subscription of the Microsoft account; the API finds them.
        return [new StorePurchase(owned[0].InAppOfferToken, await PurchaseKeyAsync(microsoftTicket))];
    }

    /// <summary>A purchase ID key for the API, made with its service ticket for this Gym Book account.</summary>
    async Task<string> PurchaseKeyAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket)
    {
        var ticket = await microsoftTicket();
        var key = await Context().GetCustomerPurchaseIdAsync(ticket.ServiceTicket, ticket.PublisherUserId);
        if (string.IsNullOrEmpty(key))
            throw new StoreBillingException("The Microsoft Store couldn't confirm the purchase. Check that you're signed in to the Store app.");
        return key;
    }

    static string Unit(StoreDurationUnit unit) => unit switch
    {
        StoreDurationUnit.Day => "day",
        StoreDurationUnit.Week => "week",
        StoreDurationUnit.Month => "month",
        _ => "year",
    };
}
