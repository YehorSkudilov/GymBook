using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>Month-by-month view of finished workouts; tap a day to list what was done on it.</summary>
public partial class CalendarViewModel(DataStore store, StatsService stats, Units units, RecoveryService recovery) : BaseViewModel
{
    DateTime _month = FirstOfMonth(DateTime.Today);
    DateTime? _selected = DateTime.Today;
    ILookup<DateTime, WorkoutSession> _byDay = Enumerable.Empty<WorkoutSession>().ToLookup(s => s.StartedAt.Date);

    [ObservableProperty] string monthTitle = "";
    [ObservableProperty] string monthSummary = "";
    [ObservableProperty] string avgPerWeek = "";
    [ObservableProperty] string weeklyTarget = "";
    [ObservableProperty] string streak = "";
    [ObservableProperty] bool canGoNext;
    [ObservableProperty] List<string> weekdayLetters = [];
    [ObservableProperty] List<CalendarDayItem> days = [];
    [ObservableProperty] string selectedTitle = "";
    [ObservableProperty] bool hasSelection;
    [ObservableProperty] List<SessionItem> selectedSessions = [];
    [ObservableProperty] bool selectedIsEmpty;

    // Muscle recovery on the selected day: now for today, that morning for any other day.
    [ObservableProperty] IDrawable recoveryMap = MuscleMapDrawable.Empty;
    [ObservableProperty] string recoveryTitle = "";
    [ObservableProperty] string recoverySummary = "";
    [ObservableProperty] bool canOpenRecovery;
    double _recoveryHours;

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
        MonthTitle = _month.ToString("MMMM yyyy");
        CanGoNext = _month < FirstOfMonth(DateTime.Today);

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
        // On a training day, how recovered you were going into the first workout; otherwise now (today) or that morning.
        var first = _byDay[day].OrderBy(s => s.StartedAt).FirstOrDefault();
        var at = first != null ? first.StartedAt : day == DateTime.Today ? DateTime.Now : day.AddHours(9);
        var rec = recovery.Compute(at);
        RecoveryMap = MuscleMapDrawable.ForRecovery(rec);
        RecoveryTitle = first != null ? $"Muscle recovery before {first.Name} ({first.StartedAt:HH:mm})"
            : day == DateTime.Today ? "Muscle recovery now"
            : day > DateTime.Today ? "Muscle recovery that morning (forecast)"
            : "Muscle recovery that morning";
        var tired = rec.Where(r => r.Value < 0.6).OrderBy(r => r.Value).Select(r => r.Key.Display()).ToList();
        RecoverySummary = tired.Count == 0
            ? $"Every muscle group {(day < DateTime.Today ? "was" : "is")} fresh."
            : $"Recovering: {string.Join(", ", tired)}";
        // The details page previews a week back and four days ahead.
        _recoveryHours = Math.Round((at - DateTime.Now).TotalHours);
        CanOpenRecovery = _recoveryHours >= -RecoveryService.PreviewPastHours && _recoveryHours <= RecoveryService.PreviewFutureHours;
    }

    [RelayCommand]
    Task OpenRecovery() => GoTo($"{Routes.Recovery}?hours={_recoveryHours.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

    void Select(DateTime day)
    {
        _selected = day;
        Refresh();
    }

    [RelayCommand]
    void PreviousMonth()
    {
        _month = _month.AddMonths(-1);
        Refresh();
    }

    [RelayCommand]
    void NextMonth()
    {
        if (!CanGoNext)
            return;
        _month = _month.AddMonths(1);
        Refresh();
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

    public bool Done => Count > 0;
    public string Text => InMonth ? Day : "";
    public bool ShowMulti => InMonth && Count > 1;

    public Color Background => !InMonth ? Colors.Transparent : Done ? Color.FromArgb("#3F7DFF") : IsToday ? Color.FromArgb("#272C39") : Colors.Transparent;
    public Color Border => InMonth && IsSelected ? Colors.White : InMonth && IsToday && !Done ? Color.FromArgb("#3F7DFF") : Colors.Transparent;
    public Color DayColor => Done || IsToday ? Colors.White : Color.FromArgb("#9AA3B5");
}
