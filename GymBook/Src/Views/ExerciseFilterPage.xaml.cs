using GymBook.Models;
using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>
/// All the exercise filters as a sheet over the list (the library tab or the picker): muscles, kind of training,
/// equipment, level, movement type, a few switches and the order. Changes apply to the list straight away.
/// </summary>
public partial class ExerciseFilterPage : SheetPage
{
    readonly ExerciseListViewModel _list;
    readonly List<FilterOption> _all = [];

    public ExerciseFilterPage(ExerciseListViewModel list)
    {
        InitializeComponent();
        _list = list;
        var f = list.Filters;
        Groups =
        [
            Group("Muscle", Enum.GetValues<MuscleGroup>().Select(m => Toggle(m.Display(), f.Muscles, m))),
            Group("Kind of training", ExerciseFilter.Kinds.Select(k => new FilterOption(k.Title,
                () => k.Categories.All(f.Categories.Contains),
                () =>
                {
                    if (k.Categories.All(f.Categories.Contains))
                        f.Categories.ExceptWith(k.Categories);
                    else
                        f.Categories.UnionWith(k.Categories);
                }))),
            Group("Equipment", Enum.GetValues<Equipment>().Select(e => Toggle(e.Display(), f.Equipment, e))),
            Group("Level", Enum.GetValues<ExerciseLevel>().Select(l => Toggle(l.ToString(), f.Levels, l))),
            Group("Movement", [Toggle("Compound", f.Mechanics, Mechanic.Compound), Toggle("Isolation", f.Mechanics, Mechanic.Isolation)]),
            Group("Show only",
            [
                Switch("Fits my equipment", () => f.FitsMyEquipment, v => f.FitsMyEquipment = v),
                Switch("Done before", () => f.DoneBefore, v => f.DoneBefore = v),
                Switch("My exercises", () => f.CustomOnly, v => f.CustomOnly = v),
                Switch("Count secondary muscles", () => f.IncludeSecondary, v => f.IncludeSecondary = v),
            ]),
            Group("Order", ExerciseFilter.Sorts.Select(s => new FilterOption(s.Title, () => f.Sort == s.Sort, () => f.Sort = s.Sort))),
        ];
        BindingContext = this;
        list.PropertyChanged += OnListChanged;
    }

    public List<FilterGroup> Groups { get; }

    public string ShowText => $"Show {_list.CountText}";

    // Every chip and switch changes the filter, which refilters the list; all chips then show the new state.
    FilterGroup Group(string title, IEnumerable<FilterOption> options)
    {
        var list = options.ToList();
        _all.AddRange(list);
        return new FilterGroup(title, list);
    }

    FilterOption Toggle<T>(string title, HashSet<T> set, T value) => new(title, () => set.Contains(value), () =>
    {
        if (!set.Remove(value))
            set.Add(value);
    });

    static FilterOption Switch(string title, Func<bool> get, Action<bool> set) => new(title, get, () => set(!get()));

    void OnListChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExerciseListViewModel.CountText))
            OnPropertyChanged(nameof(ShowText));
    }

    // A chip was tapped: tell the list (the switches tell it themselves), then show every chip's state.
    void OnOptionTapped(object? sender, TappedEventArgs e)
    {
        if (((sender as BindableObject)?.BindingContext ?? (sender as Element)?.Parent?.BindingContext) is not FilterOption option)
            return;
        option.Flip();
        _list.Filters.Notify();
        foreach (var o in _all)
            o.Refresh();
    }

    void OnClear(object? sender, EventArgs e)
    {
        _list.Filters.Clear();
        foreach (var o in _all)
            o.Refresh();
    }

    async void OnDone(object? sender, EventArgs e) => await CloseAsync();

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _list.PropertyChanged -= OnListChanged;
    }

    /// <summary>Opens the filters for <paramref name="list"/> as a sheet over it.</summary>
    public static Task ShowAsync(ExerciseListViewModel list) =>
        Application.Current!.Windows[0].Page!.Navigation.PushModalAsync(new ExerciseFilterPage(list), false);
}
