using GymBook.Models;

namespace GymBook.Services;

/// <summary>One muscle's recovery at a moment: how fresh it is, when it will be fully recovered, and the workout that tired it most.</summary>
public record MuscleRecovery(MuscleGroup Muscle, double Recovery, DateTime? ReadyAt, WorkoutSession? LimitingSession, double Sets);

/// <summary>
/// Estimates how recovered each muscle is (0 = just trained hard, 1 = fully fresh) from recent sets.
/// More sets means a longer recovery window (see <see cref="HoursToRecover"/>), and sleep stretches or shortens it: short
/// nights after a workout slow recovery down, long ones speed it up a little (<see cref="SleepFactor"/>, from the sleep
/// Samsung Health or Health Connect recorded). Works for any moment: in the past it only counts workouts finished by
/// then, in the future it lets today's fatigue wear off.
/// </summary>
public class RecoveryService(DataStore store)
{
    // How far back workouts still count (recovery takes three days at most, a little more after short nights), and how
    // far ahead the previews look.
    const double MaxHours = 100;

    /// <summary>Around this much sleep a night, recovery runs at its usual speed.</summary>
    public const double TypicalSleepHours = 7.5;

    /// <summary>
    /// How much longer (above 1) or shorter (below) recovery from a workout that ended at <paramref name="ended"/> takes,
    /// looked at from <paramref name="at"/>: by the nights slept since (or, before the first, the night before it), about
    /// a tenth longer for each hour under <see cref="TypicalSleepHours"/> and shorter for each hour over, between 0.85
    /// and 1.35. 1 without sleep recorded.
    /// </summary>
    public double SleepFactor(DateTime ended, DateTime at) => SleepFactor(SleepByDay(), ended, at);

    static double SleepFactor(Dictionary<DateTime, int> nights, DateTime ended, DateTime at)
    {
        var after = nights.Where(n => n.Key > ended.Date && n.Key <= at.Date).Select(n => n.Value).ToList();
        if (after.Count == 0 && nights.TryGetValue(ended.Date, out var before))
            after.Add(before);
        return after.Count == 0 ? 1 : FactorFor(after.Average() / 60);
    }

    static double FactorFor(double hours) => Math.Clamp(1 + (TypicalSleepHours - hours) * 0.1, 0.85, 1.35);

    /// <summary>Minutes slept each day (the night woken up from on it), from the health apps.</summary>
    Dictionary<DateTime, int> SleepByDay() =>
        store.Data.HealthDays.Where(h => h.SleepMinutes is > 0).GroupBy(h => h.Date.Date).ToDictionary(g => g.Key, g => g.First().SleepMinutes!.Value);

    /// <summary>
    /// The latest night slept by <paramref name="at"/> and what it does to recovery, for showing: "Last night: 6 h 10 min
    /// of sleep · recovery about 13% slower". Null without sleep recorded in the last two days.
    /// </summary>
    public string? SleepNote(DateTime at)
    {
        var nights = SleepByDay();
        if (nights.Where(n => n.Key <= at.Date && n.Key >= at.Date.AddDays(-1)).OrderByDescending(n => n.Key).Select(n => (DateTime?)n.Key).FirstOrDefault() is not { } night)
            return null;
        var minutes = nights[night];
        var when = night == DateTime.Today ? "Last night" : night == DateTime.Today.AddDays(-1) ? "The night before" : $"Night to {night:ddd d MMM}";
        var factor = FactorFor(minutes / 60.0);
        var effect = factor > 1.02 ? $"recovery about {(factor - 1) * 100:0}% slower"
            : factor < 0.98 ? $"recovery about {(1 - factor) * 100:0}% faster"
            : "recovery at its usual pace";
        return $"{when}: {minutes / 60} h {minutes % 60:00} min of sleep · {effect}";
    }

    public Dictionary<MuscleGroup, double> Compute(DateTime at) => Details(at).ToDictionary(r => r.Muscle, r => r.Recovery);

    public List<MuscleRecovery> Details(DateTime at)
    {
        var result = Enum.GetValues<MuscleGroup>().ToDictionary(m => m, m => new MuscleRecovery(m, 1, null, null, 0));
        var nights = SleepByDay();

        // Workouts that ended by then, and one still going on at that moment, counted by the sets done so far (so a
        // look through a workout shows the fatigue building exercise by exercise).
        foreach (var session in store.History.Where(s => s.StartedAt <= at).TakeWhile(s => (at - s.EndedAt!.Value).TotalHours < MaxHours))
        {
            var during = at < session.EndedAt!.Value;
            var hours = during ? 0 : (at - session.EndedAt!.Value).TotalHours;
            var sleep = SleepFactor(nights, session.EndedAt!.Value, at);
            foreach (var (muscle, sets) in SetsPerMuscle(session, during ? at : null))
            {
                var needed = HoursToRecover(muscle, sets) * sleep;
                var recovered = RecoveredAfter(hours / needed);
                if (recovered < result[muscle].Recovery)
                    result[muscle] = new MuscleRecovery(muscle, recovered, session.EndedAt!.Value.AddHours(needed), session, sets);
            }
        }
        return [.. result.Values];
    }

