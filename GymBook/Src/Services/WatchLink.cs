using GymBook.Contracts;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// The phone's side of the Wear OS app, minus the transport (Platforms/Android/WatchSync.cs): what the watch is shown of
/// the workout in progress, and ticking a set the watch asked for.
/// </summary>
public class WatchLink(DataStore store, WorkoutService workouts, Units units)
{
    /// <summary>
    /// Set by the workout page while it's open: ticks the set the way a tap on the page does (rest timer, moving on to
    /// the next exercise). True when the set ended up ticked.
    /// </summary>
    public Func<SetEntry, bool>? LiveCompleteSet { get; set; }

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
                e.Sets.Select(s => new WearSet(
                    Math.Round(units.ToDisplay(s.WeightKg), 1),
                    s.Reps,
                    s.IsWarmup,
                    s.IsCompleted,
                    s.CompletedAt is { } at ? new DateTimeOffset(at) : null)).ToList())).ToList());
    }

    /// <summary>Ticks the set the watch asked for. Call on the main thread.</summary>
    public void CompleteSet(WearCompleteSet request)
    {
        if (workouts.Active is not { } session || session.Id != request.SessionId)
            return;
        if (session.Exercises.ElementAtOrDefault(request.Exercise)?.Sets.ElementAtOrDefault(request.Set) is not { IsCompleted: false } set)
            return;
        if (LiveCompleteSet?.Invoke(set) == true)
            return;
        // The workout page isn't open: tick it the way the page would. A set without reps can't be ticked there either
        // (the watch knows, and doesn't offer it).
        if (set.Reps <= 0)
            return;
        SetTimes.Complete(session, set, DateTime.Now);
        workouts.Save();
    }
}
