using GymBook.Models;

namespace GymBook.Services.Health;

/// <summary>
/// A day's totals from the phone's health data. Null where nothing was recorded or it wasn't allowed. A total with no
/// basal (resting) figure is an app's own complete total (Samsung Health's, read from it directly), used as it is.
/// </summary>
public record HealthDayReading(
    DateTime Date,
    double? TotalBurnedKcal,
    double? ActiveBurnedKcal,
    double? BasalBurnedKcal,
    int? Steps,
    double? FoodKcal,
    double? FoodProteinG,
    double? FoodCarbsG,
    double? FoodFatG);

/// <summary>A day's body measurements, the last of each kind that day.</summary>
public record BodyReading(
    DateTime Date,
    double? WeightKg,
    double? BodyFatPercent,
    double? LeanMassKg,
    double? BoneMassKg,
    double? BodyWaterKg,
    double? BmrKcal);

/// <summary>
/// One food logged in a health app (a Health Connect nutrition record): its record id, when it was eaten, the meal it
/// was logged under (null when the app didn't say), its name and amounts, and the app's package.
/// </summary>
public record FoodReading(
    string Id,
    DateTime Time,
    MealType? Meal,
    string? Name,
    double? Kcal,
    double? ProteinG,
    double? CarbsG,
    double? FatG,
    string? Package);

/// <summary>
/// What was read. <paramref name="Foods"/> is null when food wasn't read (not allowed), as opposed to none logged.
/// </summary>
public record HealthReadResult(IReadOnlyList<HealthDayReading> Days, IReadOnlyList<BodyReading> Body, double? HeightCm,
    IReadOnlyList<FoodReading>? Foods = null);

/// <summary>An app that shares health data (through Health Connect): its Android package and its name.</summary>
public record HealthApp(string Package, string Name);

public enum HealthAvailability
{
    Available,
    /// <summary>Android, but older than 14, where Health Connect isn't part of the system.</summary>
    NeedsNewerAndroid,
    /// <summary>Not Android: no Samsung Health or Health Connect here. Data read on a phone still syncs in.</summary>
    NotSupported,
    /// <summary>Android, but the app read from (Samsung Health) isn't on this phone, or is too old for it.</summary>
    NotInstalled,
}

/// <summary>
/// Where the phone's health data is read from (<see cref="Source"/>): Samsung Health itself
/// (Platforms/Android/SamsungHealthPlatform.cs), or Android's Health Connect (Platforms/Android/HealthConnectPlatform.cs),
/// which most fitness apps, scales and watches share their data through. Elsewhere there's none (<see cref="NoHealthPlatform"/>).
/// </summary>
public interface IHealthPlatform
{
    HealthSource Source { get; }

    HealthAvailability Availability { get; }

    /// <summary>Whether reading anything at all has been allowed.</summary>
    bool HasAnyPermission { get; }

    /// <summary>
    /// Asks for read access to everything the Nutrition tab uses; true if at least some was allowed. When it can't be
    /// asked (Samsung Health not sharing yet, say), <see cref="PermissionProblem"/> says why.
    /// </summary>
    Task<bool> RequestPermissionsAsync();

    /// <summary>Why the last request for access failed, to show; null when it didn't.</summary>
    string? PermissionProblem { get; }

    /// <summary>
    /// Days from <paramref name="from"/> up to (not including) <paramref name="to"/>, both local midnights: only what
    /// <paramref name="apps"/> recorded (Android package names), or every app's when that's null or empty. A day's
    /// total burned with no basal (resting) figure is complete as it is (Samsung Health's own).
    /// </summary>
    Task<HealthReadResult> ReadAsync(DateTime from, DateTime to, IReadOnlyCollection<string>? apps, CancellationToken ct = default);

    /// <summary>The apps that shared any of the data Gym Book reads over the last 30 days, by name (Health Connect only).</summary>
    Task<IReadOnlyList<HealthApp>> FindAppsAsync(CancellationToken ct = default);

    /// <summary>An app's name as the phone knows it, or its package when it can't say.</summary>
    string AppName(string package);

    /// <summary>Opens where access can be changed: Health Connect's settings, or Samsung Health.</summary>
    void OpenSettings();
}

public class NoHealthPlatform : IHealthPlatform
{
    public HealthSource Source => HealthSource.None;

    public HealthAvailability Availability => HealthAvailability.NotSupported;

    public bool HasAnyPermission => false;

    public Task<bool> RequestPermissionsAsync() => Task.FromResult(false);

    public string? PermissionProblem => null;

    public Task<HealthReadResult> ReadAsync(DateTime from, DateTime to, IReadOnlyCollection<string>? apps, CancellationToken ct = default) =>
        Task.FromResult(new HealthReadResult([], [], null));

    public Task<IReadOnlyList<HealthApp>> FindAppsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HealthApp>>([]);

    public string AppName(string package) => package;

    public void OpenSettings()
    {
    }
}
