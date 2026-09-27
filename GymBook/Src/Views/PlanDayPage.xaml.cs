using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanDayPage : BasePage
{
    public PlanDayPage(PlanDayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
