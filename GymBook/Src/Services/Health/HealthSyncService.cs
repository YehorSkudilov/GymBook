using GymBook.Models;

namespace GymBook.Services.Health;

/// <summary>
/// Brings the phone's health data (see <see cref="IHealthPlatform"/>) into Gym Book: each day's calories burned, steps and
/// food logged elsewhere become <see cref="HealthDay"/>s, and scale and watch measurements fill in
/// <see cref="BodyWeightEntry"/>s and the body fat and height on the profile. Everything it writes syncs to the account like
/// the rest, so other devices see it too. A weight logged in Gym Book itself is never overwritten. The profile's body weight
/// is left alone: the AI features send it, and health data never goes to them (see the API's privacy policy).
/// </summary>
public class HealthSyncService(DataStore store, IEnumerable<IHealthPlatform> platforms)
{
    /// <summary>How far back each read goes. Health Connect only shares the last 30 days with an app by default.</summary>
    public const int Days = 30;

    static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(10);

    readonly SemaphoreSlim _gate = new(1, 1);

    readonly IReadOnlyList<IHealthPlatform> _platforms = [.. platforms];

    /// <summary>Where data is read from: the chosen source's platform (Health Connect's before one is chosen).</summary>
    public IHealthPlatform Platform => PlatformFor(store.Profile.HealthSource == HealthSource.None ? HealthSource.HealthConnect : store.Profile.HealthSource);

    /// <summary>The platform that reads <paramref name="source"/>, or the only one there is (none, off Android).</summary>
    public IHealthPlatform PlatformFor(HealthSource source) => _platforms.FirstOrDefault(p => p.Source == source) ?? _platforms[0];

    /// <summary>Samsung Health can be read directly on this device.</summary>
    public bool CanReadSamsungHealth => PlatformFor(HealthSource.SamsungHealth) is { Source: HealthSource.SamsungHealth, Availability: HealthAvailability.Available };

    public DateTimeOffset? LastSyncedAt { get; private set; }

    /// <summary>Why the last read failed, or null.</summary>
    public string? LastError { get; private set; }

    /// <summary>A source is chosen, this device can read it, and reading was allowed.</summary>
    public bool IsConnected =>
        store.Profile.HealthSource != HealthSource.None && Platform.Availability == HealthAvailability.Available && Platform.HasAnyPermission;

    /// <summary>
    /// Connects to <paramref name="source"/> (Samsung Health itself, or Health Connect): asks for access, and on success
    /// remembers the choice and reads straight away. Returns whether any access was given; when not,
    /// <see cref="IHealthPlatform.PermissionProblem"/> may say why.
    /// </summary>
    public async Task<bool> ConnectAsync(HealthSource source)
    {
        var platform = PlatformFor(source);
        if (source == HealthSource.None || platform.Source != source || platform.Availability != HealthAvailability.Available)
            return false;
        if (!await platform.RequestPermissionsAsync())
            return false;
        store.Profile.HealthSource = source;
        // A fresh choice: every app through Health Connect (pick some with ChooseApps).
        store.Profile.HealthApps = null;
        store.Save();
        LastError = null;
        await SyncAsync(force: true);
        return true;
    }

    /// <summary>The apps read from through Health Connect: the ones picked, or null for every app (and for Samsung Health).</summary>
    public IReadOnlyList<string>? Apps =>
        store.Profile.HealthSource == HealthSource.HealthConnect && store.Profile.HealthApps is { Count: > 0 } picked ? picked : null;

    /// <summary>
    /// What's read from: "Samsung Health" (directly), or through Health Connect "Samsung Health, Withings",
    /// "Samsung Health + 2 more", or "every app" (names as the phone knows them; elsewhere, just how many).
    /// </summary>
    public string SourceName => store.Profile.HealthSource == HealthSource.SamsungHealth ? "Samsung Health" : Apps switch
    {
        null => "every app",
        { Count: var n } when Platform.Availability != HealthAvailability.Available => n == 1 ? "1 app" : $"{n} apps",
        { Count: <= 2 } apps => string.Join(", ", apps.Select(Platform.AppName)),
        var apps => $"{Platform.AppName(apps[0])} + {apps.Count - 1} more",
    };

