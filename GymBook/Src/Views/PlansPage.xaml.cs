using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlansPage : TabView
{
    public PlansPage(PlansViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
