using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>How many people use Gym Book, how much they train, and who joined or trained lately.</summary>
public partial class DashboardViewModel(AdminApi api, AdminSession session) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    IReadOnlyList<StatTile> tiles = [];

    [ObservableProperty] IReadOnlyList<int> signups = [];
    [ObservableProperty] string signupsRange = "";
    [ObservableProperty] IReadOnlyList<int> workouts = [];
    [ObservableProperty] string workoutsRange = "";
    [ObservableProperty] IReadOnlyList<UserItem> recentUsers = [];
    [ObservableProperty] IReadOnlyList<RankedUser> mostActive = [];

    public bool HasData => Tiles.Count > 0;

    public string SignedInAs => $"{session.Email} · {session.Role}";

    public override Task OnAppearingAsync() => Refresh();

    [RelayCommand]
    Task Refresh() => RunAsync(async () => Show(await api.GetDashboardAsync()));

    void Show(DashboardResponse d)
    {
        Tiles =
        [
            new("Users", Format.Number(d.TotalUsers), $"{Format.Number(d.VerifiedUsers)} verified"),
            new("New users", Format.Number(d.NewUsers7d), $"7 days · {Format.Number(d.NewUsers30d)} in 30"),
            new("Active users", Format.Number(d.ActiveUsers7d), "trained in 7 days"),
            new("Workouts", Format.Number(d.Workouts7d), $"7 days · {Format.Number(d.WorkoutsTotal)} total"),
            new("Plans", Format.Number(d.PlansTotal), "total"),
            new("AI", Format.Number(d.AiPlans7d + d.AiChats7d), $"7 days · {d.AiPlans7d} plans, {d.AiChats7d} chats"),
            new("Disabled", Format.Number(d.DisabledUsers)),
            new("Admins", Format.Number(d.Admins)),
        ];
        Signups = d.SignupsByDay.Select(x => x.Count).ToList();
        SignupsRange = Range(d.SignupsByDay);
        Workouts = d.WorkoutsByDay.Select(x => x.Count).ToList();
        WorkoutsRange = Range(d.WorkoutsByDay);
        RecentUsers = d.RecentUsers.Select(u => new UserItem(u, OpenUserCommand)).ToList();
        MostActive = d.MostActive7d.Select(u => new RankedUser(u.Id, u.Email, $"{u.Count} workouts", OpenUserCommand)).ToList();
    }

    static string Range(List<DayCount> days) =>
        days.Count == 0 ? "" : $"{Format.Date(days[0].Date)} – {Format.Date(days[^1].Date)} · {Format.Number(days.Sum(d => d.Count))} total";

    [RelayCommand]
    Task OpenUser(string? id) => id == null ? Task.CompletedTask : Shell.Current.GoToAsync($"{AppShell.UserRoute}?id={Uri.EscapeDataString(id)}");

    [RelayCommand]
    async Task SignOut()
    {
        if (!await ConfirmAsync("Sign out?", $"You're signed in as {session.Email}.", "Sign out"))
            return;
        await api.SignOutAsync();
        App.ShowSignIn();
    }
}
