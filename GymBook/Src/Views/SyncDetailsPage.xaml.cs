using GymBook.ViewModels;

namespace GymBook.Views;

public partial class SyncDetailsPage : BasePage
{
    public SyncDetailsPage(SyncDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
