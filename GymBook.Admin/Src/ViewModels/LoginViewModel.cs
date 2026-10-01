using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;

namespace GymBook.Admin.ViewModels;

/// <summary>Email and password sign-in for Admin and SuperAdmin accounts. Picks up a saved session by itself.</summary>
public partial class LoginViewModel(AdminApi api, AdminSession session) : BaseViewModel
{
    bool _triedSaved;

    [ObservableProperty] string email = "";
    [ObservableProperty] string password = "";

    public string Server => ApiConfig.BaseAddress.Host;

    public override async Task OnAppearingAsync()
    {
        if (_triedSaved)
            return;
        _triedSaved = true;
        await session.LoadAsync();
        Email = session.Email ?? "";
        if (session.RefreshToken == null)
            return;
        // A saved session: renew it, which also checks the account is still an admin.
        if (await RunAsync(() => api.EnsureSignedInAsync()))
            App.ShowMain();
    }

    [RelayCommand]
    async Task SignIn()
    {
        if (IsBusy)
            return;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Error = "Enter your email and password.";
            return;
        }
        if (await RunAsync(() => api.SignInAsync(Email.Trim(), Password)))
        {
            Password = "";
            App.ShowMain();
        }
    }
}
