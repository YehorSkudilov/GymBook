using CommunityToolkit.Mvvm.ComponentModel;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public enum ExerciseSort { BestMatch, Name, MostDone, RecentlyDone }

/// <summary>
/// What the exercise list is narrowed to: muscles, kinds of training, equipment, level, movement type, what fits the
/// user's equipment, what they've done before, and the order. They're chosen in the filter sheet; the chips above the
/// list show the ones that are on, each removable. Within a group options are "any of", across groups "all of".
/// </summary>
public partial class ExerciseFilter : ObservableObject
{
    /// <summary>The kinds of training that get their own chip; Power covers jumps, throws and Olympic lifts.</summary>
    public static readonly (string Title, ExerciseCategory[] Categories)[] Kinds =
    [
        ("Strength", [ExerciseCategory.Strength]),
        ("Pilates", [ExerciseCategory.Pilates]),
        ("Yoga", [ExerciseCategory.Yoga]),
        ("Stretching", [ExerciseCategory.Stretch]),
        ("Mobility", [ExerciseCategory.Mobility]),
        ("Cardio", [ExerciseCategory.Cardio]),
        ("Power", [ExerciseCategory.Plyometric, ExerciseCategory.Olympic]),
    ];

    /// <summary>The orders offered, with their names.</summary>
    public static readonly (string Title, ExerciseSort Sort)[] Sorts =
        [("Best match", ExerciseSort.BestMatch), ("A–Z", ExerciseSort.Name), ("Most done", ExerciseSort.MostDone), ("Recently done", ExerciseSort.RecentlyDone)];

    public HashSet<MuscleGroup> Muscles { get; } = [];
    public HashSet<ExerciseCategory> Categories { get; } = [];
    public HashSet<Equipment> Equipment { get; } = [];
    public HashSet<ExerciseLevel> Levels { get; } = [];
    public HashSet<Mechanic> Mechanics { get; } = [];

    /// <summary>A muscle filter also counts exercises that work it as a secondary muscle.</summary>
    [ObservableProperty] bool includeSecondary;
    [ObservableProperty] bool fitsMyEquipment;
    [ObservableProperty] bool doneBefore;
    [ObservableProperty] bool customOnly;
    [ObservableProperty] ExerciseSort sort = ExerciseSort.BestMatch;

    /// <summary>Anything changed; the list filters again.</summary>
    public event EventHandler? Changed;

    bool _quiet;

