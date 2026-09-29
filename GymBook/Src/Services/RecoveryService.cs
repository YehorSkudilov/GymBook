using GymBook.Models;

namespace GymBook.Services;

/// <summary>One muscle's recovery at a moment: how fresh it is, when it will be fully recovered, and the workout that tired it most.</summary>
public record MuscleRecovery(MuscleGroup Muscle, double Recovery, DateTime? ReadyAt, WorkoutSession? LimitingSession, double Sets);

/// <summary>
/// Estimates how recovered each muscle is (0 = just trained hard, 1 = fully fresh) from recent sets.
/// More sets means a longer recovery window: 24h base plus 8h per set, capped at 96h. Works for any moment:
/// in the past it only counts workouts finished by then, in the future it lets today's fatigue wear off.
/// </summary>
public class RecoveryService(DataStore store)
{
    const double MaxHours = 96;

    public Dictionary<MuscleGroup, double> Compute(DateTime at) => Details(at).ToDictionary(r => r.Muscle, r => r.Recovery);

    public List<MuscleRecovery> Details(DateTime at)
    {
        var result = Enum.GetValues<MuscleGroup>().ToDictionary(m => m, m => new MuscleRecovery(m, 1, null, null, 0));

        foreach (var session in store.History.Where(s => s.EndedAt <= at).TakeWhile(s => (at - s.EndedAt!.Value).TotalHours < MaxHours))
        {
            var hours = (at - session.EndedAt!.Value).TotalHours;
            foreach (var (muscle, sets) in SetsPerMuscle(session))
            {
                var needed = Math.Clamp(24 + sets * 8, 24, MaxHours);
                var recovered = Math.Clamp(hours / needed, 0, 1);
                if (recovered < result[muscle].Recovery)
                    result[muscle] = new MuscleRecovery(muscle, recovered, session.EndedAt!.Value.AddHours(needed), session, sets);
            }
        }
        return [.. result.Values];
    }

    /// <summary>Below this a muscle is still "Recovering" or "Fatigued" and shouldn't take much work yet.</summary>
    public const double ReadyThreshold = 0.6;

    /// <summary>A muscle gets a warning only when the workout gives it at least this many sets (secondary work counts half).</summary>
    const double MeaningfulSets = 2;

    /// <summary>
    /// The muscles <paramref name="workout"/> works that aren't recovered enough at <paramref name="at"/>, most tired
    /// first; empty when it's fine to train. Uses the planned sets, weighted like logged ones.
    /// </summary>
    public List<MuscleRecovery> NotReady(PlanWorkout workout, DateTime at)
    {
        var planned = PlannedSets(workout);
        var recovery = Details(at).ToDictionary(r => r.Muscle);
        return [.. planned
            .Where(p => p.Value >= MeaningfulSets && recovery[p.Key].Recovery < ReadyThreshold)
            .Select(p => recovery[p.Key])
            .OrderBy(r => r.Recovery)];
    }

    /// <summary>
    /// The plan's workout best suited to train at <paramref name="at"/> instead of <paramref name="instead"/>: one that
    /// works nothing still recovering, with the freshest muscles (weighted by its sets). Null when none qualifies.
    /// </summary>
    public PlanWorkout? FreshAlternative(WorkoutPlan plan, PlanWorkout instead, DateTime at)
    {
        var recovery = Compute(at);
        return plan.Workouts
            .Where(w => w != instead && w.Exercises.Count > 0 && NotReady(w, at).Count == 0)
            .Select(w => (Workout: w, Sets: PlannedSets(w)))
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
        var sets = PlannedSets(workout);
        var total = sets.Sum(s => s.Value);
        if (total <= 0)
            return 1;
        var recovery = Compute(at);
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

    /// <summary>Primary muscles get one set each; secondary muscles get half a set.</summary>
    public Dictionary<MuscleGroup, double> SetsPerMuscle(WorkoutSession session)
    {
        var sets = new Dictionary<MuscleGroup, double>();
        foreach (var se in session.Exercises)
        {
            var ex = store.GetExercise(se.ExerciseId);
            if (ex == null)
                continue;
            var count = se.Sets.Count(s => s.IsCompleted && !s.IsWarmup);
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
