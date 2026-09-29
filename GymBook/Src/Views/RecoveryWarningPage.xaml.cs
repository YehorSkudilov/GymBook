using GymBook.ViewModels;

namespace GymBook.Views;

public partial class RecoveryWarningPage : SheetPage
{
    public RecoveryWarningPage(RecoveryWarningViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
