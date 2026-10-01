using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class LoginPage : BasePage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
