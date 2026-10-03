using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// Daily nutrition goals (calories, protein, carbs, fat and the surplus or deficit to aim for). Suggests them from the
/// daily burn measured by the health apps. (Connecting those, and whether their food counts, is in the Profile tab.)
/// </summary>
public partial class NutritionGoalsViewModel(DataStore store, NutritionService nutrition, Units units, DialogService dialogs) : BaseViewModel
{
    bool _loaded;
    BalanceKind _balanceKind;

    enum BalanceKind { None, Deficit, Maintain, Surplus }

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string calorieGoal = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string proteinGoal = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string carbsGoal = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string fatGoal = "";
    [ObservableProperty] string balanceAmount = "";
    [ObservableProperty] bool hasBalanceAmount;
    [ObservableProperty] string burnText = "";
    [ObservableProperty] string suggestionText = "";
    [ObservableProperty] bool hasSuggestion;

    public System.Collections.ObjectModel.ObservableCollection<ChipItem> BalanceChips { get; } = [];

    UserProfile P => store.Profile;

    public override Task OnAppearingAsync()
    {
        if (_loaded)
            return Task.CompletedTask;
        _loaded = true;

        CalorieGoal = P.CalorieGoal?.ToString(CultureInfo.CurrentCulture) ?? "";
        ProteinGoal = P.ProteinGoalG?.ToString(CultureInfo.CurrentCulture) ?? "";
        CarbsGoal = P.CarbsGoalG?.ToString(CultureInfo.CurrentCulture) ?? "";
        FatGoal = P.FatGoalG?.ToString(CultureInfo.CurrentCulture) ?? "";
        _balanceKind = P.EnergyBalanceGoal switch { null => BalanceKind.None, < 0 => BalanceKind.Deficit, > 0 => BalanceKind.Surplus, _ => BalanceKind.Maintain };
        BalanceAmount = P.EnergyBalanceGoal is { } b and not 0 ? Math.Abs(b).ToString(CultureInfo.CurrentCulture) : "";

        BalanceChips.Clear();
        BalanceChips.Add(new ChipItem("None", BalanceKind.None, SelectBalance));
        BalanceChips.Add(new ChipItem("Deficit", BalanceKind.Deficit, SelectBalance));
        BalanceChips.Add(new ChipItem("Maintain", BalanceKind.Maintain, SelectBalance));
        BalanceChips.Add(new ChipItem("Surplus", BalanceKind.Surplus, SelectBalance));
        UpdateChoices();
        ShowBurn();
        return Task.CompletedTask;
    }

    void UpdateChoices()
    {
        foreach (var c in BalanceChips)
            c.IsSelected = (BalanceKind)c.Value! == _balanceKind;
        HasBalanceAmount = _balanceKind is BalanceKind.Deficit or BalanceKind.Surplus;
    }

    void SelectBalance(ChipItem chip)
    {
        _balanceKind = (BalanceKind)chip.Value!;
        if (_balanceKind is BalanceKind.Deficit or BalanceKind.Surplus && string.IsNullOrWhiteSpace(BalanceAmount))
            BalanceAmount = _balanceKind == BalanceKind.Deficit ? "500" : "300";
        UpdateChoices();
    }

    /// <summary>
    /// The daily burn the goals are suggested from: measured by the health apps over the last two weeks, else worked out
    /// from the body (see <see cref="NutritionService.EstimatedDailyBurn"/>).
    /// </summary>
    string ShowBurn() => BurnText = nutrition.MeasuredDailyBurn() is { } measured
        ? $"You burn about {NutritionService.Kcal(measured)} kcal a day, going by your health data from the last two weeks."
        : nutrition.EstimatedDailyBurn(nutrition.LatestWeightKg()) is { } estimated
            ? $"You burn about {NutritionService.Kcal(estimated)} kcal a day, estimated from your weight, height, age and activity. Connect a health app in the Profile tab to have it measured."
            : "Tell us your weight, height and goal and we'll work out your calories and macros.";

