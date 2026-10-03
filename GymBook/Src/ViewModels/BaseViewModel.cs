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
        var choice = await AskAboutActiveAsync(workouts, dialogs);
        switch (choice)
        {
            case ActiveChoice.Cancel:
                return;
            case ActiveChoice.Resume:
                await GoTo(Routes.Workout);
                return;
        }
        if (await ChooseForRecoveryAsync(recovery, plan, workout) is not { } chosen)
            return;
        if (choice == ActiveChoice.PauseAndStart)
            workouts.Pause();
        workouts.StartFromPlan(plan, chosen, week);
        await GoTo(Routes.Workout);
    }

    enum ActiveChoice { StartNew, PauseAndStart, Resume, Cancel }

    /// <summary>
    /// With a workout in progress: resume it, pause it (to carry on later) or drop it for the new one, or neither.
    /// StartNew when there's none.
    /// </summary>
    static async Task<ActiveChoice> AskAboutActiveAsync(WorkoutService workouts, DialogService dialogs)
    {
        if (workouts.Active == null)
            return ActiveChoice.StartNew;
        const string resume = "Resume current workout", pause = "Pause it and start new";
        var choice = await dialogs.ActionSheet($"\"{workouts.Active.Name}\" is still in progress", "Discard it and start new", resume, pause);
        return choice switch
        {
            null => ActiveChoice.Cancel,
            resume => ActiveChoice.Resume,
            pause => ActiveChoice.PauseAndStart,
            _ => ActiveChoice.StartNew,
        };
    }

    /// <summary>
    /// Makes <paramref name="session"/> (paused, or finished) the workout in progress again and opens it. A different
    /// workout already in progress is paused for it, after asking.
    /// </summary>
    protected static async Task ResumeWorkoutAsync(WorkoutService workouts, DialogService dialogs, WorkoutSession session)
    {
        if (workouts.Active is { } current && current.Id != session.Id && !await dialogs.Confirm($"Pause \"{current.Name}\"?",
                "It's still in progress. It's paused, with everything logged in it, and can be resumed from the Workout tab.", "Pause and resume this"))
            return;
        workouts.Resume(session);
        await GoTo(Routes.Workout);
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
        if (choice == ActiveChoice.PauseAndStart)
            workouts.Pause();
        if (choice is ActiveChoice.StartNew or ActiveChoice.PauseAndStart)
            start();
        await GoTo(Routes.Workout);
    }
}
