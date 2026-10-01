using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;

namespace GymBook.Admin.ViewModels;

/// <summary>Who's signed in, with what role, against which server, and signing out.</summary>
public partial class AccountViewModel(AdminApi api, AdminSession session) : BaseViewModel
{
    public string Email => session.Email ?? "";
    public string Initial => Initials.Of(null, Email);
    public string Role => session.Role ?? "";

    public string RoleDetail => session.IsSuperAdmin
        ? "Sees every user and the stats, acts on any account, changes roles and deletes accounts."
        : "Sees every user and the stats, and acts on regular accounts. Roles and deleting are for SuperAdmins.";

    public IReadOnlyList<InfoRow> Details =>
    [
        new("Server", ApiConfig.BaseAddress.ToString()),
        new("App version", AppInfo.Current.VersionString),
        new("User ID", session.UserId ?? ""),
    ];

    public override Task OnAppearingAsync()
    {
        // The role comes from the newest access token, which may have changed since the tab was built.
        OnPropertyChanged(nameof(Role));
        OnPropertyChanged(nameof(RoleDetail));
        return Task.CompletedTask;
    }

    [RelayCommand]
    async Task SignOut()
    {
        if (!await ConfirmAsync("Sign out?", $"You're signed in as {Email}.", "Sign out"))
            return;
        await api.SignOutAsync();
        App.ShowSignIn();
    }
}
