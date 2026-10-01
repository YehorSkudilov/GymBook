namespace GymBook.Services;

/// <summary>What the workout notification shows: the workout, where it's at, and the rest timer when resting.</summary>
public record WorkoutStatus(string Name, DateTime StartedAt, string Detail, string Progress, DateTime? RestEndsAt, int RestSeconds);

/// <summary>
/// The notification of the workout in progress (Android: Platforms/Android/WorkoutNotifier.cs), with the rest timer
/// and its bar; tapping it opens the workout. Elsewhere it does nothing.
/// </summary>
public interface IWorkoutNotifier
{
    /// <summary>Shows or updates it. Cheap to call every second: it only redraws when something visible changed.</summary>
    void Show(WorkoutStatus status);

    /// <summary>Rest ran out: an alert when the app isn't on screen (when it is, the workout page buzzes itself).</summary>
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
