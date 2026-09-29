using CommunityToolkit.Mvvm.ComponentModel;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public abstract class BaseViewModel : ObservableObject
{
    /// <summary>Called every time the page appears; reload state here.</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    public virtual void OnDisappearing()
    {
    }

    protected static Task GoTo(string route) => Shell.Current.GoToAsync(route);

    protected static Task GoTo(string route, IDictionary<string, object> parameters) => Shell.Current.GoToAsync(route, parameters);

    protected static Task GoBack() => Shell.Current.GoToAsync("..");

    /// <summary>
    /// Starts <paramref name="workout"/> from <paramref name="plan"/>, but first warns when muscles it works are still
    /// recovering, suggesting a fresher workout from the plan (or rest) instead. Resuming a workout in progress skips the check.
    /// </summary>
    protected static async Task StartPlannedWorkoutAsync(WorkoutService workouts, DialogService dialogs, RecoveryService recovery,
        WorkoutPlan plan, PlanWorkout workout, int? week)
    {
        if (workouts.Active == null)
        {
            if (await ChooseForRecoveryAsync(recovery, plan, workout) is not { } chosen)
                return;
            workout = chosen;
        }
        await StartWorkoutAsync(workouts, dialogs, () => workouts.StartFromPlan(plan, workout, week));
    }

    /// <summary>
    /// The workout to start after the recovery check: the same one, a fresher one from the plan, or null to not train now.
    /// Asks on <see cref="RecoveryWarningViewModel"/>'s sheet when muscles it works are still recovering.
    /// </summary>
    static async Task<PlanWorkout?> ChooseForRecoveryAsync(RecoveryService recovery, WorkoutPlan plan, PlanWorkout workout)
    {
        var now = DateTime.Now;
        var tired = recovery.NotReady(workout, now);
        if (tired.Count == 0)
            return workout;
        var alternative = recovery.FreshAlternative(plan, workout, now);
        return await RecoveryWarningViewModel.ShowAsync(workout, alternative, tired) switch
        {
            RecoveryChoice.StartAnyway => workout,
            RecoveryChoice.Alternative => alternative,
            _ => null,
        };
    }

    /// <summary>Starts a workout, asking first if one is already in progress.</summary>
    protected static async Task StartWorkoutAsync(WorkoutService workouts, DialogService dialogs, Func<WorkoutSession> start)
    {
        if (workouts.Active != null)
        {
            var choice = await dialogs.ActionSheet($"\"{workouts.Active.Name}\" is still in progress", "Discard it and start new", "Resume current workout");
            if (choice == null)
                return;
            if (choice != "Resume current workout")
                start();
        }
        else
        {
            start();
        }
        await GoTo(Routes.Workout);
    }
}
