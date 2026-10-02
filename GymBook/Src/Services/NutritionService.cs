using System.Globalization;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>Where a day's calories burned came from.</summary>
public enum BurnSource
{
    /// <summary>Not known: the health apps have nothing for the day (shown as N/A; nothing is estimated).</summary>
    None,
    /// <summary>Measured by the phone or watch (Samsung Health, Health Connect). For today it's what's burned so far.</summary>
    Measured,
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
    HealthDay? Health,
    bool StepsCounted = false,
    bool ImportedAsTotal = false)
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
        // Foods logged in the health apps are in their meals with the rest (unless they're turned off in the Profile tab).
        var foods = store.Data.FoodEntries
            .Where(f => f.Date.Date == date && (f.Source == null || Profile.ImportHealthFood))
            .OrderBy(f => f.Meal).ThenBy(f => f.LoggedAt).ToList();
        var health = store.Data.HealthDays.FirstOrDefault(h => h.Date.Date == date);
        // Read before foods were read one by one: only the day's total from the health app, counted on its own.
        var asTotal = Profile.ImportHealthFood && health is { FoodKcal: > 0 } && !foods.Any(f => f.Source != null);

        var (burned, source, stepsCounted) = Burned(health, date);

        return new DayNutrition(
            date,
            foods,
            foods.Where(f => f.Source == null).Sum(f => f.Calories),
            asTotal ? health!.FoodKcal ?? 0 : foods.Where(f => f.Source != null).Sum(f => f.Calories),
            foods.Sum(f => f.ProteinG) + (asTotal ? health!.FoodProteinG ?? 0 : 0),
            foods.Sum(f => f.CarbsG) + (asTotal ? health!.FoodCarbsG ?? 0 : 0),
            foods.Sum(f => f.FatG) + (asTotal ? health!.FoodFatG ?? 0 : 0),
            burned,
            source,
            health,
            stepsCounted,
            asTotal);
    }

    /// <summary>
    /// Net calories walking burns per step for each kilogram of body weight, on top of resting: about 0.5 kcal per kg per
    /// km, at roughly 1,300 steps a km (some 260 kcal for 10,000 steps at 70 kg).
    /// </summary>
    const double WalkKcalPerStepPerKg = 0.0004;

    /// <summary>
    /// The day's burn from the health apps: resting plus activity. Where an app shares no total of its own, Health
    /// Connect's total is only resting (worked out from the BMR) plus any active calories recorded, so walking that
    /// only shows up as steps would be missed: the activity is the most of the active calories recorded, what the total
    /// has beyond resting, and what the day's steps burn. Null (N/A) with nothing from the health apps.
    /// </summary>
    (double? Kcal, BurnSource Source, bool StepsCounted) Burned(HealthDay? health, DateTime date)
    {
        if (health == null)
            return (null, BurnSource.None, false);
        var total = health.TotalBurnedKcal;
        var basal = health.BasalBurnedKcal;
        var steps = health.Steps is { } s && s > 0 ? s * WalkKcalPerStepPerKg * WeightOn(date) : (double?)null;
        if (basal == null)
        {
            // No resting figure to build on: the total as it is, or active calories alone aren't a day's burn.
            return total is { } t ? (t, BurnSource.Measured, false) : (null, BurnSource.None, false);
        }
        var recorded = Math.Max(health.ActiveBurnedKcal ?? 0, total is { } all ? all - basal.Value : 0);
        var activity = Math.Max(recorded, steps ?? 0);
        return (basal.Value + activity, BurnSource.Measured, steps is { } walked && walked > recorded);
    }

    /// <summary>Body weight on a day: the last weighed by then, else the profile's.</summary>
    double WeightOn(DateTime date) =>
        store.Data.BodyWeights.Where(b => b.Date.Date <= date && b.WeightKg > 0).OrderBy(b => b.Date).LastOrDefault()?.WeightKg
        ?? Profile.BodyWeightKg;

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
    /// Daily goals for <paramref name="aim"/>: calories from the usual daily burn measured by the health apps and a balance of −500 kcal (about 0.5 kg a week) to lose fat or +300 to gain; protein 1.8–2.2 g
    /// per kg of body weight, fat a quarter of the calories, carbs the rest. Null when the burn isn't known.
    /// </summary>
    public NutritionGoals? Suggest(NutritionAim aim)
    {
        // Only from what the health apps measured: no estimate.
        var measured = MeasuredDailyBurn();
        if (measured is not { } burn)
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
