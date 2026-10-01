using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;

namespace GymBook.Admin.ViewModels;

/// <summary>Every account, newest first, searchable by email or name and filterable (the API returns up to 300).</summary>
public partial class UsersViewModel(AdminApi api) : BaseViewModel
{
    public static readonly IReadOnlyList<string> Filters = ["All", "Admins", "Disabled", "Unverified"];

    CancellationTokenSource? _search;
    int _loads;

    [ObservableProperty] string query = "";
    [ObservableProperty] string filter = "All";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    IReadOnlyList<UserItem> users = [];

    public IReadOnlyList<string> FilterOptions => Filters;

    public string Summary => Users.Count switch
    {
        0 => IsBusy ? "" : "No users match.",
        1 => "1 user",
        300 => "Showing the newest 300; search to narrow it down.",
        var n => $"{n} users",
    };

    public override Task OnAppearingAsync() => Refresh();

    // Searches as you type, once you pause.
    partial void OnQueryChanged(string value) => _ = SearchSoonAsync();

    partial void OnFilterChanged(string value) => _ = Refresh();

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
        var filter = Filter == "All" ? null : Filter.ToLowerInvariant();
        var rows = await api.GetUsersAsync(Query, filter);
        // A newer search started while this one was out: let it win.
        if (load != _loads)
            return;
        Users = rows.Select(r => new UserItem(r, OpenUserCommand)).ToList();
    });

    [RelayCommand]
    Task OpenUser(string? id) => id == null ? Task.CompletedTask : Shell.Current.GoToAsync($"{AppShell.UserRoute}?id={Uri.EscapeDataString(id)}");
}
