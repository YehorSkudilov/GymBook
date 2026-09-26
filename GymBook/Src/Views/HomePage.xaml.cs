using GymBook.ViewModels;

namespace GymBook.Views;

public partial class HomePage : BasePage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
