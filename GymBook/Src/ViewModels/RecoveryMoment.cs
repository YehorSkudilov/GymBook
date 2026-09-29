using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The moment a muscle recovery map shows, picked on the calendar's day card and on the recovery details page (see
/// <see cref="Views.RecoveryMomentView"/>): one of the day's workouts, just before or just after it, moved earlier or
/// later with a slider. On a day without workouts it's that morning (or now, for today).
/// </summary>
public partial class RecoveryMoment : ObservableObject
{
    // How far the slider moves from the chosen moment: a week back, four days ahead.
    public const double MinHours = -RecoveryService.PreviewPastHours, MaxHours = RecoveryService.PreviewFutureHours;

    List<WorkoutSession> _sessions = [];
    DateTime _day = DateTime.Today;
    // Anchored at the current time rather than a day (the recovery page opened from the Workout tab).
    bool _fromNow;
    bool _loading;

    /// <summary>Something changed the moment: the map should be redrawn.</summary>
    public event EventHandler? Changed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWorkouts), nameof(HasSeveral), nameof(WorkoutName))]
    List<string> workoutNames = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkoutName))]
    int selectedIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BeforeBackground), nameof(AfterBackground), nameof(BeforeText), nameof(AfterText))]
    bool isAfter;

    /// <summary>Hours from the chosen moment, in whole hours.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShifted))]
    double hours;

    [ObservableProperty] string when = "";

    public bool HasWorkouts => WorkoutNames.Count > 0;
    /// <summary>A dropdown only when there's a choice to make.</summary>
    public bool HasSeveral => WorkoutNames.Count > 1;
    public string WorkoutName => WorkoutNames.ElementAtOrDefault(SelectedIndex) ?? "";
    public bool IsShifted => Hours != 0;
    public WorkoutSession? Session => _sessions.ElementAtOrDefault(SelectedIndex);

    // The Before / After toggle.
    static readonly Color On = Color.FromArgb("#3F7DFF"), Off = Colors.Transparent, OnText = Colors.White, OffText = Color.FromArgb("#9AA3B5");
    public Color BeforeBackground => IsAfter ? Off : On;
    public Color AfterBackground => IsAfter ? On : Off;
    public Color BeforeText => IsAfter ? OffText : OnText;
    public Color AfterText => IsAfter ? OnText : OffText;

    /// <summary>What the slider is centred on: the workout's start or end, that morning, or now.</summary>
    public DateTime Anchor => Session is { } s ? IsAfter ? s.EndedAt ?? s.StartedAt : s.StartedAt
        : _fromNow || _day == DateTime.Today ? DateTime.Now : _day.AddHours(9);

    /// <summary>The moment the map shows.</summary>
    public DateTime At => Anchor.AddHours(Hours);

    /// <summary>A day's workouts, with one of them (by id) chosen, before or after it, and the slider where it was.</summary>
    public void SetDay(DateTime day, IEnumerable<WorkoutSession> sessions, string? sessionId = null, bool after = false, double hours = 0)
    {
        _loading = true;
        _fromNow = false;
        _day = day.Date;
        _sessions = [.. sessions.OrderBy(s => s.StartedAt)];
        WorkoutNames = [.. _sessions.Select(s => $"{s.Name} · {s.StartedAt:HH:mm}")];
        SelectedIndex = Math.Max(0, _sessions.FindIndex(s => s.Id == sessionId));
        IsAfter = after && _sessions.Count > 0;
        Hours = Math.Clamp(Math.Round(hours), MinHours, MaxHours);
        _loading = false;
        Update();
    }

    /// <summary>Hours from now, with no workout to pick (the recovery page from the Workout tab).</summary>
    public void SetFromNow(double hours)
    {
        _loading = true;
        _fromNow = true;
        _sessions = [];
        WorkoutNames = [];
        IsAfter = false;
        Hours = Math.Clamp(Math.Round(hours), MinHours, MaxHours);
        _loading = false;
        Update();
    }

    /// <summary>For the recovery page's link: the same workout, side and slider position.</summary>
    public string Query => _fromNow
        ? $"hours={Hours.ToString(CultureInfo.InvariantCulture)}"
        : $"day={_day:yyyy-MM-dd}&session={Session?.Id}&after={IsAfter}&hours={Hours.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Reads <see cref="Query"/> back; false when it isn't one.</summary>
    public bool TryApply(IDictionary<string, object> query, Func<DateTime, IEnumerable<WorkoutSession>> sessionsOn)
    {
        var hours = query.TryGetValue("hours", out var h) && double.TryParse(h?.ToString(), CultureInfo.InvariantCulture, out var value) ? value : 0;
        if (query.TryGetValue("day", out var d) && DateTime.TryParseExact(d?.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            var after = query.TryGetValue("after", out var a) && bool.TryParse(a?.ToString(), out var isAfter) && isAfter;
            SetDay(day, sessionsOn(day), query.TryGetValue("session", out var s) ? s?.ToString() : null, after, hours);
            return true;
        }
        SetFromNow(hours);
        return false;
    }

    partial void OnSelectedIndexChanged(int value) => Update();
    partial void OnIsAfterChanged(bool value) => Update();

    partial void OnHoursChanged(double value)
    {
        // Whole hours, so the label and the map move in clean steps.
        var snapped = Math.Round(value);
        if (Math.Abs(snapped - value) > 0.001)
        {
            Hours = snapped;
            return;
        }
        Update();
    }

    [RelayCommand]
    void Before() => IsAfter = false;

    [RelayCommand]
    void After() => IsAfter = true;

    /// <summary>Back to the chosen moment itself.</summary>
    [RelayCommand]
    void Reset() => Hours = 0;

    void Update()
    {
        if (_loading)
            return;
        When = Describe();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // "Before Push · Tue 3 Sep, 18:05", or moved: "+5 h after Push · Tue 3 Sep, 23:00".
    string Describe()
    {
        var isNow = Session == null && (_fromNow || _day == DateTime.Today);
        if (Hours == 0)
            return Session is { } s ? $"{(IsAfter ? "After" : "Before")} {s.Name} · {At:ddd d MMM, HH:mm}"
                : isNow ? "Now"
                : $"That morning · {At:ddd d MMM, HH:mm}";
        var h = (int)Math.Abs(Hours);
        var span = h >= 24 ? $"{h / 24} d{(h % 24 > 0 ? $" {h % 24} h" : "")}" : $"{h} h";
        var from = Session is { } w ? $"{(IsAfter ? "after" : "before")} {w.Name}" : isNow ? "now" : "that morning";
        return $"{(Hours > 0 ? "+" : "−")}{span} {(isNow ? "from now" : $"from {from}")} · {At:ddd d MMM, HH:00}";
    }
}
