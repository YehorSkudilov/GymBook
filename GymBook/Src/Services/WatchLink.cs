using GymBook.Contracts;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// The phone's side of the Wear OS app, minus the transport (Platforms/Android/WatchSync.cs): what the watch is shown of
/// the workout in progress, and what the watch asks of it while connected (the watch is then a companion: the workout
/// always runs here): ticking a set at the weight and reps it says, starting a workout, finishing or discarding it.
/// </summary>
public class WatchLink(DataStore store, WorkoutService workouts, Units units)
{
    /// <summary>
    /// Set by the workout page while it's open: ticks the set the way a tap on the page does (rest timer, moving on to
    /// the next exercise), first at the weight (kg) and reps the watch gave, if any. True when the set ended up ticked.
    /// </summary>
    public Func<SetEntry, double?, int?, bool>? LiveCompleteSet { get; set; }

    /// <summary>Set by the workout page while it's open: the workout was finished or discarded from the watch, so it closes.</summary>
    public Action? LiveEnded { get; set; }

    /// <summary>How long a heart rate from the watch counts as current: after that the watch is off or not reading.</summary>
    public static readonly TimeSpan HeartRateTimeout = TimeSpan.FromSeconds(15);

    int _heartRate;
    DateTime _heartRateAt;

    /// <summary>The heart rate the watch last sent, while it's recent; otherwise null.</summary>
    public int? HeartRate => DateTime.UtcNow - _heartRateAt < HeartRateTimeout ? _heartRate : null;

    /// <summary>A new heart rate arrived from the watch. Raised on the main thread.</summary>
    public event Action? HeartRateChanged;

    /// <summary>The watch read a heart rate. Call on the main thread.</summary>
    public void ReportHeartRate(int bpm)
    {
        if (bpm is < 25 or > 250)
            return;
        _heartRate = bpm;
        _heartRateAt = DateTime.UtcNow;
        HeartRateChanged?.Invoke();
    }

    /// <summary>The workout as the watch shows it. Call on the main thread, where the workout is changed.</summary>
    public WearWorkout Snapshot()
    {
        if (workouts.Active is not { } session)
            return WearWorkout.None;
        return new WearWorkout(
            true,
            session.Id,
            session.Name,
            new DateTimeOffset(session.StartedAt),
            units.Label,
            session.Exercises.Select(e => new WearExercise(
                store.GetExercise(e.ExerciseId)?.Name ?? "Exercise",
                e.RepMin,
                e.RepMax,
                e.RestSeconds,
                [.. e.Sets.Select(s => new WearSet(
                    Math.Round(units.ToDisplay(s.WeightKg), 1),
                    s.Reps,
                    s.IsWarmup,
                    s.IsCompleted,
                    s.CompletedAt is { } at ? new DateTimeOffset(at) : null))],
                store.GetExercise(e.ExerciseId) is { } ex ? units.Increment(ex) : 0)).ToList());
    }

    /// <summary>Ticks the set the watch asked for. Call on the main thread.</summary>
    public void CompleteSet(WearCompleteSet request)
    {
        if (workouts.Active is not { } session || session.Id != request.SessionId)
            return;
        if (session.Exercises.ElementAtOrDefault(request.Exercise) is not { } se || se.Sets.ElementAtOrDefault(request.Set) is not { IsCompleted: false } set)
            return;
        double? kg = request.Weight is double w && w >= 0 ? units.FromDisplay(w) : null;
        int? reps = request.Reps is int r && r is >= 0 and <= 100 ? r : null;
        if (LiveCompleteSet?.Invoke(set, kg, reps) == true)
            return;
        // The workout page isn't open: tick it the way the page would, at what the watch said; a new weight carries on
        // to the sets after it of the same kind. A set without reps can't be ticked there either.
        if (reps is { } newReps)
            set.Reps = newReps;
        if (kg is { } newKg)
            foreach (var later in se.Sets.SkipWhile(s => s != set).Where(s => !s.IsCompleted && s.IsWarmup == set.IsWarmup))
                later.WeightKg = newKg;
        if (set.Reps <= 0)
        {
            workouts.Save();
            return;
        }
        SetTimes.Complete(session, set, DateTime.Now);
        workouts.Save();
    }

    /// <summary>
    /// Starts the workout the watch asked for, as starting it here would (without the recovery check: it was chosen on
    /// the watch). Nothing while one is already running: the watch follows that one instead. Call on the main thread.
    /// </summary>
    public void Start(WearStartWorkout request)
    {
        if (workouts.Active != null)
            return;
        if (request.PlanId == null)
        {
            workouts.StartEmpty();
            return;
        }
        if (store.GetPlan(request.PlanId) is { } plan && plan.Workouts.FirstOrDefault(w => w.Id == request.PlanWorkoutId) is { } workout)
            workouts.StartFromPlan(plan, workout, request.Week);
    }

    /// <summary>
    /// Finishes the workout as finishing it here does (the sets done kept, the plan moved on), or discards it, when the
    /// watch asked for this one. A workout with nothing done is discarded either way. Call on the main thread.
    /// </summary>
    public void Finish(WearFinishWorkout request)
    {
        if (workouts.Active is not { } session || session.Id != request.SessionId)
            return;
        if (request.Discard || !session.Exercises.Any(e => e.Sets.Any(s => s.IsCompleted)))
            workouts.Discard();
        else
            workouts.Finish();
        LiveEnded?.Invoke();
    }
}
