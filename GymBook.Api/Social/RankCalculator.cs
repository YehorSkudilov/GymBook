using GymBook.Api.Admin;
using GymBook.Api.Data;
using GymBook.Models;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Social;

/// <summary>
/// Works out a ranked user's lifts (see <see cref="Ranks"/>) from their synced workouts of the last year, after each sync
/// and on joining. Guards against typos and cheating: sets past <see cref="Ranks.MaxReps"/> or past world-record
/// strength are ignored, a lift needs <see cref="Ranks.MinSessions"/> workouts, and a best more than
/// <see cref="Ranks.MaxJump"/> over the lift's other workouts is held back until another workout backs it up.
/// </summary>
public class RankCalculator(ApiDbContext db, TimeProvider time, ILogger<RankCalculator> log)
{
    /// <summary>Updates <paramref name="userId"/>'s lifts and score if they're in the ranks. Saves.</summary>
    public async Task RefreshAsync(string userId, CancellationToken ct)
    {
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is not { InRanks: true, RankSex: { } sex })
            return;

        var since = time.GetUtcNow().UtcDateTime.AddDays(-365);
        var sessions = await db.Sessions.OwnedBy(userId).AsNoTracking()
            .Where(s => !s.IsDeleted && s.EndedAt != null && s.StartedAt >= since)
            .ToListAsync(ct);
        var bodyWeight = await BodyWeightAsync(userId, ct);

        var existing = await db.RankedLifts.Where(l => l.UserId == userId).ToDictionaryAsync(l => l.Lift, ct);
        var scores = new List<double>();
        foreach (var lift in Ranks.Lifts)
        {
            var row = existing.GetValueOrDefault(lift) ?? db.RankedLifts.Add(new RankedLift { UserId = userId, Lift = lift }).Entity;
            Compute(row, lift, sex, bodyWeight, sessions);
            scores.Add(row.Ranked ? row.Score : 0);
        }
        profile.Score = Math.Round(Ranks.Overall(scores), 2);
        profile.RanksUpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>After a sync: refreshes only when the user is ranked, and never fails the sync.</summary>
    public async Task RefreshQuietlyAsync(string userId, CancellationToken ct)
    {
        try
        {
            await RefreshAsync(userId, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogWarning(e, "Couldn't update the ranks of {User}", userId);
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>The latest weigh-in of the last 90 days, otherwise the profile's; null when neither is believable.</summary>
    async Task<double?> BodyWeightAsync(string userId, CancellationToken ct)
    {
        var recent = time.GetUtcNow().UtcDateTime.AddDays(-90);
        var weighIn = await db.BodyWeights.OwnedBy(userId)
            .Where(b => !b.IsDeleted && b.Date >= recent)
            .OrderByDescending(b => b.Date)
            .Select(b => (double?)b.WeightKg)
            .FirstOrDefaultAsync(ct);
        var kg = weighIn ?? await db.Profiles.OwnedBy(userId).Select(p => (double?)p.BodyWeightKg).FirstOrDefaultAsync(ct);
        return kg is >= 30 and <= 250 ? kg : null;
    }

    static void Compute(RankedLift row, RankLift lift, Sex sex, double? bodyWeight, List<WorkoutSession> sessions)
    {
        // Each workout's best believable set of the lift.
        var bests = new List<(double E1Rm, SetEntry Set, DateTime Date)>();
        foreach (var session in sessions)
        {
            var best = session.Exercises
                .Where(e => Ranks.LiftOf(e.ExerciseId) == lift)
                .SelectMany(e => e.Sets)
                .Where(s => s is { IsCompleted: true, IsWarmup: false, IsSkipped: false, WeightKg: > 0, Reps: > 0 } && s.Reps + (s.Rir ?? 0) <= Ranks.MaxReps)
                .Select(s => (E1Rm: Ranks.E1Rm(s.WeightKg, s.Reps, s.Rir), Set: s))
                .Where(x => bodyWeight == null || x.E1Rm / bodyWeight <= Ranks.MaxRatio(lift))
                .OrderByDescending(x => x.E1Rm)
                .FirstOrDefault();
            if (best.Set != null)
                bests.Add((best.E1Rm, best.Set, session.StartedAt));
        }

        row.Sessions = bests.Count;
        row.BodyWeightKg = bodyWeight ?? 0;
        if (bodyWeight is not { } bw || bests.Count < Ranks.MinSessions)
        {
            row.Ranked = false;
            row.E1RmKg = row.Ratio = row.Score = row.WeightKg = 0;
            row.Reps = 0;
            row.Date = null;
            return;
        }

        bests.Sort((a, b) => b.E1Rm.CompareTo(a.E1Rm));
        var top = bests[0];
        var cap = bests[1].E1Rm * Ranks.MaxJump;
        // A jump that no other workout backs up counts only as far as the cap, until one does.
        var e1Rm = Math.Min(top.E1Rm, cap);
        row.Ranked = true;
        row.E1RmKg = Math.Round(e1Rm, 1);
        row.Ratio = Math.Round(e1Rm / bw, 3);
        row.Score = Math.Round(Ranks.Score(lift, sex, e1Rm / bw), 2);
        row.WeightKg = top.Set.WeightKg;
        row.Reps = top.Set.Reps;
        row.Date = top.Date;
    }
}
