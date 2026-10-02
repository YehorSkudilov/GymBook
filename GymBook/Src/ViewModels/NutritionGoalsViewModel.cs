using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// Daily nutrition goals (calories, protein, carbs, fat and the surplus or deficit to aim for), what calories burned are
/// estimated from when there's no health data (height, birth year, sex, activity level). Suggests goals from the usual
/// daily burn. (Whether food from health apps counts is in the Profile tab's Health data.)
/// </summary>
public partial class NutritionGoalsViewModel(DataStore store, NutritionService nutrition, Units units, DialogService dialogs) : BaseViewModel
{
    bool _loaded;
    Sex? _sex;
    ActivityLevel _activity;
    BalanceKind _balanceKind;

    enum BalanceKind { None, Deficit, Maintain, Surplus }

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string calorieGoal = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string proteinGoal = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string carbsGoal = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(MacroCheck))] string fatGoal = "";
    [ObservableProperty] string balanceAmount = "";
    [ObservableProperty] bool hasBalanceAmount;
    [ObservableProperty] string height = "";
    [ObservableProperty] string heightLabel = "HEIGHT (CM)";
    [ObservableProperty] string birthYear = "";
    [ObservableProperty] string burnText = "";
    [ObservableProperty] string suggestionText = "";
    [ObservableProperty] bool hasSuggestion;

    public System.Collections.ObjectModel.ObservableCollection<ChipItem> BalanceChips { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<ChipItem> SexChips { get; } = [];
    public System.Collections.ObjectModel.ObservableCollection<OptionItem> ActivityOptions { get; } = [];

    UserProfile P => store.Profile;

    bool Imperial => units.Unit == WeightUnit.Lbs;

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
        HeightLabel = Imperial ? "HEIGHT (INCHES)" : "HEIGHT (CM)";
        Height = P.HeightCm is { } cm ? (Imperial ? Math.Round(cm / 2.54) : Math.Round(cm)).ToString(CultureInfo.CurrentCulture) : "";
        BirthYear = P.BirthYear?.ToString(CultureInfo.InvariantCulture) ?? "";
        _sex = P.Sex;
        _activity = P.ActivityLevel;

        BalanceChips.Clear();
        BalanceChips.Add(new ChipItem("None", BalanceKind.None, SelectBalance));
        BalanceChips.Add(new ChipItem("Deficit", BalanceKind.Deficit, SelectBalance));
        BalanceChips.Add(new ChipItem("Maintain", BalanceKind.Maintain, SelectBalance));
        BalanceChips.Add(new ChipItem("Surplus", BalanceKind.Surplus, SelectBalance));
        SexChips.Clear();
        SexChips.Add(new ChipItem("Male", Sex.Male, SelectSex));
        SexChips.Add(new ChipItem("Female", Sex.Female, SelectSex));
        ActivityOptions.Clear();
        foreach (var level in Enum.GetValues<ActivityLevel>())
            ActivityOptions.Add(new OptionItem(level.Display(), level.Description(), level, SelectActivity));
        UpdateChoices();
        ShowBurn();
        return Task.CompletedTask;
    }

    void UpdateChoices()
    {
        foreach (var c in BalanceChips)
            c.IsSelected = (BalanceKind)c.Value! == _balanceKind;
        HasBalanceAmount = _balanceKind is BalanceKind.Deficit or BalanceKind.Surplus;
        foreach (var c in SexChips)
            c.IsSelected = (Sex)c.Value! == _sex;
        foreach (var o in ActivityOptions)
            o.IsSelected = (ActivityLevel)o.Value == _activity;
    }

    void SelectBalance(ChipItem chip)
    {
        _balanceKind = (BalanceKind)chip.Value!;
        if (_balanceKind is BalanceKind.Deficit or BalanceKind.Surplus && string.IsNullOrWhiteSpace(BalanceAmount))
            BalanceAmount = _balanceKind == BalanceKind.Deficit ? "500" : "300";
        UpdateChoices();
    }

    void SelectSex(ChipItem chip)
    {
        _sex = (Sex)chip.Value!;
        UpdateChoices();
    }

    void SelectActivity(OptionItem option)
    {
        _activity = (ActivityLevel)option.Value;
        UpdateChoices();
    }

    /// <summary>The usual daily burn the goals are suggested from: measured if there's enough health data, else estimated.</summary>
    string ShowBurn() => BurnText = nutrition.MeasuredDailyBurn() is { } measured
        ? $"You burn about {NutritionService.Kcal(measured)} kcal a day, going by your health data from the last two weeks."
        : nutrition.EstimatedDailyBurn() is { } estimate
            ? $"You burn about {NutritionService.Kcal(estimate)} kcal a day, estimated from your body and activity level. Connect Samsung Health or Health Connect to measure it instead."
            : "Fill in your height, birth year and sex below (or connect your health data) so your daily burn can be worked out.";

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
        // Estimated from what's typed below so far; the profile only keeps it on saving.
        var before = (P.HeightCm, P.BirthYear, P.Sex, P.ActivityLevel);
        if (!await ApplyBodyAsync())
            return;
        var aim = choice == lose ? NutritionAim.LoseFat : choice == gain ? NutritionAim.GainMuscle : NutritionAim.Maintain;
        var goals = nutrition.Suggest(aim);
        var burnText = ShowBurn();
        (P.HeightCm, P.BirthYear, P.Sex, P.ActivityLevel) = before;
        BurnText = burnText;
        if (goals == null)
        {
            await dialogs.Alert("Not enough to go on", "Fill in your height, birth year and sex, or connect Samsung Health or Health Connect, so your daily burn can be worked out.");
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
        SuggestionText = goals.Balance switch
        {
            < 0 => $"From your {burn}, minus {-goals.Balance} kcal: about {units.FormatWithUnit(-goals.Balance * 7 / NutritionService.KcalPerKg)} a week. High protein keeps your muscle while you lose fat. Save to use these.",
            > 0 => $"From your {burn}, plus {goals.Balance} kcal: a lean bulk, slow enough to keep fat gain down. Save to use these.",
            _ => $"Your {burn}, with plenty of protein for training. Save to use these.",
        };
        HasSuggestion = true;
    }

    /// <summary>Checks and applies height and birth year (sex and activity apply as picked). False after telling the user what's wrong.</summary>
    async Task<bool> ApplyBodyAsync()
    {
        double? heightCm = null;
        if (!string.IsNullOrWhiteSpace(Height))
        {
            if (Int(Height) is not { } h || (heightCm = Imperial ? h * 2.54 : h) is < 50 or > 272)
            {
                await dialogs.Alert("Check your height", Imperial ? "Enter your height in inches, like 70." : "Enter your height in centimetres, like 178.");
                return false;
            }
        }
        int? year = null;
        if (!string.IsNullOrWhiteSpace(BirthYear))
        {
            if (Int(BirthYear) is not { } y || y < DateTime.Today.Year - 100 || y > DateTime.Today.Year - 10)
            {
                await dialogs.Alert("Check your birth year", "Enter the year you were born, like 1995.");
                return false;
            }
            year = y;
        }
        P.HeightCm = heightCm is { } cm ? Math.Round(cm, 1) : null;
        P.BirthYear = year;
        P.Sex = _sex;
        P.ActivityLevel = _activity;
        return true;
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
        if (!await ApplyBodyAsync())
            return;

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
