using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.Wear;

/// <summary>
/// The watch app's settings: the account's own (weight unit, automatic rest timer; they're in the profile and sync to
/// the phone), the watch's (heart rate, buzzes), the account and syncing, and the app version.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    readonly DataStore _store;
    readonly SyncService _sync;
    readonly LiveSync _live;
    readonly WatchAccount _account;
    bool _loading;

    public SettingsViewModel(DataStore store, SyncService sync, LiveSync live, WatchAccount account)
    {
        (_store, _sync, _live, _account) = (store, sync, live, account);
        _sync.StatusChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
    }

    // The account's, synced with the phone.
    [ObservableProperty] bool usesPounds;
    [ObservableProperty] bool autoRestTimer;

    // This watch's.
    [ObservableProperty] bool heartRate;
    [ObservableProperty] bool restBuzz;
    [ObservableProperty] bool tapFeedback;

    [ObservableProperty] bool isSignedIn;
    [ObservableProperty] string account = "";
    [ObservableProperty] string syncStatus = "";

    public string Version => $"Gym Book {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    /// <summary>Signed out from here: the page goes back to the home.</summary>
    public event Action? SignedOut;

    public void Refresh()
    {
        _loading = true;
        UsesPounds = _store.Profile.Unit == WeightUnit.Lbs;
        AutoRestTimer = _store.Profile.AutoRestTimer;
        HeartRate = WatchSettings.HeartRate;
        RestBuzz = WatchSettings.RestBuzz;
        TapFeedback = WatchSettings.TapFeedback;
        IsSignedIn = _account.IsSignedIn;
        Account = _account.Email ?? "";
        SyncStatus = !IsSignedIn ? "Not signed in: workouts stay on this watch." : _sync.State switch
        {
            SyncState.Syncing => "Syncing…",
            SyncState.UpToDate => (_sync.LastSyncedAt is { } at ? $"Synced {at:HH:mm}" : "Up to date") + (_live.IsConnected ? " · live" : ""),
            SyncState.Offline => "Offline · syncs when back online",
            SyncState.SignInRequired => "Sign in again to sync",
            SyncState.Failed => _sync.Status,
            _ => "",
        };
        _loading = false;
    }

    partial void OnUsesPoundsChanged(bool value)
    {
        if (_loading)
            return;
        _store.Profile.Unit = value ? WeightUnit.Lbs : WeightUnit.Kg;
        _store.Save();
    }

    partial void OnAutoRestTimerChanged(bool value)
    {
        if (_loading)
            return;
        _store.Profile.AutoRestTimer = value;
        _store.Save();
    }

    partial void OnHeartRateChanged(bool value)
    {
        if (!_loading)
            WatchSettings.HeartRate = value;
    }

    partial void OnRestBuzzChanged(bool value)
    {
        if (!_loading)
            WatchSettings.RestBuzz = value;
    }

    partial void OnTapFeedbackChanged(bool value)
    {
        if (!_loading)
            WatchSettings.TapFeedback = value;
    }

    [RelayCommand]
    void SyncNow() => _sync.Schedule(TimeSpan.Zero);

    [RelayCommand]
    async Task SignOut()
    {
        var page = Application.Current!.Windows[0].Page!;
        var warning = _store.Local.HasPendingChanges
            ? "Some workouts haven't synced yet and will be lost."
            : "Your workouts stay in your account.";
        if (!await page.DisplayAlertAsync("Sign out?", warning, "Sign out", "Cancel"))
            return;
        await _account.SignOutAsync();
        SignedOut?.Invoke();
    }
}
