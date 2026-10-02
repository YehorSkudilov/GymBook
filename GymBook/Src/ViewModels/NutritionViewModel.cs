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

    // Creatine on the day
    [ObservableProperty] bool creatineTaken;
    [ObservableProperty] string creatineText = "";
    [ObservableProperty] string creatineCaption = "";
    [ObservableProperty] string takeCreatineText = "";
    [ObservableProperty] List<DoseDay> creatineWeek = [];

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
        ShowCreatine();
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

        BurnedText = day.BurnedKcal is { } burned ? NutritionService.Kcal(burned) : "N/A";
        BurnedCaption = day.BurnSource == BurnSource.Measured
            ? (day.IsToday ? "So far · " : "") + (day.StepsCounted ? "resting + steps" : day.Health?.Source ?? "health data")
            : "No health data";

        if (day.Balance is { } balance)
        {
            BalanceText = NutritionService.Signed(balance);
            BalanceColor = balance > 0 ? Orange : Accent;
            BalanceCaption = (balance > 0 ? "Surplus" : "Deficit") + (day.IsToday && day.BurnSource == BurnSource.Measured ? " so far" : "");
        }
        else
        {
            BalanceText = "N/A";
            BalanceColor = Secondary;
            BalanceCaption = !day.HasFood ? "Log food to see it" : "Needs calories burned from health data";
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
        NoGoalsHint = "Set calorie and protein goals to see how each day measures up.";

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
                    Detail = (f.Source != null ? $"{f.Source} · " : "") + $"P {f.ProteinG:0} · C {f.CarbsG:0} · F {f.FatG:0}",
                    Calories = NutritionService.Kcal(f.Calories),
                    // Read from a health app: changed there, not here (the next read would put it back).
                    OpenCommand = f.Source == null
                        ? new AsyncRelayCommand(() => GoTo($"{Routes.Food}?id={f.Id}"))
                        : new AsyncRelayCommand(() => dialogs.Alert(string.IsNullOrWhiteSpace(f.Name) ? "Food" : f.Name,
                            $"{NutritionService.Kcal(f.Calories)} kcal · protein {f.ProteinG:0.#} g · carbs {f.CarbsG:0.#} g · fat {f.FatG:0.#} g\n\n"
                            + $"Logged in {f.Source} at {f.LoggedAt.ToString("t", CultureInfo.CurrentCulture)}. Change or delete it there; it updates here.")),
                })],
                AddCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Food}?date={_date:yyyy-MM-dd}&meal={meal}")),
            };
        })];

        HasImportedFood = day.ImportedAsTotal;
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
        // Always all four, from the health apps; N/A for what they haven't recorded.
        var h = day.Health;
        static string Kcal(double? v) => v is { } k ? $"{NutritionService.Kcal(k)} kcal" : "N/A";
        Activity =
        [
            new("Total burned", Kcal(day.BurnSource == BurnSource.Measured ? day.BurnedKcal : null)),
            new("Active", Kcal(h?.ActiveBurnedKcal)),
            new("Resting", Kcal(h?.BasalBurnedKcal)),
            new("Steps", h?.Steps is { } steps ? steps.ToString("#,0", CultureInfo.CurrentCulture) : "N/A"),
        ];
        HasActivity = true;
    }

    void ShowBody()
    {
        // Only measurements read from the health apps (not weights logged by hand, nothing estimated); N/A for what
        // they haven't measured. Fat mass and BMI are worked out from measured values only.
        var entries = store.Data.BodyWeights.Where(b => b.Source != null).OrderBy(b => b.Date).ToList();
        var latest = entries.LastOrDefault();
        double? Latest(Func<BodyWeightEntry, double?> pick) => entries.Select(pick).LastOrDefault(v => v != null);
        var weight = latest?.WeightKg;
        var fat = Latest(b => b.BodyFatPercent);
        var height = store.Profile.HeightCm;
        string Mass(double? kg) => kg is { } v ? units.FormatWithUnit(v) : "N/A";

        Body =
        [
            new("Weight", Mass(weight)),
            new("Body fat", fat is { } f ? $"{f:0.#}%" : "N/A"),
            new("Fat mass", Mass(weight * fat / 100)),
            new("Lean mass", Mass(Latest(b => b.LeanMassKg))),
            new("Bone mass", Mass(Latest(b => b.BoneMassKg))),
            new("Body water", Mass(Latest(b => b.BodyWaterKg))),
            new("BMR", Latest(b => b.BmrKcal) is { } bmr ? $"{NutritionService.Kcal(bmr)} kcal" : "N/A"),
            new("Height", height is { } cm ? Height(cm) : "N/A"),
            new("BMI", weight is { } w && height is { } h ? $"{w / Math.Pow(h / 100, 2):0.0}" : "N/A"),
        ];

        BodyCaption = latest == null
            ? "No measurements from your health apps yet. Connect your health data in the Profile tab to bring in your scale's and watch's."
            : $"Last measured {latest.Date:d MMM} · {latest.Source}";

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

    IEnumerable<SupplementDose> Creatine(DateTime date) =>
        store.Data.Supplements.Where(d => d.Name == SupplementDose.Creatine && d.Date.Date == date.Date);

    void ShowCreatine()
    {
        var dose = store.Profile.CreatineDoseG;
        var taken = Creatine(_date).OrderBy(d => d.TakenAt).ToList();
        CreatineTaken = taken.Count > 0;
        TakeCreatineText = $"Took {Grams(dose)}";
        CreatineText = CreatineTaken
            ? $"{Grams(taken.Sum(d => d.Grams))} at {taken[^1].TakenAt.ToString("t", CultureInfo.CurrentCulture)}"
            : _date == DateTime.Today ? "Not taken yet today" : "Not taken";

        // The streak: days in a row with a dose, up to today (today not counting against it until it's over).
        var days = store.Data.Supplements.Where(d => d.Name == SupplementDose.Creatine).Select(d => d.Date.Date).ToHashSet();
        var day = days.Contains(DateTime.Today) ? DateTime.Today : DateTime.Today.AddDays(-1);
        var streak = 0;
        while (days.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        CreatineCaption = $"{Grams(dose)} a day" + (streak > 1 ? $" · {streak}-day streak" : "");

        // The week up to the day shown.
        CreatineWeek = [.. Enumerable.Range(0, 7).Select(i => _date.AddDays(i - 6)).Select(d => new DoseDay(
            d.ToString("ddd", CultureInfo.CurrentCulture)[..1],
            days.Contains(d.Date) ? Green : Color.FromArgb("#262B38"),
            d == _date))];
    }

    static string Grams(double g) => $"{g.ToString("0.#", CultureInfo.CurrentCulture)} g";

    /// <summary>Logs the usual dose on the day shown (now, for today).</summary>
    [RelayCommand]
    void TakeCreatine()
    {
        var takenAt = _date == DateTime.Today ? DateTime.Now : _date.Date + DateTime.Now.TimeOfDay;
        store.Data.Supplements.Add(new SupplementDose { Date = _date.Date, Grams = store.Profile.CreatineDoseG, TakenAt = takenAt });
        store.Save();
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Not every device has haptics.
        }
        ShowCreatine();
    }

    /// <summary>Takes back the day's last dose.</summary>
    [RelayCommand]
    async Task UndoCreatine()
    {
        if (Creatine(_date).OrderBy(d => d.TakenAt).LastOrDefault() is not { } last)
            return;
        if (!await dialogs.Confirm("Remove creatine?", $"Remove the {Grams(last.Grams)} logged at {last.TakenAt:t}?", "Remove", "Cancel"))
            return;
        store.Data.Supplements.Remove(last);
        store.Save();
        ShowCreatine();
    }

    /// <summary>The usual dose, picked on the number pad.</summary>
    [RelayCommand]
    async Task ChangeCreatineDose()
    {
        var current = store.Profile.CreatineDoseG;
        var picked = await Views.NumberPadSheet.Show(current.ToString("0.#", CultureInfo.InvariantCulture), current, 0.5, 1, 20, "g", decimals: true);
        if (picked is not { } g || double.IsNaN(g) || g < 0.5 || g > 50 || g == current)
            return;
        store.Profile.CreatineDoseG = Math.Round(g, 1);
        store.Save();
        ShowCreatine();
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

/// <summary>A day in the creatine week: its letter, filled when taken, the day shown outlined.</summary>
public record DoseDay(string Letter, Color Fill, bool IsSelected)
{
    public Color Stroke => IsSelected ? Color.FromArgb("#9AA3B5") : Colors.Transparent;
}
