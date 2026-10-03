using GymBook.ViewModels;

namespace GymBook.Views;

public partial class RanksPage : BasePage
{
    public RanksPage(RanksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
