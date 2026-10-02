using System.Globalization;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>Where a day's calories burned came from.</summary>
public enum BurnSource
{
    /// <summary>Not known: no health data and not enough about the user to estimate it.</summary>
    None,
    /// <summary>Measured by the phone or watch (Samsung Health, Health Connect). For today it's what's burned so far.</summary>
    Measured,
    /// <summary>Estimated from body size, age and activity level (see <see cref="NutritionService.EstimatedDailyBurn"/>).</summary>
    Estimated,
}

/// <summary>One day of eating against burning.</summary>
public record DayNutrition(
    DateTime Date,
    IReadOnlyList<FoodEntry> Foods,
    double LoggedKcal,
    double ImportedKcal,
    double ProteinG,
    double CarbsG,
    double FatG,
    double? BurnedKcal,
    BurnSource BurnSource,
    HealthDay? Health)
{
    public double EatenKcal => LoggedKcal + ImportedKcal;

    /// <summary>Something was eaten (logged here or in the health app); a day with nothing logged has no balance.</summary>
    public bool HasFood => EatenKcal > 0;

    /// <summary>Eaten minus burned: positive is a surplus, negative a deficit. Null without food or a burn.</summary>
    public double? Balance => HasFood && BurnedKcal is { } burned ? EatenKcal - burned : null;

    public bool IsToday => Date == DateTime.Today;
}

/// <summary>The energy balance over finished days that have both food and a burn.</summary>
public record PeriodBalance(int Days, double TotalBalance, double AverageEaten, double AverageBurned)
{
    public double AverageBalance => Days > 0 ? TotalBalance / Days : 0;

    /// <summary>The weight it adds up to, at about 7,700 kcal per kg of body fat.</summary>
    public double WeightChangeKg => TotalBalance / NutritionService.KcalPerKg;
}

/// <summary>A direction for <see cref="NutritionService.Suggest"/>.</summary>
public enum NutritionAim { LoseFat, Maintain, GainMuscle }

public record NutritionGoals(int Calories, int ProteinG, int CarbsG, int FatG, int Balance, double DailyBurn, bool BurnMeasured);

/// <summary>Calories and macros eaten, calories burned, and the balance between them, from the food log and health data.</summary>
public class NutritionService(DataStore store)
{
    /// <summary>Roughly the energy in a kilogram of body fat.</summary>
    public const double KcalPerKg = 7700;

    UserProfile Profile => store.Profile;

    public DayNutrition Day(DateTime date)
    {
        date = date.Date;
        var foods = store.Data.FoodEntries.Where(f => f.Date.Date == date).OrderBy(f => f.Meal).ThenBy(f => f.LoggedAt).ToList();
        var health = store.Data.HealthDays.FirstOrDefault(h => h.Date.Date == date);
        var import = Profile.ImportHealthFood && health != null;

        var (burned, source) = health?.TotalBurnedKcal is { } total ? (total, BurnSource.Measured)
            : health is { BasalBurnedKcal: { } basal, ActiveBurnedKcal: { } active } ? (basal + active, BurnSource.Measured)
            : EstimatedDailyBurn() is { } estimate ? (estimate, BurnSource.Estimated)
            : ((double?)null, BurnSource.None);

        return new DayNutrition(
            date,
            foods,
            foods.Sum(f => f.Calories),
            import ? health!.FoodKcal ?? 0 : 0,
            foods.Sum(f => f.ProteinG) + (import ? health!.FoodProteinG ?? 0 : 0),
            foods.Sum(f => f.CarbsG) + (import ? health!.FoodCarbsG ?? 0 : 0),
            foods.Sum(f => f.FatG) + (import ? health!.FoodFatG ?? 0 : 0),
            burned,
            source,
            health);
    }

    /// <summary>
    /// The last <paramref name="days"/> finished days (today isn't over, so it isn't counted), only those with food logged
    /// and a burn known. Null when there are none.
    /// </summary>
    public PeriodBalance? Balance(int days)
    {
        var counted = Enumerable.Range(1, days).Select(i => Day(DateTime.Today.AddDays(-i))).Where(d => d.Balance != null).ToList();
        return counted.Count == 0
            ? null
            : new PeriodBalance(counted.Count, counted.Sum(d => d.Balance!.Value), counted.Average(d => d.EatenKcal), counted.Average(d => d.BurnedKcal!.Value));
    }

    /// <summary>Each of the last <paramref name="days"/> days' balance, oldest first, today included; 0 where there's none.</summary>
    public List<ChartPoint> BalanceHistory(int days) =>
        [.. Enumerable.Range(0, days).Reverse().Select(i => Day(DateTime.Today.AddDays(-i)))
            .Select(d => new ChartPoint(d.Date.ToString(days > 7 ? "d/M" : "ddd", CultureInfo.CurrentCulture), d.Balance ?? 0))];

