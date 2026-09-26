using GymBook.Api.Data;
using GymBook.Contracts;
using GymBook.Models;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Sync;

public class SyncLimitException(string message) : Exception(message);

/// <summary>
/// Applies a client's pushed changes with last-write-wins per record, then returns the server changes the
/// client hasn't seen. All queries go through <see cref="ApiDbContext"/>'s per-user filter, and ownership is
/// always taken from the token, never from the request.
/// </summary>
public class SyncProcessor(ApiDbContext db, TimeProvider clock)
{
    /// <summary>Per-user cap on stored records of each kind, so one account can't fill the database.</summary>
    public const int MaxRecordsPerKind = 20_000;

    public async Task<SyncResponse> ApplyAsync(AppUser user, SyncRequest request, CancellationToken ct)
    {
        var now = Truncate(clock.GetUtcNow());
        var version = user.SyncVersion + 1;
        var changes = request.Changes;
        var accepted = new HashSet<(Type, string)>();
        var rejected = new SyncChanges();

        var wrote = await UpsertAsync(db.Plans, changes.Plans, (to, from) => to.Workouts = from.Workouts, rejected.Plans);
        wrote |= await UpsertAsync(db.Sessions, changes.Sessions, (to, from) => to.Exercises = from.Exercises, rejected.Sessions);
        wrote |= await UpsertAsync(db.CustomExercises, changes.CustomExercises, (to, from) => to.SecondaryMuscles = from.SecondaryMuscles, rejected.CustomExercises);
        wrote |= await UpsertAsync(db.BodyWeights, changes.BodyWeights, (_, _) => { }, rejected.BodyWeights);
        wrote |= await UpsertProfileAsync();

        if (wrote)
        {
            // SyncVersion is a concurrency token: a parallel sync for the same user makes this throw and the
            // caller retries, so two pushes can never share a version.
            user.SyncVersion = version;
            await db.SaveChangesAsync(ct);
        }
        return await PullAsync(user.SyncVersion, request.Since, accepted, rejected, ct);

        async Task<bool> UpsertAsync<T>(DbSet<T> set, List<T> items, Action<T, T> copyCollections, List<T> lost) where T : class, ISyncEntity
        {
            if (items.Count == 0)
                return false;

            // If the same record appears twice in one request, the newest copy is the one that counts.
            var incoming = items.GroupBy(i => i.Id).Select(g => g.MaxBy(i => i.UpdatedAt)!).ToList();
            var ids = incoming.Select(i => i.Id).ToList();
            var existing = await set.Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);

            var added = incoming.Count(i => !existing.ContainsKey(i.Id));
            if (added > 0 && await set.CountAsync(ct) + added > MaxRecordsPerKind)
                throw new SyncLimitException($"Too many {typeof(T).Name} records for one account.");

            var wroteAny = false;
            foreach (var item in incoming)
            {
                item.UpdatedAt = Normalize(item.UpdatedAt, now);
                if (existing.TryGetValue(item.Id, out var current))
                {
                    if (item.UpdatedAt < current.UpdatedAt)
                    {
                        lost.Add(current);
                        continue;
                    }
                    var entry = db.Entry(current);
                    entry.CurrentValues.SetValues(item);
                    copyCollections(current, item);
                    entry.Property(ApiDbContext.VersionColumn).CurrentValue = version;
                }
                else
                {
                    // Ownership is part of the key, so it has to be set before EF starts tracking the record.
                    var entry = db.Entry(item);
                    entry.Property(ApiDbContext.UserIdColumn).CurrentValue = user.Id;
                    entry.Property(ApiDbContext.VersionColumn).CurrentValue = version;
                    entry.State = EntityState.Added;
                }
                accepted.Add((typeof(T), item.Id));
                wroteAny = true;
            }
            return wroteAny;
        }

