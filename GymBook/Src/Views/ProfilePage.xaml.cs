using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ProfilePage : TabView
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
