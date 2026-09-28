using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>One muscle's line in the breakdown: its sets and a bar relative to the busiest muscle in the list.</summary>
public class MuscleRow
{
    public required string Name { get; init; }
    public required string Sets { get; init; }
    public required double Fraction { get; init; }
    public required Color Color { get; init; }
    public required string Exercises { get; init; }
}

/// <summary>
/// Which muscles a plan day (or the whole plan, one pass through its workouts) trains and with how many sets.
/// Direct sets count toward an exercise's primary muscle; auxiliary sets toward its secondary muscles.
/// The map uses the app's usual weighting: a direct set counts 1, an auxiliary set ½.
/// </summary>
public partial class MuscleBreakdownViewModel : ObservableObject
{
    readonly DataStore _store;
    readonly WorkoutPlan _plan;
    readonly PlanWorkout? _day;

    [ObservableProperty] bool isDayMode;
    [ObservableProperty] bool canShowDay;
    [ObservableProperty] string title = "";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] IDrawable map = MuscleMapDrawable.Empty;
    [ObservableProperty] string totalExercises = "0";
    [ObservableProperty] string totalSets = "0";
    [ObservableProperty] string totalMuscles = "0";
    [ObservableProperty] List<MuscleRow> primary = [];
    [ObservableProperty] List<MuscleRow> auxiliary = [];
    [ObservableProperty] bool hasAuxiliary;

    public MuscleBreakdownViewModel(DataStore store, WorkoutPlan plan, PlanWorkout? day)
    {
        _store = store;
        _plan = plan;
        _day = day;
        CanShowDay = day != null;
        IsDayMode = day != null;
        Refresh();
    }

    public Color DayTabBackground => IsDayMode ? Color.FromArgb("#3F7DFF") : Colors.Transparent;
    public Color PlanTabBackground => IsDayMode ? Colors.Transparent : Color.FromArgb("#3F7DFF");
    public Color DayTabText => IsDayMode ? Colors.White : Color.FromArgb("#9AA3B5");
    public Color PlanTabText => IsDayMode ? Color.FromArgb("#9AA3B5") : Colors.White;

    partial void OnIsDayModeChanged(bool value)
    {
        OnPropertyChanged(nameof(DayTabBackground));
        OnPropertyChanged(nameof(PlanTabBackground));
        OnPropertyChanged(nameof(DayTabText));
        OnPropertyChanged(nameof(PlanTabText));
    }

    [RelayCommand]
    void ShowDay()
    {
        if (!CanShowDay)
            return;
        IsDayMode = true;
        Refresh();
    }

    [RelayCommand]
    void ShowPlan()
    {
        IsDayMode = false;
        Refresh();
    }

    void Refresh()
    {
        var workouts = IsDayMode && _day != null ? [_day] : _plan.Workouts;
        Title = IsDayMode && _day != null ? _day.Name : _plan.Name;
        Subtitle = IsDayMode ? "Muscles trained this day" : $"All {workouts.Count} workouts combined · one week";

        var entries = workouts.SelectMany(w => w.Exercises)
            .Select(pe => (pe, ex: _store.GetExercise(pe.ExerciseId)))
            .Where(x => x.ex != null)
            .Select(x => (x.pe.Sets, Exercise: x.ex!))
            .ToList();

        var direct = new Dictionary<MuscleGroup, (double Sets, HashSet<string> Names)>();
        var indirect = new Dictionary<MuscleGroup, (double Sets, HashSet<string> Names)>();
        foreach (var (sets, ex) in entries)
        {
            Add(direct, ex.PrimaryMuscle, sets, ex.Name);
            foreach (var m in ex.SecondaryMuscles.Where(m => m != ex.PrimaryMuscle))
                Add(indirect, m, sets, ex.Name);
        }

        var load = Enum.GetValues<MuscleGroup>().ToDictionary(
            m => m, m => direct.GetValueOrDefault(m).Sets + indirect.GetValueOrDefault(m).Sets * 0.5);
        Map = MuscleMapDrawable.ForLoad(load);

        TotalExercises = entries.Select(e => e.Exercise.Id).Distinct().Count().ToString();
        TotalSets = entries.Sum(e => e.Sets).ToString();
        TotalMuscles = load.Count(kv => kv.Value > 0).ToString();
        Primary = Rows(direct);
        Auxiliary = Rows(indirect);
        HasAuxiliary = Auxiliary.Count > 0;
    }

    static void Add(Dictionary<MuscleGroup, (double Sets, HashSet<string> Names)> into, MuscleGroup m, int sets, string name)
    {
        var current = into.GetValueOrDefault(m, (0, []));
        current.Names.Add(name);
        into[m] = (current.Sets + sets, current.Names);
    }

    static List<MuscleRow> Rows(Dictionary<MuscleGroup, (double Sets, HashSet<string> Names)> sets)
    {
        var max = sets.Values.Select(v => v.Sets).DefaultIfEmpty(0).Max();
        return sets.OrderByDescending(kv => kv.Value.Sets).Select(kv => new MuscleRow
        {
            Name = kv.Key.Display(),
            Sets = kv.Value.Sets == 1 ? "1 set" : $"{kv.Value.Sets:0} sets",
            Fraction = max > 0 ? kv.Value.Sets / max : 0,
            Color = kv.Key.Color(),
            Exercises = string.Join(", ", kv.Value.Names),
        }).ToList();
    }

    [RelayCommand]
    // Slides the sheet down before it closes, like the other sheets.
    static Task Close() => Shell.Current.Navigation.ModalStack.LastOrDefault() is Views.SheetPage sheet
        ? sheet.CloseAsync()
        : Shell.Current.Navigation.PopModalAsync();
}
