namespace GymBook.Services;

/// <summary>Where the workout is at, for its notification.</summary>
public enum WorkoutPhase
{
    /// <summary>Doing <see cref="WorkoutStatus.Next"/>, timed since <see cref="WorkoutStatus.Since"/>.</summary>
    InSet,
    /// <summary>Resting, counting down to <see cref="WorkoutStatus.RestDueAt"/>; then <see cref="WorkoutStatus.Next"/>.</summary>
    Resting,
    /// <summary>The rest ran out at <see cref="WorkoutStatus.RestDueAt"/> and the next set hasn't been started.</summary>
    RestOver,
    /// <summary>Every set done or skipped: time to finish.</summary>
    Done,
}

/// <summary>
/// What the workout notification shows: the workout and its progress, what's being done or comes next (exercise, set,
/// weight × reps), the timer that goes with it, and how many of the workout's sets are done (its bar when not resting).
/// </summary>
public record WorkoutStatus(string Name, string Progress, WorkoutPhase Phase, string Next, DateTime Since, DateTime? RestDueAt, int RestSeconds,
    int SetsDone = 0, int SetsTotal = 0);

/// <summary>
/// The notification of the workout in progress (Android: Platforms/Android/WorkoutNotifier.cs), with the rest timer
/// and its bar; tapping it opens the workout. Elsewhere it does nothing.
/// </summary>
public interface IWorkoutNotifier
{
    /// <summary>Shows or updates it. Cheap to call every second: it only redraws when something visible changed.</summary>
    void Show(WorkoutStatus status);

    /// <summary>
    /// Rest ran out: the notification says so with a sound and buzz when the app isn't on screen (when it is, the workout
    /// page buzzes itself). The same notification, not a second one.
    /// </summary>
    void RestOver(string next);

    void Clear();
}

public class NoWorkoutNotifier : IWorkoutNotifier
{
    public void Show(WorkoutStatus status)
    {
    }

    public void RestOver(string next)
    {
    }

    public void Clear()
    {
    }
}

/// <summary>
/// Opening the workout from outside the app's pages (its notification): remembered until the app's tabs are up, then
/// the workout page is shown, unless it's already on screen.
/// </summary>
public static class WorkoutLaunch
{
    static bool _requested;

    public static void Request()
    {
        _requested = true;
        MainThread.BeginInvokeOnMainThread(() => _ = TryOpenAsync());
    }

    /// <summary>Opens the workout if asked to and the app is ready (the MainPage calls it again when it appears).</summary>
    public static async Task TryOpenAsync()
    {
        if (!_requested || Shell.Current?.CurrentPage is not { } page)
            return;
        _requested = false;
        var workouts = IPlatformApplication.Current?.Services.GetService<WorkoutService>();
        if (workouts?.Active == null || page is Views.WorkoutPage)
            return;
        await Shell.Current.GoToAsync(Routes.Workout);
    }
}
