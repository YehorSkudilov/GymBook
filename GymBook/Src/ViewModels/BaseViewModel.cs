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
