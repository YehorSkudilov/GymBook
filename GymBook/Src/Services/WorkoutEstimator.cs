using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Estimates how long a workout takes: every set's reps at the goal's tempo plus setting up, the rest between sets,
/// ramp-up sets on heavier compound lifts, and moving between exercises. Once a plan workout has been done a few times,
/// the estimate is scaled by the user's own pace on it, so it matches how long it really takes them.
/// </summary>
public class WorkoutEstimator(DataStore store)
{
    const double SetupSeconds = 20, TransitionSeconds = 60;

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
        // The warm-ups as set in the profile: each exercise's ramp, or the lighter one once its muscle is warm.
        var warmups = WarmupSettings.Global(store.Profile);
        var worked = new HashSet<MuscleGroup>();
        var seconds = 0.0;
        var count = 0;
        foreach (var (ex, sets, repMin, repMax, rest) in exercises)
        {
            if (sets <= 0)
                continue;
            var perRep = TrainingGoals.SecondsPerRep(goal, ex);
            seconds += sets * (SetupSeconds + (repMin + repMax) / 2.0 * perRep) + (sets - 1) * rest;
            if (warmups.Enabled && ex is { IsBodyweight: false } && !TrainingGoals.IsExplosive(ex))
                seconds += warmups.Steps(WarmupSettings.KindOf(ex, worked.Contains(ex.PrimaryMuscle)))
                    .Sum(s => SetupSeconds + s.Reps * perRep + warmups.RestSeconds);
            if (ex != null)
                worked.Add(ex.PrimaryMuscle);
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

    /// <summary>
    /// The user's own pace from their recent workouts, for <see cref="Remaining"/>: how long their sets of each exercise
    /// take (from when each began to when it was ticked) and how their rests go against what was planned. Built once per
    /// workout; history doesn't change during one.
    /// </summary>
    public sealed class LivePace
    {
        internal Dictionary<string, List<double>> SetSeconds { get; } = [];
        internal List<double> RestRatios { get; } = [];
    }

    const int PaceHistorySessions = 20, SetSamples = 8, RestSamples = 30;
    const double MinSetSeconds = 5, MaxSetSeconds = 300;

    public LivePace HistoryPace(string? excludeSessionId = null)
    {
        var pace = new LivePace();
        foreach (var session in store.History.Where(s => s.Id != excludeSessionId).Take(PaceHistorySessions))
            AddPace(pace.SetSeconds, pace.RestRatios, session);
        return pace;
    }

    /// <summary>Adds what <paramref name="session"/> logged (newest sets first): set lengths per exercise, and rest against plan.</summary>
    static void AddPace(Dictionary<string, List<double>> setSeconds, List<double> restRatios, WorkoutSession session)
    {
        foreach (var e in session.Exercises)
            foreach (var set in Enumerable.Reverse(e.Sets))
            {
                if (!set.IsCompleted)
                    continue;
                // Sets ticked along with another have no time of their own (a few seconds): left out.
                if (set is { StartedAt: { } began, CompletedAt: { } done } && (done - began).TotalSeconds is >= MinSetSeconds and <= MaxSetSeconds and var secs)
                {
                    var list = setSeconds.TryGetValue(e.ExerciseId, out var l) ? l : setSeconds[e.ExerciseId] = [];
                    if (list.Count < SetSamples)
                        list.Add(secs);
                }
                if (set is { RestStartedAt: { } start, RestDueAt: { } due, RestEndedAt: { } end } && (due - start).TotalSeconds >= 15 && restRatios.Count < RestSamples)
                    restRatios.Add(Math.Clamp((end - start).TotalSeconds / (due - start).TotalSeconds, 0.5, 3));
            }
    }

    /// <summary>
    /// How long is left of <paramref name="session"/>, in progress: every set not done or skipped, at the user's own
    /// pace for its exercise (this workout's sets first, then recent ones; reps × tempo when there are none), the rest
    /// after each but the last at their usual share of what's planned, a minute to move to each exercise not started,
    /// less what's under way: the rest still to go, or the set begun at <paramref name="setSince"/>.
    /// </summary>
    public TimeSpan Remaining(WorkoutSession session, LivePace pace, DateTime now, DateTime? setSince)
    {
        var goal = store.GetPlan(session.PlanId)?.Goal ?? store.Profile.Goal;
        var warmupRest = WarmupSettings.For(store.GetPlan(session.PlanId), store.Profile).RestSeconds;

        // This workout's own pace counts first.
        var setSeconds = new Dictionary<string, List<double>>();
        var restRatios = new List<double>();
        AddPace(setSeconds, restRatios, session);
        restRatios.AddRange(pace.RestRatios.Take(Math.Max(0, RestSamples - restRatios.Count)));
        var restFactor = restRatios.Count > 0 ? Median(restRatios) : 1;

        var open = session.Exercises
            .SelectMany(e => e.Sets.Where(s => !s.IsCompleted && !s.IsSkipped).Select(s => (Exercise: e, Set: s)))
            .ToList();
        if (open.Count == 0)
            return TimeSpan.Zero;

        var seconds = 0.0;
        double SetLength(SessionExercise e, SetEntry set)
        {
            var own = setSeconds.GetValueOrDefault(e.ExerciseId) ?? [];
            var samples = own.Concat(pace.SetSeconds.GetValueOrDefault(e.ExerciseId) ?? []).Take(SetSamples).ToList();
            if (samples.Count > 0)
                return Median(samples);
            var reps = set.Reps > 0 ? set.Reps : (e.RepMin + e.RepMax) / 2.0;
            return SetupSeconds + reps * TrainingGoals.SecondsPerRep(goal, store.GetExercise(e.ExerciseId));
        }

        for (var i = 0; i < open.Count; i++)
        {
            var (e, set) = open[i];
            var length = SetLength(e, set);
            // The set under way (the first one left): only what's left of it.
            if (i == 0 && setSince is { } began)
                length = Math.Max(0, length - (now - began).TotalSeconds);
            seconds += length;
            if (i < open.Count - 1)
                seconds += (set.IsWarmup ? warmupRest : e.RestSeconds) * restFactor;
        }
        // Moving to each exercise not started yet (the one under way already is).
        var started = session.Exercises.Count(e => e.Sets.Any(s => s.IsCompleted));
        seconds += open.Select(x => x.Exercise).Distinct().Count(e => !e.Sets.Any(s => s.IsCompleted)) * TransitionSeconds
            - (started == 0 ? TransitionSeconds : 0);

        // Resting now: what's left of it, as the user usually rests.
        if (SetTimes.RunningRest(session) is { RestStartedAt: { } restStart, RestDueAt: { } restDue })
            seconds += Math.Max(0, (restStart + (restDue - restStart) * restFactor - now).TotalSeconds);
        return TimeSpan.FromSeconds(Math.Max(0, seconds));
    }

    static double Median(List<double> values)
    {
        var sorted = values.Order().ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    // Round to 5 minutes: it's an estimate, not a timer.
    static int Round(double seconds) => seconds <= 0 ? 0 : Math.Max(5, (int)Math.Round(seconds / 60 / 5) * 5);

    public static string Format(int minutes) => minutes >= 60 ? $"~{minutes / 60} h {minutes % 60:00} min" : $"~{minutes} min";
}
