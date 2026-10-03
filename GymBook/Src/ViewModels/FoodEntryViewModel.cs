using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>Where the food page is: finding a food, setting how much, making a food of one's own, or adding calories alone.</summary>
public enum FoodStep { Search, Portion, Create, Quick }

/// <summary>How a portion is measured: in servings, by weight, or by its calories.</summary>
public enum PortionUnit { Servings, Grams, Calories }

/// <summary>
/// Logging food, in steps: <b>search</b> (filled in as it's typed from the foods on the phone, then the online
/// databases; with recent foods and My foods before anything's typed), then <b>portion</b> (how much, in servings, grams
/// or calories, with the macros and a nutrition label for that amount, the meal, and a star to keep it in My foods).
/// A food can also be <b>made</b> (name, serving and its nutrition, kept in My foods) or calories <b>quick-added</b>.
/// <c>food?id=…</c> edits a logged food (as one portion of itself); <c>food?date=yyyy-MM-dd&amp;meal=Lunch</c> adds one.
/// </summary>
public partial class FoodEntryViewModel(DataStore store, NutritionService nutrition, FoodSearchService search, IBarcodeScanner scanner,
    DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    // Daily values on a 2,000 kcal diet (FDA), for the label's percentages.
    const double FatDv = 78, CarbsDv = 275, ProteinDv = 50;

    CancellationTokenSource? _search;
    FoodEntry? _editing;
    DateTime _date = DateTime.Today;
    MealType _meal;
    bool _loaded;

    [ObservableProperty] string title = "Add food";
    [ObservableProperty] string dateText = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchStep), nameof(IsPortionStep), nameof(IsCreateStep), nameof(IsQuickStep), nameof(ShowMeal))]
    FoodStep step;
    public bool IsSearchStep => Step == FoodStep.Search;
    public bool IsPortionStep => Step == FoodStep.Portion;
    public bool IsCreateStep => Step == FoodStep.Create;
    public bool IsQuickStep => Step == FoodStep.Quick;
    /// <summary>The meal is picked while logging: setting a portion or quick-adding.</summary>
    public bool ShowMeal => Step is FoodStep.Portion or FoodStep.Quick;
    [ObservableProperty] bool isEditing;
    public bool CanScan => scanner.IsAvailable;

    public System.Collections.ObjectModel.ObservableCollection<ChipItem> MealChips { get; } = [];

    // ---------- Search ----------

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasQuery))] string searchText = "";
    public bool HasQuery => SearchText.Trim().Length > 0;
    [ObservableProperty] List<FoodMatchItem> searchResults = [];
    [ObservableProperty] bool isSearching;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasSearchMessage))] string searchMessage = "";
    public bool HasSearchMessage => SearchMessage.Length > 0;
    /// <summary>FatSecret's foods are in the results: its terms ask for "Powered by FatSecret" with them.</summary>
    [ObservableProperty] bool poweredByFatSecret;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasRecent))] List<FoodMatchItem> recent = [];
    public bool HasRecent => Recent.Count > 0;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasMyFoods))] List<FoodMatchItem> myFoods = [];
    public bool HasMyFoods => MyFoods.Count > 0;

    // ---------- Portion ----------

    FoodMatch? _food;
    (double Kcal, double P, double C, double F)? _perServing, _perGram;
    PortionUnit _unit;
    public System.Collections.ObjectModel.ObservableCollection<ChipItem> UnitChips { get; } = [];
    [ObservableProperty] string portionTitle = "";
    [ObservableProperty] string portionSource = "";
    [ObservableProperty] string portionKcalText = "";
    [ObservableProperty] string servingInfo = "";
    [ObservableProperty] string unitLabel = "";
    [ObservableProperty] string amountText = "1";
    [ObservableProperty] bool isFavorite;
    [ObservableProperty] bool canFavorite;
    [ObservableProperty] IDrawable? portionMacros;
    [ObservableProperty] List<FactRow> facts = [];
    [ObservableProperty] string factsServing = "";

    // ---------- Create ----------

    [ObservableProperty] string newName = "";
    [ObservableProperty] string newBrand = "";
    [ObservableProperty] string newServingText = "";
    [ObservableProperty] string newServingG = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NewCaloriesHint))] string newCalories = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NewCaloriesHint))] string newProtein = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NewCaloriesHint))] string newCarbs = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NewCaloriesHint))] string newFat = "";
    public string NewCaloriesHint => CaloriesHint(NewCalories, NewProtein, NewCarbs, NewFat);

    // ---------- Quick add ----------

    [ObservableProperty] string quickName = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(QuickCaloriesHint))] string quickCalories = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(QuickCaloriesHint))] string quickProtein = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(QuickCaloriesHint))] string quickCarbs = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(QuickCaloriesHint))] string quickFat = "";
    public string QuickCaloriesHint => CaloriesHint(QuickCalories, QuickProtein, QuickCarbs, QuickFat);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _loaded = false;
        _editing = query.TryGetValue("id", out var id) ? store.Data.FoodEntries.FirstOrDefault(f => f.Id == id?.ToString()) : null;
        _date = query.TryGetValue("date", out var date) && DateTime.TryParseExact(date?.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : _editing?.Date ?? DateTime.Today;
        _meal = query.TryGetValue("meal", out var meal) && Enum.TryParse<MealType>(meal?.ToString(), out var m) ? m : _editing?.Meal ?? MealType.Snack;
    }

    public override Task OnAppearingAsync()
    {
        // Once per visit: coming back to the page (e.g. from a dialog) keeps where it was.
        if (_loaded)
            return Task.CompletedTask;
        _loaded = true;

        IsEditing = _editing != null;
        if (_editing is { } e)
        {
            _date = e.Date;
            _meal = e.Meal;
        }
        DateText = _date == DateTime.Today ? "Today" : _date.ToString("dddd d MMMM", CultureInfo.CurrentCulture);
        MealChips.Clear();
        foreach (var meal in EnumDisplay.Meals)
            MealChips.Add(new ChipItem(meal.Display(), meal, SelectMeal) { IsSelected = meal == _meal });

        _search?.Cancel();
        SearchText = "";
        SearchResults = [];
        SearchMessage = "";
        PoweredByFatSecret = false;
        IsSearching = false;
        if (_editing is { } entry)
        {
            // A logged food opens as one portion of itself, to change how much (or the meal), or delete it.
            OpenPortion(new FoodMatch(entry.Name, null, entry.Calories, entry.ProteinG, entry.CarbsG, entry.FatG, "as logged", null, null, null, "Logged"));
        }
        else
        {
            ShowLists();
            GoToStep(FoodStep.Search);
        }
        return Task.CompletedTask;
    }

    void GoToStep(FoodStep step)
    {
        Step = step;
        Title = step switch
        {
            FoodStep.Portion => IsEditing ? "Edit food" : "Set portion",
            FoodStep.Create => "Create a food",
            FoodStep.Quick => "Quick add",
            _ => "Add food",
        };
    }

    /// <summary>Before anything's typed: foods logged lately, and My foods.</summary>
    void ShowLists()
    {
        Recent = [.. nutrition.RecentFoods(10).Select(f => Item(new FoodMatch(f.Name, null, f.Calories, f.ProteinG, f.CarbsG, f.FatG,
            "as logged", null, null, null, "Logged before")))];
        MyFoods = [.. search.MyFoods().Select(Item)];
    }

    // ---------- Search ----------

    partial void OnSearchTextChanged(string value) => _ = SearchSoon(value);

    /// <summary>
    /// Fills in as it's typed: the foods on the phone straight away, then everything (the online databases too) once typing
    /// pauses. A newer search replaces one still going.
    /// </summary>
    async Task SearchSoon(string text)
    {
        _search?.Cancel();
        var cts = _search = new CancellationTokenSource();
        if (text.Trim().Length == 0)
        {
            SearchResults = [];
            SearchMessage = "";
            PoweredByFatSecret = false;
            IsSearching = false;
            return;
        }
        try
        {
            await Task.Delay(120, cts.Token);
            var local = await search.LocalAsync(text);
            if (cts.IsCancellationRequested)
                return;
            SearchResults = [.. local.Select(Item)];
            SearchMessage = "";
            if (text.Trim().Length < 2)
                return;
            await Task.Delay(350, cts.Token);
            IsSearching = true;
            var found = await search.SearchAsync(text, cts.Token);
            if (cts.IsCancellationRequested)
                return;
            SearchResults = [.. found.Foods.Select(Item)];
            PoweredByFatSecret = found.PoweredByFatSecret;
            SearchMessage = found.Foods.Count == 0
                ? found.Offline ? "Nothing found offline. Connect to search more foods, or create it." : "Nothing found. Try other words, or create it."
                : found.Offline ? "Offline: only built-in foods and My foods." : "";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            SearchMessage = SearchResults.Count > 0 ? "Couldn't search online: check your connection." : "Couldn't search: check your connection.";
        }
        if (_search == cts)
            IsSearching = false;
    }

    FoodMatchItem Item(FoodMatch f) => new(f, new RelayCommand(() => OpenPortion(f)),
        f.Source == FoodSearchService.MyFoodSource ? new AsyncRelayCommand(() => Forget(f)) : null);

    async Task Forget(FoodMatch food)
    {
        if (!await dialogs.Confirm("Remove from My foods?", food.Title, "Remove"))
            return;
        search.Forget(food.Name, food.Brand);
        SearchResults = [.. SearchResults.Where(i => i.Food != food)];
        MyFoods = [.. MyFoods.Where(i => i.Food != food)];
    }

    /// <summary>FatSecret's terms: the credit links to it.</summary>
    [RelayCommand]
    static Task OpenFatSecret() => Launcher.Default.OpenAsync("https://www.fatsecret.com");

    /// <summary>Scans a package's barcode and looks it up (Open Food Facts, then USDA's branded foods).</summary>
    [RelayCommand]
    async Task ScanBarcode()
    {
        if (!scanner.IsAvailable || IsSearching)
            return;
        string? code;
        try
        {
            code = await scanner.ScanAsync();
        }
        catch (Exception e)
        {
            await dialogs.Alert("Couldn't scan", e.Message);
            return;
        }
        if (string.IsNullOrWhiteSpace(code))
            return;
        IsSearching = true;
        FoodMatch? found;
        try
        {
            found = await search.BarcodeAsync(code.Trim(), CancellationToken.None);
        }
        finally
        {
            IsSearching = false;
        }
        if (found == null)
        {
            await dialogs.Alert("Not found", $"No food with barcode {code} is known yet. Search by name, or create it.");
            return;
        }
        OpenPortion(found);
    }

    [RelayCommand]
    void StartCreate()
    {
        // What was searched for is a good start for the name.
        NewName = SearchText.Trim();
        NewBrand = NewServingText = NewServingG = NewCalories = NewProtein = NewCarbs = NewFat = "";
        GoToStep(FoodStep.Create);
    }

    [RelayCommand]
    void StartQuick()
    {
        QuickName = QuickCalories = QuickProtein = QuickCarbs = QuickFat = "";
        GoToStep(FoodStep.Quick);
    }

    /// <summary>Back a step: to the search (or out, when editing a logged food).</summary>
    [RelayCommand]
    async Task Cancel()
    {
        if (Step == FoodStep.Search || IsEditing)
        {
            await GoBack();
            return;
        }
        ShowLists();
        GoToStep(FoodStep.Search);
    }

    // ---------- Portion ----------

    /// <summary>Sets how much of <paramref name="food"/>: in servings when it has them, else by weight; calories always.</summary>
    void OpenPortion(FoodMatch food)
    {
        _food = food;
        _perGram = food.ByWeight ? Per(food, 1 / food.PerGrams!.Value)
            : food.ServingG is > 0 ? Per(food, 1 / food.ServingG.Value)
            : null;
        _perServing = !food.ByWeight ? Per(food, 1)
            : food.ServingG is > 0 ? Per(food, food.ServingG.Value / food.PerGrams!.Value)
            : null;

        UnitChips.Clear();
        if (_perServing != null)
            UnitChips.Add(new ChipItem("Servings", PortionUnit.Servings, SelectUnit));
        if (_perGram != null)
            UnitChips.Add(new ChipItem("Grams", PortionUnit.Grams, SelectUnit));
        if ((_perServing ?? _perGram)?.Kcal > 0)
            UnitChips.Add(new ChipItem("Calories", PortionUnit.Calories, SelectUnit));
        _unit = _perServing != null ? PortionUnit.Servings : PortionUnit.Grams;
        foreach (var c in UnitChips)
            c.IsSelected = (PortionUnit)c.Value! == _unit;
        AmountText = Number(_unit == PortionUnit.Servings ? 1 : food.ServingG ?? 100);

        PortionTitle = food.Title;
        PortionSource = food.Source;
        ServingInfo = food.ServingG is { } g
            ? $"1 {food.ServingText ?? "serving"} = {g:0.#} g"
            : food.PerText == "as logged" ? "1 serving = what was logged"
            : _perServing != null ? $"1 serving = {food.PerText}" : "Per 100 g";
        CanFavorite = food.Source != "Logged" && food.Source != "Logged before";
        IsFavorite = food.Source == FoodSearchService.MyFoodSource || search.IsSaved(food.Name, food.Brand);
        GoToStep(FoodStep.Portion);
        ShowPortion();

        static (double, double, double, double) Per(FoodMatch f, double factor) => (f.Kcal * factor, f.Protein * factor, f.Carbs * factor, f.Fat * factor);
    }

    partial void OnAmountTextChanged(string value) => ShowPortion();

    /// <summary>What the amount comes to in the chosen unit; null when it isn't a number.</summary>
    (double Kcal, double P, double C, double F)? Totals()
    {
        if (Parse(AmountText) is not { } amount)
            return null;
        (double Kcal, double P, double C, double F)? basis = _unit switch
        {
            PortionUnit.Servings => _perServing,
            PortionUnit.Grams => _perGram,
            _ => _perServing ?? _perGram,
        };
        if (basis is not { } b)
            return null;
        var factor = _unit == PortionUnit.Calories ? (b.Kcal > 0 ? amount / b.Kcal : 0) : amount;
        return (b.Kcal * factor, b.P * factor, b.C * factor, b.F * factor);
    }

    void ShowPortion()
    {
        if (_food == null)
            return;
        UnitLabel = _unit switch
        {
            PortionUnit.Servings => Parse(AmountText) == 1 ? _food.ServingText ?? "serving" : "servings",
            PortionUnit.Grams => "g",
            _ => "Cal",
        };
        var t = Totals() ?? (0, 0, 0, 0);
        PortionKcalText = $"{NutritionService.Kcal(t.Kcal)} Cal";
        PortionMacros = new MacroSplitDrawable([t.C * 4, t.F * 9, t.P * 4], [], [NutritionViewModel.Carbs, NutritionViewModel.Fat, NutritionViewModel.Protein]);
        FactsServing = $"{AmountText.Trim()} {UnitLabel}";
        static string Dv(double grams, double dv) => $"{grams / dv * 100:0} %";
        Facts =
        [
            new("Calories", NutritionService.Kcal(t.Kcal), "", true),
            new("Total fat", $"{t.F:0.#} g", Dv(t.F, FatDv), true),
            new("Total carbohydrate", $"{t.C:0.#} g", Dv(t.C, CarbsDv), true),
            new("Protein", $"{t.P:0.#} g", Dv(t.P, ProteinDv), true),
        ];
    }

    void SelectUnit(ChipItem chip)
    {
        var unit = (PortionUnit)chip.Value!;
        if (unit == _unit)
            return;
        // The same food either way: the amount converts.
        var kcal = Totals()?.Kcal;
        var grams = _unit switch
        {
            PortionUnit.Grams => Parse(AmountText),
            PortionUnit.Servings when _food?.ServingG is { } g => Parse(AmountText) * g,
            _ => null,
        };
        var servings = _unit == PortionUnit.Servings ? Parse(AmountText) : null;
        _unit = unit;
        foreach (var c in UnitChips)
            c.IsSelected = c == chip;
        double? amount = unit switch
        {
            PortionUnit.Calories => kcal,
            PortionUnit.Grams => grams ?? (kcal is { } k && _perGram is { Kcal: > 0 } pg ? k / pg.Kcal : null),
            _ => servings ?? (kcal is { } k2 && _perServing is { Kcal: > 0 } ps ? k2 / ps.Kcal : null),
        };
        AmountText = Number(amount ?? (unit == PortionUnit.Servings ? 1 : 100));
        ShowPortion();
    }

    /// <summary>A step down or up: half a serving, 10 g or 25 Cal.</summary>
    [RelayCommand]
    void Nudge(string direction)
    {
        var step = _unit switch { PortionUnit.Servings => 0.5, PortionUnit.Grams => 10, _ => 25 };
        var amount = Math.Max(0, (Parse(AmountText) ?? 0) + (direction == "-" ? -step : step));
        AmountText = Number(Math.Round(amount / step) * step);
    }

    /// <summary>The star: keeps the food in My foods (one serving of it, or 100 g), or takes it out.</summary>
    [RelayCommand]
    void ToggleFavorite()
    {
        if (_food == null || !CanFavorite)
            return;
        if (IsFavorite)
        {
            search.Forget(_food.Name, _food.Brand);
            IsFavorite = false;
            return;
        }
        (double Kcal, double P, double C, double F) per;
        if (_perServing is { } serving)
            per = serving;
        else if (_perGram is { } gram)
            per = (gram.Kcal * 100, gram.P * 100, gram.C * 100, gram.F * 100);
        else
            return;
        search.Save(_food.Name, _food.Brand, per.Kcal, per.P, per.C, per.F,
            _perServing != null ? _food.ServingText ?? (_food.ByWeight ? null : _food.PerText) : "100 g",
            _perServing != null ? _food.ServingG : 100);
        IsFavorite = true;
    }

    [RelayCommand]
    async Task SavePortion()
    {
        if (_food == null)
            return;
        if (Totals() is not { } t || Parse(AmountText) is not > 0)
        {
            await dialogs.Alert("How much?", "Enter how much you had, more than 0.");
            return;
        }
        await Log(_food.Title, t.Kcal, t.P, t.C, t.F);
    }

    // ---------- Create ----------

    /// <summary>Makes a food of one's own (kept in My foods), then sets how much of it was had.</summary>
    [RelayCommand]
    async Task SaveCreated()
    {
        if (NewName.Trim().Length == 0)
        {
            await dialogs.Alert("Name it", "Give the food a name, to find it again.");
            return;
        }
        if (Invalid(NewCalories) || Invalid(NewProtein) || Invalid(NewCarbs) || Invalid(NewFat) || Invalid(NewServingG))
        {
            await dialogs.Alert("Check the numbers", "Calories, macros and the serving's weight need to be numbers.");
            return;
        }
        var (protein, carbs, fat) = (Parse(NewProtein) ?? 0, Parse(NewCarbs) ?? 0, Parse(NewFat) ?? 0);
        var kcal = Parse(NewCalories) ?? NutritionService.CaloriesFromMacros(protein, carbs, fat);
        if (kcal <= 0 && protein + carbs + fat <= 0)
        {
            await dialogs.Alert("Nothing in it", "Enter a serving's calories, or its protein, carbs and fat.");
            return;
        }
        var food = search.Save(NewName, NewBrand, kcal, protein, carbs, fat, NewServingText, Parse(NewServingG));
        OpenPortion(food);
    }

    // ---------- Quick add ----------

    [RelayCommand]
    async Task SaveQuick()
    {
        if (Invalid(QuickCalories) || Invalid(QuickProtein) || Invalid(QuickCarbs) || Invalid(QuickFat))
        {
            await dialogs.Alert("Check the numbers", "Calories and macros need to be numbers.");
            return;
        }
        var (protein, carbs, fat) = (Parse(QuickProtein) ?? 0, Parse(QuickCarbs) ?? 0, Parse(QuickFat) ?? 0);
        var kcal = Parse(QuickCalories) ?? NutritionService.CaloriesFromMacros(protein, carbs, fat);
        await Log(QuickName.Trim().Length > 0 ? QuickName.Trim() : "Quick add", kcal, protein, carbs, fat);
    }

    // ---------- Logging ----------

    /// <summary>Logs it (or updates the food being edited) on the day, in the meal picked, and goes back.</summary>
    async Task Log(string name, double kcal, double protein, double carbs, double fat)
    {
        if (kcal <= 0 && protein + carbs + fat <= 0)
        {
            await dialogs.Alert("Nothing to log", "Enter the calories, or the protein, carbs and fat.");
            return;
        }
        if (kcal > 20000 || protein > 2000 || carbs > 2000 || fat > 2000)
        {
            await dialogs.Alert("That's a lot", "Check the amounts: one entry can be up to 20,000 kcal and 2,000 g of each macro.");
            return;
        }
        name = name.Trim();
        if (name.Length > SyncLimits.NameLength)
            name = name[..SyncLimits.NameLength];
        var entry = _editing ?? new FoodEntry { Date = _date, LoggedAt = DateTime.Now };
        entry.Name = name.Length > 0 ? name : $"{_meal.Display()} food";
        entry.Meal = _meal;
        entry.Calories = Math.Round(kcal, 1);
        entry.ProteinG = Math.Round(protein, 1);
        entry.CarbsG = Math.Round(carbs, 1);
        entry.FatG = Math.Round(fat, 1);
        if (_editing == null)
            store.Data.FoodEntries.Add(entry);
        store.Save();
        await GoBack();
    }

    [RelayCommand]
    async Task Delete()
    {
        if (_editing is not { } entry || !await dialogs.Confirm("Delete this food?", entry.Name, "Delete"))
            return;
        store.Data.FoodEntries.Remove(entry);
        store.Save();
        await GoBack();
    }

    void SelectMeal(ChipItem chip)
    {
        _meal = (MealType)chip.Value!;
        foreach (var c in MealChips)
            c.IsSelected = c == chip;
    }

    /// <summary>What the macros come to while the calories are left empty.</summary>
    static string CaloriesHint(string calories, string protein, string carbs, string fat)
    {
        if (Parse(calories) != null)
            return "";
        var fromMacros = NutritionService.CaloriesFromMacros(Parse(protein) ?? 0, Parse(carbs) ?? 0, Parse(fat) ?? 0);
        return fromMacros > 0 ? $"From the macros: {NutritionService.Kcal(fromMacros)} Cal" : "Leave calories empty to work them out from the macros.";
    }

    static string Number(double v) => Math.Round(v, 1).ToString("0.#", CultureInfo.CurrentCulture);

    static double? Parse(string? text) =>
        double.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0 && double.IsFinite(v) ? v : null;

    static bool Invalid(string text) => !string.IsNullOrWhiteSpace(text) && Parse(text) == null;
}

