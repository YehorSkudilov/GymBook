using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services.Sync;
using GymBook.Views;

namespace GymBook.ViewModels;

/// <summary>
/// The sync pill in the Home header: whether the account is syncing, up to date, offline or needs attention.
/// Hidden while signed out. A singleton, so it follows the sync service for the app's lifetime.
/// </summary>
public partial class SyncIndicator : ObservableObject
{
    readonly SyncService _sync;
    readonly AuthSession _session;
    readonly IServiceProvider _services;

    [ObservableProperty] bool isVisible;
    [ObservableProperty] bool isSyncing;
    [ObservableProperty] string text = "";
    [ObservableProperty] Color dotColor = Colors.Transparent;

    public SyncIndicator(SyncService sync, AuthSession session, IServiceProvider services)
    {
        (_sync, _session, _services) = (sync, session, services);
        sync.StatusChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Update);
        session.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(Update);
        Update();
    }

    void Update()
    {
        var state = _session.IsSignedIn || _sync.State == SyncState.SignInRequired ? _sync.State : SyncState.SignedOut;
        IsVisible = state != SyncState.SignedOut;
        IsSyncing = state == SyncState.Syncing;
        (Text, var color) = state switch
        {
            SyncState.Syncing => ("Syncing", "Accent"),
            SyncState.UpToDate => ("Synced", "Success"),
            SyncState.Offline => ("Offline", "TextTertiary"),
            SyncState.SignInRequired => ("Sign in", "Warning"),
            SyncState.Failed => ("Sync issue", "Warning"),
            _ => ("", "TextTertiary"),
        };
        DotColor = Application.Current!.Resources.TryGetValue(color, out var c) && c is Color resolved ? resolved : Colors.Gray;
    }

    /// <summary>Tapping syncs now, or asks to sign in again when the session has ended.</summary>
    [RelayCommand]
    async Task Tap()
    {
        if (_sync.State == SyncState.SignInRequired || !_session.IsSignedIn)
        {
            await AccountPage.ShowAsync(_services);
            return;
        }
        if (_sync.State == SyncState.Syncing)
            return;
        await _sync.SyncNowAsync();
    }
}
