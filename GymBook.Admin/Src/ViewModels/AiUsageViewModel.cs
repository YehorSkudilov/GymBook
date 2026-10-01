using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>AI plans and plan chat messages (each a paid OpenAI call) over the last days, the quotas, and the heaviest users.</summary>
public partial class AiUsageViewModel(AdminApi api) : BaseViewModel
{
    public static readonly IReadOnlyList<string> Ranges = ["7 days", "30 days", "90 days"];

    [ObservableProperty] string range = "30 days";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    IReadOnlyList<StatTile> tiles = [];

    [ObservableProperty] IReadOnlyList<int> plansByDay = [];
    [ObservableProperty] IReadOnlyList<int> chatsByDay = [];
    [ObservableProperty] string daysRange = "";
    [ObservableProperty] IReadOnlyList<RankedUser> topUsers = [];

    public IReadOnlyList<string> RangeOptions => Ranges;

    public bool HasData => Tiles.Count > 0;

    public override Task OnAppearingAsync() => Refresh();

    partial void OnRangeChanged(string value) => _ = Refresh();

    [RelayCommand]
    Task Refresh() => RunAsync(async () => Show(await api.GetAiUsageAsync(int.Parse(Range.Split(' ')[0]))));

    void Show(AiUsageResponse a)
    {
        Tiles =
        [
            new("AI plans", Format.Number(a.Plans), $"in {a.Days} days"),
            new("Chat messages", Format.Number(a.Chats), $"in {a.Days} days"),
            new("Users", Format.Number(a.Users), "used AI"),
            new("Plan quota", Format.Number(a.PlanQuota.Limit), Format.Period(a.PlanQuota.Period)),
            new("Chat quota", Format.Number(a.ChatQuota.Limit), Format.Period(a.ChatQuota.Period)),
        ];
        PlansByDay = a.ByDay.Select(d => d.Plans).ToList();
        ChatsByDay = a.ByDay.Select(d => d.Chats).ToList();
        DaysRange = a.ByDay.Count == 0 ? "" : $"{Format.Date(a.ByDay[0].Date)} – {Format.Date(a.ByDay[^1].Date)}";
        TopUsers = a.TopUsers.Select(u => new RankedUser(u.Id, u.Email, $"{u.Plans} plans · {u.Chats} chats", OpenUserCommand)).ToList();
    }

    [RelayCommand]
    Task OpenUser(string? id) => id == null ? Task.CompletedTask : Shell.Current.GoToAsync($"{AppShell.UserRoute}?id={Uri.EscapeDataString(id)}");
}
