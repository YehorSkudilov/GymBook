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

    public bool CanImport => IsReady && !IsImporting;

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
        Load(picked.FileName, text);
    }

    void Load(string name, string text)
    {
        Clear();
        try
        {
            if (Kind == CsvImportKind.Plans)
            {
                _plans = CsvExportFormat.ParsePlans(text);
                var days = _plans.Sum(p => p.Days.Count);
                Summary = $"{Count(_plans.Count, "plan")}: {string.Join(", ", _plans.Select(p => p.Name))} · {Count(days, "day")}";
                _mappings = _importer.Mappings(_plans.SelectMany(p => p.Days).SelectMany(d => d.Exercises).Select(e => e.Exercise));
            }
            else
            {
                _workouts = CsvExportFormat.ParseWorkouts(text);
                var first = _workouts.Min(w => w.StartedAt);
                var last = _workouts.Max(w => w.StartedAt);
                var sets = _workouts.Sum(w => w.Exercises.Sum(e => e.Sets.Count));
                Summary = $"{Count(_workouts.Count, "workout")} from {first:d MMM yyyy} to {last:d MMM yyyy} · {Count(sets, "set")}";
                _mappings = _importer.Mappings(_workouts.SelectMany(w => w.Exercises).Select(e => e.Exercise));
            }
        }
        catch (FormatException e)
        {
            Error = e.Message;
            return;
        }
        FileName = name;
        foreach (var mapping in _mappings)
            Mappings.Add(new MappingItem(mapping, Change));
        UpdateMappingSummary();
        IsReady = true;
    }

    void Clear()
    {
        _plans = null;
        _workouts = null;
        _mappings = [];
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
        var matched = _mappings.Count(m => m.Target != null);
        var check = Mappings.Count(m => m.Status == "Check");
        var chosen = Mappings.Count(m => m.IsChosen);
        var created = _mappings.Count - matched;
        MappingSummary = $"{Count(_mappings.Count, "exercise")}: {matched} matched to the app's" +
            (check > 0 ? $" ({check} worth a check)" : "") + (created > 0 ? $", {created} added as new" : "") +
            (chosen > 0 ? $", {chosen} chosen by you" : "") + ". Tap one to change it.";
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
                added = _importer.ImportWorkouts(workouts, _mappings);
                what = Count(added, "workout");
            }
            else
                return;
            await _dialogs.Alert(added == 0 ? "Nothing new" : "Imported",
                added == 0 ? "Everything in this file is already in the app." : $"{what} added. They sync to your account like anything else.");
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
    public string Uses => $"{Mapping.Uses}×";

    public string Target => Mapping.Target?.Name ?? $"New: {CsvImporter.Name(Mapping.Source)}";

    /// <summary>Picked by hand (blue), matched (green), matched but worth a look (amber) or new (grey).</summary>
    public string Status => IsChosen ? (Mapping.Target == null ? "Kept as new" : "Picked")
        : Mapping.Target == null ? "New exercise"
        : Mapping.Target != Mapping.Match.Exercise ? "Picked"
        : Mapping.Match.Confidence == MatchConfidence.Check ? "Check"
        : "Matched";

    public Color StatusColor => Status switch
    {
        "Matched" => Color.FromArgb("#2ED47A"),
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
