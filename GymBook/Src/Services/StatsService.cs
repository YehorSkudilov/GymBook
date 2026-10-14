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

    /// <summary>The workouts from <paramref name="first"/> to <paramref name="last"/>, both days included.</summary>
    public List<WorkoutSession> SessionsIn(DateTime first, DateTime last) =>
        [.. store.History.Where(s => s.StartedAt >= first.Date && s.StartedAt < last.Date.AddDays(1))];

    public List<ChartPoint> Volume(DateTime first, DateTime last) => Grouped(first, last, list => list.Sum(SessionVolume));

    public List<ChartPoint> Workouts(DateTime first, DateTime last) => Grouped(first, last, list => list.Count);

    /// <summary>Average workout length in minutes, per day, week or month with any workouts (the empty ones left out).</summary>
    public List<ChartPoint> Minutes(DateTime first, DateTime last) =>
        Grouped(first, last, list => list.Count == 0 ? 0 : list.Average(s => s.Duration.TotalMinutes)).Where(p => p.Value > 0).ToList();

    /// <summary>The Monday of <paramref name="first"/>'s week, and the volume of every day from it to <paramref name="last"/>.</summary>
    public (DateTime FirstMonday, List<double> Days) DailyVolume(DateTime first, DateTime last)
    {
        var start = WeekStart(first);
        var byDay = SessionsIn(start, last).GroupBy(s => s.StartedAt.Date).ToDictionary(g => g.Key, g => g.Sum(SessionVolume));
        var count = (last.Date - start).Days + 1;
        return (start, Enumerable.Range(0, count).Select(i => byDay.GetValueOrDefault(start.AddDays(i))).ToList());
    }

    /// <summary>How a range's charts group it: by day up to a fortnight, by week up to about four months, by month beyond.</summary>
    public static Period PeriodFor(DateTime first, DateTime last) => ((last.Date - first.Date).Days + 1) switch
    {
        <= 14 => Period.Day,
        <= 120 => Period.Week,
        _ => Period.Month,
    };

    /// <summary>
    /// The range's days, weeks or months (see <see cref="PeriodFor"/>), oldest first: each one's start, the day after its
    /// end, and its label. Weeks start on a Monday and months on the 1st, so the first can start before the range does.
    /// </summary>
    public static List<(DateTime From, DateTime To, string Label)> Periods(DateTime first, DateTime last)
    {
        var period = PeriodFor(first, last);
        var from = period switch
        {
            Period.Day => first.Date,
            Period.Week => WeekStart(first),
            _ => new DateTime(first.Year, first.Month, 1),
        };
        var format = period switch
        {
            Period.Day when (last.Date - first.Date).Days < 7 => "ddd",
            Period.Month => from.Year == last.Year ? "MMM" : "MMM yy",
            _ => "d MMM",
        };
        var list = new List<(DateTime, DateTime, string)>();
        while (from <= last.Date)
        {
            var to = period switch { Period.Day => from.AddDays(1), Period.Week => from.AddDays(7), _ => from.AddMonths(1) };
            list.Add((from, to, from.ToString(format, CultureInfo.CurrentCulture)));
            from = to;
        }
        return list;
    }

    List<ChartPoint> Grouped(DateTime first, DateTime last, Func<List<WorkoutSession>, double> value)
    {
        var periods = Periods(first, last);
        var sessions = SessionsIn(periods[0].From, last);
        return [.. periods.Select(p => new ChartPoint(p.Label, value([.. sessions.Where(s => s.StartedAt >= p.From && s.StartedAt < p.To)])))];
    }

    public Dictionary<MuscleGroup, double> SetsPerMuscleSince(DateTime from) => SetsPerMuscle(store.History.Where(s => s.StartedAt >= from));

    /// <summary>Hard sets per muscle across <paramref name="sessions"/> (e.g. one day's, on Progress's Day tab).</summary>
    public Dictionary<MuscleGroup, double> SetsPerMuscle(IEnumerable<WorkoutSession> sessions)
    {
        var total = Enum.GetValues<MuscleGroup>().ToDictionary(m => m, _ => 0.0);
        foreach (var s in sessions)
            foreach (var (m, sets) in recovery.SetsPerMuscle(s))
                total[m] += sets;
        return total;
    }

    public Dictionary<SubMuscle, double> SetsPerPartSince(DateTime from) => SetsPerPart(store.History.Where(s => s.StartedAt >= from));

    /// <summary>Like <see cref="SetsPerMuscle"/>, by the part of each muscle (front, side and rear delts, …; see <see cref="SubMuscles"/>).</summary>
    public Dictionary<SubMuscle, double> SetsPerPart(IEnumerable<WorkoutSession> sessions)
    {
        var total = Enum.GetValues<SubMuscle>().ToDictionary(m => m, _ => 0.0);
        foreach (var s in sessions)
            foreach (var (m, sets) in recovery.SetsPerPart(s))
                total[m] += sets;
        return total;
    }

    /// <summary>Each session's best estimated 1RM for the exercise, oldest first: all of them, or those from <paramref name="first"/> to <paramref name="last"/>.</summary>
    public List<ChartPoint> E1RmHistory(string exerciseId, DateTime? first = null, DateTime? last = null) =>
        [.. BestPerSession(exerciseId)
            .Where(p => (first == null || p.Date >= first.Value.Date) && (last == null || p.Date < last.Value.Date.AddDays(1)))
            .Select(p => new ChartPoint(p.Date.ToString("d MMM", CultureInfo.CurrentCulture), p.E1Rm))];

    /// <summary>The loaded lifts trained in the most sessions, most first: the ones worth charting strength for.</summary>
    public List<Exercise> TrackedLifts(int max) =>
        [.. store.History
            .SelectMany(s => s.Exercises.Where(e => e.Sets.Any(x => x.IsCompleted && !x.IsWarmup && x.WeightKg > 0)).Select(e => e.ExerciseId).Distinct())
            .GroupBy(id => id)
            .Select(g => (Exercise: store.GetExercise(g.Key), Sessions: g.Count()))
            .Where(x => x.Exercise is { IsBodyweight: false })
            .OrderByDescending(x => x.Sessions)
            .Take(max)
            .Select(x => x.Exercise!)];

    /// <summary>Each session's best estimated 1RM for the exercise, oldest first.</summary>
    List<(DateTime Date, double E1Rm)> BestPerSession(string exerciseId) =>
        [.. store.History.Reverse()
            .Select(s => (s.StartedAt, best: s.Exercises.Where(e => e.ExerciseId == exerciseId)
                .SelectMany(e => e.Sets).Where(x => x.IsCompleted && !x.IsWarmup)
                .Select(x => ProgressionEngine.E1Rm(x.WeightKg, x.Reps, x.Rir)).DefaultIfEmpty(0).Max()))
            .Where(p => p.best > 0)];

    /// <summary>
    /// Overall strength over the range, a point a day, week or month (see <see cref="Periods"/>): for each of
    /// <paramref name="lifts"/>, the latest estimated 1RM by that period's end against its first one, averaged, with 100
    /// as where the lifts started (110 = 10% stronger). Periods before any of the lifts were trained are left out.
    /// </summary>
    public List<ChartPoint> StrengthTrend(IReadOnlyList<Exercise> lifts, DateTime first, DateTime last)
    {
        var histories = lifts.Select(l => BestPerSession(l.Id)).Where(h => h.Count > 0).ToList();
        var points = new List<ChartPoint>();
        foreach (var (_, end, label) in Periods(first, last))
        {
            var ratios = histories
                .Select(h => (First: h[0].E1Rm, Latest: h.LastOrDefault(p => p.Date < end)))
                .Where(x => x.Latest.E1Rm > 0)
                .Select(x => x.Latest.E1Rm / x.First)
                .ToList();
            if (ratios.Count > 0)
                points.Add(new ChartPoint(label, ratios.Average() * 100));
        }
        return points;
    }

    /// <summary>How a lift's estimated 1RM changed over a range: its first session there then against the better of its last two (so one off day doesn't hide the trend).</summary>
    public record LiftGain(Exercise Exercise, double FromKg, double ToKg, int Sessions)
    {
        public double Percent => FromKg > 0 ? (ToKg / FromKg - 1) * 100 : 0;
    }

    /// <summary>The lifts trained at least twice from <paramref name="first"/> to <paramref name="last"/>, biggest gain first.</summary>
    public List<LiftGain> Gains(DateTime first, DateTime last, int max)
    {
        return [.. TrackedLifts(30)
            .Select(l => (Lift: l, History: BestPerSession(l.Id).Where(p => p.Date >= first.Date && p.Date < last.Date.AddDays(1)).ToList()))
            .Where(x => x.History.Count >= 2)
            .Select(x => new LiftGain(x.Lift, x.History[0].E1Rm, x.History.TakeLast(2).Max(p => p.E1Rm), x.History.Count))
            .OrderByDescending(g => g.Percent)
            .Take(max)];
    }

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

    /// <summary>
    /// Workouts per week the user aims for: the active plan's workouts (its rest days don't count), or without a plan the
    /// profile's training days. Counted from the workouts themselves, not the plan's stored day count, which edits to
    /// its days don't keep up to date.
    /// </summary>
    public int WeeklyTarget => Math.Max(1, store.ActivePlan is { Workouts.Count: > 0 } plan ? plan.Workouts.Count : store.Profile.DaysPerWeek);

    /// <summary>
    /// Consecutive weeks that hit <see cref="WeeklyTarget"/>. The current week only adds to the streak once it
    /// hits the target; until then the streak runs up to last week, so an unfinished week doesn't break it.
    /// Past weeks are judged against today's target, since the target at the time isn't recorded.
    /// </summary>
    public int WeekStreak() => WeekStreakOn(DateTime.Today);

    /// <summary>
    /// The week streak as it stood on <paramref name="day"/>: its week counts once it had hit the target by then (its
    /// workouts after that day aren't counted), else the streak runs up to the week before.
    /// </summary>
    public int WeekStreakOn(DateTime day)
    {
        var target = WeeklyTarget;
        var thisWeek = WeekStart(day);
        var perWeek = store.History.Where(s => s.StartedAt.Date <= day.Date)
            .GroupBy(s => WeekStart(s.StartedAt)).ToDictionary(g => g.Key, g => g.Count());
        bool Hit(DateTime week) => perWeek.GetValueOrDefault(week) >= target;

        var week = Hit(thisWeek) ? thisWeek : thisWeek.AddDays(-7);
        var streak = 0;
        while (Hit(week))
        {
            streak++;
            week = week.AddDays(-7);
        }
        return streak;
    }

    /// <summary>
    /// The most weeks in a row that hit <see cref="WeeklyTarget"/> among the weeks <paramref name="from"/>–<paramref name="to"/>
    /// touch (all of history without them), and the Monday of the last of them (null with none). An unfinished week
    /// counts once it's hit.
    /// </summary>
    public (int Weeks, DateTime? End) LongestWeekStreak(DateTime? from = null, DateTime? to = null)
    {
        var target = WeeklyTarget;
        var perWeek = store.History.GroupBy(s => WeekStart(s.StartedAt)).ToDictionary(g => g.Key, g => g.Count());
        if (perWeek.Count == 0)
            return (0, null);
        var first = WeekStart(from ?? perWeek.Keys.Min());
        var last = WeekStart(to ?? DateTime.Today);
        var (best, run) = (0, 0);
        DateTime? end = null;
        for (var week = first; week <= last; week = week.AddDays(7))
        {
            run = perWeek.GetValueOrDefault(week) >= target ? run + 1 : 0;
            if (run > best)
                (best, end) = (run, week);
        }
        return (best, end);
    }
}

/// <summary>What each point of a range's charts covers: a day, a week or a month of it (see <see cref="StatsService.PeriodFor"/>).</summary>
public enum Period { Day, Week, Month }
