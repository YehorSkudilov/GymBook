using GymBook.Api.Admin;
using GymBook.Api.Data;
using GymBook.Models;
using GymBook.Services;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Social;

/// <summary>
/// Works out a ranked user's standing in each category (see <see cref="Ranks"/>) from their synced workouts, after each
/// sync, on joining, and when their ranks are asked for and are a day old. A strength category is the best of every
/// weighted exercise for it in the last year, built-in or the user's own; Consistency is workouts a week over the last
/// <see cref="Ranks.ConsistencyWeeks"/> weeks. Guards against typos and cheating, exercise by exercise: sets past
/// <see cref="Ranks.MaxReps"/> or past believable strength are ignored, an exercise needs <see cref="Ranks.MinSessions"/>
/// workouts, and a best more than <see cref="Ranks.MaxJump"/> over its other workouts is held back until another workout
/// backs it up.
/// </summary>
public class RankCalculator(ApiDbContext db, TimeProvider time, ILogger<RankCalculator> log)
{
    /// <summary>Updates <paramref name="userId"/>'s categories and score if they're in the ranks. Saves.</summary>
    public async Task RefreshAsync(string userId, CancellationToken ct)
    {
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is not { InRanks: true, RankSex: { } sex })
            return;

        // Sessions and weigh-ins are wall-clock times (timestamp without time zone): Npgsql won't compare one with a UTC
        // DateTime, so the cut-offs are Unspecified, like AdminQueries.WallClockDaysAgo. Hours off either way don't matter here.
        var since = WallClockDaysAgo(365);
        var sessions = await db.Sessions.OwnedBy(userId).AsNoTracking()
            .Where(s => !s.IsDeleted && s.EndedAt != null && s.StartedAt >= since)
            .ToListAsync(ct);
        var custom = await db.CustomExercises.OwnedBy(userId).AsNoTracking().Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Id, ct);
        var bodyWeight = await BodyWeightAsync(userId, ct);

        // Every rankable exercise's lift, best first within each category.
        var lifts = Lifts(sessions, id => custom.GetValueOrDefault(id) ?? ExerciseLibrary.Find(id), sex, bodyWeight);

        var existing = await db.RankedLifts.Where(l => l.UserId == userId).ToDictionaryAsync(l => l.Lift, ct);
        var scores = new List<double>();
        foreach (var category in Ranks.Categories)
        {
            var row = existing.GetValueOrDefault(category) ?? db.RankedLifts.Add(new RankedLift { UserId = userId, Lift = category }).Entity;
            if (category == RankCategory.Consistency)
                Consistency(row, sessions);
            else
                Strength(row, lifts.Where(l => l.Category == category).MaxBy(l => l.Score), bodyWeight);
            scores.Add(row.Ranked ? row.Score : 0);
        }
        profile.Score = Math.Round(Ranks.Overall(scores), 2);
        profile.RanksUpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    DateTime WallClockDaysAgo(int days) => DateTime.SpecifyKind(time.GetUtcNow().UtcDateTime.AddDays(-days), DateTimeKind.Unspecified);

    /// <summary>After a sync, or before showing ranks: refreshes only when the user is ranked, and never fails the caller.</summary>
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
        var recent = WallClockDaysAgo(90);
        var weighIn = await db.BodyWeights.OwnedBy(userId)
            .Where(b => !b.IsDeleted && b.Date >= recent)
            .OrderByDescending(b => b.Date)
            .Select(b => (double?)b.WeightKg)
            .FirstOrDefaultAsync(ct);
        var kg = weighIn ?? await db.Profiles.OwnedBy(userId).Select(p => (double?)p.BodyWeightKg).FirstOrDefaultAsync(ct);
        return kg is >= 30 and <= 250 ? kg : null;
    }

    /// <summary>A rankable exercise's believable best, and what it scores.</summary>
    record Lift(RankCategory Category, string ExerciseId, double E1Rm, SetEntry Set, DateTime Date, int Sessions, double Score);

    /// <summary>
    /// The best of each rankable exercise in <paramref name="sessions"/> that has been done in enough workouts: each
    /// workout's best believable set, the top one held to <see cref="Ranks.MaxJump"/> over the next until another workout
    /// backs it up. None without a believable body weight, which every score is relative to.
    /// </summary>
    static List<Lift> Lifts(List<WorkoutSession> sessions, Func<string, Exercise?> exercise, Sex sex, double? bodyWeight)
    {
        if (bodyWeight is not { } bw)
            return [];
        var bests = new Dictionary<string, (Exercise Exercise, List<(double E1Rm, SetEntry Set, DateTime Date)> Workouts)>();
        foreach (var session in sessions)
        {
            foreach (var group in session.Exercises.GroupBy(e => e.ExerciseId))
            {
                if (exercise(group.Key) is not { } ex || !Ranks.IsRankable(ex.Id, ex.Equipment, ex.Mechanic, ex.PrimaryMuscle))
                    continue;
                var max = Ranks.MaxExerciseRatio(ex.Id, ex.Equipment, ex.Mechanic, ex.PrimaryMuscle) * bw;
                var best = group.SelectMany(e => e.Sets)
                    .Where(s => s is { IsCompleted: true, IsWarmup: false, IsSkipped: false, WeightKg: > 0, Reps: > 0 } && s.Reps + (s.Rir ?? 0) <= Ranks.MaxReps)
                    .Select(s => (E1Rm: Ranks.E1Rm(s.WeightKg, s.Reps, s.Rir), Set: s))
                    .Where(x => x.E1Rm <= max)
                    .OrderByDescending(x => x.E1Rm)
                    .FirstOrDefault();
                if (best.Set == null)
                    continue;
                // Keyed by today's id, so an exercise logged under an id from an earlier library counts as one.
                if (!bests.TryGetValue(ex.Id, out var entry))
                    bests[ex.Id] = entry = (ex, []);
                entry.Workouts.Add((best.E1Rm, best.Set, session.StartedAt));
            }
        }

        var lifts = new List<Lift>();
        foreach (var (ex, workouts) in bests.Values)
        {
            if (workouts.Count < Ranks.MinSessions || Ranks.CategoryOf(ex.PrimaryMuscle) is not { } category)
                continue;
            workouts.Sort((a, b) => b.E1Rm.CompareTo(a.E1Rm));
            var top = workouts[0];
            // A jump that no other workout backs up counts only as far as the cap, until one does.
            var e1Rm = Math.Min(top.E1Rm, workouts[1].E1Rm * Ranks.MaxJump);
            var score = Ranks.ExerciseScore(ex.Id, ex.Equipment, ex.Mechanic, ex.PrimaryMuscle, sex, e1Rm / bw);
            lifts.Add(new Lift(category, ex.Id, e1Rm, top.Set, top.Date, workouts.Count, score));
        }
        return lifts;
    }

    /// <summary>A strength category: its best exercise's lift, or unranked with none.</summary>
    static void Strength(RankedLift row, Lift? best, double? bodyWeight)
    {
        row.BodyWeightKg = bodyWeight ?? 0;
        if (best == null || bodyWeight is not { } bw)
        {
            row.Ranked = false;
            row.ExerciseId = null;
            row.E1RmKg = row.Ratio = row.Score = row.WeightKg = 0;
            row.Reps = row.Sessions = 0;
            row.Date = null;
            return;
        }
        row.Ranked = true;
        row.ExerciseId = best.ExerciseId;
        row.E1RmKg = Math.Round(best.E1Rm, 1);
        row.Ratio = Math.Round(best.E1Rm / bw, 3);
        row.Score = Math.Round(best.Score, 2);
        row.WeightKg = best.Set.WeightKg;
        row.Reps = best.Set.Reps;
        row.Date = best.Date;
        row.Sessions = best.Sessions;
    }

    /// <summary>Consistency: workouts a week over the last <see cref="Ranks.ConsistencyWeeks"/> weeks, in <see cref="RankedLift.Ratio"/>.</summary>
    void Consistency(RankedLift row, List<WorkoutSession> sessions)
    {
        var since = WallClockDaysAgo(7 * Ranks.ConsistencyWeeks);
        var count = sessions.Count(s => s.StartedAt >= since);
        var perWeek = count / (double)Ranks.ConsistencyWeeks;
        row.Ranked = count > 0;
        row.ExerciseId = null;
        row.Sessions = count;
        row.Ratio = Math.Round(perWeek, 2);
        row.Score = Math.Round(Ranks.ConsistencyScore(perWeek), 2);
        row.E1RmKg = row.WeightKg = row.BodyWeightKg = 0;
        row.Reps = 0;
        row.Date = sessions.Where(s => s.StartedAt >= since).Select(s => (DateTime?)s.StartedAt).Max();
    }
}