    /// <summary>Reads through Health Connect, only from <paramref name="packages"/> (null or empty: every app), and reads again.</summary>
    public async Task ChooseAppsAsync(IReadOnlyCollection<string>? packages)
    {
        store.Profile.HealthSource = HealthSource.HealthConnect;
        store.Profile.HealthApps = packages is { Count: > 0 } ? [.. packages] : null;
        store.Save();
        LastError = null;
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
            var from = today.AddDays(1 - Days);
            var to = today.AddDays(1);
            var result = await Platform.ReadAsync(from, to, Apps);
            LastSyncedAt = DateTimeOffset.Now;
            LastError = null;
            return await MainThread.InvokeOnMainThreadAsync(() => Apply(result, name) | ApplyFoods(result.Foods, from, to));
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

    /// <summary>
    /// Each food logged in the health apps between the two days, as a food in its meal: added, changed to match, and
    /// those no longer there (deleted in the app, or from an app no longer read) taken out. Foods logged in Gym Book
    /// aren't touched.
    /// </summary>
    bool ApplyFoods(IReadOnlyList<FoodReading>? foods, DateTime from, DateTime to)
    {
        if (foods == null)
            return false;
        var data = store.Data;
        var changed = false;
        var read = new HashSet<string>();
        foreach (var food in foods)
        {
            var kcal = InRange(food.Kcal, 0, 20000, 1) ?? 0;
            var protein = InRange(food.ProteinG, 0, 2000, 1) ?? 0;
            var carbs = InRange(food.CarbsG, 0, 2000, 1) ?? 0;
            var fat = InRange(food.FatG, 0, 2000, 1) ?? 0;
            if (kcal <= 0 && protein + carbs + fat <= 0)
                continue;
            var id = $"hc-{food.Id}";
            if (id.Length > SyncLimits.IdLength)
                id = id[..SyncLimits.IdLength];
            if (!read.Add(id))
                continue;
            var meal = food.Meal ?? MealAt(food.Time);
            var name = string.IsNullOrWhiteSpace(food.Name) ? $"{meal} food" : food.Name.Trim();
            if (name.Length > SyncLimits.NameLength)
                name = name[..SyncLimits.NameLength];
            var source = food.Package is { } package ? Platform.AppName(package) : "Health Connect";
            if (source.Length > SyncLimits.NameLength)
                source = source[..SyncLimits.NameLength];

            var entry = data.FoodEntries.FirstOrDefault(f => f.Id == id);
            if (entry == null)
            {
                entry = new FoodEntry { Id = id };
                data.FoodEntries.Add(entry);
                changed = true;
            }
            if (entry.Date != food.Time.Date || entry.Meal != meal || entry.Name != name || entry.Calories != kcal
                || entry.ProteinG != protein || entry.CarbsG != carbs || entry.FatG != fat || entry.LoggedAt != food.Time
                || entry.Source != source)
            {
                entry.Date = food.Time.Date;
                entry.Meal = meal;
                entry.Name = name;
                entry.Calories = kcal;
                entry.ProteinG = protein;
                entry.CarbsG = carbs;
                entry.FatG = fat;
                entry.LoggedAt = food.Time;
                entry.Source = source;
                changed = true;
            }
        }
        changed |= data.FoodEntries.RemoveAll(f => f.Source != null && f.Date >= from && f.Date < to && !read.Contains(f.Id)) > 0;
        if (changed)
            store.Save();
        return changed;
    }

    /// <summary>The meal for a food the app didn't put in one, by the time it was eaten.</summary>
    static MealType MealAt(DateTime time) => time.Hour switch
    {
        >= 4 and < 11 => MealType.Breakfast,
        >= 11 and < 15 => MealType.Lunch,
        >= 17 and < 22 => MealType.Dinner,
        _ => MealType.Snack,
    };

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