    /// <summary>
    /// How long a muscle takes to recover fully from <paramref name="sets"/> hard sets: a day for a little work, 5 hours
    /// more per set, up to three days; smaller muscles bounce back quicker than the big movers of the legs and back.
    /// </summary>
    static double HoursToRecover(MuscleGroup muscle, double sets) => Math.Clamp(24 + sets * 5, 24, 72) * SizeFactor(muscle);

    static double SizeFactor(MuscleGroup muscle) => muscle switch
    {
        MuscleGroup.Quads or MuscleGroup.Hamstrings or MuscleGroup.Glutes or MuscleGroup.Back or MuscleGroup.LowerBack => 1,
        MuscleGroup.Chest => 0.9,
        _ => 0.75,
    };

    /// <summary>
    /// Recovery by the share of the time elapsed: most of it in the first part, levelling off towards the end (not a
    /// straight line, which undersold the first day).
    /// </summary>
    static double RecoveredAfter(double share)
    {
        var x = Math.Clamp(share, 0, 1);
        return 1 - (1 - x) * (1 - x);
    }

    /// <summary>Below this a muscle is still "Recovering" or "Fatigued" and shouldn't take much work yet.</summary>
    public const double ReadyThreshold = 0.6;

    /// <summary>A muscle gets a warning only when the workout gives it at least this many sets (secondary work counts half).</summary>
    const double MeaningfulSets = 2;

    /// <summary>
    /// The muscles <paramref name="workout"/> works that aren't recovered enough at <paramref name="at"/>, most tired
    /// first; empty when it's fine to train. Uses the planned sets, weighted like logged ones.
    /// Judged by the part of each muscle the workout works (see <see cref="SubMuscles"/>): a push day's front and side
    /// delts don't stop a pull day's face pulls (rear delts), nor flat presses an incline the next day the same way.
    /// </summary>
    public List<MuscleRecovery> NotReady(PlanWorkout workout, DateTime at)
    {
        var planned = PlannedParts(workout);
        var parts = PartRecovery(at);
        var tired = planned.Where(p => p.Value >= MeaningfulSets && parts[p.Key] < ReadyThreshold).Select(p => p.Key.Group()).ToHashSet();
        return [.. Details(at).Where(r => tired.Contains(r.Muscle)).OrderBy(r => r.Recovery)];
    }

    /// <summary>
    /// How recovered each part of each muscle is at <paramref name="at"/> (front, side and rear delts, upper chest, lats
    /// and so on), worked out like <see cref="Details"/> from the sets each part got.
    /// </summary>
    public Dictionary<SubMuscle, double> PartRecovery(DateTime at)
    {
        var result = Enum.GetValues<SubMuscle>().ToDictionary(m => m, _ => 1.0);
        var nights = SleepByDay();
        foreach (var session in store.History.Where(s => s.StartedAt <= at).TakeWhile(s => (at - s.EndedAt!.Value).TotalHours < MaxHours))
        {
            var during = at < session.EndedAt!.Value;
            var hours = during ? 0 : (at - session.EndedAt!.Value).TotalHours;
            var sleep = SleepFactor(nights, session.EndedAt!.Value, at);
            foreach (var (part, sets) in PartSets(session, during ? at : null))
                result[part] = Math.Min(result[part], RecoveredAfter(hours / (HoursToRecover(part.Group(), sets) * sleep)));
        }
        return result;
    }

    /// <summary>Like <see cref="SetsPerMuscle"/>, by the part of each muscle each exercise works.</summary>
    Dictionary<SubMuscle, double> PartSets(WorkoutSession session, DateTime? upTo)
    {
        var sets = new Dictionary<SubMuscle, double>();
        foreach (var se in session.Exercises)
        {
            if (store.GetExercise(se.ExerciseId) is not { } ex)
                continue;
            var count = se.Sets.Count(s => s.IsCompleted && !s.IsWarmup && (upTo is not { } cutoff || (s.CompletedAt ?? session.EndedAt ?? session.StartedAt) <= cutoff));
            if (count > 0)
                AddParts(sets, ex, count);
        }
        return sets;
    }

    /// <summary>Like <see cref="PlannedSets"/>, by the part of each muscle each exercise works.</summary>
    Dictionary<SubMuscle, double> PlannedParts(PlanWorkout workout)
    {
        var sets = new Dictionary<SubMuscle, double>();
        foreach (var pe in workout.Exercises)
            if (store.GetExercise(pe.ExerciseId) is { } ex)
                AddParts(sets, ex, pe.Sets);
        return sets;
    }

