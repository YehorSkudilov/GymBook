using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>AI plans and plan chat messages (each a paid OpenAI call) over the last days, the quotas, and the heaviest users.</summary>
public partial class AiUsageViewModel : BaseViewModel
{
    readonly AdminApi _api;
    readonly AdminSession _session;
    int _days = 30;

    public AiUsageViewModel(AdminApi api, AdminSession session)
    {
        _api = api;
        _session = session;
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
    Task Refresh() => RunAsync(async () =>
    {
        Show(await _api.GetAiUsageAsync(_days));
        ProRequired = (await _api.GetSettingsAsync()).ProRequired;
    });

    /// <summary>Whether the AI features need Gym Book Pro (see AdminSettings); null until loaded.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProSetting), nameof(ProSettingText), nameof(ProSettingButton))]
    bool? proRequired;

    public bool HasProSetting => ProRequired != null;

    public string ProSettingText => ProRequired == false
        ? "Off: the AI features are free for everyone, within the quotas."
        : "On: the AI features need Gym Book Pro (bought, or given by an admin).";

    public string ProSettingButton => ProRequired == false ? "Require Pro" : "Make AI free for everyone";

    /// <summary>Only a SuperAdmin changes it; the API refuses anyone else.</summary>
    public bool CanChangeSettings => _session.IsSuperAdmin;

    [RelayCommand]
    async Task ToggleProRequired()
    {
        if (ProRequired is not { } required)
            return;
        var (title, message) = required
            ? ("Make AI free for everyone?", "Every signed-in user can use the AI features without Gym Book Pro, within the quotas. Each use is a paid OpenAI call.")
            : ("Require Gym Book Pro?", "Users without Pro (bought, or given by an admin) can't use the AI features any more.");
        if (!await ConfirmAsync(title, message, required ? "Make free" : "Require Pro"))
            return;
        if (await RunAsync(() => _api.SetSettingsAsync(new AdminSettings(!required))))
            ProRequired = !required;
        else if (HasError)
            await AlertAsync("Couldn't do that", Error);
    }

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
