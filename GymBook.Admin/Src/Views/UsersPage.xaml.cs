using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class UsersPage : BasePage
{
    public UsersPage(UsersViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
