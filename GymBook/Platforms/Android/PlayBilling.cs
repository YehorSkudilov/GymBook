using Android.BillingClient.Api;
using GymBook.Contracts;
using GymBook.Services.Billing;

namespace GymBook;

/// <summary>
/// Gym Book Pro through Google Play Billing. The free trial is an offer on each subscription in Play Console; Play only
/// returns it to users who can still have it, and it's preferred when there. Purchases are acknowledged by the API once
/// it has checked them (Play refunds one that isn't acknowledged within three days).
/// </summary>
public class PlayBilling : IStoreBilling
{
    BillingClient? _client;
    TaskCompletionSource<(BillingResult Result, IList<Purchase>? Purchases)>? _purchase;
    readonly Dictionary<string, (ProductDetails Details, ProductDetails.SubscriptionOfferDetails Offer)> _offers = [];

    public event EventHandler<StorePurchase>? PurchaseArrived;

    public SubscriptionStore? Store => SubscriptionStore.GooglePlay;

    public Uri ManageUrl => new($"https://play.google.com/store/account/subscriptions?package={Android.App.Application.Context.PackageName}");

    async Task<BillingClient> ClientAsync()
    {
        if (_client is { IsReady: true })
            return _client;
        _client ??= BillingClient.NewBuilder(Android.App.Application.Context)
            .SetListener(new PurchasesListener(this))
            .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
            .Build();
        var setup = new TaskCompletionSource<BillingResult>();
        _client.StartConnection(new ConnectionListener(setup));
        var result = await setup.Task;
        if (result.ResponseCode != BillingResponseCode.Ok)
            throw new StoreBillingException(Message(result, "Google Play isn't available. Check that you're signed in to the Play Store."));
        return _client;
    }

    public async Task<IReadOnlyList<StoreOffer>> GetOffersAsync()
    {
        var client = await ClientAsync();
        var query = QueryProductDetailsParams.NewBuilder()
            .SetProductList([.. SubscriptionProducts.All.Select(id => QueryProductDetailsParams.Product.NewBuilder()
                .SetProductId(id).SetProductType(BillingClient.ProductType.Subs).Build())])
            .Build();
        var done = new TaskCompletionSource<(BillingResult, QueryProductDetailsResult)>();
        client.QueryProductDetails(query, new DetailsListener(done));
        var (result, details) = await done.Task;
        if (result.ResponseCode != BillingResponseCode.Ok)
            throw new StoreBillingException(Message(result, "Couldn't load the plans from Google Play."));

        var offers = new List<StoreOffer>();
        foreach (var id in SubscriptionProducts.All)
        {
            var product = details.ProductDetailsList.FirstOrDefault(d => d.ProductId == id);
            var options = product?.GetSubscriptionOfferDetails();
            if (product == null || options == null || options.Count == 0)
                continue;
            // The free trial offer when Play offers it (only to users who haven't had it), else the base plan.
            var offer = options.FirstOrDefault(o => o.PricingPhases.PricingPhaseList.Any(p => p.PriceAmountMicros == 0)) ?? options[0];
            var phases = offer.PricingPhases.PricingPhaseList;
            var recurring = phases[^1];
            var free = phases.FirstOrDefault(p => p.PriceAmountMicros == 0);
            _offers[id] = (product, offer);
            offers.Add(new StoreOffer(id, recurring.FormattedPrice, Period(recurring.BillingPeriod), free == null ? null : $"{Length(free.BillingPeriod)} free trial"));
        }
        return offers;
    }

    public async Task<StorePurchase?> PurchaseAsync(string productId, string accountToken, Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket)
    {
        var client = await ClientAsync();
        if (!_offers.ContainsKey(productId))
            await GetOffersAsync();
        if (!_offers.TryGetValue(productId, out var chosen))
            throw new StoreBillingException("That plan isn't available on Google Play right now.");
        var flow = BillingFlowParams.NewBuilder()
            .SetProductDetailsParamsList([BillingFlowParams.ProductDetailsParams.NewBuilder()
                .SetProductDetails(chosen.Details).SetOfferToken(chosen.Offer.OfferToken).Build()])
            // Ties the purchase to the Gym Book account; the API checks it.
            .SetObfuscatedAccountId(accountToken)
            .Build();
        if (Platform.CurrentActivity is not { } activity)
            throw new StoreBillingException("Couldn't open Google Play.");

        _purchase = new TaskCompletionSource<(BillingResult, IList<Purchase>?)>();
        var launch = client.LaunchBillingFlow(activity, flow);
        if (launch.ResponseCode != BillingResponseCode.Ok)
        {
            _purchase = null;
            if (launch.ResponseCode == BillingResponseCode.ItemAlreadyOwned)
                return (await RestoreAsync(microsoftTicket)).FirstOrDefault();
            throw new StoreBillingException(Message(launch, "Google Play couldn't start the purchase."));
        }
        var (result, purchases) = await _purchase.Task;
        _purchase = null;
        if (result.ResponseCode == BillingResponseCode.UserCancelled)
            return null;
        if (result.ResponseCode == BillingResponseCode.ItemAlreadyOwned)
            return (await RestoreAsync(microsoftTicket)).FirstOrDefault();
        if (result.ResponseCode != BillingResponseCode.Ok)
            throw new StoreBillingException(Message(result, "The purchase didn't go through."));
        var purchase = purchases?.FirstOrDefault(p => p.Products.Contains(productId)) ?? purchases?.FirstOrDefault();
        if (purchase == null)
            return null;
        if (purchase.PurchaseState == PurchaseState.Pending)
            throw new StoreBillingException("Your payment is pending. Gym Book Pro starts once Google Play confirms it; come back here and tap Restore.");
        return new StorePurchase(purchase.Products.FirstOrDefault() ?? productId, purchase.PurchaseToken);
    }

