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
/// the surplus or deficit that leaves, creatine and macros (activity and body are on Progress).
/// </summary>
public partial class NutritionViewModel(
    DataStore store,
    NutritionService nutrition,
    HealthSyncService health,
    DialogService dialogs) : BaseViewModel
{
    public static readonly Color Accent = Color.FromArgb("#3F7DFF");
    public static readonly Color Green = Color.FromArgb("#2ED47A");
    public static readonly Color Orange = Color.FromArgb("#FF8A3D");
    public static readonly Color Violet = Color.FromArgb("#7C5CFF");
    public static readonly Color Yellow = Color.FromArgb("#FFB020");
    public static readonly Color Red = Color.FromArgb("#FF4D5E");
    static readonly Color Secondary = Color.FromArgb("#9AA3B5");
    static readonly Color Text = Color.FromArgb("#F4F6FB");
    public static readonly Color Carbs = Color.FromArgb("#C58CFF");
    public static readonly Color Fat = Color.FromArgb("#FF7B72");
    public static readonly Color Protein = Color.FromArgb("#FFD84D");

    DateTime _date = DateTime.Today;
    bool _visible;

    /// <summary>The tab is on screen: charts and numbers play in each time it comes into view.</summary>
    [ObservableProperty] bool isShowing;

    // The day
    [ObservableProperty] string dateTitle = "Today";
    [ObservableProperty] string dateSubtitle = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NextOpacity))] bool canGoNext;
    [ObservableProperty] IDrawable? calorieBar;
    [ObservableProperty] bool hasGoal;
    [ObservableProperty] string goalRangeText = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasBalanceLine))] string balanceLine = "";
    [ObservableProperty] Color balanceColor = Secondary;
    public bool HasBalanceLine => BalanceLine.Length > 0;

    // The keys beside the figures.
    public IDrawable GoalIcon { get; } = new LegendIconDrawable(LegendIcon.Target, Accent);
    public IDrawable RangeIcon { get; } = new LegendIconDrawable(LegendIcon.Range, Secondary);
    public IDrawable BurnedIcon { get; } = new LegendIconDrawable(LegendIcon.Tick, Text);
    [ObservableProperty] string eatenText = "0";
    [ObservableProperty] string goalText = "";
    [ObservableProperty] string burnedText = "–";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasBurnedCaption))] string burnedCaption = "";
    public bool HasBurnedCaption => BurnedCaption.Length > 0;
    [ObservableProperty] string balanceTargetText = "";
    [ObservableProperty] Color balanceTargetColor = Secondary;
    [ObservableProperty] bool hasBalanceTarget;
    [ObservableProperty] string noGoalsHint = "";
    [ObservableProperty] bool hasNoGoals;
    // Always three (carbs, fat, protein): the tiles bind to each by position.
    [ObservableProperty] List<MacroItem> macros = [Macro("Carb", 0, null, Carbs), Macro("Fat", 0, null, Fat), Macro("Protein", 0, null, Protein)];
    [ObservableProperty] IDrawable? macroSplit;
    [ObservableProperty] string macroTargetLabel = "Recommended";
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

    /// <summary>The next-day arrow dims on today.</summary>
    public double NextOpacity => CanGoNext ? 1 : 0.3;

    public override async Task OnAppearingAsync()
    {
        IsShowing = false;
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
    }

    void ShowDay(DayNutrition day)
    {
        var p = store.Profile;
        DateTitle = day.IsToday ? "Today" : day.Date == DateTime.Today.AddDays(-1) ? "Yesterday" : day.Date.ToString("dddd", CultureInfo.CurrentCulture);
        DateSubtitle = day.Date.ToString("d MMMM", CultureInfo.CurrentCulture);
        CanGoNext = _date < DateTime.Today;

        // Calories, as Samsung Health shows them: eaten large, the goal and its on-target range beside it, then a bar
        // from 0 with eaten filling it (orange past what was burned), the range hatched, a tick at the burn and a target
        // at the goal. Under the number, the surplus or deficit; without a burn, what's left of the goal.
        var goal = p.CalorieGoal;
        var burnedKcal = day.BurnSource == BurnSource.Measured ? day.BurnedKcal : null;
        CalorieBar = new CalorieBarDrawable(day.EatenKcal, burnedKcal, goal, Green, Orange, Accent);
        EatenText = NutritionService.Kcal(day.EatenKcal);
        HasGoal = goal is > 0;
        GoalText = goal is { } goalKcal ? $"{NutritionService.Kcal(goalKcal)} Cal" : "No goal";
        GoalRangeText = goal is { } r
            ? $"{NutritionService.Kcal(r * (1 - CalorieBarDrawable.Range))} – {NutritionService.Kcal(r * (1 + CalorieBarDrawable.Range))}"
            : "";
        var soFar = day.IsToday ? " so far" : "";
        (BalanceLine, BalanceColor) = day.Balance is { } balance
            ? ($"{NutritionService.Signed(balance)} {(balance > 0 ? "surplus" : "deficit")}{soFar}", balance > 0 ? Orange : Accent)
            : goal is { } g
                ? day.EatenKcal <= g ? ($"{NutritionService.Kcal(g - day.EatenKcal)} left", Secondary) : ($"{NutritionService.Kcal(day.EatenKcal - g)} over", Red)
                : ("", Secondary);

        BurnedText = day.BurnedKcal is { } burned ? $"{NutritionService.Kcal(burned)} burned" : "Burned: N/A";
        // Only what qualifies the number: so far today, worked out from steps, or nothing to go on; not the app it came from.
        BurnedCaption = day.BurnSource != BurnSource.Measured ? "No health data"
            : string.Join(" · ", new[] { day.IsToday ? "So far" : null, day.StepsCounted ? "resting + steps" : null }.OfType<string>());

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

        // Carbs, fat and protein as Samsung Health shows them: grams each, then their share of the calories against the
        // split aimed for: the goals when all three are set, else a usual one (55% carbs, 25% fat, 20% protein).
        Macros =
        [
            Macro("Carb", day.CarbsG, p.CarbsGoalG, Carbs),
            Macro("Fat", day.FatG, p.FatGoalG, Fat),
            Macro("Protein", day.ProteinG, p.ProteinGoalG, Protein),
        ];
        var hasGoals = p.CarbsGoalG is > 0 && p.FatGoalG is > 0 && p.ProteinGoalG is > 0;
        MacroTargetLabel = hasGoals ? "Your goals" : "Recommended";
        MacroSplit = new MacroSplitDrawable(
            [day.CarbsG * 4, day.FatG * 9, day.ProteinG * 4],
            hasGoals ? new double[] { p.CarbsGoalG!.Value * 4, p.FatGoalG!.Value * 9, p.ProteinGoalG!.Value * 4 } : new double[] { 55, 25, 20 },
            [Carbs, Fat, Protein]);

        Meals = [.. Enum.GetValues<MealType>().Select(meal =>
        {
            var foods = day.Foods.Where(f => f.Meal == meal).ToList();
            return new MealItem
            {
                Title = meal.ToString(),
                Total = foods.Count > 0 ? $"{NutritionService.Kcal(foods.Sum(f => f.Calories))} kcal" : "",
                TotalKcal = foods.Sum(f => f.Calories),
                ShowDivider = meal != MealType.Breakfast,
                Foods = [.. foods.Select(f => new FoodItem
                {
                    Name = string.IsNullOrWhiteSpace(f.Name) ? "Food" : f.Name,
                    Detail = (f.Source != null ? $"{f.Source} · " : "") + $"P {f.ProteinG:0} · C {f.CarbsG:0} · F {f.FatG:0}",
                    Calories = NutritionService.Kcal(f.Calories),
                    OpenCommand = new AsyncRelayCommand(() => OpenFoodAsync(f)),
                })],
                AddCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Food}?date={_date:yyyy-MM-dd}&meal={meal}")),
                OpenCommand = new AsyncRelayCommand(() => OpenMealAsync(meal, foods)),
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

    /// <summary>A food logged here opens to change it; one read from a health app says so (changed there, the next read would put it back).</summary>
    Task OpenFoodAsync(FoodEntry f) => f.Source == null
        ? GoTo($"{Routes.Food}?id={f.Id}")
        : dialogs.Alert(string.IsNullOrWhiteSpace(f.Name) ? "Food" : f.Name,
            $"{NutritionService.Kcal(f.Calories)} kcal · protein {f.ProteinG:0.#} g · carbs {f.CarbsG:0.#} g · fat {f.FatG:0.#} g\n\n"
            + $"Logged in {f.Source} at {f.LoggedAt.ToString("t", CultureInfo.CurrentCulture)}. Change or delete it there; it updates here.");

    /// <summary>A meal's row tapped: adds a food to it when empty, opens its food when there's one, else lists them to pick.</summary>
    async Task OpenMealAsync(MealType meal, List<FoodEntry> foods)
    {
        if (foods.Count == 0)
        {
            await GoTo($"{Routes.Food}?date={_date:yyyy-MM-dd}&meal={meal}");
            return;
        }
        if (foods.Count == 1)
        {
            await OpenFoodAsync(foods[0]);
            return;
        }
        const string add = "Add food";
        var labels = foods.Select(f => $"{(string.IsNullOrWhiteSpace(f.Name) ? "Food" : f.Name)} · {NutritionService.Kcal(f.Calories)} kcal").ToList();
        // Two foods can read the same: tell them apart by position.
        for (var i = 0; i < labels.Count; i++)
            if (labels.IndexOf(labels[i]) != i)
                labels[i] += $" ({i + 1})";
        var picked = await dialogs.ActionSheet(meal.ToString(), null, [.. labels, add]);
        if (picked == add)
            await GoTo($"{Routes.Food}?date={_date:yyyy-MM-dd}&meal={meal}");
        else if (picked != null && labels.IndexOf(picked) is >= 0 and var index)
            await OpenFoodAsync(foods[index]);
    }

    static MacroItem Macro(string name, double grams, int? goal, Color color) => new()
    {
        Name = name,
        Grams = grams.ToString("0.#", CultureInfo.CurrentCulture),
        GoalText = goal is { } g ? $"of {g} g" : "",
        Color = color,
        IsOver = goal is > 0 && grams > goal.Value * 1.1 && name != "Protein",
    };

    IEnumerable<SupplementDose> Creatine(DateTime date) =>
        store.Data.Supplements.Where(d => d.Name == SupplementDose.Creatine && d.Date.Date == date.Date);

    void ShowCreatine()
    {
        var dose = store.Profile.CreatineDoseG;
        var taken = Creatine(_date).OrderBy(d => d.TakenAt).ToList();
        CreatineTaken = taken.Count > 0;
        TakeCreatineText = $"Took {Grams(dose)}";
        CreatineCaption = $"{Grams(dose)} a day";
        CreatineText = CreatineTaken
            ? $"{Grams(taken.Sum(d => d.Grams))} at {taken[^1].TakenAt.ToString("t", CultureInfo.CurrentCulture)}"
            : _date == DateTime.Today ? "Not taken yet today" : "Not taken";
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

    /// <summary>Picks the day on a calendar (tapping the title), with a dot on each day that has food, calories burned or creatine.</summary>
    [RelayCommand]
    async Task PickDay()
    {
        var data = store.Data;
        var days = data.FoodEntries.Select(f => f.Date.Date)
            .Concat(data.HealthDays.Where(h => h.TotalBurnedKcal != null || h.FoodKcal != null).Select(h => h.Date.Date))
            .Concat(data.Supplements.Select(s => s.Date.Date))
            .ToHashSet();
        if (await dialogs.Calendar("Pick a day", _date, DateTime.Today, days.Contains) is not { } day || day.Date == _date)
            return;
        _date = day.Date;
        Refresh();
    }

    [RelayCommand]
    Task OpenGoals() => GoTo(Routes.NutritionGoals);
}

public class MacroItem
{
    public required string Name { get; init; }
    /// <summary>Grams eaten, without the unit ("339.7").</summary>
    public required string Grams { get; init; }
    /// <summary>"of 180 g" with a goal, else empty.</summary>
    public required string GoalText { get; init; }
    public bool HasGoal => GoalText.Length > 0;
    public required Color Color { get; init; }
    /// <summary>Well past the goal (carbs or fat: more protein is fine).</summary>
    public bool IsOver { get; init; }
    public Color GoalColor => IsOver ? NutritionViewModel.Yellow : Color.FromArgb("#9AA3B5");
}

/// <summary>A meal's row, as Samsung Health lays it out: its calories in a circle, its name and foods, and + to add one.</summary>
public class MealItem
{
    public required string Title { get; init; }
    public string Total { get; init; } = "";
    public List<FoodItem> Foods { get; init; } = [];
    public bool HasFoods => Foods.Count > 0;
    /// <summary>The meal's calories as a bare number ("1,098"; "0" with nothing logged).</summary>
    public string Calories => NutritionService.Kcal(TotalKcal);
    public double TotalKcal { get; init; }
    /// <summary>Its foods' names, one after another.</summary>
    public string Names => string.Join(", ", Foods.Select(f => f.Name));
    /// <summary>A line above every meal but the first.</summary>
    public bool ShowDivider { get; init; }
    public required ICommand AddCommand { get; init; }
    /// <summary>Tapping the row: the food in it, a list of them to pick from, or adding one when there are none.</summary>
    public required ICommand OpenCommand { get; init; }
}

public class FoodItem
{
    public required string Name { get; init; }
    public string Detail { get; init; } = "";
    public string Calories { get; init; } = "";
    public required ICommand OpenCommand { get; init; }
}

public record StatItem(string Label, string Value);
