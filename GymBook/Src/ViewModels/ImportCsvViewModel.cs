using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services;
using GymBook.Services.Import;

namespace GymBook.ViewModels;

public enum CsvImportKind { Plans, Workouts }

/// <summary>
/// One CSV import tab of the import sheet: plans, or the workout history, each from its own export file. Picking the
/// file shows what's in it and what each exercise becomes; any of those can be changed before importing.
/// </summary>
public partial class ImportCsvViewModel : ObservableObject
{
    static readonly FilePickerFileType CsvTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["text/csv", "text/comma-separated-values", "text/plain", "application/csv", "application/vnd.ms-excel", "application/octet-stream"],
        [DevicePlatform.iOS] = ["public.comma-separated-values-text", "public.plain-text"],
        [DevicePlatform.MacCatalyst] = ["public.comma-separated-values-text", "public.plain-text"],
        [DevicePlatform.WinUI] = [".csv", ".txt"],
    });

    readonly CsvImporter _importer;
    readonly ExercisePickerService _picker;
    readonly DialogService _dialogs;
    List<ImportedPlan>? _plans;
    List<ImportedWorkout>? _workouts;
    List<ExerciseMapping> _mappings = [];
    // A row for every exercise in the file; Mappings shows those in the workouts being imported (see ShowMappings).
    List<MappingItem> _allItems = [];
    // Importing workouts for one plan only: those matching its days. Null: every workout.
    Models.WorkoutPlan? _onlyPlan;

    public ImportCsvViewModel(CsvImportKind kind, CsvImporter importer, ExercisePickerService picker, DialogService dialogs)
    {
        Kind = kind;
        (_importer, _picker, _dialogs) = (importer, picker, dialogs);
    }

    public CsvImportKind Kind { get; }

    public string Title => Kind == CsvImportKind.Plans ? "Import plans" : "Import workout history";

    public string Explanation => Kind == CsvImportKind.Plans
        ? "Pick the plans export (the CSV with each plan's days and their exercises, sets and reps). Import plans before the workout history, so the workouts link to their plan days and weeks."
        : "Pick the workout history export (the CSV with each workout's date, exercises and sets). Weights in lb are converted. Workouts already in the app aren't added again.";

    public ObservableCollection<MappingItem> Mappings { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFile))]
    string fileName = "";

    public bool HasFile => FileName.Length > 0;

    [ObservableProperty] string summary = "";
    [ObservableProperty] string mappingSummary = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    string error = "";

    public bool HasError => Error.Length > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    bool isReady;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    bool isImporting;

    public bool CanImport => IsReady && !IsImporting && (_onlyPlan == null || Matching > 0);

    /// <summary>Workouts: the row to import them for one plan only (when there are plans in the app).</summary>
    [ObservableProperty] bool showPlanFilter;
    [ObservableProperty] string planFilterText = "";
    [ObservableProperty] string planFilterDetail = "";

    /// <summary>How many of the file's workouts match the chosen plan's days.</summary>
    int Matching => _workouts == null || _onlyPlan == null ? 0 : _workouts.Count(w => CsvImporter.DayIn(_onlyPlan, w) != null);

    /// <summary>The workouts that will be imported: those matching the chosen plan's days, or all.</summary>
    List<ImportedWorkout> Importing => _workouts == null ? [] : _onlyPlan == null ? _workouts : [.. _workouts.Where(w => CsvImporter.DayIn(_onlyPlan, w) != null)];

    static string WorkoutsSummary(List<ImportedWorkout> workouts)
    {
        if (workouts.Count == 0)
            return "No workouts";
        var sets = workouts.Sum(w => w.Exercises.Sum(e => e.Sets.Count));
        return $"{Count(workouts.Count, "workout")} from {workouts.Min(w => w.StartedAt):d MMM yyyy} to {workouts.Max(w => w.StartedAt):d MMM yyyy} · {Count(sets, "set")}";
    }

    /// <summary>
    /// The exercises of the workouts being imported (for one plan: only its workouts), each with how often it's used in
    /// them. Changes made to one stay when another plan is picked.
    /// </summary>
    void ShowMappings()
    {
        var uses = (Kind == CsvImportKind.Workouts ? Importing.SelectMany(w => w.Exercises).Select(e => e.Exercise.Key) : _allItems.Select(i => i.Source))
            .GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());
        Mappings.Clear();
        foreach (var item in _allItems)
        {
            if (Kind == CsvImportKind.Workouts && !uses.ContainsKey(item.Source))
                continue;
            if (Kind == CsvImportKind.Workouts)
                item.SetUses(uses[item.Source]);
            Mappings.Add(item);
        }
        UpdateMappingSummary();
    }

    void UpdatePlanFilter()
    {
        ShowPlanFilter = Kind == CsvImportKind.Workouts && _workouts != null && _importer.Plans.Count > 0;
        PlanFilterText = _onlyPlan?.Name ?? "Every plan";
        PlanFilterDetail = _onlyPlan == null
            ? "Each workout goes with the plan the file names, if it's in the app. Tap to import only one plan's workouts."
            : Matching == 0
                ? $"None of the workouts match a day of {_onlyPlan.Name} (by workout name, or day number when the file names this plan)."
                : $"{Count(Matching, "workout")} of {_workouts!.Count} match a day of {_onlyPlan.Name} and are imported for it; the rest are left out.";
        if (_workouts != null)
        {
            Summary = _onlyPlan == null ? WorkoutsSummary(_workouts) : $"{WorkoutsSummary(Importing)} for {_onlyPlan.Name} (of {_workouts.Count} in the file)";
        }
        ShowMappings();
        OnPropertyChanged(nameof(CanImport));
        ImportCommand.NotifyCanExecuteChanged();
    }

    /// <summary>The plan row tapped: every plan, or one plan's workouts only.</summary>
    [RelayCommand]
    async Task ChoosePlan()
    {
        const string every = "Every plan";
        var plans = _importer.Plans;
        // Numbered so plans with the same name stay distinguishable.
        var labels = plans.Select((p, i) => $"{i + 1}. {p.Name}").ToList();
        var pick = await _dialogs.ActionSheet("Import workouts for", null, [every, .. labels]);
        if (pick == null)
            return;
        _onlyPlan = pick == every ? null : plans[labels.IndexOf(pick)];
        UpdatePlanFilter();
    }

    public string ImportText => Kind == CsvImportKind.Plans ? "Import plans" : "Import workouts";

    /// <summary>Done: the sheet closes.</summary>
    public event Action? Imported;

    [RelayCommand]
    async Task PickFile()
    {
        Error = "";
        FileResult? picked;
        try
        {
            picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = Title, FileTypes = CsvTypes });
        }
        catch (Exception)
        {
            Error = "Couldn't open the file picker.";
            return;
        }
        if (picked == null)
            return;
        string text;
        try
        {
            await using var stream = await picked.OpenReadAsync();
            using var reader = new StreamReader(stream);
            text = await reader.ReadToEndAsync();
        }
        catch (Exception)
        {
            Error = "Couldn't read that file.";
            return;
        }
        await LoadAsync(picked.FileName, text);
    }

    async Task LoadAsync(string name, string text)
    {
        Clear();
        IEnumerable<ImportedExerciseRef> used;
        try
        {
            if (Kind == CsvImportKind.Plans)
            {
                _plans = CsvExportFormat.ParsePlans(text);
                var workouts = _plans.Sum(p => p.Days.Count(d => d.Exercises.Count > 0));
                var rest = _plans.Sum(p => p.Days.Count(d => d.Exercises.Count == 0));
                Summary = $"{Count(_plans.Count, "plan")}: {string.Join(", ", _plans.Select(p => p.Name))} · {Count(workouts, "workout")}" +
                    (rest > 0 ? $", {Count(rest, "rest day")}" : "");
                used = _plans.SelectMany(p => p.Days).SelectMany(d => d.Exercises).Select(e => e.Exercise).ToList();
            }
            else
            {
                _workouts = CsvExportFormat.ParseWorkouts(text);
                Summary = WorkoutsSummary(_workouts);
                used = _workouts.SelectMany(w => w.Exercises).Select(e => e.Exercise).ToList();
            }
        }
        catch (FormatException e)
        {
            Error = e.Message;
            return;
        }
        FileName = name;
        // The AI matches the names the word matcher isn't sure of; it takes a few seconds.
        IsMatching = true;
        try
        {
            (_mappings, _aiChecked) = await _importer.MappingsAsync(used);
        }
        finally
        {
            IsMatching = false;
        }
        // Another file was picked, or this one removed, while matching.
        if (FileName != name)
            return;
        _allItems = [.. _mappings.Select(m => new MappingItem(m, Change))];
        IsReady = true;
        UpdatePlanFilter();
    }

    /// <summary>The AI is matching the file's exercises.</summary>
    [ObservableProperty] bool isMatching;

    bool _aiChecked;

    void Clear()
    {
        _plans = null;
        _workouts = null;
        _mappings = [];
        _onlyPlan = null;
        _allItems = [];
        ShowPlanFilter = false;
        Mappings.Clear();
        FileName = Summary = MappingSummary = "";
        IsReady = false;
    }

    [RelayCommand]
    void RemoveFile()
    {
        Clear();
        Error = "";
    }

    void UpdateMappingSummary()
    {
        // Of the exercises shown: those in the workouts being imported.
        var shown = Mappings.Select(i => i.Mapping).ToList();
        var matched = shown.Count(m => m.Target != null);
        var check = Mappings.Count(m => m.Status == "Check");
        var chosen = Mappings.Count(m => m.IsChosen);
        var created = shown.Count - matched;
        var byAi = shown.Count(m => m.Match.Source == MatchSource.Ai && m.Target == m.Match.Exercise);
        MappingSummary = $"{Count(shown.Count, "exercise")}: {matched} matched to the app's" +
            (byAi > 0 ? $" ({byAi} by AI)" : "") +
            (check > 0 ? $" ({check} worth a check)" : "") + (created > 0 ? $", {created} added as new" : "") +
            (chosen > 0 ? $", {chosen} chosen by you" : "") + ". Tap one to change it." +
            (_aiChecked ? "" : " Sign in and go online for AI matching of the rest.");
    }

    /// <summary>
    /// One exercise tapped: pick another of the app's, keep it as a new exercise of its own, or go back to the suggestion
    /// (for a match, that's how it started: matched or worth a check again).
    /// </summary>
    async Task Change(MappingItem item)
    {
        const string pick = "Pick an exercise", keep = "Add as a new exercise", suggested = "Use the suggestion";
        var options = new List<string> { pick };
        if (item.Mapping.Target != null)
            options.Add(keep);
        if (item.Mapping.Match.Exercise is { } guess && (item.Mapping.Target != guess || item.IsChosen))
            options.Add(suggested);
        switch (await _dialogs.ActionSheet(item.Source, null, [.. options]))
        {
            case pick:
                if (await _picker.PickOneAsync($"For {item.Mapping.Source.Name}") is { } exercise)
                    item.SetTarget(exercise);
                break;
            case keep:
                item.SetTarget(null);
                break;
            case suggested:
                item.UseSuggestion();
                break;
        }
        UpdateMappingSummary();
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    async Task Import()
    {
        IsImporting = true;
        try
        {
            // The data is changed on the main thread, as everywhere; a moment first so the spinner shows (a few hundred
            // workouts take a second or two to save).
            await Task.Delay(50);
            int added;
            string what;
            if (Kind == CsvImportKind.Plans && _plans != null)
            {
                var plans = _plans;
                added = _importer.ImportPlans(plans, _mappings);
                what = Count(added, "plan");
            }
            else if (_workouts != null)
            {
                var workouts = _workouts;
                added = _importer.ImportWorkouts(workouts, _mappings, _onlyPlan);
                what = Count(added, "workout");
            }
            else
                return;
            await _dialogs.Alert(added == 0 ? "Nothing new" : "Imported",
                added == 0 ? (_onlyPlan == null ? "Everything in this file is already in the app." : $"The workouts matching {_onlyPlan.Name} are already in the app.")
                    : $"{what} added. They sync to your account like anything else.");
            if (added > 0)
                Imported?.Invoke();
        }
        catch (Exception e)
        {
            Error = $"Couldn't import: {e.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    static string Count(int n, string noun) => n == 1 ? $"1 {noun}" : $"{n} {noun}s";
}

/// <summary>A row of the import's exercises: theirs, what it becomes in the app, and how sure that is.</summary>
public partial class MappingItem : ObservableObject
{
    readonly Func<MappingItem, Task> _change;

    public MappingItem(ExerciseMapping mapping, Func<MappingItem, Task> change)
    {
        Mapping = mapping;
        _change = change;
        ChangeCommand = new AsyncRelayCommand(() => _change(this));
    }

    public ExerciseMapping Mapping { get; }
    public IAsyncRelayCommand ChangeCommand { get; }

    /// <summary>Settled by hand (even to the suggestion itself), so it's no longer worth a check.</summary>
    public bool IsChosen { get; private set; }

    public string Source => Mapping.Source.Key;

    int? _uses;

    /// <summary>How often it's used in the workouts being imported (all of the file's, until one plan is picked).</summary>
    public string Uses => $"{_uses ?? Mapping.Uses}×";

    public void SetUses(int uses)
    {
        _uses = uses;
        OnPropertyChanged(nameof(Uses));
    }

    public string Target => Mapping.Target?.Name ?? $"New: {CsvImporter.Name(Mapping.Source)}";

    /// <summary>Picked by hand (blue), matched (green), matched but worth a look (amber) or new (grey).</summary>
    public string Status => IsChosen ? (Mapping.Target == null ? "Kept as new" : "Picked")
        : Mapping.Target == null ? "New exercise"
        : Mapping.Target != Mapping.Match.Exercise ? "Picked"
        : Mapping.Match.Confidence == MatchConfidence.Check ? "Check"
        : Mapping.Match.Source == MatchSource.Ai ? "AI match"
        : "Matched";

    public Color StatusColor => Status switch
    {
        "Matched" or "AI match" => Color.FromArgb("#2ED47A"),
        "Check" => Color.FromArgb("#FFB020"),
        "Picked" or "Kept as new" => Color.FromArgb("#3F7DFF"),
        _ => Color.FromArgb("#9AA3B5"),
    };

    public void SetTarget(Models.Exercise? exercise) => SetTarget(exercise, chosen: true);

    /// <summary>
    /// Back to the app's suggestion. A match goes back to how it started (matched, or worth a check); an exercise that
    /// had only a weak guess counts as picked, since it started as new.
    /// </summary>
    public void UseSuggestion() => SetTarget(Mapping.Match.Exercise, chosen: Mapping.Match.Confidence == MatchConfidence.None);

    void SetTarget(Models.Exercise? exercise, bool chosen)
    {
        Mapping.Target = exercise;
        IsChosen = chosen;
        OnPropertyChanged(nameof(Target));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusColor));
    }
}
