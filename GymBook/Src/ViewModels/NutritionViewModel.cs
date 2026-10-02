using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Health;

namespace GymBook.ViewModels;

/// <summary>
/// The Nutrition tab: a day's food against its goals, calories burned (from Samsung Health or Health Connect, or estimated),
/// the surplus or deficit that leaves, how that adds up over a week or a month, and the latest body measurements.
/// </summary>
public partial class NutritionViewModel(
    DataStore store,
    NutritionService nutrition,
    HealthSyncService health,
    Units units,
    DialogService dialogs) : BaseViewModel
{
    public static readonly Color Accent = Color.FromArgb("#3F7DFF");
    public static readonly Color Green = Color.FromArgb("#2ED47A");
    public static readonly Color Orange = Color.FromArgb("#FF8A3D");
    public static readonly Color Violet = Color.FromArgb("#7C5CFF");
    public static readonly Color Yellow = Color.FromArgb("#FFB020");
    public static readonly Color Red = Color.FromArgb("#FF4D5E");
    static readonly Color Secondary = Color.FromArgb("#9AA3B5");

    DateTime _date = DateTime.Today;
    int _periodDays = 7;
    bool _visible;

    /// <summary>The tab is on screen: charts and numbers play in each time it comes into view.</summary>
    [ObservableProperty] bool isShowing;

    // The day
    [ObservableProperty] string dateTitle = "Today";
    [ObservableProperty] string dateSubtitle = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NextOpacity))] bool canGoNext;
    [ObservableProperty] IDrawable? calorieRing;
    [ObservableProperty] string eatenText = "0";
    [ObservableProperty] string goalText = "";
    [ObservableProperty] string burnedText = "–";
    [ObservableProperty] string burnedCaption = "";
    [ObservableProperty] string balanceText = "–";
    [ObservableProperty] string balanceCaption = "";
    [ObservableProperty] Color balanceColor = Secondary;
    [ObservableProperty] string balanceTargetText = "";
    [ObservableProperty] Color balanceTargetColor = Secondary;
    [ObservableProperty] bool hasBalanceTarget;
    [ObservableProperty] string noGoalsHint = "";
    [ObservableProperty] bool hasNoGoals;
    [ObservableProperty] List<MacroItem> macros = [];
    [ObservableProperty] List<MealItem> meals = [];
    [ObservableProperty] bool hasImportedFood;
    [ObservableProperty] string importedFoodTitle = "";
    [ObservableProperty] string importedFoodText = "";
    [ObservableProperty] string importedFoodCalories = "";

    // Over time
    [ObservableProperty] IDrawable? balanceChart;
    [ObservableProperty] string periodTotalText = "";
    [ObservableProperty] string periodTotalCaption = "";
    [ObservableProperty] Color periodTotalColor = Secondary;
    [ObservableProperty] string periodAverageText = "";
    [ObservableProperty] string periodWeightText = "";
    [ObservableProperty] string periodSummary = "";
    [ObservableProperty] bool hasPeriod;
    [ObservableProperty] IDrawable? eatenChart;

    // Activity on the day (health data only)
    [ObservableProperty] bool hasActivity;
    [ObservableProperty] List<StatItem> activity = [];

    // Body
    [ObservableProperty] List<StatItem> body = [];
    [ObservableProperty] string bodyCaption = "";
    [ObservableProperty] IDrawable? bodyFatChart;
    [ObservableProperty] bool hasBodyFatChart;


    /// <summary>The next-day arrow dims on today.</summary>
    public double NextOpacity => CanGoNext ? 1 : 0.3;

    public System.Collections.ObjectModel.ObservableCollection<ChipItem> PeriodChips { get; } = [];

    public override async Task OnAppearingAsync()
    {
        IsShowing = false;
        if (PeriodChips.Count == 0)
        {
            PeriodChips.Add(new ChipItem("7 days", 7, SelectPeriod) { IsSelected = _periodDays == 7 });
            PeriodChips.Add(new ChipItem("30 days", 30, SelectPeriod) { IsSelected = _periodDays == 30 });
        }
        if (!_visible)
        {
            _visible = true;
            store.Changed += OnDataChanged;
        }
        Refresh();
        IsShowing = true;
        await SyncHealthAsync(force: false);
    }

    public override void OnDisappearing()
    {
        IsShowing = false;
        if (_visible)
        {
            _visible = false;
            store.Changed -= OnDataChanged;
        }
    }

    // Food or measurements synced in from another device, or read from the health app.
    void OnDataChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    /// <summary>Reads the health apps (connected in the Profile tab) now and then; what changes refreshes the tab.</summary>
    Task SyncHealthAsync(bool force) => health.IsConnected ? health.SyncAsync(force) : Task.CompletedTask;

    void Refresh()
    {
        if (_date > DateTime.Today)
            _date = DateTime.Today;
        var day = nutrition.Day(_date);
        ShowDay(day);
        ShowPeriod();
        ShowActivity(day);
        ShowBody();
    }

    void ShowDay(DayNutrition day)
    {
        var p = store.Profile;
        DateTitle = day.IsToday ? "Today" : day.Date == DateTime.Today.AddDays(-1) ? "Yesterday" : day.Date.ToString("dddd", CultureInfo.CurrentCulture);
        DateSubtitle = day.Date.ToString("d MMMM", CultureInfo.CurrentCulture);
        CanGoNext = _date < DateTime.Today;

        // Calories: the ring fills toward the goal, the middle says what's left.
        var goal = p.CalorieGoal;
        var (center, label) = goal is { } g
            ? day.EatenKcal <= g ? (NutritionService.Kcal(g - day.EatenKcal), "kcal left") : (NutritionService.Kcal(day.EatenKcal - g), "kcal over")
            : (NutritionService.Kcal(day.EatenKcal), "kcal eaten");
        CalorieRing = new CalorieRingDrawable(day.EatenKcal, goal, Accent, Red, center, label);
        EatenText = NutritionService.Kcal(day.EatenKcal);
        GoalText = goal is { } goalKcal ? NutritionService.Kcal(goalKcal) : "Not set";

        BurnedText = day.BurnedKcal is { } burned ? NutritionService.Kcal(burned) : "–";
        BurnedCaption = day.BurnSource switch
        {
            BurnSource.Measured => day.IsToday ? $"So far · {day.Health?.Source ?? "health data"}" : day.Health?.Source ?? "Health data",
            BurnSource.Estimated => "Estimated",
            _ => "Unknown",
        };

        if (day.Balance is { } balance)
        {
            BalanceText = NutritionService.Signed(balance);
            BalanceColor = balance > 0 ? Orange : Accent;
            BalanceCaption = (balance > 0 ? "Surplus" : "Deficit") + (day.IsToday && day.BurnSource == BurnSource.Measured ? " so far" : "");
        }
        else
        {
            BalanceText = "–";
            BalanceColor = Secondary;
            BalanceCaption = !day.HasFood ? "Log food to see it" : "Needs calories burned";
        }

        HasBalanceTarget = p.EnergyBalanceGoal is not null && day.Balance is not null;
        if (p.EnergyBalanceGoal is { } target && day.Balance is { } b)
        {
            var diff = b - target;
            var aimLabel = target < 0 ? $"a {NutritionService.Kcal(-target)} kcal deficit" : target > 0 ? $"a {NutritionService.Kcal(target)} kcal surplus" : "maintenance";
            if (Math.Abs(diff) <= 100)
            {
                BalanceTargetText = $"On target for {aimLabel}";
                BalanceTargetColor = Green;
            }
            else
            {
                BalanceTargetText = $"{NutritionService.Kcal(Math.Abs(diff))} kcal {(diff > 0 ? "over" : "under")} your target of {aimLabel}";
                // Over the target eats into a deficit (or overshoots a surplus); under it falls short of a surplus.
                BalanceTargetColor = diff > 0 ? Yellow : target > 0 ? Yellow : Green;
            }
        }

        HasNoGoals = p.CalorieGoal is null && p.ProteinGoalG is null;
        NoGoalsHint = day.BurnSource == BurnSource.None
            ? "Set your goals, and your height, age and sex so calories burned can be estimated."
            : "Set calorie and protein goals to see how each day measures up.";

        Macros =
        [
            Macro("Protein", day.ProteinG, p.ProteinGoalG, Green),
            Macro("Carbs", day.CarbsG, p.CarbsGoalG, Yellow),
            Macro("Fat", day.FatG, p.FatGoalG, Violet),
        ];

        Meals = [.. Enum.GetValues<MealType>().Select(meal =>
        {
            var foods = day.Foods.Where(f => f.Meal == meal).ToList();
            return new MealItem
            {
                Title = meal.ToString(),
                Total = foods.Count > 0 ? $"{NutritionService.Kcal(foods.Sum(f => f.Calories))} kcal" : "",
                Foods = [.. foods.Select(f => new FoodItem
                {
                    Name = string.IsNullOrWhiteSpace(f.Name) ? "Food" : f.Name,
                    Detail = $"P {f.ProteinG:0} · C {f.CarbsG:0} · F {f.FatG:0}",
                    Calories = NutritionService.Kcal(f.Calories),
                    OpenCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Food}?id={f.Id}")),
                })],
                AddCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Food}?date={_date:yyyy-MM-dd}&meal={meal}")),
            };
        })];

        HasImportedFood = day.ImportedKcal > 0;
        if (HasImportedFood && day.Health is { } h)
        {
            ImportedFoodTitle = $"Logged in {h.Source ?? "your health app"}";
            ImportedFoodText = $"P {h.FoodProteinG ?? 0:0} · C {h.FoodCarbsG ?? 0:0} · F {h.FoodFatG ?? 0:0}";
            ImportedFoodCalories = NutritionService.Kcal(day.ImportedKcal);
        }
    }

    static MacroItem Macro(string name, double grams, int? goal, Color color) => new()
    {
        Name = name,
        Value = goal is { } g ? $"{grams:0} / {g} g" : $"{grams:0} g",
        Progress = goal is > 0 ? Math.Clamp(grams / goal.Value, 0, 1) : 0,
        Color = color,
        IsOver = goal is > 0 && grams > goal.Value * 1.1 && name != "Protein",
    };

    void ShowPeriod()
    {
        var target = store.Profile.EnergyBalanceGoal;
        BalanceChart = new BalanceChartDrawable(nutrition.BalanceHistory(_periodDays), target, Orange, Accent);
        EatenChart = new BarChartDrawable(nutrition.EatenHistory(_periodDays), Accent, v => v >= 1000 ? $"{v / 1000:0.#}k" : $"{v:0}");

        var period = nutrition.Balance(_periodDays);
        HasPeriod = period != null;
        if (period == null)
        {
            PeriodSummary = "Log food for a few days to see your surplus or deficit add up.";
            return;
        }
        PeriodTotalText = NutritionService.Signed(period.TotalBalance);
        PeriodTotalColor = period.TotalBalance > 0 ? Orange : Accent;
        PeriodTotalCaption = period.TotalBalance > 0 ? "Total surplus" : "Total deficit";
        PeriodAverageText = NutritionService.Signed(period.AverageBalance);
        var kg = period.WeightChangeKg;
        PeriodWeightText = $"{(kg >= 0 ? "+" : "−")}{units.FormatWithUnit(Math.Abs(kg))}";
        var days = period.Days == 1 ? "1 day" : $"{period.Days} days";
        PeriodSummary = $"Over {days} with food logged (today not counted): eating {NutritionService.Kcal(period.AverageEaten)} "
            + $"and burning {NutritionService.Kcal(period.AverageBurned)} kcal a day on average.";
    }

    void SelectPeriod(ChipItem chip)
    {
        _periodDays = (int)chip.Value!;
        foreach (var c in PeriodChips)
            c.IsSelected = c == chip;
        ShowPeriod();
    }

    void ShowActivity(DayNutrition day)
    {
        var h = day.Health;
        var items = new List<StatItem>();
        if (h?.TotalBurnedKcal is { } total)
            items.Add(new("Total burned", $"{NutritionService.Kcal(total)} kcal"));
        if (h?.ActiveBurnedKcal is { } active)
            items.Add(new("Active", $"{NutritionService.Kcal(active)} kcal"));
        if (h?.BasalBurnedKcal is { } basal)
            items.Add(new("Resting", $"{NutritionService.Kcal(basal)} kcal"));
        if (h?.Steps is { } steps)
            items.Add(new("Steps", steps.ToString("#,0", CultureInfo.CurrentCulture)));
        Activity = items;
        HasActivity = items.Count > 0;
    }

    void ShowBody()
    {
        var p = store.Profile;
        var entries = store.Data.BodyWeights.OrderBy(b => b.Date).ToList();
        var latest = entries.LastOrDefault();
        var weight = latest?.WeightKg ?? p.BodyWeightKg;
        // Each measurement from the latest entry that has it.
        double? Latest(Func<BodyWeightEntry, double?> pick) => entries.Select(pick).LastOrDefault(v => v != null);
        var fat = Latest(b => b.BodyFatPercent) ?? p.BodyFatPercent;

        var items = new List<StatItem> { new("Weight", units.FormatWithUnit(weight)) };
        if (fat is { } f)
        {
            items.Add(new("Body fat", $"{f:0.#}%"));
            items.Add(new("Fat mass", units.FormatWithUnit(weight * f / 100)));
        }
        if (Latest(b => b.LeanMassKg) is { } lean)
            items.Add(new("Lean mass", units.FormatWithUnit(lean)));
        else if (fat is { } f2)
            items.Add(new("Lean mass", units.FormatWithUnit(weight * (1 - f2 / 100))));
        if (Latest(b => b.BoneMassKg) is { } bone)
            items.Add(new("Bone mass", units.FormatWithUnit(bone)));
        if (Latest(b => b.BodyWaterKg) is { } water)
            items.Add(new("Body water", units.FormatWithUnit(water)));
        if (nutrition.Bmr() is { } bmr)
            items.Add(new(Latest(b => b.BmrKcal) != null ? "BMR" : "BMR (est.)", $"{NutritionService.Kcal(bmr)} kcal"));
        if (p.HeightCm is { } height)
        {
            items.Add(new("Height", Height(height)));
            items.Add(new("BMI", $"{weight / Math.Pow(height / 100, 2):0.0}"));
        }
        Body = items;

        BodyCaption = latest == null
            ? "Log your weight, or connect your health data in the Profile tab to bring in your scale's measurements."
            : $"Last measured {latest.Date:d MMM}{(latest.Source != null ? $" · {latest.Source}" : "")}";

        var fats = entries.Where(b => b.BodyFatPercent != null).TakeLast(12).ToList();
        HasBodyFatChart = fats.Count >= 2;
        BodyFatChart = new LineChartDrawable(
            [.. fats.Select(b => new ChartPoint(b.Date.ToString("d MMM", CultureInfo.CurrentCulture), b.BodyFatPercent!.Value))],
            Violet, v => $"{v:0.#}%");
    }

    string Height(double cm)
    {
        if (units.Unit == WeightUnit.Kg)
            return $"{cm:0} cm";
        var inches = (int)Math.Round(cm / 2.54);
        return $"{inches / 12}′ {inches % 12}″";
    }

    [RelayCommand]
    void PreviousDay()
    {
        _date = _date.AddDays(-1);
        Refresh();
    }

    [RelayCommand]
    void NextDay()
    {
        if (_date >= DateTime.Today)
            return;
        _date = _date.AddDays(1);
        Refresh();
    }

    [RelayCommand]
    void GoToToday()
    {
        _date = DateTime.Today;
        Refresh();
    }

    [RelayCommand]
    Task AddFood() => GoTo($"{Routes.Food}?date={_date:yyyy-MM-dd}&meal={DefaultMeal()}");

    /// <summary>The meal it's time for, or a snack.</summary>
    static MealType DefaultMeal() => DateTime.Now.Hour switch
    {
        < 11 => MealType.Breakfast,
        < 15 => MealType.Lunch,
        >= 17 and < 22 => MealType.Dinner,
        _ => MealType.Snack,
    };

    [RelayCommand]
    Task OpenGoals() => GoTo(Routes.NutritionGoals);

    [RelayCommand]
    async Task LogWeight()
    {
        var value = await dialogs.Prompt("Log body weight", $"Today's weight in {units.Label}", units.Format(nutrition.LatestWeightKg()), Keyboard.Numeric);
        if (value == null)
            return;
        if (!units.TryParse(value, out var kg) || kg is < 20 or > 400)
        {
            await dialogs.Alert("Invalid weight", "Please enter a number.");
            return;
        }
        var today = store.Data.BodyWeights.FirstOrDefault(b => b.Date == DateTime.Today);
        if (today != null)
        {
            // Logged by hand: it's the user's own now, and the health app's next reading won't replace it.
            today.WeightKg = kg;
            today.Source = null;
        }
        else
        {
            store.Data.BodyWeights.Add(new BodyWeightEntry { Date = DateTime.Today, WeightKg = kg });
        }
        store.Profile.BodyWeightKg = kg;
        store.Save();
    }
}

public class MacroItem
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required double Progress { get; init; }
    public required Color Color { get; init; }
    /// <summary>Well past the goal (carbs or fat: more protein is fine).</summary>
    public bool IsOver { get; init; }
    public Color ValueColor => IsOver ? NutritionViewModel.Yellow : Color.FromArgb("#9AA3B5");
}

public class MealItem
{
    public required string Title { get; init; }
    public string Total { get; init; } = "";
    public List<FoodItem> Foods { get; init; } = [];
    public bool HasFoods => Foods.Count > 0;
    public required ICommand AddCommand { get; init; }
}

public class FoodItem
{
    public required string Name { get; init; }
    public string Detail { get; init; } = "";
    public string Calories { get; init; } = "";
    public required ICommand OpenCommand { get; init; }
}

public record StatItem(string Label, string Value);
