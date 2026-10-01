using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>
/// All the exercise filters as a sheet over the list (the library tab or the picker): a pyramid of levels (type of
/// training → muscle → movement → compound or isolation → equipment → level), each offering only what's left after
/// the levels above it, with counts; then a few switches and the order. Changes apply to the list straight away.
/// </summary>
public partial class ExerciseFilterPage : SheetPage
{
    readonly ExerciseListViewModel _list;

    public ExerciseFilterPage(ExerciseListViewModel list)
    {
        InitializeComponent();
        _list = list;
        Groups = Build();
        BindingContext = this;
        list.PropertyChanged += OnListChanged;
    }

    public List<FilterGroup> Groups { get; private set; }

    // The groups as the filter stands: every level's options with what they'd show (the list has just counted them),
    // without the ones that would show nothing unless they're picked; levels with nothing to choose between are left out.
    List<FilterGroup> Build()
    {
        var f = _list.Filters;
        var groups = new List<FilterGroup>();
        foreach (var level in f.Levels)
        {
            var options = level.Order
                .Where(v => level.Available.ContainsKey(v) || level.Selected.Contains(v))
                .Select(v => new FilterOption(level.Label(v), () => level.Selected.Contains(v), () =>
                {
                    if (!level.Selected.Remove(v))
                        level.Selected.Add(v);
                }, level.Available.GetValueOrDefault(v)))
                .ToList();
            if (options.Count > 1 || level.Selected.Count > 0)
                groups.Add(new FilterGroup(level.Title, options));
        }
        groups.Add(new FilterGroup("Show only",
        [
            Switch("Fits my equipment", () => f.FitsMyEquipment, v => f.FitsMyEquipment = v),
            Switch("Done before", () => f.DoneBefore, v => f.DoneBefore = v),
            Switch("My exercises", () => f.CustomOnly, v => f.CustomOnly = v),
        ]));
        groups.Add(new FilterGroup("Order", new (string, ExerciseSort)[] { ("Best match", ExerciseSort.BestMatch), ("A–Z", ExerciseSort.Name),
                ("Most done", ExerciseSort.MostDone), ("Recently done", ExerciseSort.RecentlyDone) }
            .Select(o => new FilterOption(o.Item1, () => f.Sort == o.Item2, () => f.Sort = o.Item2)).ToList()));
        return groups;
    }

    void Rebuild()
    {
        Groups = Build();
        OnPropertyChanged(nameof(Groups));
    }

    public string ShowText => $"Show {_list.CountText}";

    static FilterOption Switch(string title, Func<bool> get, Action<bool> set) => new(title, get, () => set(!get()));

    // The list filtered again (a chip here, or the search behind): new counts, and the levels below may have shrunk.
    void OnListChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExerciseListViewModel.Items))
            return;
        OnPropertyChanged(nameof(ShowText));
        Rebuild();
    }

    // A chip was tapped: tell the list (the switches tell it themselves), which filters again and rebuilds the sheet.
    void OnOptionTapped(object? sender, TappedEventArgs e)
    {
        if (((sender as BindableObject)?.BindingContext ?? (sender as Element)?.Parent?.BindingContext) is not FilterOption option)
            return;
        option.Flip();
        _list.Filters.Notify();
    }

    void OnClear(object? sender, EventArgs e) => _list.Filters.Clear();

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
