using GymBook.ViewModels;

namespace GymBook.Views;

public partial class HistoryPage : BasePage
{
    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
