using GymBook.ViewModels;

namespace GymBook.Views;

public partial class AccountPage : BasePage
{
    public AccountPage(AccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>Opens the page modally over whatever is showing, inside or outside the shell.</summary>
    public static Task ShowAsync(IServiceProvider services, bool register = false)
    {
        var page = services.GetRequiredService<AccountPage>();
        ((AccountViewModel)page.BindingContext).IsRegister = register;
        return Application.Current!.Windows[0].Page!.Navigation.PushModalAsync(page);
    }
}
