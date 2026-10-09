namespace GymBook.Models;

/// <summary>Why XP was earned or lost (see <see cref="Levels"/>).</summary>
public enum XpReason { Workout, WeekGoal, Record, RepRecord, MissedWorkout, EgoLift }

/// <summary>XP earned (or, negative, lost) at <paramref name="At"/>; the exercise for a record or an ego lift.</summary>
public record XpEvent(DateTime At, int Amount, XpReason Reason, string? SessionId = null, string? ExerciseId = null);

/// <summary>A league: every level from <paramref name="FromLevel"/> until the next league's.</summary>
public record League(string Name, int FromLevel);

/// <summary>Where XP stands: the level, how far into it, and what it takes to reach the next.</summary>
public record LevelStatus(int Xp, int Level, int IntoLevel, int LevelSize, League League)
{
    public double Progress => LevelSize == 0 ? 0 : (double)IntoLevel / LevelSize;
    public int ToNext => LevelSize - IntoLevel;
}

/// <summary>
/// Experience: XP earned for training and lost for real misses, worked out from the finished workouts alone (so every
/// device agrees and nothing extra is stored or synced). Levels never end; every 50 levels is a league.
/// <list type="bullet">
/// <item>Each finished workout: <see cref="WorkoutXp"/>, plus <see cref="SetXp"/> a working set (up to <see cref="MaxSetsCounted"/>).</item>
/// <item>A week with as many workouts as planned: <see cref="WeekGoalXp"/>, as soon as the last one is done.</item>
/// <item>An exercise's best estimated one-rep max ever: <see cref="RecordXp"/>; otherwise more reps than ever at a weight
/// (or heavier): <see cref="RepRecordXp"/>. The first workout with an exercise only sets the mark.</item>
/// <item>A finished week short of its planned workouts: <see cref="MissedXp"/> each one missed. Not counted after two empty
/// weeks in a row (a break) until training starts again.</item>
/// <item>Going up in weight but falling short of the exercise's minimum reps (an ego lift): <see cref="EgoLiftXp"/>, and no
/// record for it. Going lighter or doing fewer reps otherwise never costs XP, so deloads and easier days are safe.</item>
/// </list>
/// XP never goes below zero.
/// </summary>
public static class Levels
{
    public const int WorkoutXp = 100, SetXp = 5, MaxSetsCounted = 30, WeekGoalXp = 75, RecordXp = 50, RepRecordXp = 20;
    public const int MissedXp = -40, EgoLiftXp = -30;
    /// <summary>Empty weeks in a row that still cost XP; after them it's a break.</summary>
    const int PenalizedEmptyWeeks = 2;

    public static readonly League[] Leagues =
    [
        new("Rookie", 1), new("Iron", 50), new("Steel", 100), new("Titanium", 150),
        new("Titan", 200), new("Olympian", 250), new("Legend", 300), new("Mythic", 350),
    ];

    public static League LeagueOf(int level) => Leagues.Last(l => l.FromLevel <= level);

    /// <summary>XP from <paramref name="level"/> to the next: 100 at first, 10 more each level up to level 100, then flat.</summary>
    public static int LevelSize(int level) => 100 + 10 * (Math.Clamp(level, 1, 100) - 1);

    /// <summary>XP needed to reach <paramref name="level"/> from nothing.</summary>
    public static int XpToReach(int level)
    {
        var xp = 0;
        for (var l = 1; l < level; l++)
            xp += LevelSize(l);
        return xp;
    }

    public static LevelStatus StatusOf(int xp)
    {
        xp = Math.Max(0, xp);
        var (level, left) = (1, xp);
        while (left >= LevelSize(level))
            left -= LevelSize(level++);
        return new LevelStatus(xp, level, left, LevelSize(level), LeagueOf(level));
    }

    /// <summary>The total after every event in order, never below zero along the way.</summary>
    public static int Total(IEnumerable<XpEvent> events) => events.Aggregate(0, (total, e) => Math.Max(0, total + e.Amount));

