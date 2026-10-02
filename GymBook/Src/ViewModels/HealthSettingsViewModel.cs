using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Health;
using GymBook.Views;

namespace GymBook.ViewModels;

/// <summary>
/// The Health data section of the Profile tab: connecting to Health Connect (which Samsung Health and most fitness apps,
/// watches and scales share their data through), which apps to read from, reading now, Health Connect's own permissions,
/// whether food logged in those apps counts, and disconnecting. What's read shows on the Nutrition tab.
/// </summary>
public partial class HealthSettingsViewModel(DataStore store, HealthSyncService health, DialogService dialogs) : ObservableObject
{
    bool _loading;

    [ObservableProperty] string title = "";
    [ObservableProperty] string status = "";
    /// <summary>The first row's action: Connect, or Read now once connected.</summary>
    [ObservableProperty] string actionText = "";
    /// <summary>This phone can read health data (Android 14+): the rows that do something show.</summary>
    [ObservableProperty] bool isAvailable;
    [ObservableProperty] bool isConnected;
    [ObservableProperty] string appsText = "";
    [ObservableProperty] bool isBusy;
    [ObservableProperty] bool importFood;

    public void Refresh()
    {
        _loading = true;
        var platform = health.Platform;
        var source = store.Profile.HealthSource;
        IsAvailable = platform.Availability == HealthAvailability.Available;
        IsConnected = health.IsConnected;
        ImportFood = store.Profile.ImportHealthFood;
        AppsText = health.Apps == null ? "Every app" : health.SourceName;
        switch (platform.Availability)
        {
            case HealthAvailability.NotSupported:
                Title = source == HealthSource.None ? "Not connected" : $"Reading {health.SourceName}";
                Status = source == HealthSource.None
                    ? "Connect in Gym Book on your Android phone: calories burned, steps and body measurements read there sync here."
                    : "Read on your Android phone and synced here.";
                break;
            case HealthAvailability.NeedsNewerAndroid:
                Title = "Not available";
                Status = "Reading health data needs Android 14 or later.";
                break;
            default:
                if (source == HealthSource.None || !platform.HasAnyPermission)
                {
                    Title = source == HealthSource.None ? "Not connected" : "Not allowed on this phone";
                    Status = "Read calories burned, steps, food, weight and body composition from Samsung Health and your other health apps, through Health Connect.";
                    ActionText = "Connect";
                }
                else
                {
                    Title = $"Reading {health.SourceName}";
                    Status = IsBusy ? "Reading…"
                        : health.LastError is { } error ? $"Couldn't read: {error}"
                        : health.LastSyncedAt is { } at ? $"Up to date · read {Ago(at)}"
                        : "Connected";
                    ActionText = "Read now";
                }
                break;
        }
        _loading = false;
    }

    static string Ago(DateTimeOffset at)
    {
        var minutes = (int)(DateTimeOffset.Now - at).TotalMinutes;
        return minutes < 1 ? "just now" : minutes < 60 ? $"{minutes} min ago" : $"at {at.ToLocalTime():t}";
    }

    partial void OnImportFoodChanged(bool value)
    {
        if (_loading || store.Profile.ImportHealthFood == value)
            return;
        store.Profile.ImportHealthFood = value;
        store.Save();
    }

    /// <summary>Connect when not connected; read now when connected.</summary>
    [RelayCommand]
    async Task Action()
    {
        if (!IsAvailable || IsBusy)
            return;
        if (!health.IsConnected)
        {
            if (!await dialogs.Confirm("Connect health data",
                    "Gym Book reads through Health Connect, where Samsung Health and most fitness apps, watches and scales share their data. Allow it to read on the next screen. In Samsung Health, also turn on Settings › Health Connect.",
                    "Continue"))
                return;
            await Busy(async () =>
            {
                if (!await health.ConnectAsync(HealthSource.HealthConnect))
                    await dialogs.Alert("Not connected", "Gym Book wasn't allowed to read anything. You can allow it any time from here or in Health Connect's settings.");
            });
            return;
        }
        await Busy(() => health.SyncAsync(force: true));
    }

    /// <summary>
    /// Which apps' data to read: a switch for each app that shared any in the last 30 days (plus any picked before); all
    /// on, or Every app, reads every app's, including ones added later.
    /// </summary>
    [RelayCommand]
    async Task ChooseApps()
    {
        if (!IsConnected || IsBusy)
            return;
        IReadOnlyList<HealthApp> found = [];
        Exception? failed = null;
        await Busy(async () =>
        {
            Status = "Finding your health apps…";
            try
            {
                found = await health.Platform.FindAppsAsync();
            }
            catch (Exception e)
            {
                failed = e;
            }
        });
        if (failed != null)
        {
            await dialogs.Alert("Couldn't look", $"Health Connect didn't answer: {failed.Message}");
            return;
        }
        var current = health.Apps;
        var apps = found.ToList();
        foreach (var package in current ?? [])
            if (apps.All(a => a.Package != package))
                apps.Add(new HealthApp(package, health.Platform.AppName(package)));
        if (apps.Count == 0)
        {
            await dialogs.Alert("No apps found",
                "No app has shared calories, steps, food or body measurements with Health Connect in the last 30 days. Turn on sharing in your health app (in Samsung Health: Settings › Health Connect) and try again.");
            return;
        }

        var picked = new HashSet<string>(current ?? apps.Select(a => a.Package));
        var switches = apps.Select(a => new MenuSwitch(a.Name, null, picked.Contains(a.Package), on =>
        {
            if (on)
                picked.Add(a.Package);
            else
                picked.Remove(a.Package);
        })).ToList();
        const string save = "Save", every = "Every app";
        var choice = await dialogs.ActionSheet("Read health data from", null, switches, save, every);
        if (choice == null)
            return;
        if (choice == save && picked.Count == 0)
        {
            await dialogs.Alert("Pick an app", "Choose at least one app to read from, or Every app.");
            return;
        }
        // Every one picked is the same as every app, which also takes in apps added later.
        await Busy(() => health.ChooseAppsAsync(choice == every || apps.All(a => picked.Contains(a.Package)) ? null : [.. picked]));
    }

    [RelayCommand]
    void OpenPermissions() => health.Platform.OpenSettings();

    [RelayCommand]
    async Task Disconnect()
    {
        if (!await dialogs.Confirm("Disconnect?", "Gym Book stops reading your health data. What was already read stays.", "Disconnect"))
            return;
        health.Disconnect();
        Refresh();
    }

    async Task Busy(Func<Task> work)
    {
        IsBusy = true;
        Refresh();
        try
        {
            await work();
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }
}
