using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Plans;

/// <summary>What the quota counts; each kind has its own limit.</summary>
public static class QuotaKind
{
    public const string Plan = "plan";
    public const string Chat = "chat";
}

public class PlanQuotaOptions
{
    /// <summary>How many a user may use in any <see cref="Window"/>.</summary>
    public int Limit { get; init; } = 10;
    public TimeSpan Window { get; init; } = TimeSpan.FromHours(1);
    /// <summary>The window as the user sees it, e.g. "day" or "30 days".</summary>
    public string Period { get; init; } = "hour";

    /// <summary>Parses a limit per window like "90m", "12h", "1d", "30d" or "2w".</summary>
    public static PlanQuotaOptions Parse(int limit, string window)
    {
        var text = window.Trim().ToLowerInvariant();
        if (limit < 0 || text.Length < 2 || !int.TryParse(text[..^1], out var n) || n <= 0)
            throw new InvalidOperationException($"Quota \"{limit} per {window}\" isn't valid; use a window like 90m, 12h, 1d or 30d.");
        var (span, unit) = text[^1] switch
        {
            'm' => (TimeSpan.FromMinutes(n), "minute"),
            'h' => (TimeSpan.FromHours(n), "hour"),
            'd' => (TimeSpan.FromDays(n), "day"),
            'w' => (TimeSpan.FromDays(n * 7), "week"),
            _ => throw new InvalidOperationException($"Quota window \"{window}\" isn't valid; use e.g. 90m, 12h, 1d or 30d."),
        };
        return new PlanQuotaOptions { Limit = limit, Window = span, Period = n == 1 ? unit : $"{n} {unit}s" };
    }
}

/// <summary>The limits of each <see cref="QuotaKind"/>.</summary>
public class PlanQuotaSettings
{
    public required PlanQuotaOptions Plan { get; init; }
    public required PlanQuotaOptions Chat { get; init; }

    public PlanQuotaOptions For(string kind) => kind == QuotaKind.Chat ? Chat : Plan;
}

/// <summary>
/// Per-user quotas of paid AI calls (plans, and plan chat messages), over a sliding window. Kept in the database so
/// they survive restarts and deploys (a monthly limit would otherwise reset with each one), and so the app can show
/// how many are left.
/// </summary>
public class PlanQuota(ApiDbContext db, PlanQuotaSettings settings, TimeProvider time)
{
    public async Task<PlanQuotaResponse> GetAsync(string userId, string kind, CancellationToken ct)
    {
        var options = settings.For(kind);
        var since = time.GetUtcNow() - options.Window;
        var used = await db.PlanGenerations
            .Where(g => g.UserId == userId && g.Kind == kind && g.CreatedAt > since)
            .Select(g => g.CreatedAt)
            .ToListAsync(ct);
        return new PlanQuotaResponse
        {
            Limit = options.Limit,
            Remaining = Math.Max(0, options.Limit - used.Count),
            Period = options.Period,
            // When the oldest one in the window expires and frees up another.
            NextAvailableAt = used.Count > 0 ? used.Min() + options.Window : null,
        };
    }

    /// <summary>Takes one from the quota, or returns null when none are left. <see cref="ReleaseAsync"/> gives it back.</summary>
    public async Task<Guid?> TryReserveAsync(string userId, string kind, CancellationToken ct)
    {
        var options = settings.For(kind);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Locks the user's row, so two requests at once can't both take the last one.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"AspNetUsers\" WHERE \"Id\" = {userId} FOR UPDATE", ct);

        var now = time.GetUtcNow();
        var since = now - options.Window;
        // Older ones are kept (not deleted once out of the window) for the admin app's AI usage history.
        var used = await db.PlanGenerations.CountAsync(g => g.UserId == userId && g.Kind == kind && g.CreatedAt > since, ct);
        if (used >= options.Limit)
            return null;

        var generation = new PlanGeneration { UserId = userId, Kind = kind, CreatedAt = now };
        db.PlanGenerations.Add(generation);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return generation.Id;
    }

    /// <summary>A call that failed doesn't count.</summary>
    public Task ReleaseAsync(Guid id) => db.PlanGenerations.Where(g => g.Id == id).ExecuteDeleteAsync();
}
