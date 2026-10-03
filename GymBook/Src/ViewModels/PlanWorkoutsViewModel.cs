using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>A finished workout in the list of a plan's workouts, which can be selected.</summary>
public partial class SelectableWorkout(WorkoutSession session, string title, string detail, Action<SelectableWorkout> toggle, Func<SelectableWorkout, Task> open)
    : ObservableObject
{
    public WorkoutSession Session { get; } = session;
    public string Title { get; } = title;
    public string Detail { get; } = detail;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckBackground), nameof(CheckStroke), nameof(RowBackground))]
    bool isSelected;

    public Color CheckBackground => IsSelected ? Color.FromArgb("#3F7DFF") : Colors.Transparent;
    public Color CheckStroke => IsSelected ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#626B7E");
    public Color RowBackground => IsSelected ? Color.FromArgb("#1A2A4F") : Colors.Transparent;

    [RelayCommand]
    void Toggle() => toggle(this);

    [RelayCommand]
    Task Open() => open(this);
}

/// <summary>
/// Every finished workout of one plan (?plan=), or those linked to none (?plan=none), newest first, to delete some: tap
/// rows to select them, drag over rows (from a checkbox, or after a long-press) to select a run of them, pick a range of
/// dates, or all. Deleting closes the gaps in the plan's weeks (see <see cref="DataStore.DeleteSessions"/>).
/// </summary>
public partial class PlanWorkoutsViewModel(DataStore store, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    string? _planId;
    // The drag in progress: the row it started on, whether it selects or unselects, and the selection before it.
    int _anchor = -1;
    bool _selecting;
    bool[] _before = [];

    [ObservableProperty] string title = "Workouts";
    [ObservableProperty] string summary = "";
    [ObservableProperty] List<SelectableWorkout> items = [];
    [ObservableProperty] bool isEmpty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(DeleteText))]
    int selectedCount;

    public bool HasSelection => SelectedCount > 0;
    public string DeleteText => SelectedCount == 1 ? "Delete 1 workout" : $"Delete {SelectedCount} workouts";

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _planId = query.TryGetValue("plan", out var plan) ? plan?.ToString() : null;

    public override Task OnAppearingAsync()
    {
        Load();
        return Task.CompletedTask;
    }

    void Load()
    {
        var plan = _planId == "none" ? null : store.GetPlan(_planId);
        Title = plan?.Name ?? "Not linked to a plan";
        var sessions = store.History.Where(s => plan != null ? s.PlanId == plan.Id : store.GetPlan(s.PlanId) == null).ToList();
        var selected = Items.Where(i => i.IsSelected).Select(i => i.Session.Id).ToHashSet();
        Items = [.. sessions.Select(s =>
        {
            var day = plan?.Workouts.FirstOrDefault(w => w.Id == s.PlanWorkoutId)?.Name;
            var title = plan == null ? s.Name : $"{(s.PlanWeek is { } week ? $"Week {week} · " : "")}{day ?? s.Name}";
            var detail = $"{s.StartedAt:ddd d MMM yyyy, HH:mm} · {Units.Duration(s.Duration)} · {s.WorkingSets.Count()} sets";
            return new SelectableWorkout(s, title, detail, Toggle, Open) { IsSelected = selected.Contains(s.Id) };
        })];
        IsEmpty = Items.Count == 0;
        Summary = plan == null
            ? $"{Count(Items.Count)} not linked to any plan. Tap to select, or drag over them from the checkboxes (or after holding one)."
            : $"{Count(Items.Count)} done with this plan. Tap to select, or drag over them from the checkboxes (or after holding one).";
        UpdateCount();
    }

    static string Count(int n) => n == 1 ? "1 workout" : $"{n} workouts";

    void UpdateCount() => SelectedCount = Items.Count(i => i.IsSelected);

    void Toggle(SelectableWorkout item)
    {
        item.IsSelected = !item.IsSelected;
        UpdateCount();
    }

    // Opens the workout's own sheet: what was done, and its menu (relinking it, say).
    Task Open(SelectableWorkout item) => GoTo($"{Routes.PlanDay}?session={item.Session.Id}");

    /// <summary>A drag starts on a row: that row flips, and the rows dragged over follow it.</summary>
    [RelayCommand]
    void DragStart(int index)
    {
        if (index < 0 || index >= Items.Count)
            return;
        _anchor = index;
        _before = [.. Items.Select(i => i.IsSelected)];
        _selecting = !_before[index];
        DragTo(index);
    }

    /// <summary>The drag reaches a row: every row between it and where the drag started is as the first, the rest as before.</summary>
    [RelayCommand]
    void DragTo(int index)
    {
        if (_anchor < 0 || index < 0 || index >= Items.Count)
            return;
        var (from, to) = (Math.Min(_anchor, index), Math.Max(_anchor, index));
        for (var i = 0; i < Items.Count; i++)
            Items[i].IsSelected = i >= from && i <= to ? _selecting : _before[i];
        UpdateCount();
    }

    [RelayCommand]
    void SelectAll()
    {
        var all = Items.Any(i => !i.IsSelected);
        foreach (var item in Items)
            item.IsSelected = all;
        UpdateCount();
    }

    /// <summary>Selects every workout between two days picked on the calendar (added to what's selected).</summary>
    [RelayCommand]
    async Task SelectDates()
    {
        if (Items.Count == 0)
            return;
        var days = Items.Select(i => i.Session.StartedAt.Date).ToHashSet();
        var (first, last) = (Items.Min(i => i.Session.StartedAt.Date), Items.Max(i => i.Session.StartedAt.Date));
        if (await dialogs.RangeCalendar("Select workouts from", first, last, DateTime.Today, days.Contains) is not { } range)
            return;
        foreach (var item in Items.Where(i => i.Session.StartedAt.Date >= range.Start.Date && i.Session.StartedAt.Date <= range.End.Date))
            item.IsSelected = true;
        UpdateCount();
    }

    [RelayCommand]
    async Task Delete()
    {
        var chosen = Items.Where(i => i.IsSelected).Select(i => i.Session).ToList();
        if (chosen.Count == 0)
            return;
        var (first, last) = (chosen.Min(s => s.StartedAt), chosen.Max(s => s.StartedAt));
        var when = first.Date == last.Date ? $"on {first:d MMM yyyy}" : $"from {first:d MMM yyyy} to {last:d MMM yyyy}";
        if (!await dialogs.Confirm(chosen.Count == 1 ? "Delete 1 workout?" : $"Delete {chosen.Count} workouts?",
                $"The selected workouts {when} are deleted, from the calendar, History and your stats too. This can't be undone.", "Delete"))
            return;
        store.DeleteSessions(chosen);
        Load();
    }
}
