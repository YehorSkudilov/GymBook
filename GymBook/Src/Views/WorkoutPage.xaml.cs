using GymBook.ViewModels;

namespace GymBook.Views;

public partial class WorkoutPage : SheetPage
{
    public WorkoutPage(WorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HorizontalMouseScroll.Attach(Strip);
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkoutViewModel.CurrentIndex))
                Dispatcher.Dispatch(() =>
                {
                    ScrollStripTo(viewModel.CurrentIndex);
                    // Each exercise starts at its top.
                    _ = ExerciseScroll.ScrollToAsync(0, 0, false);
                });
        };
    }

    // Keeps the current exercise's photo in view as you swipe through the workout.
    void ScrollStripTo(int index)
    {
        if (BindingContext is WorkoutViewModel vm && index >= 0 && index < vm.Exercises.Count)
            Strip.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
    }
}
