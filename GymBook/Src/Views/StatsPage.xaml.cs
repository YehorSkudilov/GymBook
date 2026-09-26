using GymBook.ViewModels;

namespace GymBook.Views;

public partial class StatsPage : BasePage
{
    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
