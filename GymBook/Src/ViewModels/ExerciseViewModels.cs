using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>Searchable, muscle-filterable exercise list shared by the library tab and the picker.</summary>
public abstract partial class ExerciseListViewModel : BaseViewModel
{
    protected readonly DataStore Store;
    MuscleGroup? _muscle;

    protected ExerciseListViewModel(DataStore store)
    {
        Store = store;
        Chips.Add(new ChipItem("All", null, SelectChip) { IsSelected = true });
        foreach (var m in Enum.GetValues<MuscleGroup>())
            Chips.Add(new ChipItem(m.Display(), m, SelectChip));
    }

    public ObservableCollection<ChipItem> Chips { get; } = [];

    [ObservableProperty] string searchText = "";
    [ObservableProperty] List<ExerciseItem> items = [];
    [ObservableProperty] string countText = "";

    partial void OnSearchTextChanged(string value) => Filter();

    void SelectChip(ChipItem chip)
    {
        foreach (var c in Chips)
            c.IsSelected = c == chip;
        _muscle = (MuscleGroup?)chip.Value;
        Filter();
    }

    protected void Filter()
    {
        var q = SearchText.Trim();
        Items = Store.AllExercises
            .Where(e => _muscle == null || e.PrimaryMuscle == _muscle)
            .Where(e => q.Length == 0
                || e.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.PrimaryMuscle.Display().Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Equipment.Display().Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Name)
            .Select(e => new ExerciseItem(e, OnTap) { IsSelected = IsSelected(e) })
            .ToList();
        CountText = Items.Count == 1 ? "1 exercise" : $"{Items.Count} exercises";
    }

    protected virtual bool IsSelected(Exercise e) => false;

    protected abstract void OnTap(ExerciseItem item);
}

public partial class ExercisesViewModel(DataStore store, DialogService dialogs) : ExerciseListViewModel(store)
{
    public override Task OnAppearingAsync()
    {
        Filter();
        return Task.CompletedTask;
    }

    protected override void OnTap(ExerciseItem item) => _ = GoTo($"{Routes.Exercise}?id={item.Exercise.Id}");

    [RelayCommand]
    async Task CreateCustom()
    {
        var name = await dialogs.Prompt("New exercise", "Exercise name", accept: "Next");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var muscle = await dialogs.ActionSheet("Primary muscle", null, [.. Enum.GetValues<MuscleGroup>().Select(m => m.Display())]);
        if (muscle == null)
            return;
        var equipment = await dialogs.ActionSheet("Equipment", null, [.. Enum.GetValues<Equipment>().Select(e => e.Display())]);
        if (equipment == null)
            return;
        var mechanic = await dialogs.ActionSheet("Type", null, "Compound (multi-joint)", "Isolation (single-joint)");
        var ex = new Exercise
        {
            Id = "custom_" + Guid.NewGuid().ToString("N")[..8],
            Name = name.Trim(),
            PrimaryMuscle = Enum.GetValues<MuscleGroup>().First(m => m.Display() == muscle),
            Equipment = Enum.GetValues<Equipment>().First(e => e.Display() == equipment),
            Mechanic = mechanic?.StartsWith("Compound") == true ? Mechanic.Compound : Mechanic.Isolation,
            Instructions = "Custom exercise.",
            IsCustom = true,
        };
        Store.Data.CustomExercises.Add(ex);
        Store.Save();
        Filter();
    }
}

public partial class ExercisePickerViewModel(DataStore store) : ExerciseListViewModel(store)
{
    readonly List<Exercise> _selected = [];

    public Action<List<Exercise>>? Completed { get; set; }

    [ObservableProperty] string addText = "Add";
    [ObservableProperty] bool canAdd;

    public void Reset()
    {
        _selected.Clear();
        SearchText = "";
        UpdateAdd();
        Filter();
    }

    protected override bool IsSelected(Exercise e) => _selected.Contains(e);

    protected override void OnTap(ExerciseItem item)
    {
        if (!_selected.Remove(item.Exercise))
            _selected.Add(item.Exercise);
        item.IsSelected = _selected.Contains(item.Exercise);
        UpdateAdd();
    }

    void UpdateAdd()
    {
        CanAdd = _selected.Count > 0;
        AddText = _selected.Count > 0 ? $"Add ({_selected.Count})" : "Add";
    }

    [RelayCommand]
    void Add() => Completed?.Invoke([.. _selected]);

    [RelayCommand]
    public void Cancel() => Completed?.Invoke([]);
}

