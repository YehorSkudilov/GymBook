using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>Month-by-month view of finished workouts; tap a day to list what was done on it.</summary>
public partial class CalendarViewModel(DataStore store, StatsService stats, Units units, RecoveryService recovery, DialogService dialogs) : BaseViewModel
{
    DateTime _month = FirstOfMonth(DateTime.Today);
    DateTime? _selected = DateTime.Today;
    ILookup<DateTime, WorkoutSession> _byDay = Enumerable.Empty<WorkoutSession>().ToLookup(s => s.StartedAt.Date);

    [ObservableProperty] string monthSummary = "";
    [ObservableProperty] string avgPerWeek = "";
    [ObservableProperty] string weeklyTarget = "";
    [ObservableProperty] string streak = "";
    [ObservableProperty] bool canGoNext;
    [ObservableProperty] List<string> weekdayLetters = [];
    [ObservableProperty] List<CalendarDayItem> days = [];
    /// <summary>The month shown; tapping it picks another, a year and then its month (<see cref="PickMonth"/>).</summary>
    [ObservableProperty] string monthTitle = "";
    [ObservableProperty] string selectedTitle = "";
    [ObservableProperty] bool hasSelection;
    [ObservableProperty] List<SessionItem> selectedSessions = [];
    [ObservableProperty] bool selectedIsEmpty;

