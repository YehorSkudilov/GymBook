using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class UsersPage : TabView
{
    public UsersPage(UsersViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
