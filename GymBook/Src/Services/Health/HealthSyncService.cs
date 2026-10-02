using GymBook.Models;

namespace GymBook.Services.Health;

/// <summary>
/// Brings the phone's health data (see <see cref="IHealthPlatform"/>) into Gym Book: each day's calories burned, steps and
/// food logged elsewhere become <see cref="HealthDay"/>s, and scale and watch measurements fill in
/// <see cref="BodyWeightEntry"/>s and the body fat and height on the profile. Everything it writes syncs to the account like
/// the rest, so other devices see it too. A weight logged in Gym Book itself is never overwritten. The profile's body weight
/// is left alone: the AI features send it, and health data never goes to them (see the API's privacy policy).
/// </summary>
public class HealthSyncService(DataStore store, IHealthPlatform platform)
{
    /// <summary>How far back each read goes. Health Connect only shares the last 30 days with an app by default.</summary>
    public const int Days = 30;

    static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(10);

    readonly SemaphoreSlim _gate = new(1, 1);

    public IHealthPlatform Platform => platform;

    public DateTimeOffset? LastSyncedAt { get; private set; }

    /// <summary>Why the last read failed, or null.</summary>
    public string? LastError { get; private set; }

    /// <summary>A source is chosen, this device can read it, and reading was allowed.</summary>
    public bool IsConnected =>
        store.Profile.HealthSource != HealthSource.None && platform.Availability == HealthAvailability.Available && platform.HasAnyPermission;

    /// <summary>
    /// Connects to <paramref name="source"/>: asks for access, and on success remembers the choice and reads straight away.
    /// Returns whether any access was given.
    /// </summary>
    public async Task<bool> ConnectAsync(HealthSource source)
    {
        if (source == HealthSource.None || platform.Availability != HealthAvailability.Available)
            return false;
        if (!await platform.RequestPermissionsAsync())
            return false;
        store.Profile.HealthSource = source;
        // A fresh choice: Samsung Health alone, or every app (pick some with ChooseApps).
        store.Profile.HealthApps = null;
        store.Save();
        await SyncAsync(force: true);
        return true;
    }

    /// <summary>The apps read from: the ones picked, Samsung Health with that source, or null for every app.</summary>
    public IReadOnlyList<string>? Apps =>
        store.Profile.HealthApps is { Count: > 0 } picked ? picked
        : store.Profile.HealthSource == HealthSource.SamsungHealth ? [HealthApp.SamsungHealth]
        : null;

    /// <summary>"Samsung Health", "Samsung Health, Withings", or "Health Connect" (every app): what's read from.</summary>
    public string SourceName => Apps is { } apps
        ? apps.Count <= 2 ? string.Join(", ", apps.Select(HealthApp.NameOf)) : $"{HealthApp.NameOf(apps[0])} + {apps.Count - 1} more"
        : HealthSource.HealthConnect.Display();

    /// <summary>Reads only from <paramref name="packages"/> from now on (null or empty: every app), and reads again.</summary>
    public async Task ChooseAppsAsync(IReadOnlyCollection<string>? packages)
    {
        store.Profile.HealthSource = HealthSource.HealthConnect;
        store.Profile.HealthApps = packages is { Count: > 0 } ? [.. packages] : null;
        store.Save();
        await SyncAsync(force: true);
    }

    /// <summary>Stops reading. What was already read stays.</summary>
    public void Disconnect()
    {
        store.Profile.HealthSource = HealthSource.None;
        store.Save();
        LastSyncedAt = null;
        LastError = null;
    }

