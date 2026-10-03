using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanWorkoutsPage : BasePage
{
    public PlanWorkoutsPage(PlanWorkoutsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
