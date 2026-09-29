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
    /// Starts <paramref name="workout"/> from <paramref name="plan"/>. With a workout already in progress, asks first
    /// whether to resume it (no recovery check: it's already underway) or start the new one. Before starting, warns when
    /// muscles it works are still recovering, suggesting a fresher workout from the plan (or rest) instead. The workout
    /// in progress is only replaced once the new one really starts, so backing out of the warning keeps it.
    /// </summary>
    protected static async Task StartPlannedWorkoutAsync(WorkoutService workouts, DialogService dialogs, RecoveryService recovery,
        WorkoutPlan plan, PlanWorkout workout, int? week)
    {
        switch (await AskAboutActiveAsync(workouts, dialogs))
        {
            case ActiveChoice.Cancel:
                return;
            case ActiveChoice.Resume:
                await GoTo(Routes.Workout);
                return;
        }
        if (await ChooseForRecoveryAsync(recovery, plan, workout) is not { } chosen)
            return;
        workouts.StartFromPlan(plan, chosen, week);
        await GoTo(Routes.Workout);
    }

    enum ActiveChoice { StartNew, Resume, Cancel }

    /// <summary>With a workout in progress: resume it, drop it for the new one, or neither. StartNew when there's none.</summary>
    static async Task<ActiveChoice> AskAboutActiveAsync(WorkoutService workouts, DialogService dialogs)
    {
        if (workouts.Active == null)
            return ActiveChoice.StartNew;
        var choice = await dialogs.ActionSheet($"\"{workouts.Active.Name}\" is still in progress", "Discard it and start new", "Resume current workout");
        return choice switch
        {
            null => ActiveChoice.Cancel,
            "Resume current workout" => ActiveChoice.Resume,
            _ => ActiveChoice.StartNew,
        };
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
        var choice = await AskAboutActiveAsync(workouts, dialogs);
        if (choice == ActiveChoice.Cancel)
            return;
        if (choice == ActiveChoice.StartNew)
            start();
        await GoTo(Routes.Workout);
    }
}
