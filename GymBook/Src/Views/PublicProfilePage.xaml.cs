using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PublicProfilePage : BasePage
{
    public PublicProfilePage(PublicProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