    // Muscle recovery on the selected day: going into it, or after everything done on it (a toggle under the map).
    [ObservableProperty] IDrawable recoveryMap = MuscleMapDrawable.Empty;
    [ObservableProperty] string recoveryTitle = "";
    [ObservableProperty] string recoverySummary = "";
    [ObservableProperty] List<MuscleRecoveryItem> majorMuscles = [];
    [ObservableProperty] List<MuscleRecoveryItem> supportingMuscles = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BeforeBackground), nameof(AfterBackground), nameof(BeforeText), nameof(AfterText))]
    bool isAfterDay;

    // The Before this day | After this day toggle: the chosen half lit up.
    static readonly Color ToggleOn = Color.FromArgb("#3F7DFF"), ToggleOnText = Colors.White, ToggleOffText = Color.FromArgb("#9AA3B5");
    public Color BeforeBackground => IsAfterDay ? Colors.Transparent : ToggleOn;
    public Color AfterBackground => IsAfterDay ? ToggleOn : Colors.Transparent;
    public Color BeforeText => IsAfterDay ? ToggleOffText : ToggleOnText;
    public Color AfterText => IsAfterDay ? ToggleOnText : ToggleOffText;

    DateTime _recoveryDay = DateTime.Today;

    public override Task OnAppearingAsync()
    {
        // Reload every time: a workout may have been deleted from its detail page.
        _byDay = store.History.ToLookup(s => s.StartedAt.Date);
        WeekdayLetters = Enumerable.Range(0, 7).Select(i => StatsService.WeekStart(DateTime.Today).AddDays(i).ToString("ddd")[..1]).ToList();
        WeeklyTarget = stats.WeeklyTarget.ToString();
        var streakWeeks = stats.WeekStreak();
        Streak = streakWeeks == 1 ? "1 week" : $"{streakWeeks} weeks";
        Refresh();
        return Task.CompletedTask;
    }

    static DateTime FirstOfMonth(DateTime d) => new(d.Year, d.Month, 1);

    void Refresh()
    {
        CanGoNext = _month < FirstOfMonth(DateTime.Today);

        MonthTitle = _month.ToString("MMMM yyyy");
        var monthEnd = _month.AddMonths(1);
        var monthSessions = _byDay.Where(g => g.Key >= _month && g.Key < monthEnd).SelectMany(g => g).ToList();
        var activeDays = monthSessions.Select(s => s.StartedAt.Date).Distinct().Count();
        MonthSummary = monthSessions.Count == 0
            ? "No workouts this month"
            : $"{monthSessions.Count} {(monthSessions.Count == 1 ? "workout" : "workouts")} · {activeDays} {(activeDays == 1 ? "day" : "days")} · {units.FormatVolume(monthSessions.Sum(stats.SessionVolume))}";

        // The current month is averaged over the days so far, so it isn't dragged down by weeks that haven't happened.
        var daysCounted = _month == FirstOfMonth(DateTime.Today) ? DateTime.Today.Day : DateTime.DaysInMonth(_month.Year, _month.Month);
        AvgPerWeek = (monthSessions.Count / (daysCounted / 7.0)).ToString("0.#");

        // Six Monday-first weeks cover every month; days outside it render as blanks.
        var gridStart = StatsService.WeekStart(_month);
        // Weeks that reached the target (judged against today's target, like the streak).
        var target = stats.WeeklyTarget;
        var hitWeeks = Enumerable.Range(0, 6)
            .Where(w => Enumerable.Range(0, 7).Sum(d => _byDay[gridStart.AddDays(w * 7 + d)].Count()) >= target)
            .ToHashSet();
        Days = Enumerable.Range(0, 42).Select(i =>
        {
            var d = gridStart.AddDays(i);
            var inMonth = d.Month == _month.Month;
            return new CalendarDayItem
            {
                Day = d.Day.ToString(),
                InMonth = inMonth,
                IsToday = d == DateTime.Today,
                IsSelected = d == _selected,
                Count = _byDay[d].Count(),
                SelectCommand = new RelayCommand(() => Select(d), () => inMonth),
                WeekHit = hitWeeks.Contains(i / 7),
                Weekday = i % 7,
            };
        }).ToList();

        HasSelection = _selected is DateTime sel && sel >= _month && sel < monthEnd;
        if (HasSelection)
        {
            var day = _selected!.Value;
            SelectedTitle = day.ToString("dddd, d MMMM");
            // Tapping one opens the finished-workout sheet: what was done, its stats and the fatigue it left.
            SelectedSessions = _byDay[day].OrderBy(s => s.StartedAt)
                .Select(s => SessionItem.Create(s, store, stats, units, new AsyncRelayCommand(() => GoTo($"{Routes.PlanDay}?session={s.Id}"))))
                .ToList();
            UpdateRecovery(day);
        }
        else
        {
            SelectedSessions = [];
        }
        SelectedIsEmpty = HasSelection && SelectedSessions.Count == 0;
    }

    void UpdateRecovery(DateTime day)
    {
        _recoveryDay = day;
        ShowRecovery();
    }

    /// <summary>When the day's last workout ended; null on a day without one.</summary>
    DateTime? LastWorkoutEnd => _byDay[_recoveryDay].Select(s => s.EndedAt).Max();

    partial void OnIsAfterDayChanged(bool value) => ShowRecovery();

    [RelayCommand]
    void BeforeDay() => IsAfterDay = false;

    [RelayCommand]
    void AfterDay() => IsAfterDay = true;

    /// <summary>
    /// Going into the day (its start), or after it: right after its last workout, or on a day without one, the end of
    /// the day (now, for today).
    /// </summary>
    DateTime RecoveryAt => !IsAfterDay ? _recoveryDay : LastWorkoutEnd ?? DayEnd(_recoveryDay);

    internal static DateTime DayEnd(DateTime day) => day == DateTime.Today ? DateTime.Now : day.AddDays(1);

    void ShowRecovery()
    {
        var at = RecoveryAt;
        var details = recovery.Details(at);
        var rec = details.ToDictionary(d => d.Muscle, d => d.Recovery);
        RecoveryMap = MuscleMapDrawable.ForRecovery(rec);
        (MajorMuscles, SupportingMuscles) = RecoveryViewModel.Lists(details, at);
        RecoveryTitle = !IsAfterDay ? "Muscle recovery going into the day"
            : LastWorkoutEnd is { } end ? $"Muscle recovery after the last workout ({end:HH:mm})"
            : _recoveryDay == DateTime.Today ? "Muscle recovery right now" : "Muscle recovery at the end of the day";
        var tired = rec.Where(r => r.Value < 0.6).OrderBy(r => r.Value).Select(r => r.Key.Display()).ToList();
        RecoverySummary = tired.Count == 0
            ? $"Every muscle group {(at < DateTime.Now ? "was" : "is")} fresh."
            : $"Recovering: {string.Join(", ", tired)}";
    }

    /// <summary>The details page, on this day's timeline: its first stop for Before, the end of its last workout for After.</summary>
    [RelayCommand]
    Task OpenRecovery() => GoTo($"{Routes.Recovery}?day={_recoveryDay:yyyy-MM-dd}&stop={(IsAfterDay ? "after" : "0")}");

    /// <summary>The ··· beside the selected day's date: its recovery details, or discard all its workouts.</summary>
    [RelayCommand]
    async Task DayOptions()
    {
        if (_selected is not { } day)
            return;
        var sessions = _byDay[day].ToList();
        var discard = sessions.Count switch
        {
            0 => null,
            1 => "Discard workout",
            _ => $"Discard all {sessions.Count} workouts",
        };
        var choice = await dialogs.ActionSheet(day.ToString("dddd, d MMMM"), discard, "Recovery details");
        if (choice == "Recovery details")
            await OpenRecovery();
        else if (choice != null && choice == discard)
        {
            var title = sessions.Count == 1 ? "Discard workout?" : $"Discard {sessions.Count} workouts?";
            var what = sessions.Count == 1 ? $"{sessions[0].Name} is" : "They're";
            if (!await dialogs.Confirm(title, $"{what} removed from your history and statistics. This can't be undone.", "Discard"))
                return;
            foreach (var s in sessions)
                store.Data.Sessions.Remove(s);
            store.CompactPlanWeeks();
            store.Save();
            await OnAppearingAsync();
        }
    }

    void Select(DateTime day)
    {
        _selected = day;
        Refresh();
    }

    /// <summary>
    /// The month title: pick a year (back to the first workout's, and at least two), then a month of it (up to this one).
    /// </summary>
    [RelayCommand]
    async Task PickMonth()
    {
        var thisMonth = FirstOfMonth(DateTime.Today);
        var firstYear = new[] { thisMonth.Year - 2, _month.Year }.Concat(_byDay.Select(g => g.Key.Year)).Min();
        var years = Enumerable.Range(firstYear, thisMonth.Year - firstYear + 1).Reverse()
            .Select(y => y == _month.Year ? $"{y} ✓" : $"{y}").ToList();
        var pickedYear = await dialogs.ActionSheet("Year", null, [.. years]);
        var y = pickedYear == null ? -1 : years.IndexOf(pickedYear);
        if (y < 0)
            return;
        var year = thisMonth.Year - y;
        var months = Enumerable.Range(1, year == thisMonth.Year ? thisMonth.Month : 12).Select(m => new DateTime(year, m, 1)).ToList();
        var labels = months.Select(m => m == _month ? $"{m:MMMM} ✓" : m.ToString("MMMM")).ToList();
        var pickedMonth = await dialogs.ActionSheet($"{year}", null, [.. labels]);
        var m = pickedMonth == null ? -1 : labels.IndexOf(pickedMonth);
        if (m < 0)
            return;
        ShowMonth(months[m]);
    }

    /// <summary>Another month, with its first day selected: a day is always selected.</summary>
    void ShowMonth(DateTime month)
    {
        _month = month;
        _selected = month;
        Refresh();
    }

    [RelayCommand]
    void PreviousMonth() => ShowMonth(_month.AddMonths(-1));

    [RelayCommand]
    void NextMonth()
    {
        if (!CanGoNext)
            return;
        ShowMonth(_month.AddMonths(1));
    }

    [RelayCommand]
    void GoToToday()
    {
        _month = FirstOfMonth(DateTime.Today);
        _selected = DateTime.Today;
        Refresh();
    }
}

