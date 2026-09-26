using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlansPage : BasePage
{
    public PlansPage(PlansViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