        async Task<bool> UpsertProfileAsync()
        {
            if (changes.Profile is not { } profile)
                return false;
            profile.UpdatedAt = Normalize(profile.UpdatedAt, now);
            var current = await db.Profiles.FirstOrDefaultAsync(ct);
            if (current != null && profile.UpdatedAt < current.UpdatedAt)
            {
                rejected.Profile = current;
                return false;
            }
            var entry = db.Entry(current ?? profile);
            if (current == null)
                entry.Property(ApiDbContext.UserIdColumn).CurrentValue = user.Id;
            else
                entry.CurrentValues.SetValues(profile);
            entry.Property(ApiDbContext.VersionColumn).CurrentValue = version;
            if (current == null)
                entry.State = EntityState.Added;
            accepted.Add((typeof(UserProfile), ""));
            return true;
        }
    }

    /// <summary>
    /// Returns records with a version in (since, cursor]. Pages on version boundaries so a client never
    /// sees half of one push, and never more than <see cref="SyncLimits.BatchSize"/> of each kind.
    /// </summary>
    async Task<SyncResponse> PullAsync(long current, long since, HashSet<(Type, string)> accepted, SyncChanges rejected, CancellationToken ct)
    {
        // A cursor from the future means the server data was reset; start the client over.
        if (since > current)
            since = 0;

        var counts = new[]
        {
            await CountsAsync(db.Plans), await CountsAsync(db.Sessions), await CountsAsync(db.CustomExercises),
            await CountsAsync(db.BodyWeights), await CountsAsync(db.Profiles),
        };
        var totals = new int[counts.Length];
        var cursor = since;
        var hasMore = false;
        foreach (var v in counts.SelectMany(c => c.Keys).Distinct().Order())
        {
            if (cursor > since && Enumerable.Range(0, counts.Length).Any(k => totals[k] + counts[k].GetValueOrDefault(v) > SyncLimits.BatchSize))
            {
                hasMore = true;
                break;
            }
            for (var k = 0; k < counts.Length; k++)
                totals[k] += counts[k].GetValueOrDefault(v);
            cursor = v;
        }
        if (!hasMore)
            cursor = current;

        var response = new SyncResponse { Cursor = cursor, HasMore = hasMore };
        var c = response.Changes;
        c.Plans = Merge(await InRangeAsync(db.Plans), rejected.Plans);
        c.Sessions = Merge(await InRangeAsync(db.Sessions), rejected.Sessions);
        c.CustomExercises = Merge(await InRangeAsync(db.CustomExercises), rejected.CustomExercises);
        c.BodyWeights = Merge(await InRangeAsync(db.BodyWeights), rejected.BodyWeights);
        c.Profile = rejected.Profile;
        if (c.Profile == null && !accepted.Contains((typeof(UserProfile), "")))
            c.Profile = (await InRangeAsync(db.Profiles)).FirstOrDefault();
        return response;

        Task<Dictionary<long, int>> CountsAsync<T>(DbSet<T> set) where T : class =>
            set.Where(e => EF.Property<long>(e, ApiDbContext.VersionColumn) > since)
                .GroupBy(e => EF.Property<long>(e, ApiDbContext.VersionColumn))
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        Task<List<T>> InRangeAsync<T>(DbSet<T> set) where T : class =>
            set.AsNoTracking()
                .Where(e => EF.Property<long>(e, ApiDbContext.VersionColumn) > since && EF.Property<long>(e, ApiDbContext.VersionColumn) <= cursor)
                .ToListAsync(ct);

        // The client already has what it just pushed; send everything else, plus the winning copy of anything it lost.
        List<T> Merge<T>(List<T> pulled, List<T> lost) where T : ISyncEntity =>
            [.. pulled.Where(e => !accepted.Contains((typeof(T), e.Id)) && lost.All(l => l.Id != e.Id)), .. lost];
    }

    /// <summary>
    /// Timestamps come from client clocks. One set in the future would win every conflict until then, so they
    /// are capped at the server's now. Postgres stores microseconds, so drop the rest to compare exactly.
    /// </summary>
    static DateTimeOffset Normalize(DateTimeOffset value, DateTimeOffset now)
    {
        var utc = Truncate(value.ToUniversalTime());
        return utc > now ? now : utc;
    }

    static DateTimeOffset Truncate(DateTimeOffset t) => new(t.UtcTicks - t.UtcTicks % 10, TimeSpan.Zero);
}
