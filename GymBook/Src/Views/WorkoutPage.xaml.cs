using GymBook.ViewModels;

namespace GymBook.Views;

public partial class WorkoutPage : BasePage
{
    public WorkoutPage(WorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
