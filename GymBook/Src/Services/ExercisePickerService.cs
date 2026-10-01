using GymBook.Models;
using GymBook.ViewModels;
using GymBook.Views;

namespace GymBook.Services;

/// <summary>Shows the exercise picker modally and returns the chosen exercises (empty when cancelled).</summary>
public class ExercisePickerService(IServiceProvider services)
{
    public Task<List<Exercise>> PickAsync() => ShowAsync(single: false, "Add exercises");

    /// <summary>
    /// Picks one exercise; null when cancelled. Replacing <paramref name="similarTo"/>, the list starts on the exercises
    /// like it (the full list is a tap away).
    /// </summary>
    public async Task<Exercise?> PickOneAsync(string title, Exercise? similarTo = null) => (await ShowAsync(single: true, title, similarTo)).FirstOrDefault();

    async Task<List<Exercise>> ShowAsync(bool single, string title, Exercise? similarTo = null)
    {
        var page = services.GetRequiredService<ExercisePickerPage>();
        var vm = (ExercisePickerViewModel)page.BindingContext;
        var tcs = new TaskCompletionSource<List<Exercise>>();
        vm.Completed = list => tcs.TrySetResult(list);
        vm.Reset(single, title, similarTo);

        // A sheet: it slides up, and slides down again before the exercises are handed back.
        await Shell.Current.Navigation.PushModalAsync(page, false);
        var result = await tcs.Task;
        await page.CloseAsync();
        return result;
    }
}
