using Foundation;
using GymBook.Contracts;
using GymBook.Services.Billing;
using StoreKit;

namespace GymBook;

/// <summary>
/// Gym Book Pro through the App Store (StoreKit; .NET binds StoreKit 1). The API checks each transaction with the App
/// Store Server API, so a transaction id is all it needs. The account's UUID goes with each payment as its
/// applicationUsername, which the App Store keeps as the transaction's appAccountToken. The free trial is each
/// subscription's introductory offer in App Store Connect.
/// </summary>
public class AppStoreBilling : IStoreBilling
{
    readonly Observer _observer;
    readonly Dictionary<string, SKProduct> _products = [];

    public AppStoreBilling()
    {
        _observer = new Observer(p => PurchaseArrived?.Invoke(this, p));
        // Watching from the start, so transactions finished while the app was closed are finished here too.
        SKPaymentQueue.DefaultQueue.AddTransactionObserver(_observer);
    }

    public event EventHandler<StorePurchase>? PurchaseArrived;

    public SubscriptionStore? Store => SubscriptionStore.AppStore;

    public Uri ManageUrl => new("https://apps.apple.com/account/subscriptions");

    /// <summary>Only purchases that finished with nothing waiting: restoring can ask for the Apple ID password.</summary>
    public Task<IReadOnlyList<StorePurchase>> OwnedQuietlyAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket) =>
        Task.FromResult<IReadOnlyList<StorePurchase>>(_observer.TakeUnclaimed());

    public async Task<IReadOnlyList<StoreOffer>> GetOffersAsync()
    {
        if (!SKPaymentQueue.CanMakePayments)
            throw new StoreBillingException("Purchases are turned off on this device (Settings › Screen Time › Content & Privacy Restrictions).");
        var done = new TaskCompletionSource<SKProduct[]>();
        var request = new SKProductsRequest(new NSSet<NSString>(SubscriptionProducts.All.Select(id => new NSString(id)).ToArray()))
        {
            Delegate = new ProductsDelegate(done),
        };
        request.Start();
        var products = await done.Task;

        var offers = new List<StoreOffer>();
        foreach (var id in SubscriptionProducts.All)
        {
            if (products.FirstOrDefault(p => p.ProductIdentifier == id) is not { } product)
                continue;
            _products[id] = product;
            var trial = product.IntroductoryPrice is { PaymentMode: SKProductDiscountPaymentMode.FreeTrial } intro
                ? $"{Length(intro.SubscriptionPeriod)} free trial"
                : null;
            offers.Add(new StoreOffer(id, Price(product), product.SubscriptionPeriod?.Unit == SKProductPeriodUnit.Year ? "year" : "month", trial));
        }
        return offers;
    }

    public async Task<StorePurchase?> PurchaseAsync(string productId, string accountToken, Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket)
    {
        if (!_products.ContainsKey(productId))
            await GetOffersAsync();
        if (!_products.TryGetValue(productId, out var product))
            throw new StoreBillingException("That plan isn't available on the App Store right now.");
        var payment = SKMutablePayment.PaymentWithProduct(product);
        payment.ApplicationUsername = accountToken;
        var transaction = await _observer.BuyAsync(productId, () => SKPaymentQueue.DefaultQueue.AddPayment(payment));
        return transaction == null ? null : new StorePurchase(productId, transaction);
    }

    public async Task<IReadOnlyList<StorePurchase>> RestoreAsync(Func<Task<MicrosoftStoreTicketResponse>> microsoftTicket)
    {
        var restored = await _observer.RestoreAsync(() => SKPaymentQueue.DefaultQueue.RestoreCompletedTransactions());
        // The API looks the subscription up by any one of its transactions.
        return [.. restored.DistinctBy(r => r.ProductId)];
    }

    static string Price(SKProduct product)
    {
        var format = new NSNumberFormatter { NumberStyle = NSNumberFormatterStyle.Currency, Locale = product.PriceLocale };
        return format.StringFromNumber(product.Price) ?? product.Price.ToString();
    }

    static string Length(SKProductSubscriptionPeriod period)
    {
        var unit = period.Unit switch
        {
            SKProductPeriodUnit.Day => "day",
            SKProductPeriodUnit.Week => "week",
            SKProductPeriodUnit.Month => "month",
            _ => "year",
        };
        return $"{period.NumberOfUnits}-{unit}";
    }

    sealed class ProductsDelegate(TaskCompletionSource<SKProduct[]> done) : SKProductsRequestDelegate
    {
        public override void ReceivedResponse(SKProductsRequest request, SKProductsResponse response) => done.TrySetResult(response.Products);

        public override void RequestFailed(SKRequest request, NSError error) =>
            done.TrySetException(new StoreBillingException($"Couldn't load the plans from the App Store. {error.LocalizedDescription}"));
    }

    /// <summary>The payment queue's events, turned into the purchase or restore that's waiting for them.</summary>
    sealed class Observer(Action<StorePurchase> arrived) : SKPaymentTransactionObserver
    {
        TaskCompletionSource<string?>? _buy;
        string? _buying;
        TaskCompletionSource<List<StorePurchase>>? _restore;
        readonly List<StorePurchase> _restored = [];
        // Bought while nothing was waiting (finished after the app was closed, say): handed over at the next restore.
        readonly List<StorePurchase> _unclaimed = [];

        public Task<string?> BuyAsync(string productId, Action start)
        {
            _buy = new TaskCompletionSource<string?>();
            _buying = productId;
            start();
            return _buy.Task;
        }

        public List<StorePurchase> TakeUnclaimed()
        {
            List<StorePurchase> taken = [.. _unclaimed];
            _unclaimed.Clear();
            return taken;
        }

        public Task<List<StorePurchase>> RestoreAsync(Action start)
        {
            _restore = new TaskCompletionSource<List<StorePurchase>>();
            _restored.Clear();
            _restored.AddRange(_unclaimed);
            _unclaimed.Clear();
            start();
            return _restore.Task;
        }

        public override void UpdatedTransactions(SKPaymentQueue queue, SKPaymentTransaction[] transactions)
        {
            foreach (var t in transactions)
            {
                var id = t.TransactionIdentifier;
                var product = t.Payment.ProductIdentifier;
                switch (t.TransactionState)
                {
                    case SKPaymentTransactionState.Purchased:
                        queue.FinishTransaction(t);
                        if (_buy != null && product == _buying)
                        {
                            _buy.TrySetResult(id);
                            // Done waiting: a later one of the same plan is a new arrival.
                            _buy = null;
                        }
                        else if (id != null)
                        {
                            var purchase = new StorePurchase(product, id);
                            _unclaimed.Add(purchase);
                            arrived(purchase);
                        }
                        break;
                    case SKPaymentTransactionState.Restored:
                        queue.FinishTransaction(t);
                        if (id != null)
                            (_restore != null ? _restored : _unclaimed).Add(new StorePurchase(product, id));
                        break;
                    case SKPaymentTransactionState.Failed:
                        queue.FinishTransaction(t);
                        if (_buy != null && product == _buying)
                        {
                            if (t.Error != null && t.Error.Code == (int)SKError.PaymentCancelled)
                                _buy.TrySetResult(null);
                            else
                                _buy.TrySetException(new StoreBillingException(t.Error?.LocalizedDescription ?? "The purchase didn't go through."));
                        }
                        break;
                    case SKPaymentTransactionState.Deferred:
                        _buy?.TrySetException(new StoreBillingException("Your purchase is waiting for approval (Ask to Buy). Gym Book Pro starts once it's approved."));
                        break;
                }
            }
        }

        public override void RestoreCompletedTransactionsFinished(SKPaymentQueue queue) => _restore?.TrySetResult([.. _restored]);

        public override void RestoreCompletedTransactionsFailedWithError(SKPaymentQueue queue, NSError error) =>
            _restore?.TrySetException(new StoreBillingException($"Couldn't restore purchases. {error.LocalizedDescription}"));
    }
}
