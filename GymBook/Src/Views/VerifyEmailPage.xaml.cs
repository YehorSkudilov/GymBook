using GymBook.Services.Sync;
using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>The email verification sheet. It can't be closed by the user; it closes itself once it isn't needed.</summary>
public partial class VerifyEmailPage : SheetPage
{
    public VerifyEmailPage(VerifyEmailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.Done += async (_, _) => await CloseWhenOnTopAsync();
    }

    // Closing pops the topmost modal, so wait out anything on top first (e.g. "Email changed" after fixing the address).
    async Task CloseWhenOnTopAsync()
    {
        while (Navigation.ModalStack.Contains(this) && Navigation.ModalStack[^1] != this)
            await Task.Delay(300);
        if (Navigation.ModalStack.Contains(this))
            await CloseAsync();
    }

    // Back doesn't get around it.
    protected override bool OnBackButtonPressed() => true;

    /// <summary>
    /// Shows the sheet when the signed-in account's email isn't verified, unless it's already up. Waits while another
    /// sheet or dialog is open (e.g. the sign-in sheet still closing), so it lands on top of the app rather than under it.
    /// </summary>
    public static async Task ShowIfNeededAsync(IServiceProvider services)
    {
        if (_checking)
            return;
        _checking = true;
        try
        {
            var account = services.GetRequiredService<AccountService>();
            await account.EnsureLoadedAsync();
            while (account.NeedsEmailVerification && Application.Current?.Windows.FirstOrDefault()?.Page is { } root)
            {
                var modals = root.Navigation.ModalStack;
                if (modals.Any(p => p is VerifyEmailPage))
                    return;
                if (modals.Count == 0)
                {
                    await root.Navigation.PushModalAsync(services.GetRequiredService<VerifyEmailPage>(), false);
                    return;
                }
                await Task.Delay(500);
            }
        }
        finally
        {
            _checking = false;
        }
    }

    static bool _checking;
}
