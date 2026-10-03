using GymBook.Contracts;
using GymBook.Services.Sync;

namespace GymBook.Services.Billing;

/// <summary>
/// Gym Book Pro, which the AI features need: whether the account has it (the API decides, from purchases it checked
/// with the stores), buying it through this platform's store, and restoring a purchase made before. The last known
/// state is kept on the device, so the app knows offline (the API still checks every AI call).
/// </summary>
public class SubscriptionService(ApiClient api, IStoreBilling billing, AccountService account)
{
    // Per account, so another account signing in on the device doesn't inherit it.
    string ActiveKey => $"pro.active.{account.Email}";
    string ExpiresKey => $"pro.expires.{account.Email}";

    /// <summary>Raised when <see cref="Status"/> changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The signed-in account's, as last heard from the API; null before that.</summary>
    public SubscriptionStatusResponse? Status => _statusFor == account.Email ? _status : null;

    SubscriptionStatusResponse? _status;
    string? _statusFor;

    /// <summary>Has Gym Book Pro, as last heard from the API.</summary>
    public bool IsActive => account.IsSignedIn && (Status?.Active
        ?? (Preferences.Default.Get(ActiveKey, false) && Preferences.Default.Get(ExpiresKey, DateTime.MinValue) > DateTime.UtcNow));

    /// <summary>The store purchases go through on this device; null where it can't buy.</summary>
    public SubscriptionStore? Store => billing.Store;

    public Uri ManageUrl => billing.ManageUrl;

    public Task<IReadOnlyList<StoreOffer>> GetOffersAsync() => billing.GetOffersAsync();

    /// <summary>How often <see cref="SyncQuietlyAsync"/> sends the store's purchases again (the Microsoft Store's key lasts 90 days).</summary>
    static readonly TimeSpan SyncInterval = TimeSpan.FromDays(1);

    string SyncedKey => $"pro.synced.{account.Email}";
    bool _listening, _syncing;

    /// <summary>
    /// On opening the app and coming back to it: the API's state, and at most daily what the store says is owned (asking
    /// the user nothing), so the API keeps up: a pending payment that went through, a plan switched in the store, the
    /// Microsoft Store's key renewed. From then on, purchases completing on their own are sent as they come.
    /// </summary>
    public async Task SyncQuietlyAsync()
    {
        if (!_listening)
        {
            billing.PurchaseArrived += OnPurchaseArrived;
            _listening = true;
        }
        if (_syncing || !account.IsSignedIn || account.NeedsEmailVerification)
            return;
        _syncing = true;
        try
        {
            if (billing.Store is { } store && DateTime.UtcNow - Preferences.Default.Get(SyncedKey, DateTime.MinValue) > SyncInterval)
            {
                foreach (var purchase in await billing.OwnedQuietlyAsync(() => api.GetMicrosoftStoreTicketAsync()))
                    await SendQuietlyAsync(store, purchase);
                Preferences.Default.Set(SyncedKey, DateTime.UtcNow);
            }
            await RefreshAsync();
        }
        catch (Exception e) when (e is StoreBillingException or HttpRequestException or TaskCanceledException or ApiException or SessionExpiredException)
        {
            // Tried again next time.
        }
        finally
        {
            _syncing = false;
        }
    }

    void OnPurchaseArrived(object? sender, StorePurchase purchase)
    {
        if (billing.Store is { } store && account.IsSignedIn)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await SendQuietlyAsync(store, purchase);
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException or ApiException or SessionExpiredException)
                {
                    // The daily sync sends it again.
                }
            });
    }

    /// <summary>A purchase for the API to record; one it refuses (another account's, say) is left alone.</summary>
    async Task SendQuietlyAsync(SubscriptionStore store, StorePurchase purchase)
    {
        try
        {
            Set(await api.VerifyPurchaseAsync(new VerifyPurchaseRequest { Store = store, ProductId = purchase.ProductId, Token = purchase.Token }));
        }
        catch (ApiException e) when (e.Status == System.Net.HttpStatusCode.Conflict)
        {
        }
    }

    /// <summary>Asks the API. Quietly keeps the last known state when offline.</summary>
    public async Task<SubscriptionStatusResponse?> RefreshAsync()
    {
        if (!account.IsSignedIn)
            return null;
        try
        {
            Set(await api.GetSubscriptionAsync());
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or ApiException or SessionExpiredException)
        {
        }
        return Status;
    }

    /// <summary>
    /// Buys <paramref name="productId"/> and has the API record it. False when cancelled. Throws
    /// <see cref="StoreBillingException"/> or <see cref="ApiException"/> with a message to show.
    /// </summary>
    public async Task<bool> PurchaseAsync(string productId)
    {
        if (billing.Store is not { } store)
            throw new StoreBillingException($"{SubscriptionProducts.Name} can't be bought on this device yet.");
        var token = Status?.AccountToken is { Length: > 0 } t ? t : (await api.GetSubscriptionAsync()).AccountToken;
        if (await billing.PurchaseAsync(productId, token, () => api.GetMicrosoftStoreTicketAsync()) is not { } purchase)
            return false;
        Set(await api.VerifyPurchaseAsync(new VerifyPurchaseRequest { Store = store, ProductId = purchase.ProductId, Token = purchase.Token }));
        return Status?.Active == true;
    }

    /// <summary>
    /// Records again what the store says this store account owns, e.g. after reinstalling or on a new device. True when
    /// that leaves the account with Gym Book Pro.
    /// </summary>
    public async Task<bool> RestoreAsync()
    {
        if (billing.Store is not { } store)
            return (await RefreshAsync())?.Active == true;
        ApiException? refused = null;
        foreach (var purchase in await billing.RestoreAsync(() => api.GetMicrosoftStoreTicketAsync()))
        {
            try
            {
                Set(await api.VerifyPurchaseAsync(new VerifyPurchaseRequest { Store = store, ProductId = purchase.ProductId, Token = purchase.Token }));
            }
            catch (ApiException e)
            {
                // Another one may still be good; the reason is shown if none is.
                refused = e;
            }
        }
        if ((await RefreshAsync())?.Active == true)
            return true;
        if (refused != null)
            throw refused;
        return false;
    }

    void Set(SubscriptionStatusResponse status)
    {
        _status = status;
        _statusFor = account.Email;
        Preferences.Default.Set(ActiveKey, status.Active);
        Preferences.Default.Set(ExpiresKey, status.ExpiresAt?.UtcDateTime ?? DateTime.MinValue);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
