using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>One part of a muscle under its muscle's line (upper, mid and lower chest, …): its sets and the exercises giving them.</summary>
public class MusclePartRow
{
    public required string Name { get; init; }
    public required string Sets { get; init; }
    public required string Exercises { get; init; }
    public bool HasExercises => Exercises.Length > 0;
    /// <summary>A part with no sets, shown faded so a gap stands out without shouting.</summary>
    public required double Opacity { get; init; }
}

/// <summary>
/// One whole muscle in the breakdown: its sets (an exercise's main muscle counts 1 a set, a muscle it helps ½), and on
/// the whole plan its weekly target and what to change to reach it.
/// </summary>
public class MuscleRow
{
    public required string Name { get; init; }
    public required string Sets { get; init; }
    public required double Fraction { get; init; }
    public required Color Color { get; init; }
    public required Color BarColor { get; init; }
    public string Advice { get; init; } = "";
    public bool HasAdvice => Advice.Length > 0;
    public Color AdviceColor { get; init; } = Color.FromArgb("#9AA3B5");
    public List<MusclePartRow> Parts { get; init; } = [];
    public bool HasParts => Parts.Count > 0;
    /// <summary>For a muscle in one piece, the exercises training it (a split one lists them by part).</summary>
    public string Exercises { get; init; } = "";
    public bool HasExercises => Exercises.Length > 0;
}

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
    [ObservableProperty] string musclesTitle = "";
    [ObservableProperty] string musclesCaption = "";
    [ObservableProperty] List<MuscleRow> muscles = [];

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

    static readonly Color Under = Color.FromArgb("#FFB020"), OnTarget = Color.FromArgb("#2ED47A"), Over = Color.FromArgb("#FF4D5E");

    // Parts most people needn't train on their own (any press reaches the mid chest, squats and lunges the glute medius),
    // so a gap in them isn't worth pointing out.
    static readonly HashSet<SubMuscle> NoGapHint =
        [SubMuscle.MidChest, SubMuscle.GluteMedius, SubMuscle.Brachialis, SubMuscle.Tibialis, SubMuscle.ForearmExtensors, SubMuscle.ForearmFlexors];

    void Refresh()
    {
        var workouts = IsDayMode && _day != null ? [_day] : _plan.Workouts;
        Title = IsDayMode && _day != null ? _day.Name : _plan.Name;
        // The plan's sets are per pass through its days, workouts and rest days; targets are per week, so a cycle longer
        // or shorter than a week is scaled to one.
        var cycleDays = PlanSchedule.Days(_plan).Count;
        var perWeek = IsDayMode || cycleDays is 0 or 7 ? 1.0 : 7.0 / cycleDays;
        Subtitle = IsDayMode ? "Muscles trained this day"
            : perWeek == 1 ? $"All {workouts.Count} workouts combined · one week"
            : $"All {workouts.Count} workouts over the {cycleDays}-day cycle, per week";

        var entries = workouts.SelectMany(w => w.Exercises)
            .Select(pe => (pe, ex: _store.GetExercise(pe.ExerciseId)))
            .Where(x => x.ex != null)
            .Select(x => (x.pe.Sets, Exercise: x.ex!))
            .ToList();

        // Each part's sets (main muscle 1, helped ½) and the exercises behind them, main ones first.
        var parts = new Dictionary<SubMuscle, (double Sets, List<string> Main, List<string> Helping)>();
        foreach (var (sets, ex) in entries)
        {
            Add(parts, SubMuscles.For(ex, ex.PrimaryMuscle), sets, ex.Name, true);
            foreach (var m in ex.SecondaryMuscles.Where(m => m != ex.PrimaryMuscle))
                Add(parts, SubMuscles.For(ex, m), sets * 0.5, ex.Name, false);
        }

        var load = Enum.GetValues<SubMuscle>().ToDictionary(m => m, m => parts.GetValueOrDefault(m).Sets);
        Map = MuscleMapDrawable.ForLoad(load);

        TotalExercises = entries.Select(e => e.Exercise.Id).Distinct().Count().ToString();
        TotalSets = entries.Sum(e => e.Sets).ToString();
        TotalMuscles = parts.Keys.Select(p => p.Group()).Distinct().Count().ToString();

        var goal = _plan.Goal;
        var experience = _store.Profile.Experience;
        var groups = Enum.GetValues<MuscleGroup>()
            .Select(g => (Group: g, Sets: parts.Where(p => p.Key.Group() == g).Sum(p => p.Value.Sets) * perWeek,
                Target: IsDayMode ? null : VolumeTargets.For(g, goal, experience, _plan.Priorities)))
            // The whole plan lists every muscle with a target, trained or not: a muscle left out is what most needs saying.
            .Where(x => x.Sets > 0 || x.Target != null)
            .OrderByDescending(x => x.Sets)
            .ToList();
        var busiest = groups.Select(x => x.Sets).DefaultIfEmpty(0).Max();

        Muscles = [.. groups.Select(x => Row(x.Group, x.Sets, x.Target, busiest, parts, perWeek))];
        if (IsDayMode)
        {
            MusclesTitle = "Muscles";
            MusclesCaption = "Sets a muscle gets from the exercises it helps count half.";
        }
        else
        {
            var targeted = groups.Where(x => x.Target != null).Select(x => VolumeTargets.Of(x.Sets, x.Target!.Value)).ToList();
            var under = targeted.Count(s => s == VolumeTargets.Status.Under);
            var over = targeted.Count(s => s == VolumeTargets.Status.Over);
            MusclesTitle = "Weekly sets per muscle";
            MusclesCaption = $"{targeted.Count - under - over} of {targeted.Count} muscles on target"
                + (under > 0 ? $", {under} under" : "") + (over > 0 ? $", {over} over" : "")
                + $". Targets for {Goals.Display(Goals.Of(goal, _plan.Priorities)).ToLowerInvariant()}, {experience.Display().ToLowerInvariant()}; "
                + "sets a muscle gets from the exercises it helps count half.";
        }
    }

    MuscleRow Row(MuscleGroup group, double sets, VolumeTargets.Range? target,
        double busiest, Dictionary<SubMuscle, (double Sets, List<string> Main, List<string> Helping)> parts, double perWeek)
    {
        var all = Enum.GetValues<SubMuscle>().Where(p => p.Group() == group).ToList();
        // A muscle in one piece has nothing more to show under it; a split one lists its parts, on the whole plan even
        // the untrained ones, faded (unless the whole muscle is untrained: its line says so).
        List<SubMuscle> shown = all.Count < 2 || sets <= 0 ? [] : all.Where(p => !IsDayMode || parts.ContainsKey(p))
            .OrderByDescending(p => parts.GetValueOrDefault(p).Sets).ToList();
        var partRows = shown.Select(p =>
        {
            var (partSets, main, helping) = parts.GetValueOrDefault(p, (0, [], []));
            return new MusclePartRow
            {
                Name = p.Display(),
                Sets = SetsText(partSets * perWeek),
                Exercises = string.Join(", ", main.Concat(helping.Except(main))),
                Opacity = partSets > 0 ? 1 : 0.5,
            };
        }).ToList();
        var exercises = all.Count == 1 && parts.TryGetValue(all[0], out var only)
            ? string.Join(", ", only.Main.Concat(only.Helping.Except(only.Main)))
            : "";

        if (target is not { } range)
            return new MuscleRow
            {
                Name = group.Display(),
                Sets = SetsText(sets),
                Fraction = busiest > 0 ? sets / busiest : 0,
                Color = group.Color(),
                BarColor = group.Color(),
                Advice = IsDayMode ? "" : group == MuscleGroup.Neck ? "No weekly target" : "No weekly target: the big lifts train it",
                Parts = partRows,
                Exercises = exercises,
            };

        var status = VolumeTargets.Of(sets, range);
        List<SubMuscle> gaps = sets > 0 ? all.Where(p => all.Count > 1 && !NoGapHint.Contains(p) && !parts.ContainsKey(p)).ToList() : [];
        var advice = status switch
        {
            _ when sets <= 0 => $"Not trained. Add {range.Min} sets a week",
            VolumeTargets.Status.Under => $"Add about {Math.Ceiling(range.Min - sets):0} sets a week",
            VolumeTargets.Status.Over => $"{Math.Ceiling(sets - range.Max):0} sets above the range: more than most recover from",
            _ => "On target",
        };
        if (gaps.Count > 0)
            advice += $" · no {string.Join(" or ", gaps.Select(p => p.Display().ToLowerInvariant()))} work";
        var color = status switch
        {
            VolumeTargets.Status.Under => Under,
            VolumeTargets.Status.Over => Over,
            _ => OnTarget,
        };
        return new MuscleRow
        {
            Name = group.Display(),
            Sets = $"{SetsText(sets)} · target {range}",
            Fraction = Math.Clamp(sets / range.Max, 0, 1),
            Color = group.Color(),
            BarColor = color,
            Advice = advice,
            AdviceColor = color,
            Parts = partRows,
            Exercises = exercises,
        };
    }

    static string SetsText(double sets) => Math.Abs(sets - 1) < 0.05 ? "1 set" : $"{sets:0.#} sets";

    static void Add(Dictionary<SubMuscle, (double Sets, List<string> Main, List<string> Helping)> into, SubMuscle m, double sets, string name, bool main)
    {
        var current = into.GetValueOrDefault(m, (0, [], []));
        var names = main ? current.Main : current.Helping;
        if (!names.Contains(name))
            names.Add(name);
        into[m] = (current.Sets + sets, current.Main, current.Helping);
    }

    [RelayCommand]
    // Slides the sheet down before it closes, like the other sheets.
    static Task Close() => Shell.Current.Navigation.ModalStack.LastOrDefault() is Views.SheetPage sheet
        ? sheet.CloseAsync()
        : Shell.Current.Navigation.PopModalAsync();
}
