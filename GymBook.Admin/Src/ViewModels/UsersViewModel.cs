using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GymBook.Admin.ViewModels;

/// <summary>Every account, newest first, searchable by email or name and filterable (the API returns up to 300).</summary>
public partial class UsersViewModel : BaseViewModel
{
    const int ListLimit = 300;

    readonly Services.AdminApi _api;
    CancellationTokenSource? _search;
    int _loads;

    public UsersViewModel(Services.AdminApi api)
    {
        _api = api;
        var select = new RelayCommand<ChipItem>(chip => SelectFilter(chip?.Value));
        // What the API's filter parameter takes; see AdminUsersController.List.
        Chips =
        [
            new("All", null, select),
            new("New", "new", select),
            new("Active", "active", select),
            new("Inactive", "inactive", select),
            new("Unverified", "unverified", select),
            new("Google", "google", select),
            new("Admins", "admins", select),
            new("Disabled", "disabled", select),
            new("Locked out", "locked", select),
        ];
        Chips[0].IsSelected = true;
    }

    public IReadOnlyList<ChipItem> Chips { get; }

    string? Filter => Chips.FirstOrDefault(c => c.IsSelected)?.Value;

    [ObservableProperty] string query = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    IReadOnlyList<UserItem> users = [];

    public string Summary => Users.Count switch
    {
        0 => IsBusy ? "Loading…" : "No users match.",
        1 => "1 user",
        ListLimit => $"The newest {ListLimit}. Search to narrow it down.",
        var n => $"{n} users",
    };

    public override Task OnAppearingAsync() => Refresh();

    /// <summary>Shows only <paramref name="filter"/> (an API filter name, or null for everyone), e.g. from a dashboard tile.</summary>
    public void SelectFilter(string? filter)
    {
        foreach (var chip in Chips)
            chip.IsSelected = chip.Value == filter;
        _ = Refresh();
    }

    // Searches as you type, once you pause.
    partial void OnQueryChanged(string value) => _ = SearchSoonAsync();

    async Task SearchSoonAsync()
    {
        _search?.Cancel();
        var cts = _search = new CancellationTokenSource();
        try
        {
            await Task.Delay(350, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        await Refresh();
    }

    [RelayCommand]
    Task Refresh() => RunAsync(async () =>
    {
        var load = ++_loads;
        var rows = await _api.GetUsersAsync(Query, Filter);
        // A newer search started while this one was out: let it win.
        if (load != _loads)
            return;
        Users = rows.Select(r => new UserItem(r, OpenUserCommand)).ToList();
    });

    [RelayCommand]
    Task OpenUser(string? id) => UserNavigation.OpenAsync(id);
}
