using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Billing;

/// <summary>
/// Who has Gym Book Pro. Purchases are checked with their store when the app reports one (on buying and on restoring)
/// and recorded against the account; once a recorded one's expiry passes, the store is asked again (it may have
/// renewed), at most every <see cref="RecheckAfter"/>. A purchase belongs to the account that first recorded it.
/// </summary>
public class Subscriptions(ApiDbContext db, GooglePlayVerifier google, AppStoreVerifier apple, MicrosoftStoreVerifier microsoft,
    TimeProvider time, ILogger<Subscriptions> log)
{
    static readonly TimeSpan RecheckAfter = TimeSpan.FromHours(6);

    /// <summary>How long after expiring a subscription is still asked about: a renewal after a lapse longer than this needs a restore in the app.</summary>
    static readonly TimeSpan LapseChecked = TimeSpan.FromDays(60);

    public async Task<bool> IsActiveAsync(string userId, CancellationToken ct) => await CurrentAsync(userId, ct) != null;

    /// <summary>The user's subscription that's running now, if any.</summary>
    public async Task<StoreSubscription?> CurrentAsync(string userId, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var rows = await db.Subscriptions.Where(s => s.UserId == userId && !s.Revoked).ToListAsync(ct);
        if (Running(rows, now) is { } running)
            return running;

        var due = rows.Where(r => r.ExpiresAt > now - LapseChecked && (r.CheckedAt < r.ExpiresAt || now - r.CheckedAt > RecheckAfter)).ToList();
        foreach (var row in due)
        {
            try
            {
                if (await CheckAsync(row.Store, row.Store == SubscriptionStore.MicrosoftStore ? row.RefreshToken : row.PurchaseKey, row.Environment, ct) is { } info)
                    Apply(row, info, now);
                else
                    row.CheckedAt = now;
            }
            catch (Exception e) when (e is BillingException or HttpRequestException or TaskCanceledException)
            {
                log.LogWarning(e, "Couldn't check subscription {Id} again", row.Id);
                row.CheckedAt = now;
            }
        }
        if (due.Count > 0)
            await db.SaveChangesAsync(ct);
        return Running(rows, now);
    }

    static StoreSubscription? Running(List<StoreSubscription> rows, DateTimeOffset now) =>
        rows.Where(r => !r.Revoked && r.ExpiresAt > now).MaxBy(r => r.ExpiresAt);

    /// <summary>Checks a purchase the app reports with its store and records it for the user.</summary>
    public async Task<SubscriptionStatusResponse> VerifyAsync(string userId, VerifyPurchaseRequest request, CancellationToken ct)
    {
        var info = await CheckAsync(request.Store, request.Token, null, ct)
            ?? throw new BillingException("Purchases from this store can't be checked right now. Try again later.");
        if (info.AccountToken is { Length: > 0 } account && !string.Equals(account, BillingIds.AccountToken(userId), StringComparison.OrdinalIgnoreCase))
            throw new BillingException("This subscription was bought with another Gym Book account. Sign in with that account to use it.");
        // The Microsoft Store reports its own product ids; it only reports this app's.
        if (request.Store != SubscriptionStore.MicrosoftStore && !SubscriptionProducts.All.Contains(info.ProductId))
            throw new BillingException("That purchase isn't a Gym Book Pro subscription.");

        var now = time.GetUtcNow();
        var row = await db.Subscriptions.FirstOrDefaultAsync(s => s.Store == request.Store && s.PurchaseKey == info.PurchaseKey, ct);
        if (row != null && row.UserId != userId)
            throw new BillingException("This subscription is already used by another Gym Book account. Sign in with that account to use it.");
        if (row == null)
            db.Subscriptions.Add(row = new StoreSubscription { UserId = userId, Store = request.Store, PurchaseKey = info.PurchaseKey, CreatedAt = now });
        Apply(row, info, now);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Recorded {Store} subscription {Product} for {User}, until {Expires}", request.Store, info.ProductId, userId, info.ExpiresAt);
        return await StatusAsync(userId, ct);
    }

    /// <summary>
    /// A Google Play real-time notification about <paramref name="purchaseToken"/> (renewed, cancelled, on hold, revoked,
    /// refunded…): what it says isn't trusted, the purchase is checked with Google again. A purchase not recorded yet is
    /// taken on when it continues one that is (a switch between monthly and yearly, or a resubscribe), for that account.
    /// </summary>
    public async Task OnGoogleNotificationAsync(string purchaseToken, CancellationToken ct)
    {
        if (!google.IsConfigured)
            return;
        var now = time.GetUtcNow();
        StorePurchaseInfo info;
        try
        {
            info = await google.CheckAsync(purchaseToken, ct);
        }
        catch (BillingException)
        {
            // Not a purchase of this app.
            return;
        }
        var row = await db.Subscriptions.FirstOrDefaultAsync(s => s.Store == SubscriptionStore.GooglePlay && s.PurchaseKey == purchaseToken, ct);
        if (row == null)
        {
            if (info.LinkedKey is not { } linked
                || await db.Subscriptions.FirstOrDefaultAsync(s => s.Store == SubscriptionStore.GooglePlay && s.PurchaseKey == linked, ct) is not { } previous)
                return;
            db.Subscriptions.Add(row = new StoreSubscription
            {
                UserId = previous.UserId, Store = SubscriptionStore.GooglePlay, PurchaseKey = info.PurchaseKey, CreatedAt = now,
            });
            log.LogInformation("Google Play purchase continues {Previous} for {User}", previous.Id, previous.UserId);
        }
        Apply(row, info, now);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// An App Store Server Notification about the subscription <paramref name="originalTransactionId"/>: checked with
    /// Apple again, if it's one recorded here (new purchases are recorded when the app reports them).
    /// </summary>
    public async Task OnAppleNotificationAsync(string originalTransactionId, CancellationToken ct)
    {
        if (!apple.IsConfigured)
            return;
        var row = await db.Subscriptions.FirstOrDefaultAsync(s => s.Store == SubscriptionStore.AppStore && s.PurchaseKey == originalTransactionId, ct);
        if (row == null)
            return;
        Apply(row, await apple.CheckAsync(originalTransactionId, row.Environment, ct), time.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    /// <summary>How often a running subscription is checked with its store anyway, for what no notification reports.</summary>
    static readonly TimeSpan RecheckRunning = TimeSpan.FromDays(1);

    /// <summary>
    /// Checks the running subscriptions not checked for <see cref="RecheckRunning"/> with their stores (up to
    /// <paramref name="max"/>, longest unchecked first): refunds and revocations, and the Microsoft Store, which sends
    /// no notifications. The number checked.
    /// </summary>
    public async Task<int> RecheckDueAsync(int max, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var before = now - RecheckRunning;
        var rows = await db.Subscriptions
            .Where(s => !s.Revoked && s.ExpiresAt > now && s.CheckedAt < before)
            .OrderBy(s => s.CheckedAt)
            .Take(max)
            .ToListAsync(ct);
        foreach (var row in rows)
        {
            try
            {
                if (await CheckAsync(row.Store, row.Store == SubscriptionStore.MicrosoftStore ? row.RefreshToken : row.PurchaseKey, row.Environment, ct) is { } info)
                    Apply(row, info, now);
                else
                    row.CheckedAt = now;
            }
            catch (Exception e) when (e is BillingException or HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                // A Microsoft Store key past its 90 days ends up here until the app sends a new one; the expiry stands.
                log.LogWarning(e, "Couldn't check subscription {Id} again", row.Id);
                row.CheckedAt = now;
            }
        }
        if (rows.Count > 0)
            await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    public async Task<SubscriptionStatusResponse> StatusAsync(string userId, CancellationToken ct)
    {
        var current = await CurrentAsync(userId, ct);
        return new SubscriptionStatusResponse
        {
            Active = current != null,
            Store = current?.Store,
            ProductId = current?.ProductId,
            ExpiresAt = current?.ExpiresAt,
            AutoRenewing = current?.AutoRenewing ?? false,
            InTrial = current?.InTrial ?? false,
            AccountToken = BillingIds.AccountToken(userId),
        };
    }

    /// <summary>The purchase as its store reports it; null when that store isn't set up here (or can't be asked again).</summary>
    async Task<StorePurchaseInfo?> CheckAsync(SubscriptionStore store, string? token, string? environment, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        return store switch
        {
            SubscriptionStore.GooglePlay when google.IsConfigured => await google.CheckAsync(token, ct),
            SubscriptionStore.AppStore when apple.IsConfigured => await apple.CheckAsync(token, environment, ct),
            SubscriptionStore.MicrosoftStore when microsoft.IsConfigured => await microsoft.CheckAsync(token, ct),
            _ => null,
        };
    }

    static void Apply(StoreSubscription row, StorePurchaseInfo info, DateTimeOffset now)
    {
        row.ProductId = info.ProductId;
        row.ExpiresAt = info.ExpiresAt;
        row.AutoRenewing = info.AutoRenewing;
        row.InTrial = info.InTrial;
        row.Revoked = info.Revoked;
        row.Environment = info.Environment ?? row.Environment;
        row.RefreshToken = info.RefreshToken ?? row.RefreshToken;
        row.CheckedAt = now;
    }
}
