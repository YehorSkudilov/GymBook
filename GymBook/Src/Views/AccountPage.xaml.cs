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
    // A required sign-in taken away by something else (e.g. a notification opening a page) comes straight back.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (ViewModel.IsRequired)
            _ = SignInGate.ShowIfNeededAsync(Handler?.MauiContext?.Services ?? IPlatformApplication.Current!.Services);
    }

    // The hardware back button slides it down too, unless signing in is required (see SignInGate).
    protected override bool OnBackButtonPressed()
    {
        if (ViewModel.CanClose)
            _ = CloseAsync();
        return true;
    }

    /// <param name="required">From <see cref="SignInGate"/>: it can't be closed without signing in.</param>
    public static Task ShowAsync(IServiceProvider services, bool register = false, bool required = false)
    {
        var page = services.GetRequiredService<AccountPage>();
        var viewModel = (AccountViewModel)page.BindingContext;
        viewModel.Mode = register ? AccountMode.Register : AccountMode.SignIn;
        viewModel.IsRequired = required;
        return Application.Current!.Windows[0].Page!.Navigation.PushModalAsync(page, false);
    }
}
