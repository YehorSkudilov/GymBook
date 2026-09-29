using GymBook.Models;
using GymBook.ViewModels;
using GymBook.Views;

namespace GymBook.Services;

/// <summary>Shows the exercise picker modally and returns the chosen exercises (empty when cancelled).</summary>
public class ExercisePickerService(IServiceProvider services)
{
    public async Task<List<Exercise>> PickAsync()
    {
        var page = services.GetRequiredService<ExercisePickerPage>();
        var vm = (ExercisePickerViewModel)page.BindingContext;
        var tcs = new TaskCompletionSource<List<Exercise>>();
        vm.Completed = list => tcs.TrySetResult(list);
        vm.Reset();

        // A sheet: it slides up, and slides down again before the exercises are handed back.
        await Shell.Current.Navigation.PushModalAsync(page, false);
        var result = await tcs.Task;
        await page.CloseAsync();
        return result;
    }
}
