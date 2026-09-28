using GymBook.ViewModels;

namespace GymBook.Views;

public partial class WorkoutPage : BasePage
{
    public WorkoutPage(WorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkoutViewModel.CurrentIndex))
                Dispatcher.Dispatch(() => _ = ScrollStripTo(viewModel.CurrentIndex));
        };
    }

    // Keeps the current exercise's photo in view as you swipe through the workout.
    async Task ScrollStripTo(int index)
    {
        if (index >= 0 && index < StripItems.Children.Count && StripItems.Children[index] is Element item)
            await Strip.ScrollToAsync(item, ScrollToPosition.Center, true);
    }
}