    public void Notify()
    {
        if (_quiet)
            return;
        OnPropertyChanged(nameof(ActiveCount));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIncludeSecondaryChanged(bool value) => Notify();
    partial void OnFitsMyEquipmentChanged(bool value) => Notify();
    partial void OnDoneBeforeChanged(bool value) => Notify();
    partial void OnCustomOnlyChanged(bool value) => Notify();
    partial void OnSortChanged(ExerciseSort value) => Notify();

    /// <summary>How many filters are on, for the badge on the filter button.</summary>
    public int ActiveCount => Active().Count();

    public bool IsEmpty => ActiveCount == 0;

    /// <summary>Every filter that's on, in the sheet's order, with how to turn it off: the chips above the list.</summary>
    public IEnumerable<(string Title, Action Remove)> Active()
    {
        foreach (var m in Enum.GetValues<MuscleGroup>().Where(Muscles.Contains))
            yield return (m.Display(), () => Muscles.Remove(m));
        foreach (var kind in Kinds.Where(k => k.Categories.All(Categories.Contains)))
            yield return (kind.Title, () => Categories.ExceptWith(kind.Categories));
        foreach (var e in Enum.GetValues<Models.Equipment>().Where(Equipment.Contains))
            yield return (e.Display(), () => Equipment.Remove(e));
        foreach (var l in Enum.GetValues<ExerciseLevel>().Where(Levels.Contains))
            yield return (l.ToString(), () => Levels.Remove(l));
        foreach (var m in Enum.GetValues<Mechanic>().Where(Mechanics.Contains))
            yield return (m.ToString(), () => Mechanics.Remove(m));
        if (FitsMyEquipment)
            yield return ("Fits my equipment", () => FitsMyEquipment = false);
        if (DoneBefore)
            yield return ("Done before", () => DoneBefore = false);
        if (CustomOnly)
            yield return ("My exercises", () => CustomOnly = false);
        if (IncludeSecondary)
            yield return ("Secondary muscles", () => IncludeSecondary = false);
        if (Sort != ExerciseSort.BestMatch)
            yield return ($"Order: {Sorts.First(s => s.Sort == Sort).Title}", () => Sort = ExerciseSort.BestMatch);
    }

    public void Clear()
    {
        Muscles.Clear();
        Categories.Clear();
        Equipment.Clear();
        Levels.Clear();
        Mechanics.Clear();
        // One refilter for the lot, not one per switch.
        _quiet = true;
        (IncludeSecondary, FitsMyEquipment, DoneBefore, CustomOnly, Sort) = (false, false, false, false, ExerciseSort.BestMatch);
        _quiet = false;
        Notify();
    }

    /// <summary>The exercises this filter lets through, in its order (a search orders by match instead, see <see cref="ExerciseListViewModel"/>).</summary>
    public IEnumerable<Exercise> Apply(IEnumerable<Exercise> exercises, DataStore store, bool searching)
    {
        // How often and how lately each exercise was done, for "done before" and the history orders.
        var done = new Dictionary<string, (int Count, DateTimeOffset Last)>();
        if (DoneBefore || Sort is ExerciseSort.MostDone or ExerciseSort.RecentlyDone)
            foreach (var session in store.History)
                foreach (var e in session.Exercises)
                {
                    var (count, last) = done.GetValueOrDefault(e.ExerciseId);
                    done[e.ExerciseId] = (count + 1, session.StartedAt > last ? session.StartedAt : last);
                }
        var access = store.Profile.EquipmentAccess;

        var result = exercises.Where(e =>
        {
            var details = ExerciseLibrary.Details(e.Id);
            return (Muscles.Count == 0 || Muscles.Contains(e.PrimaryMuscle) || IncludeSecondary && e.SecondaryMuscles.Any(Muscles.Contains))
                && (Categories.Count == 0 || details != null && Categories.Contains(details.Category))
                && (Equipment.Count == 0 || Equipment.Contains(e.Equipment))
                && (Levels.Count == 0 || details != null && Levels.Contains(details.Level))
                && (Mechanics.Count == 0 || Mechanics.Contains(e.Mechanic))
                && (!FitsMyEquipment || access.Allows(e.Equipment))
                && (!DoneBefore || done.ContainsKey(e.Id))
                && (!CustomOnly || e.IsCustom);
        });
        return Sort switch
        {
            ExerciseSort.Name => result.OrderBy(e => e.Name),
            ExerciseSort.MostDone => result.OrderByDescending(e => done.GetValueOrDefault(e.Id).Count).ThenBy(e => e.Name),
            ExerciseSort.RecentlyDone => result.OrderByDescending(e => done.GetValueOrDefault(e.Id).Last).ThenBy(e => e.Name),
            // Best match: the search's order when searching, otherwise alphabetical.
            _ => searching ? result : result.OrderBy(e => e.Name),
        };
    }
}

/// <summary>One option in the filter sheet: a chip that toggles a value in one of the filter's groups.</summary>
public partial class FilterOption(string title, Func<bool> isOn, Action toggle) : ObservableObject
{
    public string Title { get; } = title;
    public bool IsOn => isOn();
    public Color Background => IsOn ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#1D212C");
    public Color TextColor => IsOn ? Colors.White : Color.FromArgb("#9AA3B5");

    /// <summary>Turns it on or off; the filter sheet then refilters and refreshes the chips.</summary>
    public void Flip() => toggle();

    public void Refresh()
    {
        OnPropertyChanged(nameof(IsOn));
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(TextColor));
    }
}

/// <summary>A titled group of options in the filter sheet.</summary>
public record FilterGroup(string Title, List<FilterOption> Options);
