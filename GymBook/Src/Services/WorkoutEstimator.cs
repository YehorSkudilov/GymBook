using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Estimates how long a workout takes: every set's reps at the goal's tempo plus setting up, the rest between sets,
/// ramp-up sets on heavier compound lifts, and moving between exercises. Once a plan workout has been done a few times,
/// the estimate is scaled by the user's own pace on it, so it matches how long it really takes them.
/// </summary>
public class WorkoutEstimator(DataStore store)
{
    const double SetupSeconds = 20, TransitionSeconds = 60, WarmupRestSeconds = 60, WarmupSets = 3, WarmupReps = 5;

    // Pace: how long the last few sessions of a workout really took, against what this estimate said for them.
    const int PaceSessions = 5, MinPaceSessions = 2;
    static readonly TimeSpan MinSession = TimeSpan.FromMinutes(5), MaxSession = TimeSpan.FromHours(4);
    const double MinPace = 0.5, MaxPace = 2.5;

    public int Minutes(PlanWorkout workout, Goal goal) =>
        Round(Seconds(workout.Exercises.Select(pe => (store.GetExercise(pe.ExerciseId), pe.Sets, pe.RepMin, pe.RepMax, pe.RestSeconds)), goal) * Pace(workout, goal));

    public int Minutes(IEnumerable<(Exercise? Exercise, int Sets, int RepMin, int RepMax, int RestSeconds)> exercises, Goal goal) =>
        Round(Seconds(exercises, goal));

    double Seconds(IEnumerable<(Exercise? Exercise, int Sets, int RepMin, int RepMax, int RestSeconds)> exercises, Goal goal)
    {
        var warmups = store.Profile.WarmupSuggestions;
        var seconds = 0.0;
        var count = 0;
        foreach (var (ex, sets, repMin, repMax, rest) in exercises)
        {
            if (sets <= 0)
                continue;
            var perRep = TrainingGoals.SecondsPerRep(goal, ex);
            seconds += sets * (SetupSeconds + (repMin + repMax) / 2.0 * perRep) + (sets - 1) * rest;
            if (warmups && ex is { Mechanic: Mechanic.Compound, IsBodyweight: false } && !TrainingGoals.IsExplosive(ex))
                seconds += WarmupSets * (SetupSeconds + WarmupReps * perRep + WarmupRestSeconds);
            count++;
        }
        return count == 0 ? 0 : seconds + (count - 1) * TransitionSeconds;
    }

    /// <summary>
    /// How much longer (or shorter) than estimated the user takes on <paramref name="workout"/>: the median, over its
    /// last few finished sessions, of how long each took against the estimate for what it had then. Being a ratio, it
    /// still applies after sets are added or removed. 1 until there are enough sessions to go on.
    /// </summary>
    double Pace(PlanWorkout workout, Goal goal)
    {
        var paces = store.History
            .Where(s => s.PlanWorkoutId == workout.Id && s.Duration >= MinSession && s.Duration <= MaxSession && s.WorkingSets.Any())
            .Take(PaceSessions)
            .Select(s => (Actual: s.Duration.TotalSeconds, Estimated: Seconds(s.Exercises.Select(e =>
                (store.GetExercise(e.ExerciseId), e.Sets.Count(x => !x.IsWarmup), e.RepMin, e.RepMax, e.RestSeconds)), goal)))
            .Where(x => x.Estimated > 0)
            .Select(x => Math.Clamp(x.Actual / x.Estimated, MinPace, MaxPace))
            .Order()
            .ToList();
        if (paces.Count < MinPaceSessions)
            return 1;
        var mid = paces.Count / 2;
        return paces.Count % 2 == 1 ? paces[mid] : (paces[mid - 1] + paces[mid]) / 2;
    }

    // Round to 5 minutes: it's an estimate, not a timer.
    static int Round(double seconds) => seconds <= 0 ? 0 : Math.Max(5, (int)Math.Round(seconds / 60 / 5) * 5);

    public static string Format(int minutes) => minutes >= 60 ? $"~{minutes / 60} h {minutes % 60:00} min" : $"~{minutes} min";
}
