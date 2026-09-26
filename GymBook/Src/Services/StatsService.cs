using System.Globalization;
using GymBook.Models;

namespace GymBook.Services;

public record ChartPoint(string Label, double Value);

public record PersonalRecord(Exercise Exercise, double E1RmKg, double WeightKg, int Reps, DateTime Date);

public class StatsService(DataStore store, RecoveryService recovery)
{
    public static DateTime WeekStart(DateTime d)
    {
        var diff = (7 + (d.DayOfWeek - DayOfWeek.Monday)) % 7;
        return d.Date.AddDays(-diff);
    }

    public double SetVolume(Exercise? ex, SetEntry s)
    {
        var load = s.WeightKg + (ex?.IsBodyweight == true ? store.Profile.BodyWeightKg : 0);
        return load * s.Reps;
    }

    public double SessionVolume(WorkoutSession session) =>
        session.Exercises.Sum(e =>
        {
            var ex = store.GetExercise(e.ExerciseId);
            return e.Sets.Where(s => s.IsCompleted && !s.IsWarmup).Sum(s => SetVolume(ex, s));
        });

    public List<ChartPoint> WeeklyVolume(int weeks) => Weekly(weeks, list => list.Sum(SessionVolume));

    public List<ChartPoint> WorkoutsPerWeek(int weeks) => Weekly(weeks, list => list.Count);

    List<ChartPoint> Weekly(int weeks, Func<List<WorkoutSession>, double> value)
    {
        var start = WeekStart(DateTime.Today).AddDays(-7 * (weeks - 1));
        var sessions = store.History.Where(s => s.StartedAt >= start).ToList();
        return Enumerable.Range(0, weeks).Select(i =>
        {
            var from = start.AddDays(7 * i);
            var to = from.AddDays(7);
            var inWeek = sessions.Where(s => s.StartedAt >= from && s.StartedAt < to).ToList();
            return new ChartPoint(from.ToString("d MMM", CultureInfo.CurrentCulture), value(inWeek));
        }).ToList();
    }

    public Dictionary<MuscleGroup, double> SetsPerMuscleSince(DateTime from)
    {
        var total = Enum.GetValues<MuscleGroup>().ToDictionary(m => m, _ => 0.0);
        foreach (var s in store.History.Where(s => s.StartedAt >= from))
            foreach (var (m, sets) in recovery.SetsPerMuscle(s))
                total[m] += sets;
        return total;
    }

    public List<ChartPoint> E1RmHistory(string exerciseId) =>
        store.History.Reverse()
            .Select(s => (s.StartedAt, best: s.Exercises.Where(e => e.ExerciseId == exerciseId)
                .SelectMany(e => e.Sets).Where(x => x.IsCompleted && !x.IsWarmup)
                .Select(x => ProgressionEngine.E1Rm(x.WeightKg, x.Reps, x.Rir)).DefaultIfEmpty(0).Max()))
            .Where(p => p.best > 0)
            .Select(p => new ChartPoint(p.StartedAt.ToString("d MMM", CultureInfo.CurrentCulture), p.best))
            .ToList();

    public PersonalRecord? BestFor(string exerciseId, string? excludeSessionId = null)
    {
        PersonalRecord? best = null;
        foreach (var s in store.History.Where(s => s.Id != excludeSessionId))
            foreach (var e in s.Exercises.Where(e => e.ExerciseId == exerciseId))
                foreach (var set in e.Sets.Where(x => x.IsCompleted && !x.IsWarmup))
                {
                    var e1 = ProgressionEngine.E1Rm(set.WeightKg, set.Reps, set.Rir);
                    if (best == null || e1 > best.E1RmKg)
                        best = new PersonalRecord(store.GetExercise(exerciseId)!, e1, set.WeightKg, set.Reps, s.StartedAt);
                }
        return best;
    }

    public List<PersonalRecord> PersonalRecords() =>
        store.History.SelectMany(s => s.Exercises.Select(e => e.ExerciseId)).Distinct()
            .Where(id => store.GetExercise(id) is { IsBodyweight: false })
            .Select(id => BestFor(id))
            .OfType<PersonalRecord>()
            .Where(r => r.E1RmKg > 0)
            .OrderByDescending(r => r.Date)
            .ToList();

    /// <summary>Exercises in the session whose best estimated 1RM beats everything before it.</summary>
    public List<PersonalRecord> RecordsIn(WorkoutSession session)
    {
        var list = new List<PersonalRecord>();
        foreach (var e in session.Exercises)
        {
            var ex = store.GetExercise(e.ExerciseId);
            if (ex == null || ex.IsBodyweight)
                continue;
            var top = e.Sets.Where(s => s.IsCompleted && !s.IsWarmup)
                .Select(s => (s, e1: ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir)))
                .OrderByDescending(x => x.e1).FirstOrDefault();
            if (top.s == null || top.e1 <= 0)
                continue;
            var previous = store.History.Where(s => s.Id != session.Id && s.StartedAt < session.StartedAt)
                .SelectMany(s => s.Exercises.Where(x => x.ExerciseId == e.ExerciseId))
                .SelectMany(x => x.Sets.Where(s => s.IsCompleted && !s.IsWarmup))
                .Select(s => ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir))
                .DefaultIfEmpty(0).Max();
            if (previous > 0 && top.e1 > previous)
                list.Add(new PersonalRecord(ex, top.e1, top.s.WeightKg, top.s.Reps, session.StartedAt));
        }
        return list;
    }

    /// <summary>Workouts per week the user aims for: the profile's training days or the active plan's, whichever is higher.</summary>
    public int WeeklyTarget => Math.Max(1, Math.Max(store.Profile.DaysPerWeek, store.ActivePlan?.DaysPerWeek ?? 0));

    /// <summary>
    /// Consecutive weeks that hit <see cref="WeeklyTarget"/>. The current week only adds to the streak once it
    /// hits the target; until then the streak runs up to last week, so an unfinished week doesn't break it.
    /// Past weeks are judged against today's target, since the target at the time isn't recorded.
    /// </summary>
    public int WeekStreak()
    {
        var target = WeeklyTarget;
        var perWeek = store.History.GroupBy(s => WeekStart(s.StartedAt)).ToDictionary(g => g.Key, g => g.Count());
        bool Hit(DateTime week) => perWeek.GetValueOrDefault(week) >= target;

        var week = WeekStart(DateTime.Today);
        if (!Hit(week))
            week = week.AddDays(-7);
        var streak = 0;
        while (Hit(week))
        {
            streak++;
            week = week.AddDays(-7);
        }
        return streak;
    }
}
