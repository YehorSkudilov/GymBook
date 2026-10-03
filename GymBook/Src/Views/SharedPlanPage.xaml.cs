using GymBook.ViewModels;

namespace GymBook.Views;

public partial class SharedPlanPage : BasePage
{
    public SharedPlanPage(SharedPlanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