/// <summary>A food in the search or the lists: its name, where it's from, and a preview of what's in it.</summary>
public class FoodMatchItem(FoodMatch food, ICommand select, ICommand? remove)
{
    public FoodMatch Food => food;
    public string Title => food.Title;
    public string Source => food.Source;
    /// <summary>Its calories for the usual amount: a serving when known, else 100 g.</summary>
    public string Calories => $"{NutritionService.Kcal(Usual.Kcal)} Cal";
    /// <summary>What that amount is: "1 cup (158 g)", "1 serving", "100 g".</summary>
    public string Amount => food.ByWeight && food.ServingG is { } g
        ? $"{food.ServingText ?? "1 serving"} ({g:0.#} g)"
        : food.PerText;
    public string Macros => $"P {Usual.P:0.#} · C {Usual.C:0.#} · F {Usual.F:0.#}";
    public IDrawable MacroBar => new MacroMiniBarDrawable(Usual.C * 4, Usual.F * 9, Usual.P * 4);
    public ICommand SelectCommand => select;
    /// <summary>Takes a food out of My foods; null for the rest.</summary>
    public ICommand? RemoveCommand => remove;
    public bool CanRemove => remove != null;

    (double Kcal, double P, double C, double F) Usual => food.ByWeight && food.ServingG is { } g ? food.For(g) : food.For(food.ByWeight ? food.PerGrams!.Value : 1);
}

/// <summary>A line of the nutrition label: what, how much, and its share of the daily value.</summary>
public record FactRow(string Name, string Amount, string DailyValue, bool Bold);
