using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>One change waiting to sync, as listed on the sync details page.</summary>
public class SyncDetailItem
{
    public required string Title { get; init; }
    public required string Detail { get; init; }
    public required bool Blocked { get; init; }
    public Color DetailColor => Blocked ? Color.FromArgb("#FFB020") : Color.FromArgb("#9AA3B5");
}

/// <summary>What hasn't synced yet and why: the last sync's error, and every local change still waiting to upload.</summary>
public partial class SyncDetailsViewModel(SyncService sync, DataStore store) : BaseViewModel
{
    [ObservableProperty] string status = "";
    [ObservableProperty] bool hasProblem;
    [ObservableProperty] List<SyncDetailItem> items = [];
    [ObservableProperty] bool isEmpty;
    [ObservableProperty] bool isSyncing;

    public override Task OnAppearingAsync()
    {
        sync.StatusChanged += OnChanged;
        store.Changed += OnChanged;
        Refresh();
        return Task.CompletedTask;
    }

    public override void OnDisappearing()
    {
        sync.StatusChanged -= OnChanged;
        store.Changed -= OnChanged;
    }

    void OnChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    void Refresh()
    {
        IsSyncing = sync.State == SyncState.Syncing;
        HasProblem = sync.State is SyncState.Failed or SyncState.Offline or SyncState.SignInRequired;
        Status = sync.State switch
        {
            SyncState.UpToDate when sync.LastSyncedAt is { } at => $"Last synced {at:t}",
            SyncState.SignedOut => "Sign in to sync your data.",
            _ => sync.Status,
        };
        Items = sync.PendingItems().Select(i => new SyncDetailItem
        {
            Title = i.Name,
            Detail = (i.IsDeletion ? $"{i.Kind} · deleted" : i.Kind) + (i.Blocked ? " · too large to sync: shorten its name or notes" : " · waiting to upload"),
            Blocked = i.Blocked,
        }).ToList();
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    async Task SyncNow()
    {
        await sync.SyncNowAsync();
        Refresh();
    }
}
