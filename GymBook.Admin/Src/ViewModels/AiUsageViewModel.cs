using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>AI plans and plan chat messages (each a paid OpenAI call) over the last days, the quotas, and the heaviest users.</summary>
public partial class AiUsageViewModel : BaseViewModel
{
    readonly AdminApi _api;
    int _days = 30;

    public AiUsageViewModel(AdminApi api)
    {
        _api = api;
        var select = new RelayCommand<ChipItem>(chip => SelectRange(int.Parse(chip!.Value!)));
        Ranges = [new("7 days", "7", select), new("30 days", "30", select), new("90 days", "90", select)];
        Ranges[1].IsSelected = true;
    }

    public IReadOnlyList<ChipItem> Ranges { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    IReadOnlyList<StatTile> tiles = [];

    [ObservableProperty] IReadOnlyList<StatTile> quotaTiles = [];
    [ObservableProperty] IReadOnlyList<int> plansByDay = [];
    [ObservableProperty] IReadOnlyList<int> chatsByDay = [];
    [ObservableProperty] string daysRange = "";
    [ObservableProperty] IReadOnlyList<RankedUser> topUsers = [];

    public bool HasData => Tiles.Count > 0;

    public bool HasTopUsers => TopUsers.Count > 0;

    partial void OnTopUsersChanged(IReadOnlyList<RankedUser> value) => OnPropertyChanged(nameof(HasTopUsers));

    public override Task OnAppearingAsync() => Refresh();

    void SelectRange(int days)
    {
        _days = days;
        foreach (var range in Ranges)
            range.IsSelected = range.Value == days.ToString();
        _ = Refresh();
    }

    [RelayCommand]
    Task Refresh() => RunAsync(async () => Show(await _api.GetAiUsageAsync(_days)));

    void Show(AiUsageResponse a)
    {
        var perUser = a.Users == 0 ? 0 : (double)(a.Plans + a.Chats) / a.Users;
        Tiles =
        [
            new("AI plans", Format.Number(a.Plans), $"in {a.Days} days"),
            new("Chat messages", Format.Number(a.Chats), $"in {a.Days} days"),
            new("Users", Format.Number(a.Users), "used AI"),
            new("Per user", perUser.ToString("0.#"), "calls on average"),
        ];
        QuotaTiles =
        [
            new("Plan quota", Format.Number(a.PlanQuota.Limit), Format.Period(a.PlanQuota.Period)),
            new("Chat quota", Format.Number(a.ChatQuota.Limit), Format.Period(a.ChatQuota.Period)),
        ];
        PlansByDay = a.ByDay.Select(d => d.Plans).ToList();
        ChatsByDay = a.ByDay.Select(d => d.Chats).ToList();
        DaysRange = a.ByDay.Count == 0 ? "" : $"{Format.Date(a.ByDay[0].Date)} – {Format.Date(a.ByDay[^1].Date)}";
        TopUsers = a.TopUsers.Select(u => new RankedUser(u.Id, u.Email, $"{u.Plans} plans · {u.Chats} chats", OpenUserCommand)).ToList();
    }

    [RelayCommand]
    Task OpenUser(string? id) => UserNavigation.OpenAsync(id);
}
