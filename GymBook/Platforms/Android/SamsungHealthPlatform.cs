using Android.Content;
using GymBook.Models;
using GymBook.Services.Health;

namespace GymBook;

/// <summary>
/// Health data read from Samsung Health itself (<see cref="SamsungHealthData"/>), without Health Connect: each day's
/// calories burned, activity calories and steps exactly as Samsung Health shows them, the food in its diary, weight and
/// body composition, and the height on its profile. Gym Book's workouts, food and weights can go the other way too (as an
/// <see cref="IHealthWriter"/>, once Samsung allows Gym Book to write).
/// </summary>
public class SamsungHealthPlatform : IHealthPlatform, IHealthWriter
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
        // Sleep, by the day it ended; a failure there (an older Samsung Health) doesn't lose the rest.
        if (allowed.Contains(SamsungHealthData.Sleep))
        {
            try
            {
                days = HealthSleep.Merge(days, await _samsung.ReadSleepAsync(from, to));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Samsung Health sleep couldn't be read: {e.Message}");
            }
        }
        return new HealthReadResult(days, body, height, foods);
    }

    // ---------- Writing ----------

    public string WriterName => "Samsung Health";

    public bool CanWrite => SamsungHealthData.IsInstalled;

    public async Task<bool> RequestWriteAsync()
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
                Platform.CurrentActivity is { } activity && await _samsung.RequestWriteAsync(activity));
        }
        catch (SamsungHealthData.SamsungHealthException e) when (e.NotApproved)
        {
            throw new HealthWriteRefusedException(e.Message);
        }
    }

    public async Task WriteAsync(IReadOnlyList<HealthWrite> upserts, IReadOnlyList<(HealthWriteKind Kind, string ClientId)> deletes)
    {
        try
        {
            // Changed ones go out and back in (Samsung Health updates by its own id, which Gym Book doesn't keep).
            foreach (var (kind, id) in deletes.Concat(upserts.Select(u => (u.Kind, u.ClientId))))
                await _samsung.DeleteAsync(Kind(kind), id);
            foreach (var group in upserts.GroupBy(u => u.Kind))
                await _samsung.InsertAsync(Kind(group.Key), [.. group.Select(Point)]);
        }
        catch (SamsungHealthData.SamsungHealthException e) when (e.NotApproved)
        {
            throw new HealthWriteRefusedException(e.Message);
        }
    }

    static string Kind(HealthWriteKind kind) => kind switch
    {
        HealthWriteKind.Workout => SamsungHealthData.ExerciseKind,
        HealthWriteKind.Food => SamsungHealthData.Nutrition,
        _ => SamsungHealthData.Body,
    };

    Java.Lang.Object Point(HealthWrite w) => w switch
    {
        WorkoutWrite x => _samsung.WorkoutPoint(x.ClientId, x.Title, x.Start, x.End, x.ActiveKcal),
        FoodWrite x => _samsung.FoodPoint(x.ClientId, x.Time, x.Meal, x.Name, x.Kcal, x.ProteinG, x.CarbsG, x.FatG),
        BodyWrite x => _samsung.BodyPoint(x.ClientId, x.Time, x.WeightKg, x.BodyFatPercent),
        _ => throw new ArgumentOutOfRangeException(nameof(w)),
    };

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