    /// <summary>
    /// Every XP event from <paramref name="sessions"/> (finished ones count), oldest first, up to <paramref name="now"/>.
    /// <paramref name="planOf"/> finds a workout's plan, for the workouts a week it plans; without one,
    /// <paramref name="defaultDaysPerWeek"/>.
    /// </summary>
    public static List<XpEvent> Events(IEnumerable<WorkoutSession> sessions, Func<string?, WorkoutPlan?> planOf, int defaultDaysPerWeek, DateTime now)
    {
        var done = sessions
            .Where(s => !s.IsDeleted && s.EndedAt != null && s.StartedAt <= now && s.WorkingSets.Any())
            .OrderBy(s => s.StartedAt)
            .ToList();
        var events = new List<XpEvent>();
        if (done.Count == 0)
            return events;

        // Every exercise's sets before the workout being looked at, and its last workout's heaviest set.
        var history = new Dictionary<string, List<SetEntry>>();
        var lastTop = new Dictionary<string, double>();
        foreach (var session in done)
        {
            var at = session.EndedAt!.Value;
            events.Add(new(at, WorkoutXp + SetXp * Math.Min(MaxSetsCounted, session.WorkingSets.Count()), XpReason.Workout, session.Id));
            foreach (var group in session.Exercises.GroupBy(e => e.ExerciseId))
            {
                var sets = group.SelectMany(e => e.Sets).Where(s => s.IsCompleted && !s.IsWarmup).ToList();
                if (sets.Count == 0)
                    continue;
                if (history.TryGetValue(group.Key, out var before) && Judge(group.ToList(), sets, before, lastTop[group.Key]) is { } reason)
                    events.Add(new(at, Amount(reason), reason, session.Id, group.Key));
                history[group.Key] = [.. history.GetValueOrDefault(group.Key) ?? [], .. sets];
                lastTop[group.Key] = sets.Max(s => s.WeightKg);
            }
        }
        events.AddRange(Weeks(done, planOf, defaultDaysPerWeek, now));
        return [.. events.OrderBy(e => e.At)];
    }

    static int Amount(XpReason reason) => reason switch
    {
        XpReason.Record => RecordXp,
        XpReason.RepRecord => RepRecordXp,
        _ => EgoLiftXp,
    };

    /// <summary>An exercise in one workout against every earlier set of it: an ego lift, a record, or nothing.</summary>
    static XpReason? Judge(List<SessionExercise> entries, List<SetEntry> sets, List<SetEntry> before, double lastTop)
    {
        // Heavier than last time, and short of the minimum reps at that weight.
        var top = sets.Max(s => s.WeightKg);
        var repMin = entries.Min(e => e.RepMin);
        if (top > lastTop + 0.01 && sets.Any(s => s.WeightKg >= top - 0.01 && s.Reps < repMin))
            return XpReason.EgoLift;

        static double Best(IEnumerable<SetEntry> s) =>
            s.Where(x => x.Reps + (x.Rir ?? 0) <= Ranks.MaxReps).Select(x => Ranks.E1Rm(x.WeightKg, x.Reps, x.Rir)).DefaultIfEmpty(0).Max();
        var best = Best(sets);
        if (best > 0 && best > Best(before) + 0.01)
            return XpReason.Record;
        // More reps at a weight than ever at it or anything heavier.
        if (sets.Any(s => s.Reps > 0 && !before.Any(b => b.WeightKg >= s.WeightKg - 0.01 && b.Reps >= s.Reps)))
            return XpReason.RepRecord;
        return null;
    }

    /// <summary>The week's goal met, or the workouts it missed, Monday to Sunday.</summary>
    static IEnumerable<XpEvent> Weeks(List<WorkoutSession> done, Func<string?, WorkoutPlan?> planOf, int defaultDaysPerWeek, DateTime now)
    {
        var week = WeekStart(done[0].StartedAt);
        var emptyRun = 0;
        WorkoutSession? latest = null;
        for (; week <= now; week = week.AddDays(7))
        {
            var end = week.AddDays(7);
            var inWeek = done.Where(s => s.StartedAt >= week && s.StartedAt < end).ToList();
            latest = inWeek.LastOrDefault() ?? latest;
            var target = Planned(planOf(latest?.PlanId), defaultDaysPerWeek);
            if (inWeek.Count >= target)
            {
                emptyRun = 0;
                yield return new(inWeek[target - 1].EndedAt!.Value, WeekGoalXp, XpReason.WeekGoal);
                continue;
            }
            // The week in progress isn't missed yet.
            if (end > now)
                yield break;
            emptyRun = inWeek.Count == 0 ? emptyRun + 1 : 0;
            if (emptyRun <= PenalizedEmptyWeeks)
                yield return new(end.AddSeconds(-1), MissedXp * (target - inWeek.Count), XpReason.MissedWorkout);
        }
    }

    /// <summary>Workouts a week: the plan's, else its number of workouts, else the profile's.</summary>
    static int Planned(WorkoutPlan? plan, int defaultDaysPerWeek) => Math.Clamp(
        plan is { DaysPerWeek: > 0 } ? plan.DaysPerWeek : plan is { Workouts.Count: > 0 } ? plan.Workouts.Count : defaultDaysPerWeek, 1, 7);

    static DateTime WeekStart(DateTime at) => at.Date.AddDays(-(((int)at.DayOfWeek + 6) % 7));
}
