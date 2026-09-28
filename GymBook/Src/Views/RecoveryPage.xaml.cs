using GymBook.ViewModels;

namespace GymBook.Views;

public partial class RecoveryPage : BasePage
{
    public RecoveryPage(RecoveryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
