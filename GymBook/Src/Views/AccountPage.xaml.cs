using GymBook.ViewModels;

namespace GymBook.Views;

public partial class AccountPage : SheetPage
{
    AccountViewModel ViewModel => (AccountViewModel)BindingContext;

    public AccountPage(AccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            Card.SizeChanged += (_, _) => FitColumns();
    }

    // Desktop: the app's side next to the form when the window has room for both, the form alone when it doesn't.
    void FitColumns()
    {
        var wide = Card.Width >= 760;
        if (Brand.IsVisible == wide)
            return;
        Brand.IsVisible = wide;
        Columns.ColumnDefinitions[0].Width = wide ? new GridLength(0.42, GridUnitType.Star) : new GridLength(0);
        Columns.ColumnDefinitions[1].Width = wide ? new GridLength(0.58, GridUnitType.Star) : GridLength.Star;
    }

    // A required sign-in taken away by something else (e.g. a notification opening a page) comes straight back.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Closed while Google sign-in waits on the browser: stop waiting.
        if (ViewModel.IsWaitingForGoogle)
            ViewModel.CancelGoogleCommand.Execute(null);
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

    /// <summary>Opens the page as a sheet over whatever is showing, inside or outside the shell.</summary>
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
