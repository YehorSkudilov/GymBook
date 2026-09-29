using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ManageAccountPage : SheetPage
{
    ManageAccountViewModel ViewModel => (ManageAccountViewModel)BindingContext;

    public ManageAccountPage(ManageAccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // The keyboard's return key moves to the next field shown, and submits from the last one.
    void OnEmailCompleted(object? sender, EventArgs e)
    {
        if (ViewModel.ShowCurrentPassword)
            CurrentEntry.Focus();
        else
            OnSubmit(sender, e);
    }

    void OnCurrentCompleted(object? sender, EventArgs e)
    {
        if (ViewModel.IsPassword)
            NewEntry.Focus();
        else
            OnSubmit(sender, e);
    }

    void OnCodeCompleted(object? sender, EventArgs e)
    {
        if (ViewModel.Resetting)
            NewEntry.Focus();
        else
            OnSubmit(sender, e);
    }

    void OnNewCompleted(object? sender, EventArgs e) => ConfirmEntry.Focus();

    void OnSubmit(object? sender, EventArgs e) => ViewModel.SubmitCommand.Execute(null);

    // The hardware back button slides it down too.
    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }

    /// <summary>Opens the sheet over whatever is showing.</summary>
    public static Task ShowAsync(IServiceProvider services, AccountChange change)
    {
        var page = services.GetRequiredService<ManageAccountPage>();
        ((ManageAccountViewModel)page.BindingContext).Change = change;
        return Application.Current!.Windows[0].Page!.Navigation.PushModalAsync(page, false);
    }
}
