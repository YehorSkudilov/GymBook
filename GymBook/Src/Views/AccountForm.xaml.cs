using GymBook.ViewModels;

namespace GymBook.Views;

public partial class AccountForm : ContentView
{
    AccountViewModel ViewModel => (AccountViewModel)BindingContext;

    public AccountForm()
    {
        InitializeComponent();
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
}
