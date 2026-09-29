using GymBook.ViewModels;

namespace GymBook.Views;

public partial class AccountPage : SheetPage
{
    AccountViewModel ViewModel => (AccountViewModel)BindingContext;

    public AccountPage(AccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // The keyboard's return key moves to the next field shown, and submits from the last one.
    void OnEmailCompleted(object? sender, EventArgs e)
    {
        if (ViewModel.ShowCode)
            CodeEntry.Focus();
        else if (ViewModel.ShowPassword)
            PasswordEntry.Focus();
        else
            ViewModel.SubmitCommand.Execute(null);
    }

    void OnCodeCompleted(object? sender, EventArgs e) => PasswordEntry.Focus();

    void OnPasswordCompleted(object? sender, EventArgs e)
    {
        if (ViewModel.ShowConfirm)
            ConfirmEntry.Focus();
        else
            ViewModel.SubmitCommand.Execute(null);
    }

    void OnConfirmCompleted(object? sender, EventArgs e) => ViewModel.SubmitCommand.Execute(null);

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
        ((AccountViewModel)page.BindingContext).Mode = register ? AccountMode.Register : AccountMode.SignIn;
        return Application.Current!.Windows[0].Page!.Navigation.PushModalAsync(page, false);
    }
}
