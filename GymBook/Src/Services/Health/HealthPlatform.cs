using GymBook.Models;

namespace GymBook.Services.Health;

/// <summary>A day's totals from the phone's health data. Null where nothing was recorded or it wasn't allowed.</summary>
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

public record HealthReadResult(IReadOnlyList<HealthDayReading> Days, IReadOnlyList<BodyReading> Body, double? HeightCm);

public enum HealthAvailability
{
    Available,
    /// <summary>Android, but older than 14, where Health Connect isn't part of the system.</summary>
    NeedsNewerAndroid,
    /// <summary>Not Android: no Samsung Health or Health Connect here. Data read on a phone still syncs in.</summary>
    NotSupported,
}

/// <summary>
/// The phone's health data: Android's Health Connect (Platforms/Android/HealthConnectPlatform.cs), which Samsung Health
/// and most fitness apps, scales and watches share their data through. Elsewhere there's none (<see cref="NoHealthPlatform"/>).
/// </summary>
public interface IHealthPlatform
{
    HealthAvailability Availability { get; }

    /// <summary>Whether reading anything at all has been allowed.</summary>
    bool HasAnyPermission { get; }

    /// <summary>Asks for read access to everything the Nutrition tab uses; true if at least some was allowed.</summary>
    Task<bool> RequestPermissionsAsync();

    /// <summary>
    /// Days from <paramref name="from"/> up to (not including) <paramref name="to"/>, both local midnights. With
    /// <see cref="HealthSource.SamsungHealth"/>, only what Samsung Health recorded; otherwise every app's data.
    /// </summary>
    Task<HealthReadResult> ReadAsync(DateTime from, DateTime to, HealthSource source, CancellationToken ct = default);

    /// <summary>Opens Health Connect's own settings, where access can be changed.</summary>
    void OpenSettings();
}

public class NoHealthPlatform : IHealthPlatform
{
    public HealthAvailability Availability => HealthAvailability.NotSupported;

    public bool HasAnyPermission => false;

    public Task<bool> RequestPermissionsAsync() => Task.FromResult(false);

    public Task<HealthReadResult> ReadAsync(DateTime from, DateTime to, HealthSource source, CancellationToken ct = default) =>
        Task.FromResult(new HealthReadResult([], [], null));

    public void OpenSettings()
    {
    }
}
