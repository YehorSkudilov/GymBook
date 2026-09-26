using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanWorkoutEditPage : BasePage
{
    public PlanWorkoutEditPage(PlanWorkoutEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
