using GymBook.ViewModels;

namespace GymBook.Views;

public partial class SessionDetailPage : BasePage
{
    public SessionDetailPage(SessionDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
