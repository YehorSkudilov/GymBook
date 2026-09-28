using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Plans;

public class PlanQuotaOptions
{
    /// <summary>How many AI plans a user may generate in any <see cref="Window"/>.</summary>
    public int Limit { get; init; } = 10;
    public TimeSpan Window { get; init; } = TimeSpan.FromHours(1);
    /// <summary>The window as the user sees it, e.g. "day" or "30 days".</summary>
    public string Period { get; init; } = "hour";
}

/// <summary>
/// The per-user quota of AI plans, over a sliding window. Kept in the database so it survives restarts and deploys
/// (a monthly limit would otherwise reset with each one), and so the app can show how many are left.
/// </summary>
public class PlanQuota(ApiDbContext db, PlanQuotaOptions options, TimeProvider time)
{
    public async Task<PlanQuotaResponse> GetAsync(string userId, CancellationToken ct)
    {
        var since = time.GetUtcNow() - options.Window;
        var used = await db.PlanGenerations.Where(g => g.UserId == userId && g.CreatedAt > since).Select(g => g.CreatedAt).ToListAsync(ct);
        return Response(used);
    }

    /// <summary>Takes one generation from the quota, or returns null when none are left. <see cref="ReleaseAsync"/> gives it back.</summary>
    public async Task<Guid?> TryReserveAsync(string userId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Locks the user's row, so two requests at once can't both take the last one.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"AspNetUsers\" WHERE \"Id\" = {userId} FOR UPDATE", ct);

        var now = time.GetUtcNow();
        var since = now - options.Window;
        // Generations that fell out of the window aren't needed any more.
        await db.PlanGenerations.Where(g => g.UserId == userId && g.CreatedAt <= since).ExecuteDeleteAsync(ct);
        var used = await db.PlanGenerations.CountAsync(g => g.UserId == userId, ct);
        if (used >= options.Limit)
            return null;

        var generation = new PlanGeneration { UserId = userId, CreatedAt = now };
        db.PlanGenerations.Add(generation);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return generation.Id;
    }

    /// <summary>A generation that failed doesn't count.</summary>
    public Task ReleaseAsync(Guid id) => db.PlanGenerations.Where(g => g.Id == id).ExecuteDeleteAsync();

    PlanQuotaResponse Response(List<DateTimeOffset> used) => new()
    {
        Limit = options.Limit,
        Remaining = Math.Max(0, options.Limit - used.Count),
        Period = options.Period,
        // When the oldest one in the window expires and frees up another.
        NextAvailableAt = used.Count > 0 ? used.Min() + options.Window : null,
    };

    /// <summary>Parses a window like "90m", "12h", "1d", "30d" or "2w" into the options.</summary>
    public static PlanQuotaOptions Parse(int limit, string window)
    {
        var text = window.Trim().ToLowerInvariant();
        if (limit < 0 || text.Length < 2 || !int.TryParse(text[..^1], out var n) || n <= 0)
            throw new InvalidOperationException($"Plan generation limit \"{limit} per {window}\" isn't valid; use e.g. 90m, 12h, 1d or 30d.");
        var (span, unit) = text[^1] switch
        {
            'm' => (TimeSpan.FromMinutes(n), "minute"),
            'h' => (TimeSpan.FromHours(n), "hour"),
            'd' => (TimeSpan.FromDays(n), "day"),
            'w' => (TimeSpan.FromDays(n * 7), "week"),
            _ => throw new InvalidOperationException($"Plan generation window \"{window}\" isn't valid; use e.g. 90m, 12h, 1d or 30d."),
        };
        return new PlanQuotaOptions { Limit = limit, Window = span, Period = n == 1 ? unit : $"{n} {unit}s" };
    }
}