public partial class ExerciseDetailViewModel(DataStore store, StatsService stats, Units units, DialogService dialogs)
    : BaseViewModel, IQueryAttributable
{
    string? _id;

    [ObservableProperty] string name = "";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] string instructions = "";
    [ObservableProperty] string primaryMuscle = "";
    [ObservableProperty] string secondaryMuscles = "";
    [ObservableProperty] bool hasSecondary;
    [ObservableProperty] string mechanic = "";
    [ObservableProperty] IDrawable muscleMap = MuscleMapDrawable.Empty;
    [ObservableProperty] bool hasHistory;
    [ObservableProperty] string bestE1Rm = "—";
    [ObservableProperty] string bestSet = "—";
    [ObservableProperty] string timesPerformed = "0";
    [ObservableProperty] IDrawable? chart;
    [ObservableProperty] List<LineItem> history = [];
    [ObservableProperty] bool isCustom;
    [ObservableProperty] string? imageStart;
    [ObservableProperty] string? imageEnd;
    [ObservableProperty] bool hasImages;
    [ObservableProperty] string tags = "";
    [ObservableProperty] bool hasTags;
    [ObservableProperty] bool showSummary;
    [ObservableProperty] List<LineItem> steps = [];
    [ObservableProperty] bool hasSteps;

    public void ApplyQueryAttributes(IDictionary<string, object> query) => _id = query["id"]?.ToString();

    public override Task OnAppearingAsync()
    {
        var ex = store.GetExercise(_id ?? "");
        if (ex == null)
            return GoBack();

        Name = ex.Name;
        Subtitle = ex.Subtitle;
        Instructions = ex.Instructions;
        PrimaryMuscle = ex.PrimaryMuscle.Display();
        SecondaryMuscles = string.Join(", ", ex.SecondaryMuscles.Select(m => m.Display()));
        HasSecondary = ex.SecondaryMuscles.Count > 0;
        Mechanic = ex.Mechanic == Models.Mechanic.Compound ? "Compound" : "Isolation";
        MuscleMap = MuscleMapDrawable.ForExercise(ex);
        IsCustom = ex.IsCustom;

        var details = ExerciseLibrary.Details(ex.Id);
        ImageStart = details?.Images.ElementAtOrDefault(0);
        ImageEnd = details?.Images.ElementAtOrDefault(1);
        HasImages = ImageStart != null;
        var tags = new[] { details?.Level, details?.Force, details?.Category }.Where(t => !string.IsNullOrEmpty(t)).ToList();
        Tags = string.Join(" · ", tags);
        HasTags = tags.Count > 0;
        Steps = details?.Steps.Select((s, i) => new LineItem { Title = (i + 1).ToString(), Detail = s }).ToList() ?? [];
        HasSteps = Steps.Count > 0;
        // Imported exercises' instructions are just their steps joined; curated ones have their own short summary.
        ShowSummary = details == null || ex.Instructions != string.Join(" ", details.Steps);

        var sessions = store.History
            .Select(s => (s, e: s.Exercises.FirstOrDefault(x => x.ExerciseId == ex.Id)))
            .Where(x => x.e != null)
            .ToList();
        HasHistory = sessions.Count > 0;
        TimesPerformed = sessions.Count.ToString();

        var best = stats.BestFor(ex.Id);
        BestE1Rm = best == null || ex.IsBodyweight ? "—" : units.FormatWithUnit(best.E1RmKg);
        BestSet = best == null ? "—" : $"{(ex.IsBodyweight && best.WeightKg <= 0 ? "BW" : units.Format(best.WeightKg))} × {best.Reps}";

        var points = stats.E1RmHistory(ex.Id).Select(p => p with { Value = units.ToDisplay(p.Value) }).ToList();
        Chart = new LineChartDrawable(points, Color.FromArgb("#3F7DFF"), v => $"{v:0.#}");

        History = sessions.Take(20).Select(x => new LineItem
        {
            Title = x.s.StartedAt.ToString("ddd, d MMM yyyy"),
            Detail = string.Join("   ", x.e!.Sets.Where(s => !s.IsWarmup).Select(s => $"{units.Format(s.WeightKg)}×{s.Reps}")),
            Value = x.s.Name,
        }).ToList();
        return Task.CompletedTask;
    }

    /// <summary>No open dataset has licensable demo videos, so this searches YouTube for form videos instead.</summary>
    [RelayCommand]
    Task WatchVideo() => Browser.Default.OpenAsync(
        $"https://www.youtube.com/results?search_query={Uri.EscapeDataString($"{Name} exercise proper form")}", BrowserLaunchMode.External);

    [RelayCommand]
    async Task Delete()
    {
        var ex = store.Data.CustomExercises.FirstOrDefault(e => e.Id == _id);
        if (ex == null || !await dialogs.Confirm("Delete exercise?", $"Delete \"{ex.Name}\"? It will also be removed from your plans.", "Delete"))
            return;
        store.Data.CustomExercises.Remove(ex);
        foreach (var w in store.Data.Plans.SelectMany(p => p.Workouts))
            w.Exercises.RemoveAll(e => e.ExerciseId == ex.Id);
        store.Save();
        await GoBack();
    }
}
