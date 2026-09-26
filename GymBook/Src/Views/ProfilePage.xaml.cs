using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ProfilePage : BasePage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
