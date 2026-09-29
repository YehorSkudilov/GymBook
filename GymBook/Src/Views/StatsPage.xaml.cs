using GymBook.ViewModels;

namespace GymBook.Views;

public partial class StatsPage : TabView
{
    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HorizontalMouseScroll.Attach(LiftStrip);
    }
}
