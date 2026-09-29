using GymBook.ViewModels;

namespace GymBook.Views;

public partial class AccountPage : SheetPage
{
    public AccountPage(AccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>Opens the page as a sheet over whatever is showing, inside or outside the shell.</summary>
    // The hardware back button slides it down too.
    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }

    public static Task ShowAsync(IServiceProvider services, bool register = false)
    {
        var page = services.GetRequiredService<AccountPage>();
        ((AccountViewModel)page.BindingContext).IsRegister = register;
        return Application.Current!.Windows[0].Page!.Navigation.PushModalAsync(page, false);
    }
}
