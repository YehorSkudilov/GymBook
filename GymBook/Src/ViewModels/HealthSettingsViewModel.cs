using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Health;
using GymBook.Views;

namespace GymBook.ViewModels;

/// <summary>
/// The Health data section of the Profile tab: connecting to Samsung Health itself or to Health Connect (which most
/// fitness apps, watches and scales share their data through) and which of its apps to read, reading now, where access
/// is changed, and disconnecting. What's read (food logged in those apps included) shows on the Nutrition tab.
/// </summary>
public partial class HealthSettingsViewModel(DataStore store, HealthSyncService health, DialogService dialogs) : ObservableObject
{
    [ObservableProperty] string title = "";
    [ObservableProperty] string status = "";
    /// <summary>The first row's action: Connect, or Sync now once connected.</summary>
    [ObservableProperty] string actionText = "";
    /// <summary>This phone can read health data (Samsung Health, or Health Connect on Android 14+): the rows that do something show.</summary>
    [ObservableProperty] bool isAvailable;
    [ObservableProperty] bool isConnected;
    [ObservableProperty] string appsText = "";
    [ObservableProperty] bool isBusy;

    // Sending Gym Book's workouts, food and weights to the health app
    [ObservableProperty] bool showSendBack;
    [ObservableProperty] bool sendBack;
    [ObservableProperty] string sendBackTitle = "";
    [ObservableProperty] string sendBackStatus = "";
    bool _showingSendBack;

    const string SamsungHealthOption = "Samsung Health", HealthConnectOption = "Health Connect";

