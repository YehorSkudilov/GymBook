using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GymBook.ViewModels;

public enum FeedbackSort { Votes, Created, Updated, Comments }

/// <summary>How the feedback list is narrowed and ordered.</summary>
public record FeedbackFilter(IReadOnlySet<string> Statuses, bool Mine, FeedbackSort Sort, bool Descending)
{
    /// <summary>Feature requests: the most voted first; bug reports: the newest.</summary>
    public static FeedbackFilter Default(bool features) =>
        new(new HashSet<string>(), false, features ? FeedbackSort.Votes : FeedbackSort.Created, true);

    public bool IsDefault(bool features) =>
        Statuses.Count == 0 && !Mine && Sort == Default(features).Sort && Descending;
}

public partial class StatusOption(string name, bool isChecked, Action changed) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty] bool isChecked = isChecked;

    partial void OnIsCheckedChanged(bool value) => changed();

    [RelayCommand] void Toggle() => IsChecked = !IsChecked;
}

/// <summary>
/// The feedback list's filter sheet (FeedbackFilterPage): which statuses (any of several, from a dropdown of the board's
/// columns), only the user's own (feature requests), and the order, either way round. Each change applies at once.
/// </summary>
public partial class FeedbackFilterViewModel : ObservableObject
{
    readonly bool _features;
    readonly Action<FeedbackFilter> _apply;
    bool _loading;

    public FeedbackFilterViewModel(IReadOnlyList<string> statuses, FeedbackFilter current, bool features, Action<FeedbackFilter> apply)
    {
        (_features, _apply) = (features, apply);
        _loading = true;
        foreach (var s in statuses)
            Statuses.Add(new StatusOption(s, current.Statuses.Contains(s), Changed));
        FeedbackSort[] sorts = features
            ? [FeedbackSort.Votes, FeedbackSort.Created, FeedbackSort.Updated, FeedbackSort.Comments]
            : [FeedbackSort.Created, FeedbackSort.Updated, FeedbackSort.Comments];
        foreach (var s in sorts)
            Sorts.Add(new ChipItem(SortName(s), s, SelectSort) { IsSelected = s == current.Sort });
        Directions.Add(new ChipItem("Descending", true, SelectDirection) { IsSelected = current.Descending });
        Directions.Add(new ChipItem("Ascending", false, SelectDirection) { IsSelected = !current.Descending });
        Mine = current.Mine;
        _loading = false;
        UpdateSummary();
    }

    public bool IsFeatures => _features;
    public bool HasStatuses => Statuses.Count > 0;

    public ObservableCollection<StatusOption> Statuses { get; } = [];
    public ObservableCollection<ChipItem> Sorts { get; } = [];
    public ObservableCollection<ChipItem> Directions { get; } = [];

    [ObservableProperty] bool mine;
    /// <summary>The statuses dropdown is open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DropdownGlyph))]
    bool isStatusOpen;
    [ObservableProperty] string statusSummary = "";

    public string DropdownGlyph => IsStatusOpen ? "expand_less" : "expand_more";

    static string SortName(FeedbackSort sort) => sort switch
    {
        FeedbackSort.Votes => "Votes",
        FeedbackSort.Updated => "Last updated",
        FeedbackSort.Comments => "Comments",
        _ => "Date sent",
    };

    [RelayCommand] void ToggleStatuses() => IsStatusOpen = !IsStatusOpen;

    partial void OnMineChanged(bool value) => Changed();

    void SelectSort(ChipItem chip)
    {
        foreach (var c in Sorts)
            c.IsSelected = c == chip;
        Changed();
    }

    void SelectDirection(ChipItem chip)
    {
        foreach (var c in Directions)
            c.IsSelected = c == chip;
        Changed();
    }

    /// <summary>Back to everything, in the usual order.</summary>
    [RelayCommand]
    void Reset()
    {
        var d = FeedbackFilter.Default(_features);
        _loading = true;
        foreach (var s in Statuses)
            s.IsChecked = false;
        foreach (var c in Sorts)
            c.IsSelected = (FeedbackSort)c.Value! == d.Sort;
        foreach (var c in Directions)
            c.IsSelected = (bool)c.Value! == d.Descending;
        Mine = false;
        _loading = false;
        Changed();
    }

    void Changed()
    {
        if (_loading)
            return;
        UpdateSummary();
        _apply(new FeedbackFilter(
            Statuses.Where(s => s.IsChecked).Select(s => s.Name).ToHashSet(),
            Mine,
            (FeedbackSort)(Sorts.FirstOrDefault(c => c.IsSelected)?.Value ?? FeedbackFilter.Default(_features).Sort),
            (bool)(Directions.FirstOrDefault(c => c.IsSelected)?.Value ?? true)));
    }

    void UpdateSummary()
    {
        var picked = Statuses.Where(s => s.IsChecked).Select(s => s.Name).ToList();
        StatusSummary = picked.Count == 0 ? "All statuses" : picked.Count <= 2 ? string.Join(", ", picked) : $"{picked.Count} statuses";
    }
}
