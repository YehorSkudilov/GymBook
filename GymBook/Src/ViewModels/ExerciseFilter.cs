using CommunityToolkit.Mvvm.ComponentModel;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public enum ExerciseSort { BestMatch, Name, MostDone, RecentlyDone }

/// <summary>
/// One level of the filters (type of training, muscle, movement...): the value each exercise has for it, and which
/// values are picked. Within a level, picks are "any of"; levels narrow one after another.
/// </summary>
public sealed class FilterLevel(string title, Func<Exercise, string?> key, Func<string, string> label, IReadOnlyList<string> order)
{
    public string Title { get; } = title;

    /// <summary>The exercise's value for this level; null when it has none (a custom exercise has no level).</summary>
    public string? KeyOf(Exercise e) => key(e);

    public string Label(string value) => label(value);

    /// <summary>Every value, in the order they're listed.</summary>
    public IReadOnlyList<string> Order { get; } = order;

    public HashSet<string> Selected { get; } = [];

    /// <summary>
    /// How many exercises have each value among those the levels above let through (and the search, and the
    /// switches): the options worth offering, so each level gets smaller as the ones above narrow.
    /// </summary>
    public Dictionary<string, int> Available { get; } = [];
}

/// <summary>
/// What the exercise list is narrowed to, as a pyramid of levels: type of training, then muscle, movement pattern,
/// compound or isolation, equipment and level. Each level only offers what's left after the levels above it, with
/// counts. Plus a few switches (fits my equipment, done before, my exercises) and the order. The muscle chips above the
/// list and the filter sheet both change this one set.
/// </summary>
public partial class ExerciseFilter : ObservableObject
{
    /// <summary>The types of training; Power covers jumps, throws and Olympic lifts.</summary>
    public static readonly (string Title, ExerciseCategory[] Categories)[] Kinds =
    [
        ("Strength", [ExerciseCategory.Strength]),
        ("Power", [ExerciseCategory.Plyometric, ExerciseCategory.Olympic]),
        ("Cardio", [ExerciseCategory.Cardio]),
        ("Pilates", [ExerciseCategory.Pilates]),
        ("Yoga", [ExerciseCategory.Yoga]),
        ("Stretching", [ExerciseCategory.Stretch]),
        ("Mobility", [ExerciseCategory.Mobility]),
    ];

    static string Kind(Exercise e)
    {
        var category = ExerciseLibrary.Details(e.Id)?.Category ?? ExerciseCategory.Strength;
        return Kinds.First(k => k.Categories.Contains(category)).Title;
    }

    public FilterLevel Training { get; } = new("Type of training", Kind, k => k, [.. Kinds.Select(k => k.Title)]);

    public FilterLevel Muscle { get; } = new("Muscle group", e => e.PrimaryMuscle.ToString(),
        m => Enum.Parse<MuscleGroup>(m).Display(), [.. Enum.GetValues<MuscleGroup>().Select(m => m.ToString())]);

    public FilterLevel Movement { get; } = new("Movement", e => ExercisePatterns.Of(e).Id,
        id => ExercisePatterns.All.First(p => p.Id == id).Title, [.. ExercisePatterns.All.Select(p => p.Id)]);

    public FilterLevel Mechanics { get; } = new("Compound or isolation", e => e.Mechanic.ToString(),
        m => m == nameof(Mechanic.Compound) ? "Compound (multi-joint)" : "Isolation (single-joint)", [nameof(Mechanic.Compound), nameof(Mechanic.Isolation)]);

    public FilterLevel Equipment { get; } = new("Equipment", e => e.Equipment.ToString(),
        q => Enum.Parse<Models.Equipment>(q).Display(), [.. Enum.GetValues<Models.Equipment>().Select(q => q.ToString())]);

    public FilterLevel Level { get; } = new("Level", e => ExerciseLibrary.Details(e.Id)?.Level.ToString(),
        l => l, [.. Enum.GetValues<ExerciseLevel>().Select(l => l.ToString())]);

    /// <summary>The levels, top of the pyramid first.</summary>
    public IReadOnlyList<FilterLevel> Levels => [Training, Muscle, Movement, Mechanics, Equipment, Level];

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

    partial void OnFitsMyEquipmentChanged(bool value) => Notify();
    partial void OnDoneBeforeChanged(bool value) => Notify();
    partial void OnCustomOnlyChanged(bool value) => Notify();
    partial void OnSortChanged(ExerciseSort value) => Notify();

    /// <summary>How many filters are on beyond the muscle chips, for the badge on the filter button.</summary>
    public int ActiveCount => Levels.Where(l => l != Muscle).Sum(l => l.Selected.Count)
        + (FitsMyEquipment ? 1 : 0) + (DoneBefore ? 1 : 0) + (CustomOnly ? 1 : 0) + (Sort != ExerciseSort.BestMatch ? 1 : 0);

    public bool IsEmpty => Muscle.Selected.Count == 0 && ActiveCount == 0;

    public void Clear()
    {
        foreach (var level in Levels)
            level.Selected.Clear();
        // One refilter for the lot, not one per switch.
        _quiet = true;
        (FitsMyEquipment, DoneBefore, CustomOnly, Sort) = (false, false, false, ExerciseSort.BestMatch);
        _quiet = false;
        Notify();
    }

    /// <summary>
    /// The exercises this filter lets through, in its order (a search orders by match instead, see
    /// <see cref="ExerciseListViewModel"/>), and each level's <see cref="FilterLevel.Available"/> options on the way.
    /// </summary>
    public List<Exercise> Apply(IEnumerable<Exercise> exercises, DataStore store, bool searching)
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

        var result = exercises.Where(e => (!FitsMyEquipment || access.Allows(e.Equipment))
            && (!DoneBefore || done.ContainsKey(e.Id))
            && (!CustomOnly || e.IsCustom)).ToList();
        // Down the pyramid: each level counts what's left, then narrows it to its picks.
        foreach (var level in Levels)
        {
            level.Available.Clear();
            foreach (var e in result)
                if (level.KeyOf(e) is { } key)
                    level.Available[key] = level.Available.GetValueOrDefault(key) + 1;
            if (level.Selected.Count > 0)
                result = result.Where(e => level.KeyOf(e) is { } key && level.Selected.Contains(key)).ToList();
        }
        return Sort switch
        {
            ExerciseSort.Name => [.. result.OrderBy(e => e.Name)],
            ExerciseSort.MostDone => [.. result.OrderByDescending(e => done.GetValueOrDefault(e.Id).Count).ThenBy(e => e.Name)],
            ExerciseSort.RecentlyDone => [.. result.OrderByDescending(e => done.GetValueOrDefault(e.Id).Last).ThenBy(e => e.Name)],
            // Best match: the search's order when searching, otherwise alphabetical.
            _ => searching ? result : [.. result.OrderBy(e => e.Name)],
        };
    }
}

/// <summary>One option in the filter sheet: a chip that toggles a value in one of the filter's groups.</summary>
/// <param name="count">How many exercises it would show, for the pyramid levels; null for switches and orders.</param>
public partial class FilterOption(string title, Func<bool> isOn, Action toggle, int? count = null) : ObservableObject
{
    public string Title { get; } = title;
    public string Count { get; } = count?.ToString() ?? "";
    public bool HasCount => count != null;
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