public class CalendarDayItem
{
    public required string Day { get; init; }
    public required bool InMonth { get; init; }
    public required bool IsToday { get; init; }
    public required bool IsSelected { get; init; }
    public required int Count { get; init; }
    public required ICommand SelectCommand { get; init; }
    /// <summary>Its week reached the weekly workout target: the row gets a band behind it.</summary>
    public bool WeekHit { get; init; }
    /// <summary>0 for Monday … 6 for Sunday: the band's rounded ends go on the first and last day.</summary>
    public int Weekday { get; init; }

    public bool ShowBandStart => WeekHit && Weekday == 0;
    public bool ShowBandMiddle => WeekHit && Weekday is > 0 and < 6;
    public bool ShowBandEnd => WeekHit && Weekday == 6;

    public bool Done => Count > 0;
    public string Text => InMonth ? Day : "";
    public bool ShowMulti => InMonth && Count > 1;

    public Color Background => !InMonth ? Colors.Transparent : Done ? Color.FromArgb("#3F7DFF") : IsToday ? Color.FromArgb("#272C39") : Colors.Transparent;
    public Color Border => InMonth && IsSelected ? Colors.White : InMonth && IsToday && !Done ? Color.FromArgb("#3F7DFF") : Colors.Transparent;
    public Color DayColor => Done || IsToday ? Colors.White : Color.FromArgb("#9AA3B5");
}
