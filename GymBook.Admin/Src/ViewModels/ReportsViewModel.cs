using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

public class ReportItem
{
    public required RankReportRow Row { get; init; }
    public string Title => $"@{Row.TargetUsername}" + (Row.Lift is { } lift ? $" · {lift}" : " · profile");
    public string Detail => $"{Row.TargetEmail} · reported by @{Row.Reporter} · {Format.Ago(Row.CreatedAt)}"
        + (Row.OpenAboutTarget > 1 ? $" · {Row.OpenAboutTarget} open reports" : "");
    public string Reason => Row.Reason.Length > 0 ? $"\"{Row.Reason}\"" : "No reason given";
    public bool IsHidden => Row.TargetHidden;
    public required IAsyncRelayCommand OpenCommand { get; init; }
}

/// <summary>
/// Reports from users about someone's ranked lifts or public profile: hide them from the leaderboards, clear their
/// bio and picture, look at the account, or close the reports.
/// </summary>
public partial class ReportsViewModel(AdminApi api) : BaseViewModel
{
    bool _all;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    IReadOnlyList<ReportItem> reports = [];

    [ObservableProperty] string showLabel = "Show resolved";

    public bool IsEmpty => Reports.Count == 0 && !IsBusy && !HasError;

    public override Task OnAppearingAsync() => Refresh();

    [RelayCommand]
    Task Refresh() => RunAsync(async () =>
    {
        var rows = await api.GetRankReportsAsync(_all);
        Reports = [.. rows.Select(r => new ReportItem { Row = r, OpenCommand = new AsyncRelayCommand(() => OpenAsync(r)) })];
    });

    [RelayCommand]
    Task ToggleAll()
    {
        _all = !_all;
        ShowLabel = _all ? "Open only" : "Show resolved";
        return Refresh();
    }

    async Task OpenAsync(RankReportRow r)
    {
        const string resolve = "Close reports (nothing wrong)", clear = "Clear bio, gym and picture", account = "Open account";
        var hide = r.TargetHidden ? "Show on leaderboards again" : "Hide from leaderboards";
        var choice = await CurrentPage.DisplayActionSheetAsync($"@{r.TargetUsername}", "Cancel", null, hide, clear, resolve, account);
        switch (choice)
        {
            case resolve:
                await RunAndRefresh(() => api.ResolveRankReportAsync(r.Id));
                break;
            case clear:
                if (await ConfirmAsync("Clear their public profile?", $"@{r.TargetUsername}'s bio, home gym and picture are removed. Their username stays.", "Clear"))
                    await RunAndRefresh(async () =>
                    {
                        await api.ClearPublicProfileAsync(r.TargetUserId);
                        await api.ResolveRankReportAsync(r.Id);
                    });
                break;
            case account:
                await Shell.Current.GoToAsync($"{AppShell.UserRoute}?id={Uri.EscapeDataString(r.TargetUserId)}");
                break;
            case { } c when c == hide:
                var hidden = !r.TargetHidden;
                if (await ConfirmAsync(hidden ? "Hide from leaderboards?" : "Show again?",
                        hidden ? $"@{r.TargetUsername} disappears from every board. They still see their own ranks." : $"@{r.TargetUsername} is back on the boards.",
                        hidden ? "Hide" : "Show"))
                    await RunAndRefresh(async () =>
                    {
                        await api.SetRankHiddenAsync(r.TargetUserId, hidden);
                        if (hidden)
                            await api.ResolveRankReportAsync(r.Id);
                    });
                break;
        }
    }

    async Task RunAndRefresh(Func<Task> work)
    {
        if (await RunAsync(work))
            await Refresh();
    }
}
