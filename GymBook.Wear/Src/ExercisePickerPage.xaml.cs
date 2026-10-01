using GymBook.Models;

namespace GymBook.Wear;

/// <summary>Picks an exercise from the catalogue (see <see cref="ExercisePickerViewModel"/>).</summary>
public partial class ExercisePickerPage : ContentPage
{
    readonly TaskCompletionSource<Exercise?> _result = new();

    public ExercisePickerPage(ExercisePickerViewModel vm)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = vm;
        vm.Picked += async exercise =>
        {
            _result.TrySetResult(exercise);
            await Navigation.PopAsync();
        };
    }

    /// <summary>Opens the picker; the exercise picked, or null when backed out of it.</summary>
    public static Task<Exercise?> PickAsync(INavigation navigation)
    {
        var page = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<ExercisePickerPage>();
        _ = navigation.PushAsync(page);
        return page._result.Task;
    }

    // Backed out (swipe or the back button): nothing picked.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _result.TrySetResult(null);
    }
}
