using GymBook.ViewModels;

namespace GymBook.Views;

public partial class HomePage : TabView
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
