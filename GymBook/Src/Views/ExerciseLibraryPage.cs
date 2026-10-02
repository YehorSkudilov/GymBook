namespace GymBook.Views;

/// <summary>
/// The exercise library (<see cref="ExercisesPage"/>, once a tab) as a page of its own, opened from the Profile tab: search,
/// filters, details and custom exercises, as before.
/// </summary>
public class ExerciseLibraryPage : BasePage
{
    readonly ExercisesPage _library;

    public ExerciseLibraryPage(ExercisesPage library)
    {
        _library = library;
        // Its own title is on the page; the bar above only brings the back button.
        Title = "";
        Shell.SetTabBarIsVisible(this, false);
        _library.BottomInset = 20;
        Content = _library;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _library.Load();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _library.Unload();
    }
}
