using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.Wear;

/// <summary>A row in the picker: an exercise to add, or a muscle group to open.</summary>
public record PickerItem(string Name, string Detail, ICommand Command);

/// <summary>
/// Picking an exercise on a watch: the catalogue has hundreds, so it opens on the ones used lately and the muscle
/// groups, with search (the watch keyboard or voice) for anything else.
/// </summary>
public partial class ExercisePickerViewModel : ObservableObject
{
    const int RecentCount = 8, SearchLimit = 30;

    readonly DataStore _store;
    MuscleGroup? _group;

    public ExercisePickerViewModel(DataStore store)
    {
        _store = store;
        Groups = Enum.GetValues<MuscleGroup>()
            .Where(g => _store.AllExercises.Any(e => e.PrimaryMuscle == g))
            .Select(g => new PickerItem(g.Display(), "", new RelayCommand(() => OpenGroup(g))))
            .ToList();
        Refresh();
    }

    /// <summary>The exercise picked; the page closes.</summary>
    public event Action<Exercise>? Picked;

    public IReadOnlyList<PickerItem> Groups { get; }

    [ObservableProperty] string query = "";
    [ObservableProperty] string title = "Add exercise";
    [ObservableProperty] IReadOnlyList<PickerItem> exercises = [];
    [ObservableProperty] bool showGroups = true;
    [ObservableProperty] bool inGroup;
    [ObservableProperty] string listTitle = "";

    partial void OnQueryChanged(string value) => Refresh();

    void OpenGroup(MuscleGroup group)
    {
        _group = group;
        Refresh();
    }

    [RelayCommand]
    void BackToGroups()
    {
        _group = null;
        Refresh();
    }

    void Refresh()
    {
        var all = _store.AllExercises;
        var query = Query.Trim();
        if (query.Length > 0)
        {
            Exercises = all.Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => !e.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(e => e.Name)
                .Take(SearchLimit)
                .Select(Item)
                .ToList();
            ListTitle = Exercises.Count == 0 ? "No matches" : "";
            ShowGroups = InGroup = false;
            return;
        }
        if (_group is { } group)
        {
            Exercises = all.Where(e => e.PrimaryMuscle == group).OrderBy(e => e.Name).Select(Item).ToList();
            ListTitle = group.Display();
            ShowGroups = false;
            InGroup = true;
            return;
        }
        // Lately used first: what's most likely to be added again.
        var recent = _store.History
            .SelectMany(s => s.Exercises.Select(e => e.ExerciseId))
            .Distinct()
            .Select(_store.GetExercise)
            .OfType<Exercise>()
            .Take(RecentCount)
            .Select(Item)
            .ToList();
        Exercises = recent;
        ListTitle = recent.Count > 0 ? "Recent" : "";
        ShowGroups = true;
        InGroup = false;
    }

    PickerItem Item(Exercise exercise) =>
        new(exercise.Name, $"{exercise.PrimaryMuscle.Display()} · {exercise.Equipment.Display()}", new RelayCommand(() => Picked?.Invoke(exercise)));
}