    /// <summary>
    /// Reads the last <see cref="Days"/> days and merges them in, at most every few minutes unless <paramref name="force"/>.
    /// Returns whether anything changed. Call on the UI thread.
    /// </summary>
    public async Task<bool> SyncAsync(bool force = false)
    {
        if (!IsConnected)
            return false;
        if (!force && LastSyncedAt is { } last && DateTimeOffset.Now - last < MinInterval)
            return false;
        if (!await _gate.WaitAsync(0))
            return false;
        try
        {
            var name = SourceName;
            var today = DateTime.Today;
            var result = await platform.ReadAsync(today.AddDays(1 - Days), today.AddDays(1), Apps);
            LastSyncedAt = DateTimeOffset.Now;
            LastError = null;
            return await MainThread.InvokeOnMainThreadAsync(() => Apply(result, name));
        }
        catch (Exception e)
        {
            LastError = e.Message;
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    bool Apply(HealthReadResult result, string sourceName)
    {
        var data = store.Data;
        var changed = false;

        foreach (var reading in result.Days)
        {
            var total = Kcal(reading.TotalBurnedKcal);
            var active = Kcal(reading.ActiveBurnedKcal);
            var basal = Kcal(reading.BasalBurnedKcal);
            var food = Kcal(reading.FoodKcal);
            var steps = reading.Steps is > 0 and <= 1_000_000 ? reading.Steps : null;
            if (total == null && active == null && basal == null && food == null && steps == null)
                continue;

            var day = data.HealthDays.FirstOrDefault(h => h.Date == reading.Date.Date);
            if (day == null)
            {
                day = new HealthDay { Date = reading.Date.Date };
                data.HealthDays.Add(day);
                changed = true;
            }
            changed |= Set(day.TotalBurnedKcal, total, v => day.TotalBurnedKcal = v);
            changed |= Set(day.ActiveBurnedKcal, active, v => day.ActiveBurnedKcal = v);
            changed |= Set(day.BasalBurnedKcal, basal, v => day.BasalBurnedKcal = v);
            changed |= Set(day.FoodKcal, food, v => day.FoodKcal = v);
            changed |= Set(day.FoodProteinG, Grams(reading.FoodProteinG), v => day.FoodProteinG = v);
            changed |= Set(day.FoodCarbsG, Grams(reading.FoodCarbsG), v => day.FoodCarbsG = v);
            changed |= Set(day.FoodFatG, Grams(reading.FoodFatG), v => day.FoodFatG = v);
            if (day.Steps != steps)
            {
                day.Steps = steps;
                changed = true;
            }
            if (day.Source != sourceName)
            {
                day.Source = sourceName;
                changed = true;
            }
        }

        foreach (var reading in result.Body.OrderBy(b => b.Date))
        {
            var date = reading.Date.Date;
            var entry = data.BodyWeights.FirstOrDefault(b => b.Date == date);
            var weight = InRange(reading.WeightKg, 20, 400, 2);
            if (entry == null)
            {
                // A day's entry needs a weight; composition measured without one waits for a day that has it.
                if (weight == null)
                    continue;
                entry = new BodyWeightEntry { Date = date, WeightKg = weight.Value, Source = sourceName };
                data.BodyWeights.Add(entry);
                changed = true;
            }
            else if (entry.Source != null && weight != null && Math.Abs(entry.WeightKg - weight.Value) > 0.001)
            {
                // Read before: follow the health app. Logged in Gym Book (no source): the user's own number stays.
                entry.WeightKg = weight.Value;
                changed = true;
            }
            changed |= Fill(entry.BodyFatPercent, InRange(reading.BodyFatPercent, 1, 80, 1), v => entry.BodyFatPercent = v);
            changed |= Fill(entry.LeanMassKg, InRange(reading.LeanMassKg, 0, 500, 2), v => entry.LeanMassKg = v);
            changed |= Fill(entry.BoneMassKg, InRange(reading.BoneMassKg, 0, 50, 2), v => entry.BoneMassKg = v);
            changed |= Fill(entry.BodyWaterKg, InRange(reading.BodyWaterKg, 0, 300, 2), v => entry.BodyWaterKg = v);
            changed |= Fill(entry.BmrKcal, InRange(reading.BmrKcal, 0, 10000, 0), v => entry.BmrKcal = v);
        }

        // The profile follows the latest measurements, so estimates use them too.
        var profile = store.Profile;
        if (data.BodyWeights.Where(b => b.BodyFatPercent is >= 3 and <= 60).MaxBy(b => b.Date)?.BodyFatPercent is { } fat
            && profile.BodyFatPercent != fat)
        {
            profile.BodyFatPercent = fat;
            changed = true;
        }
        if (InRange(result.HeightCm, 50, 272, 1) is { } height && profile.HeightCm != height)
        {
            profile.HeightCm = height;
            changed = true;
        }

        if (changed)
            store.Save();
        return changed;
    }

    static double? Kcal(double? v) => InRange(v, 0, 50000, 0) is { } k && k > 0 ? k : null;

    static double? Grams(double? v) => InRange(v, 0, 5000, 1);

    /// <summary>Rounded, or null when missing or outside what the record allows (which the server would reject).</summary>
    static double? InRange(double? v, double min, double max, int digits) =>
        v is { } x && double.IsFinite(x) && x >= min && x <= max ? Math.Round(x, digits) : null;

    static bool Set(double? current, double? value, Action<double?> set)
    {
        if (current == value)
            return false;
        set(value);
        return true;
    }

    /// <summary>Like <see cref="Set"/>, but a measurement that's missing this time doesn't clear the one already there.</summary>
    static bool Fill(double? current, double? value, Action<double?> set) => value != null && Set(current, value, set);
}
