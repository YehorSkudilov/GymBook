using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;
using GymBook.Admin.Views;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>
/// How many people use Gym Book, how much they train, and who joined or trained lately. The user counts open the Users
/// tab on the matching filter.
/// </summary>
public partial class DashboardViewModel(AdminApi api, AdminSession session, UsersViewModel users) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    IReadOnlyList<StatTile> userTiles = [];

    [ObservableProperty] IReadOnlyList<StatTile> trainingTiles = [];
    [ObservableProperty] IReadOnlyList<int> signups = [];
    [ObservableProperty] string signupsRange = "";
    [ObservableProperty] IReadOnlyList<int> workouts = [];
    [ObservableProperty] string workoutsRange = "";
    [ObservableProperty] IReadOnlyList<UserItem> recentUsers = [];
    [ObservableProperty] IReadOnlyList<RankedUser> mostActive = [];
    [ObservableProperty] string updated = "";

    public bool HasData => UserTiles.Count > 0;

    public string Greeting => $"Signed in as {session.Email}";

    public override Task OnAppearingAsync() => Refresh();

    [RelayCommand]
    Task Refresh() => RunAsync(async () => Show(await api.GetDashboardAsync()));

    void Show(DashboardResponse d)
    {
        UserTiles =
        [
            new("Users", Format.Number(d.TotalUsers), $"{Format.Number(d.VerifiedUsers)} verified", Filter(null)),
            new("New", Format.Number(d.NewUsers7d), $"7 days · {Format.Number(d.NewUsers30d)} in 30", Filter("new")),
            new("Active", Format.Number(d.ActiveUsers7d), "used the app in 7 days", Filter("active")),
            new("Unverified", Format.Number(d.TotalUsers - d.VerifiedUsers), "email not confirmed", Filter("unverified")),
            new("Disabled", Format.Number(d.DisabledUsers), "can't sign in", Filter("disabled")),
            new("Admins", Format.Number(d.Admins), "Admin or SuperAdmin", Filter("admins")),
        ];
        TrainingTiles =
        [
            new("Workouts", Format.Number(d.Workouts7d), $"7 days · {Format.Number(d.WorkoutsTotal)} total"),
            new("Plans", Format.Number(d.PlansTotal), "total"),
            new("AI", Format.Number(d.AiPlans7d + d.AiChats7d), $"7 days · {d.AiPlans7d} plans, {d.AiChats7d} chats",
                new AsyncRelayCommand(() => MainPage.ShowTab(AdminTab.Ai))),
        ];
        Signups = d.SignupsByDay.Select(x => x.Count).ToList();
        SignupsRange = Range(d.SignupsByDay);
        Workouts = d.WorkoutsByDay.Select(x => x.Count).ToList();
        WorkoutsRange = Range(d.WorkoutsByDay);
        RecentUsers = d.RecentUsers.Select(u => new UserItem(u, OpenUserCommand)).ToList();
        MostActive = d.MostActive7d.Select(u => new RankedUser(u.Id, u.Email, $"{u.Count} workouts", OpenUserCommand)).ToList();
        Updated = $"Updated {DateTime.Now:HH:mm}";
    }

    /// <summary>Opens the Users tab showing <paramref name="filter"/> (null: everyone).</summary>
    AsyncRelayCommand Filter(string? filter) => new(async () =>
    {
        users.SelectFilter(filter);
        await MainPage.ShowTab(AdminTab.Users);
    });

    static string Range(List<DayCount> days) =>
        days.Count == 0 ? "" : $"{Format.Date(days[0].Date)} – {Format.Date(days[^1].Date)} · {Format.Number(days.Sum(d => d.Count))} total";

    [RelayCommand]
    Task OpenUser(string? id) => UserNavigation.OpenAsync(id);

    [RelayCommand]
    Task ShowAllUsers() => Filter(null).ExecuteAsync(null);
}

public static class UserNavigation
{
    public static Task OpenAsync(string? id) =>
        id == null ? Task.CompletedTask : Shell.Current.GoToAsync($"{AppShell.UserRoute}?id={Uri.EscapeDataString(id)}");
}