    public void Refresh()
    {
        var source = store.Profile.HealthSource;
        var platform = health.Platform;
        // Before connecting, reading is possible if either Samsung Health or Health Connect can be read.
        var availability = source == HealthSource.None && health.CanReadSamsungHealth ? HealthAvailability.Available : platform.Availability;
        IsAvailable = availability == HealthAvailability.Available;
        IsConnected = health.IsConnected;
        AppsText = source == HealthSource.SamsungHealth ? "Samsung Health" : health.Apps == null ? "Health Connect" : health.SourceName;
        var writeBack = health.WriteBack;
        ShowSendBack = health.IsConnected && writeBack.IsAvailable;
        _showingSendBack = true;
        SendBack = writeBack.IsEnabled;
        _showingSendBack = false;
        SendBackTitle = $"Send to {(source == HealthSource.SamsungHealth ? "Samsung Health" : "Health Connect")}";
        SendBackStatus = writeBack.IsEnabled && writeBack.Status.Length > 0 ? writeBack.Status : "Workouts, food and weights from Gym Book go there too.";
        switch (availability)
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
            case HealthAvailability.NotInstalled:
                // Samsung Health was chosen, and it's gone: connect again (to Health Connect, say).
                Title = "Samsung Health isn't on this phone";
                Status = "Install Samsung Health, or read from your other health apps through Health Connect instead.";
                ActionText = "Connect";
                IsAvailable = true;
                break;
            default:
                if (source == HealthSource.None || !platform.HasAnyPermission)
                {
                    Title = source == HealthSource.None ? "Not connected" : "Not allowed on this phone";
                    Status = "Read calories burned, steps, food, weight and body composition from Samsung Health, or from your other health apps through Health Connect.";
                    ActionText = "Connect";
                }
                else
                {
                    Title = $"Reading {health.SourceName}";
                    Status = IsBusy ? "Reading…"
                        : health.LastError is { } error ? $"Couldn't read: {error}"
                        : health.IsReadingHistory ? "Reading your older data…"
                        : health.LastSyncedAt is { } at ? $"Up to date · read {Ago(at)}"
                        : "Connected";
                    ActionText = "Sync now";
                }
                break;
        }
    }

    static string Ago(DateTimeOffset at)
    {
        var minutes = (int)(DateTimeOffset.Now - at).TotalMinutes;
        return minutes < 1 ? "just now" : minutes < 60 ? $"{minutes} min ago" : $"at {at.ToLocalTime():t}";
    }

    /// <summary>Connect when not connected (to Samsung Health or Health Connect); sync now when connected (read, then send back if on).</summary>
    [RelayCommand]
    async Task Action()
    {
        if (!IsAvailable || IsBusy)
            return;
        if (!health.IsConnected)
        {
            if (await ChooseSourceAsync() is { } source)
                await ConnectAsync(source);
            return;
        }
        await Busy(() => health.SyncAsync(force: true));
    }

    /// <summary>Samsung Health itself or Health Connect when Samsung Health is on the phone, else Health Connect; null when cancelled.</summary>
    async Task<HealthSource?> ChooseSourceAsync()
    {
        if (!health.CanReadSamsungHealth)
            return HealthSource.HealthConnect;
        return await dialogs.ActionSheet("Read health data from", null, SamsungHealthOption, HealthConnectOption) switch
        {
            SamsungHealthOption => HealthSource.SamsungHealth,
            HealthConnectOption => HealthSource.HealthConnect,
            _ => null,
        };
    }

    async Task ConnectAsync(HealthSource source)
    {
        var samsung = source == HealthSource.SamsungHealth;
        if (!await dialogs.Confirm("Connect health data", samsung
                ? "Gym Book reads Samsung Health directly: calories burned and steps exactly as it shows them, and your food, weight and body composition. Allow it to read on the next screen."
                : "Gym Book reads through Health Connect, where most fitness apps, watches and scales share their data. Allow it to read on the next screen.",
                "Continue"))
            return;
        await Busy(async () =>
        {
            if (!await health.ConnectAsync(source))
                await dialogs.Alert("Not connected", health.PlatformFor(source).PermissionProblem
                    ?? $"Gym Book wasn't allowed to read anything. You can allow it any time from here or in {(samsung ? "Samsung Health" : "Health Connect's settings")}.");
        });
    }

    /// <summary>
    /// Where to read from: Samsung Health itself, or Health Connect and which of its apps (a switch for each app that
    /// shared any in the last 30 days, plus any picked before; all on reads every app's, including ones added later).
    /// </summary>
    [RelayCommand]
    async Task ChooseApps()
    {
        if (!IsConnected || IsBusy)
            return;
        if (await ChooseSourceAsync() is not { } source)
            return;
        if (source == HealthSource.SamsungHealth)
        {
            if (store.Profile.HealthSource != HealthSource.SamsungHealth)
                await ConnectAsync(source);
            return;
        }
        // Health Connect: allowed first, if it wasn't (coming from Samsung Health), then which apps.
        var connect = health.PlatformFor(HealthSource.HealthConnect);
        if (!connect.HasAnyPermission)
        {
            await ConnectAsync(source);
            return;
        }

        IReadOnlyList<HealthApp> found = [];
        Exception? failed = null;
        await Busy(async () =>
        {
            Status = "Finding your health apps…";
            try
            {
                found = await connect.FindAppsAsync();
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
                apps.Add(new HealthApp(package, connect.AppName(package)));
        if (apps.Count == 0)
        {
            await dialogs.Alert("No apps found",
                "No app has shared calories, steps, food or body measurements with Health Connect in the last 30 days. Turn on sharing in your health app and try again.");
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
        if (!await dialogs.Switches("Read from Health Connect", "With every app on, apps you add later are read too.", switches))
            return;
        if (picked.Count == 0)
        {
            await dialogs.Alert("Pick an app", "Turn on at least one app to read from.");
            return;
        }
        // Every one picked is the same as every app, which also takes in apps added later.
        await Busy(() => health.ChooseAppsAsync(apps.All(a => picked.Contains(a.Package)) ? null : [.. picked]));
    }

    /// <summary>
    /// What Gym Book can read: Health Connect's own settings, or for Samsung Health its permission screen again (opening
    /// Samsung Health itself lands on its home screen), then a read with whatever was changed.
    /// </summary>
    [RelayCommand]
    async Task OpenPermissions() => await OpenPermissionsCore();

    /// <summary>
    /// Sending on: asks the health app to take Gym Book's data (Samsung Health, or Health Connect while Samsung hasn't
    /// approved Gym Book), then sends; says why if nothing would. Off: stops (what was sent stays there).
    /// </summary>
    partial void OnSendBackChanged(bool value)
    {
        if (_showingSendBack)
            return;
        if (value)
        {
            _ = TurnOnSendBack();
            return;
        }
        health.WriteBack.Disable();
        Refresh();
    }

    async Task TurnOnSendBack()
    {
        var on = false;
        string? problem = null;
        await Busy(async () =>
        {
            try
            {
                on = await health.WriteBack.EnableAsync();
                problem = on ? null : health.WriteBack.Status;
            }
            catch (Exception e)
            {
                problem = e.Message;
            }
        });
        if (!on)
            await dialogs.Alert("Not sending", problem ?? "Nothing was allowed to take Gym Book's data.");
        else if (store.Profile.HealthSource == HealthSource.SamsungHealth && health.WriteBack.Status.Contains("Health Connect"))
            await dialogs.Alert("Sending through Health Connect", health.WriteBack.Status);
    }

    async Task OpenPermissionsCore()
    {
        if (IsBusy)
            return;
        var platform = health.Platform;
        if (platform.Source != HealthSource.SamsungHealth)
        {
            platform.OpenSettings();
            return;
        }
        await Busy(async () =>
        {
            if (await platform.RequestPermissionsAsync())
                await health.SyncAsync(force: true);
        });
        if (platform.PermissionProblem is { } problem)
            await dialogs.Alert("Samsung Health didn't open", problem);
    }

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