    /// <summary>Calories eaten each of the last <paramref name="days"/> days, oldest first.</summary>
    public List<ChartPoint> EatenHistory(int days) =>
        [.. Enumerable.Range(0, days).Reverse().Select(i => Day(DateTime.Today.AddDays(-i)))
            .Select(d => new ChartPoint(d.Date.ToString(days > 7 ? "d/M" : "ddd", CultureInfo.CurrentCulture), d.EatenKcal))];

    /// <summary>
    /// Calories burned at complete rest: the last measured value (a smart scale's or watch's, within two months), else from
    /// lean mass (Katch–McArdle) when body fat is known, else from weight, height, age and sex (Mifflin–St Jeor).
    /// </summary>
    public double? Bmr()
    {
        var since = DateTime.Today.AddDays(-60);
        if (store.Data.BodyWeights.Where(b => b.Date >= since && b.BmrKcal is > 500).MaxBy(b => b.Date)?.BmrKcal is { } measured)
            return measured;

        var weight = LatestWeightKg();
        if (Profile.BodyFatPercent is { } fat and >= 3 and <= 60)
            return 370 + 21.6 * weight * (1 - fat / 100);
        if (Profile.HeightCm is { } height && Profile.BirthYear is { } year && Profile.Sex is { } sex)
        {
            var age = Math.Clamp(DateTime.Today.Year - year, 10, 100);
            return 10 * weight + 6.25 * height - 5 * age + (sex == Sex.Male ? 5 : -161);
        }
        return null;
    }

    /// <summary>A whole day's burn estimated from <see cref="Bmr"/> and the activity level, for days without health data.</summary>
    public double? EstimatedDailyBurn() => Bmr() * (Profile.ActivityLevel switch
    {
        ActivityLevel.Sedentary => 1.2,
        ActivityLevel.Light => 1.375,
        ActivityLevel.Moderate => 1.55,
        _ => 1.725,
    });

    /// <summary>The average measured burn over the last two finished weeks, if at least 4 days have one.</summary>
    public double? MeasuredDailyBurn()
    {
        var days = Enumerable.Range(1, 14)
            .Select(i => Day(DateTime.Today.AddDays(-i)))
            .Where(d => d.BurnSource == BurnSource.Measured)
            .ToList();
        return days.Count >= 4 ? days.Average(d => d.BurnedKcal!.Value) : null;
    }

    public double LatestWeightKg() => store.Data.BodyWeights.MaxBy(b => b.Date)?.WeightKg ?? Profile.BodyWeightKg;

    /// <summary>
    /// Daily goals for <paramref name="aim"/>: calories from the usual daily burn (measured if there's enough health data,
    /// estimated otherwise) and a balance of −500 kcal (about 0.5 kg a week) to lose fat or +300 to gain; protein 1.8–2.2 g
    /// per kg of body weight, fat a quarter of the calories, carbs the rest. Null when the burn isn't known.
    /// </summary>
    public NutritionGoals? Suggest(NutritionAim aim)
    {
        var measured = MeasuredDailyBurn();
        if ((measured ?? EstimatedDailyBurn()) is not { } burn)
            return null;
        var balance = aim switch
        {
            NutritionAim.LoseFat => -500,
            NutritionAim.GainMuscle => 300,
            _ => 0,
        };
        var calories = (int)Math.Clamp(Math.Round((burn + balance) / 10) * 10, 1200, 6000);
        var weight = LatestWeightKg();
        var protein = (int)Math.Round(weight * (aim switch
        {
            NutritionAim.LoseFat => 2.2,
            NutritionAim.GainMuscle => 2.0,
            _ => 1.8,
        }));
        var fat = (int)Math.Round(calories * 0.25 / 9);
        var carbs = (int)Math.Max(0, Math.Round((calories - protein * 4 - fat * 9) / 4.0));
        return new NutritionGoals(calories, protein, carbs, fat, balance, burn, measured != null);
    }

    /// <summary>Foods logged before, newest first, each name once: quick to log again.</summary>
    public List<FoodEntry> RecentFoods(int max) =>
        [.. store.Data.FoodEntries
            .OrderByDescending(f => f.LoggedAt)
            .DistinctBy(f => f.Name.Trim().ToLowerInvariant())
            .Where(f => f.Name.Trim().Length > 0)
            .Take(max)];

    /// <summary>Calories from macros, at 4 kcal per gram of protein and carbs and 9 per gram of fat.</summary>
    public static double CaloriesFromMacros(double protein, double carbs, double fat) => protein * 4 + carbs * 4 + fat * 9;

    public static string Kcal(double kcal) => Math.Round(kcal).ToString("#,0", CultureInfo.CurrentCulture);

    /// <summary>A balance with its sign: "+250" or "−480".</summary>
    public static string Signed(double kcal) => $"{(kcal >= 0 ? "+" : "−")}{Kcal(Math.Abs(kcal))}";
}
