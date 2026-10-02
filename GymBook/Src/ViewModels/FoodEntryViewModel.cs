using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// Logs something eaten, or edits or deletes it: <c>food?id=…</c> edits, <c>food?date=yyyy-MM-dd&amp;meal=Lunch</c> adds.
/// Amounts are per serving, times the servings eaten; calories left empty are worked out from the macros.
/// </summary>
public partial class FoodEntryViewModel(DataStore store, NutritionService nutrition, FoodSearchService search, DialogService dialogs)
    : BaseViewModel, IQueryAttributable
{
    CancellationTokenSource? _search;

    FoodEntry? _editing;
    DateTime _date = DateTime.Today;
    MealType _meal;
    bool _loaded;

    [ObservableProperty] string title = "Add food";
    [ObservableProperty] bool isEditing;
    [ObservableProperty] string name = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CaloriesHint))] string calories = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CaloriesHint))] string protein = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CaloriesHint))] string carbs = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CaloriesHint))] string fat = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CaloriesHint))] string servings = "1";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] List<RecentFoodItem> recent = [];
    [ObservableProperty] bool hasRecent;

    // Searching for a food with its nutrition filled in
    [ObservableProperty] string searchText = "";
    [ObservableProperty] List<FoodMatchItem> searchResults = [];
    [ObservableProperty] bool isSearching;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasSearchMessage))] string searchMessage = "";
    /// <summary>What was picked from the search and how much: "150 g of Chicken breast, cooked".</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasPicked))] string pickedText = "";
    public bool HasSearchMessage => SearchMessage.Length > 0;
    public bool HasPicked => PickedText.Length > 0;

    public System.Collections.ObjectModel.ObservableCollection<ChipItem> MealChips { get; } = [];

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
        // Once per visit: coming back to the page (e.g. from a dialog) keeps what's typed.
        if (_loaded)
            return Task.CompletedTask;
        _loaded = true;

        IsEditing = _editing != null;
        Title = IsEditing ? "Edit food" : "Add food";
        DateText = _date == DateTime.Today ? "Today" : _date.ToString("dddd d MMMM", CultureInfo.CurrentCulture);
        if (_editing is { } e)
        {
            _date = e.Date;
            _meal = e.Meal;
            Fill(e);
        }

        MealChips.Clear();
        foreach (var meal in Enum.GetValues<MealType>())
            MealChips.Add(new ChipItem(meal.ToString(), meal, SelectMeal) { IsSelected = meal == _meal });

        SearchText = "";
        SearchResults = [];
        SearchMessage = "";
        PickedText = "";
        Recent = IsEditing ? [] : [.. nutrition.RecentFoods(12).Select(f => new RecentFoodItem
        {
            Name = f.Name,
            Detail = $"{NutritionService.Kcal(f.Calories)} kcal · P {f.ProteinG:0} · C {f.CarbsG:0} · F {f.FatG:0}",
            SelectCommand = new RelayCommand(() => Fill(f)),
        })];
        HasRecent = Recent.Count > 0;
        return Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value) => _ = SearchSoon(value);

    /// <summary>Searches once typing pauses; a newer search replaces one still going.</summary>
    async Task SearchSoon(string text)
    {
        _search?.Cancel();
        var cts = _search = new CancellationTokenSource();
        if (text.Trim().Length < 2)
        {
            SearchResults = [];
            SearchMessage = "";
            IsSearching = false;
            return;
        }
        try
        {
            await Task.Delay(400, cts.Token);
            IsSearching = true;
            SearchMessage = "";
            var found = await search.SearchAsync(text, cts.Token);
            if (cts.IsCancellationRequested)
                return;
            SearchResults = [.. found.Select(f => new FoodMatchItem(f, new AsyncRelayCommand(() => Pick(f))))];
            SearchMessage = found.Count == 0 ? "Nothing found. Try other words, or fill it in below." : "";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            SearchResults = [];
            SearchMessage = "Couldn't search: check your connection.";
        }
        if (_search == cts)
            IsSearching = false;
    }

    /// <summary>A food from the search: how much was eaten (a serving, or in grams), then its amounts fill the form.</summary>
    async Task Pick(FoodMatch food)
    {
        var start = food.ServingG ?? 100;
        var grams = await Views.NumberPadSheet.Show(start.ToString("0.#", CultureInfo.InvariantCulture), start, 5, 5, 1000, "g", decimals: true);
        if (grams is not { } g || double.IsNaN(g) || g <= 0)
            return;
        var (kcal, protein, carbs, fat) = food.For(g);
        Name = food.Title.Length > SyncLimits.NameLength ? food.Title[..SyncLimits.NameLength] : food.Title;
        Calories = Number(kcal);
        Protein = Number(protein);
        Carbs = Number(carbs);
        Fat = Number(fat);
        Servings = "1";
        PickedText = $"{g.ToString("0.#", CultureInfo.CurrentCulture)} g of {food.Title}";
        _search?.Cancel();
        SearchText = "";
        SearchResults = [];
        SearchMessage = "";
        IsSearching = false;
    }

    void Fill(FoodEntry f)
    {
        Name = f.Name;
        Calories = Number(f.Calories);
        Protein = Number(f.ProteinG);
        Carbs = Number(f.CarbsG);
        Fat = Number(f.FatG);
        Servings = "1";
    }

    static string Number(double v) => v == 0 ? "" : Math.Round(v, 1).ToString("0.#", CultureInfo.CurrentCulture);

    void SelectMeal(ChipItem chip)
    {
        _meal = (MealType)chip.Value!;
        foreach (var c in MealChips)
            c.IsSelected = c == chip;
    }

    /// <summary>What the macros come to, shown while the calories are left empty; or the total for several servings.</summary>
    public string CaloriesHint
    {
        get
        {
            var count = Parse(Servings) ?? 1;
            if (Parse(Calories) is { } kcal)
                return count != 1 && count > 0 ? $"{NutritionService.Kcal(kcal * count)} kcal in all" : "";
            var fromMacros = NutritionService.CaloriesFromMacros(Parse(Protein) ?? 0, Parse(Carbs) ?? 0, Parse(Fat) ?? 0);
            return fromMacros > 0 ? $"From the macros: {NutritionService.Kcal(fromMacros * Math.Max(count, 0))} kcal" : "";
        }
    }

    static double? Parse(string? text) =>
        double.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0 && double.IsFinite(v) ? v : null;

    static bool Invalid(string text) => !string.IsNullOrWhiteSpace(text) && Parse(text) == null;

    [RelayCommand]
    async Task Save()
    {
        if (Invalid(Calories) || Invalid(Protein) || Invalid(Carbs) || Invalid(Fat) || Parse(Servings) is not > 0)
        {
            await dialogs.Alert("Check the numbers", "Calories, macros and servings need to be numbers, and servings more than 0.");
            return;
        }
        var count = Parse(Servings)!.Value;
        var protein = (Parse(Protein) ?? 0) * count;
        var carbs = (Parse(Carbs) ?? 0) * count;
        var fat = (Parse(Fat) ?? 0) * count;
        var kcal = (Parse(Calories) ?? NutritionService.CaloriesFromMacros(Parse(Protein) ?? 0, Parse(Carbs) ?? 0, Parse(Fat) ?? 0)) * count;
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

        var name = Name.Trim();
        if (name.Length > SyncLimits.NameLength)
            name = name[..SyncLimits.NameLength];
        var entry = _editing ?? new FoodEntry { Date = _date, LoggedAt = DateTime.Now };
        entry.Name = name.Length > 0 ? name : $"{_meal} food";
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
}

/// <summary>A search result: name, the usual serving and its amounts per 100 g.</summary>
public class FoodMatchItem(FoodMatch food, ICommand select)
{
    public string Title => food.Title;
    public string Detail =>
        $"{NutritionService.Kcal(food.Kcal100)} kcal · P {food.Protein100:0.#} · C {food.Carbs100:0.#} · F {food.Fat100:0.#} per 100 g"
        + (food.ServingG is { } g ? $" · {food.ServingText ?? "serving"}{(food.ServingText?.Contains('g') == true ? "" : $" {g:0} g")}" : "");
    public ICommand SelectCommand => select;
}

public class RecentFoodItem
{
    public required string Name { get; init; }
    public string Detail { get; init; } = "";
    public required ICommand SelectCommand { get; init; }
}