    public async Task<IReadOnlyList<StorePurchase>> RestoreAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket)
    {
        var client = await ClientAsync();
        var done = new TaskCompletionSource<(BillingResult, IList<Purchase>)>();
        client.QueryPurchases(QueryPurchasesParams.NewBuilder().SetProductType(BillingClient.ProductType.Subs).Build(), new OwnedListener(done));
        var (result, purchases) = await done.Task;
        if (result.ResponseCode != BillingResponseCode.Ok)
            throw new StoreBillingException(Message(result, "Couldn't ask Google Play for your purchases."));
        return [.. purchases
            .Where(p => p.PurchaseState == PurchaseState.Purchased)
            .Select(p => new StorePurchase(p.Products.FirstOrDefault() ?? "", p.PurchaseToken))];
    }

    /// <summary>Asking Play for what's owned asks the user nothing, so it's the same as a restore.</summary>
    public Task<IReadOnlyList<StorePurchase>> OwnedQuietlyAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket) => RestoreAsync(microsoftTicket);

    static string Message(BillingResult result, string fallback) =>
        result.ResponseCode switch
        {
            BillingResponseCode.BillingUnavailable => "Google Play Billing isn't available on this device or for this Play account.",
            BillingResponseCode.ServiceUnavailable or BillingResponseCode.NetworkError => "Couldn't reach Google Play. Check your connection and try again.",
            _ => string.IsNullOrWhiteSpace(result.DebugMessage) ? fallback : $"{fallback} ({result.DebugMessage})",
        };

    /// <summary>"month" for P1M, "year" for P1Y.</summary>
    static string Period(string iso) => iso.EndsWith('Y') ? "year" : iso.EndsWith('W') ? "week" : "month";

    /// <summary>"7-day" for P7D, "1-month" for P1M.</summary>
    static string Length(string iso)
    {
        var n = new string(iso.Where(char.IsDigit).ToArray());
        var unit = iso[^1] switch { 'D' => "day", 'W' => "week", 'M' => "month", _ => "year" };
        return $"{n}-{unit}";
    }

    sealed class ConnectionListener(TaskCompletionSource<BillingResult> done) : Java.Lang.Object, IBillingClientStateListener
    {
        public void OnBillingSetupFinished(BillingResult result) => done.TrySetResult(result);

        // Reconnected on the next call (IsReady is false then).
        public void OnBillingServiceDisconnected() =>
            done.TrySetResult(BillingResult.NewBuilder().SetResponseCode((int)BillingResponseCode.ServiceDisconnected).Build());
    }

    sealed class DetailsListener(TaskCompletionSource<(BillingResult, QueryProductDetailsResult)> done) : Java.Lang.Object, IProductDetailsResponseListener
    {
        public void OnProductDetailsResponse(BillingResult result, QueryProductDetailsResult details) => done.TrySetResult((result, details));
    }

    sealed class OwnedListener(TaskCompletionSource<(BillingResult, IList<Purchase>)> done) : Java.Lang.Object, IPurchasesResponseListener
    {
        public void OnQueryPurchasesResponse(BillingResult result, IList<Purchase> purchases) => done.TrySetResult((result, purchases));
    }

    sealed class PurchasesListener(PlayBilling billing) : Java.Lang.Object, IPurchasesUpdatedListener
    {
        public void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases)
        {
            if (billing._purchase is { } waiting)
            {
                waiting.TrySetResult((result, purchases));
                return;
            }
            // Nothing waiting: a pending payment that went through, or a purchase finished outside the app.
            if (result.ResponseCode != BillingResponseCode.Ok || purchases == null)
                return;
            foreach (var p in purchases.Where(p => p.PurchaseState == PurchaseState.Purchased))
                billing.PurchaseArrived?.Invoke(billing, new StorePurchase(p.Products.FirstOrDefault() ?? "", p.PurchaseToken));
        }
    }
}
