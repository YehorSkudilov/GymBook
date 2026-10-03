using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanSharePage : BasePage
{
    public PlanSharePage(PlanShareViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
