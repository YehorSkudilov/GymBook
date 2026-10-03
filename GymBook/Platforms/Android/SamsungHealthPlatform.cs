using Android.Content;
using GymBook.Models;
using GymBook.Services.Health;

namespace GymBook;

/// <summary>
/// Health data read from Samsung Health itself (<see cref="SamsungHealthData"/>), without Health Connect: each day's
/// calories burned, activity calories and steps exactly as Samsung Health shows them, the food in its diary, weight and
/// body composition, and the height on its profile.
/// </summary>
public class SamsungHealthPlatform : IHealthPlatform
{
    readonly SamsungHealthData _samsung = new();

    // Samsung Health can only be asked asynchronously, so this is as of the last request or read: allowed until it
    // says otherwise, so a connection made before carries on after the app restarts.
    bool _denied;

    public HealthSource Source => HealthSource.SamsungHealth;

    public HealthAvailability Availability => SamsungHealthData.IsInstalled ? HealthAvailability.Available : HealthAvailability.NotInstalled;

    public bool HasAnyPermission => SamsungHealthData.IsInstalled && !_denied;

    public string? PermissionProblem { get; private set; }

    public async Task<bool> RequestPermissionsAsync()
    {
        if (!SamsungHealthData.IsInstalled)
            return false;
        try
        {
            var allowed = await MainThread.InvokeOnMainThreadAsync(async () =>
                Platform.CurrentActivity is { } activity ? await _samsung.RequestAsync(activity) : new HashSet<string>());
            PermissionProblem = null;
            _denied = allowed.Count == 0;
        }
        catch (Exception e)
        {
            PermissionProblem = e.Message;
            _denied = true;
        }
        return !_denied;
    }

    public async Task<HealthReadResult> ReadAsync(DateTime from, DateTime to, IReadOnlyCollection<string>? apps, CancellationToken ct = default)
    {
        if (!SamsungHealthData.IsInstalled)
            return new HealthReadResult([], [], null);
        var allowed = await _samsung.AllowedAsync();
        _denied = allowed.Count == 0;
        if (_denied)
            return new HealthReadResult([], [], null);

        // Up to now, not the end of today: today's totals are what's burned so far.
        var now = DateTime.Now;
        var days = await _samsung.ReadDaysAsync(from, to > now ? now : to,
            allowed.Contains(SamsungHealthData.Activity), allowed.Contains(SamsungHealthData.Steps));
        ct.ThrowIfCancellationRequested();
        var body = allowed.Contains(SamsungHealthData.Body) ? await _samsung.ReadBodyAsync(from, to) : new List<BodyReading>();
        ct.ThrowIfCancellationRequested();
        var foods = allowed.Contains(SamsungHealthData.Nutrition) ? await _samsung.ReadFoodsAsync(from, to) : null;
        ct.ThrowIfCancellationRequested();
        // The height on Samsung Health's profile.
        double? height = allowed.Contains(SamsungHealthData.Profile) ? await _samsung.ReadHeightAsync() : null;
        return new HealthReadResult(days, body, height, foods);
    }

    // Everything comes from Samsung Health: there are no apps to pick.
    public Task<IReadOnlyList<HealthApp>> FindAppsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HealthApp>>([]);

    public string AppName(string package) => HealthConnectPlatform.Label(package);

    /// <summary>
    /// Opens Samsung Health (on its home screen: the SDK can't open its permission settings). Profile › Health data shows
    /// the SDK's permission screen instead (<see cref="RequestPermissionsAsync"/>), where each kind can be turned on or off.
    /// </summary>
    public void OpenSettings()
    {
        var context = Android.App.Application.Context;
        if (context.PackageManager!.GetLaunchIntentForPackage(SamsungHealthData.Package) is { } intent)
            context.StartActivity(intent.AddFlags(ActivityFlags.NewTask));
    }
}