    // A set for the part of its primary muscle, half a set for the part of each secondary one.
    static void AddParts(Dictionary<SubMuscle, double> sets, Exercise ex, double count)
    {
        var primary = SubMuscles.For(ex, ex.PrimaryMuscle);
        sets[primary] = sets.GetValueOrDefault(primary) + count;
        foreach (var m in ex.SecondaryMuscles)
        {
            var part = SubMuscles.For(ex, m);
            sets[part] = sets.GetValueOrDefault(part) + count * 0.5;
        }
    }

    /// <summary>
    /// The plan's workout best suited to train at <paramref name="at"/> instead of <paramref name="instead"/>: one that
    /// works nothing still recovering, with the freshest muscles (weighted by its sets). Null when none qualifies.
    /// </summary>
    public PlanWorkout? FreshAlternative(WorkoutPlan plan, PlanWorkout instead, DateTime at)
    {
        var recovery = PartRecovery(at);
        return plan.Workouts
            .Where(w => w != instead && w.Exercises.Count > 0 && NotReady(w, at).Count == 0)
            .Select(w => (Workout: w, Sets: PlannedParts(w)))
            .Where(x => x.Sets.Count > 0)
            .OrderByDescending(x => x.Sets.Sum(s => s.Value * recovery[s.Key]) / x.Sets.Sum(s => s.Value))
            .Select(x => x.Workout)
            .FirstOrDefault();
    }

    /// <summary>
    /// How ready the muscles <paramref name="workout"/> works are at <paramref name="at"/>, 0 to 1: their recovery,
    /// weighted by how many sets each gets. 1 for a workout with nothing to go by.
    /// </summary>
    public double Readiness(PlanWorkout workout, DateTime at)
    {
        // By the parts of the muscles it works, like NotReady.
        var sets = PlannedParts(workout);
        var total = sets.Sum(s => s.Value);
        if (total <= 0)
            return 1;
        var recovery = PartRecovery(at);
        return sets.Sum(s => s.Value * recovery[s.Key]) / total;
    }

    /// <summary>Like <see cref="SetsPerMuscle"/>, for the sets a plan workout prescribes.</summary>
    Dictionary<MuscleGroup, double> PlannedSets(PlanWorkout workout)
    {
        var sets = new Dictionary<MuscleGroup, double>();
        foreach (var pe in workout.Exercises)
        {
            if (store.GetExercise(pe.ExerciseId) is not { } ex)
                continue;
            sets[ex.PrimaryMuscle] = sets.GetValueOrDefault(ex.PrimaryMuscle) + pe.Sets;
            foreach (var m in ex.SecondaryMuscles)
                sets[m] = sets.GetValueOrDefault(m) + pe.Sets * 0.5;
        }
        return sets;
    }

    /// <summary>
    /// Primary muscles get one set each; secondary muscles get half a set. With <paramref name="upTo"/>, only the sets
    /// done by then count (partway through the workout); sets without a time count as done when it ended.
    /// </summary>
    public Dictionary<MuscleGroup, double> SetsPerMuscle(WorkoutSession session, DateTime? upTo = null)
    {
        var sets = new Dictionary<MuscleGroup, double>();
        foreach (var se in session.Exercises)
        {
            var ex = store.GetExercise(se.ExerciseId);
            if (ex == null)
                continue;
            var count = se.Sets.Count(s => s.IsCompleted && !s.IsWarmup && (upTo is not { } cutoff || (s.CompletedAt ?? session.EndedAt ?? session.StartedAt) <= cutoff));
            if (count == 0)
                continue;
            sets[ex.PrimaryMuscle] = sets.GetValueOrDefault(ex.PrimaryMuscle) + count;
            foreach (var m in ex.SecondaryMuscles)
                sets[m] = sets.GetValueOrDefault(m) + count * 0.5;
        }
        return sets;
    }

    public static Color ColorFor(double recovery) => recovery switch
    {
        >= 0.9 => Color.FromArgb("#2ED47A"),
        >= 0.6 => Color.FromArgb("#B6D83F"),
        >= 0.35 => Color.FromArgb("#FFB020"),
        _ => Color.FromArgb("#FF4D5E"),
    };

    public static string StatusFor(double recovery) => recovery switch
    {
        >= 0.9 => "Fresh",
        >= 0.6 => "Almost recovered",
        >= 0.35 => "Recovering",
        _ => "Fatigued",
    };

    // The preview's range: a week back, and far enough ahead that anything trained today has recovered.
    public const double PreviewPastHours = 7 * 24, PreviewFutureHours = MaxHours;

    /// <summary>"Now", "In 1 d 4 h" or "2 d ago", with the moment itself, for the preview slider.</summary>
    public static string PreviewLabel(double hours)
    {
        var h = (int)Math.Round(hours);
        if (h == 0)
            return "Now";
        var span = Math.Abs(h) >= 24 ? $"{Math.Abs(h) / 24} d{(Math.Abs(h) % 24 > 0 ? $" {Math.Abs(h) % 24} h" : "")}" : $"{Math.Abs(h)} h";
        var when = DateTime.Now.AddHours(h).ToString("ddd HH:00");
        return h > 0 ? $"In {span} · {when}" : $"{span} ago · {when}";
    }
}
