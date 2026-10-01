using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class AccountPage : TabView
{
    public AccountPage(AccountViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