    /// <summary>What the macro goals add up to, against the calorie goal.</summary>
    public string MacroCheck
    {
        get
        {
            var p = Int(ProteinGoal);
            var c = Int(CarbsGoal);
            var f = Int(FatGoal);
            if (p == null && c == null && f == null)
                return "";
            var kcal = NutritionService.CaloriesFromMacros(p ?? 0, c ?? 0, f ?? 0);
            var text = $"Your macros add up to {NutritionService.Kcal(kcal)} kcal";
            return Int(CalorieGoal) is { } goal && Math.Abs(goal - kcal) > 50
                ? $"{text}, {NutritionService.Kcal(Math.Abs(goal - kcal))} {(kcal > goal ? "over" : "under")} the calorie goal."
                : $"{text}.";
        }
    }

    static int? Int(string? text) =>
        double.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v)
            ? (int)Math.Round(v)
            : null;

    [RelayCommand]
    async Task Suggest()
    {
        const string lose = "Lose fat", maintain = "Maintain weight", gain = "Build muscle";
        var choice = await dialogs.ActionSheet("What's your aim?", null, lose, maintain, gain);
        if (choice == null)
            return;
        var aim = choice == lose ? NutritionAim.LoseFat : choice == gain ? NutritionAim.GainMuscle : NutritionAim.Maintain;
        var measured = nutrition.MeasuredDailyBurn() != null;

        // Without a health app's measured burn, it's worked out from the body: sex and how active the days are, once.
        if (!measured && P.Sex == null)
        {
            const string male = "Male", female = "Female";
            var sex = await dialogs.ActionSheet("Your sex", null, male, female);
            if (sex == null)
                return;
            P.Sex = sex == male ? Sex.Male : Sex.Female;
        }
        if (!measured)
        {
            string[] levels = ["Mostly sitting", "On my feet some of the day", "On my feet most of the day", "Physical work"];
            var level = await dialogs.ActionSheet("Outside workouts, your days are", null, levels);
            if (level == null)
                return;
            P.ActivityLevel = (ActivityLevel)Array.IndexOf(levels, level);
        }

        // Weight, height and age (for the estimate), and where to get to by when.
        var metric = units.Unit == WeightUnit.Kg;
        var weightKg = nutrition.LatestWeightKg();
        var fields = new List<Views.NumberField> { new($"Weight ({units.Label})", (int)Math.Round(units.ToDisplay(weightKg)), 20, 700) };
        if (!measured)
        {
            fields.Add(metric
                ? new Views.NumberField("Height (cm)", (int)Math.Round(P.HeightCm ?? 175), 100, 250)
                : new Views.NumberField("Height (in)", (int)Math.Round((P.HeightCm ?? 175) / 2.54), 40, 98));
            fields.Add(new("Age", P.BirthYear is { } year ? DateTime.Today.Year - year : 30, 14, 90));
        }
        var hasGoal = aim != NutritionAim.Maintain;
        if (hasGoal)
        {
            var toward = units.ToDisplay(aim == NutritionAim.LoseFat ? weightKg * 0.93 : weightKg * 1.04);
            fields.Add(new($"Goal weight ({units.Label})", (int)Math.Round(toward), 20, 700));
            fields.Add(new("In weeks", 12, 1, 104));
        }
        var values = await dialogs.Numbers(choice, measured
                ? "Your daily burn comes from your health data."
                : "Your daily burn is worked out from these. Connect a health app in the Profile tab to have it measured instead.",
            "Suggest", [.. fields]);
        if (values == null)
            return;

        var i = 0;
        weightKg = units.FromDisplay(values[i++]);
        P.BodyWeightKg = Math.Round(weightKg, 1);
        if (!measured)
        {
            P.HeightCm = metric ? values[i++] : Math.Round(values[i++] * 2.54);
            P.BirthYear = DateTime.Today.Year - values[i++];
        }
        double? goalKg = hasGoal ? units.FromDisplay(values[i++]) : null;
        int? weeks = hasGoal ? values[i++] : null;
        if (goalKg is { } g && (aim == NutritionAim.LoseFat ? g >= weightKg : g <= weightKg))
        {
            await dialogs.Alert("Check your goal weight",
                aim == NutritionAim.LoseFat ? "To lose fat, the goal weight should be below your weight now." : "To build muscle, the goal weight should be above your weight now.");
            return;
        }
        store.Save();

        var goals = nutrition.Suggest(aim, weightKg, goalKg, weeks);
        ShowBurn();
        if (goals == null)
        {
            await dialogs.Alert("Couldn't suggest goals", "Enter your height so your daily burn can be worked out.");
            return;
        }
        CalorieGoal = goals.Calories.ToString(CultureInfo.CurrentCulture);
        ProteinGoal = goals.ProteinG.ToString(CultureInfo.CurrentCulture);
        CarbsGoal = goals.CarbsG.ToString(CultureInfo.CurrentCulture);
        FatGoal = goals.FatG.ToString(CultureInfo.CurrentCulture);
        _balanceKind = goals.Balance switch { < 0 => BalanceKind.Deficit, > 0 => BalanceKind.Surplus, _ => BalanceKind.Maintain };
        BalanceAmount = goals.Balance != 0 ? Math.Abs(goals.Balance).ToString(CultureInfo.CurrentCulture) : "";
        UpdateChoices();
        var burn = $"{NutritionService.Kcal(goals.DailyBurn)} kcal {(goals.BurnMeasured ? "measured" : "estimated")} daily burn";
        var perWeek = units.FormatWithUnit(Math.Abs(goals.Balance) * 7 / NutritionService.KcalPerKg);
        var timing = goals.Weeks is not { } w ? ""
            : goals.PaceCapped ? $" That faster pace isn't healthy, so this gets you to {units.FormatWithUnit(goalKg!.Value)} in about {w} weeks instead of {weeks}."
            : $" That's {units.FormatWithUnit(goalKg!.Value)} in about {w} weeks.";
        SuggestionText = goals.Balance switch
        {
            < 0 => $"From your {burn}, minus {-goals.Balance} kcal: about {perWeek} a week.{timing} High protein keeps your muscle while you lose fat. Save to use these.",
            > 0 => $"From your {burn}, plus {goals.Balance} kcal: about {perWeek} a week, a lean bulk slow enough to keep fat gain down.{timing} Save to use these.",
            _ => $"Your {burn}, with plenty of protein for training. Save to use these.",
        };
        HasSuggestion = true;
    }

    [RelayCommand]
    async Task Save()
    {
        int? Goal(string text, int min, int max) => string.IsNullOrWhiteSpace(text) ? null : Int(text) is { } v && v >= min && v <= max ? v : -1;
        var calories = Goal(CalorieGoal, 500, 10000);
        var protein = Goal(ProteinGoal, 0, 1000);
        var carbs = Goal(CarbsGoal, 0, 1500);
        var fat = Goal(FatGoal, 0, 600);
        var amount = _balanceKind is BalanceKind.Deficit or BalanceKind.Surplus ? Goal(BalanceAmount, 0, 2000) : 0;
        if (calories == -1 || protein == -1 || carbs == -1 || fat == -1 || amount == -1)
        {
            await dialogs.Alert("Check your goals",
                "Calories can be 500–10,000 a day, protein up to 1,000 g, carbs up to 1,500 g, fat up to 600 g, and the deficit or surplus up to 2,000 kcal. Leave one empty for no goal.");
            return;
        }
        P.CalorieGoal = calories;
        P.ProteinGoalG = protein;
        P.CarbsGoalG = carbs;
        P.FatGoalG = fat;
        P.EnergyBalanceGoal = _balanceKind switch
        {
            BalanceKind.None => null,
            BalanceKind.Maintain => 0,
            BalanceKind.Deficit => -amount,
            _ => amount,
        };
        store.Save();
        await GoBack();
    }
}
