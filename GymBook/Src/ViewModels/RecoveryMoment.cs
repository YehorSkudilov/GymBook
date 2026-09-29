using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;

namespace GymBook.ViewModels;

/// <summary>One stop on the recovery timeline: a moment with a name, e.g. "Before Push", "Bench Press" or "Next morning".</summary>
public partial class RecoveryStep(string title, string time, DateTime at, bool isExercise, Action<RecoveryStep> select) : ObservableObject
{
    public string Title { get; } = title;
    public string Time { get; } = time;
    public DateTime At { get; } = at;
    /// <summary>An exercise of a workout (after its last set), rather than a moment around it.</summary>
    public bool IsExercise { get; } = isExercise;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background), nameof(TextColor))]
    bool isSelected;

    public Color Background => IsSelected ? Color.FromArgb("#3F7DFF") : IsExercise ? Color.FromArgb("#1D212C") : Color.FromArgb("#272C39");
    public Color TextColor => IsSelected ? Colors.White : Color.FromArgb("#F4F6FB");

    [RelayCommand]
    void Select() => select(this);
}

/// <summary>
/// The moment a muscle recovery map shows on the recovery details page (see <see cref="Views.RecoveryMomentView"/>).
/// Opened from a calendar day, the same Before this day | After this day toggle as the day card: going into the day,
/// or right after its last workout. Opened from the Workout tab, a timeline from now into the next few days: slide
/// along it, or tap a stop.
/// </summary>
public partial class RecoveryMoment : ObservableObject
{
    // The toggle, lit up like the calendar's.
    static readonly Color ToggleOn = Color.FromArgb("#3F7DFF"), ToggleOnText = Colors.White, ToggleOffText = Color.FromArgb("#9AA3B5");

    DateTime _day = DateTime.Today;
    // When the day's last workout ended; null on a day without one.
    DateTime? _lastEnd;
    bool _loading;

    /// <summary>Something changed the moment: the map should be redrawn.</summary>
    public event EventHandler? Changed;

    /// <summary>A calendar day, with the Before | After toggle; otherwise the timeline from now.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFromNow), nameof(HasSteps), nameof(HasToggle))]
    bool isDay;

    public bool IsFromNow => !IsDay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BeforeBackground), nameof(AfterBackground), nameof(BeforeText), nameof(AfterText))]
    bool isAfterDay;

    public bool HasToggle => IsDay && _lastEnd != null;
    public Color BeforeBackground => IsAfterDay ? Colors.Transparent : ToggleOn;
    public Color AfterBackground => IsAfterDay ? ToggleOn : Colors.Transparent;
    public Color BeforeText => IsAfterDay ? ToggleOffText : ToggleOnText;
    public Color AfterText => IsAfterDay ? ToggleOnText : ToggleOffText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxIndex), nameof(HasSteps))]
    List<RecoveryStep> steps = [];

    /// <summary>The stop picked, also the slider's value (whole numbers).</summary>
    [ObservableProperty] double index;

    [ObservableProperty] string when = "";

    public double MaxIndex => Math.Max(1, Steps.Count - 1);
    public bool HasSteps => !IsDay && Steps.Count > 1;
    public RecoveryStep? Current => Steps.ElementAtOrDefault((int)Math.Round(Index));

    /// <summary>The moment the map shows.</summary>
    public DateTime At => IsDay ? (IsAfterDay && _lastEnd is { } end ? end : _day) : Current?.At ?? DateTime.Now;

    /// <summary>A calendar day: going into it, or (<paramref name="after"/>) right after its last workout.</summary>
    public void SetDay(DateTime day, IEnumerable<WorkoutSession> sessions, bool after)
    {
        _loading = true;
        _day = day.Date;
        _lastEnd = sessions.Select(s => s.EndedAt).Max();
        Steps = [];
        IsDay = true;
        OnPropertyChanged(nameof(HasToggle));
        IsAfterDay = after && _lastEnd != null;
        _loading = false;
        Update();
    }

    partial void OnIsAfterDayChanged(bool value) => Update();

    [RelayCommand]
    void BeforeDay() => IsAfterDay = false;

    [RelayCommand]
    void AfterDay() => IsAfterDay = true;

    /// <summary>Around now, with no day to follow (the recovery page from the Workout tab): the next few days.</summary>
    public void SetFromNow(double hours)
    {
        IsDay = false;
        var now = DateTime.Now;
        // From now on only: how the muscles will recover. What happened before is in the calendar.
        var offsets = new[] { 0, 6, 12, 24, 36, 48, 72, 96 };
        var list = offsets.Select(h => new RecoveryStep(
            h == 0 ? "Now" : h % 24 == 0 ? $"+{h / 24} d" : $"+{h} h",
            now.AddHours(h).ToString("ddd HH:mm"), now.AddHours(h), false, Select)).ToList();
        // The offset closest to the one asked for.
        var start = Array.IndexOf(offsets, offsets.MinBy(o => Math.Abs(o - hours)));
        Load(list, start);
    }

    void Load(List<RecoveryStep> list, int start)
    {
        _loading = true;
        Steps = list;
        Index = Math.Clamp(start, 0, Math.Max(0, list.Count - 1));
        _loading = false;
        Update();
    }

    /// <summary>Reads the page's query: a calendar "day" (with "stop=after" for after its last workout), or "hours" from now.</summary>
    public void Apply(IDictionary<string, object> query, Func<DateTime, IEnumerable<WorkoutSession>> sessionsOn)
    {
        if (query.TryGetValue("day", out var d) && DateTime.TryParseExact(d?.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            SetDay(day, sessionsOn(day), after: query.TryGetValue("stop", out var s) && s?.ToString() == "after");
            return;
        }
        SetFromNow(query.TryGetValue("hours", out var h) && double.TryParse(h?.ToString(), CultureInfo.InvariantCulture, out var hours) ? hours : 0);
    }

    void Select(RecoveryStep step) => Index = Steps.IndexOf(step);

    partial void OnIndexChanged(double value)
    {
        // The slider moves stop to stop.
        var snapped = Math.Round(value);
        if (Math.Abs(snapped - value) > 0.001)
        {
            Index = snapped;
            return;
        }
        Update();
    }

    void Update()
    {
        if (_loading)
            return;
        if (IsDay)
            When = IsAfterDay && _lastEnd is { } end ? $"After the last workout · {end:ddd d MMM, HH:mm}" : $"Going into {_day:ddd d MMM}";
        else
        {
            var current = Current;
            foreach (var step in Steps)
                step.IsSelected = step == current;
            When = current == null ? "" : $"{current.Title} · {current.At:ddd d MMM, HH:mm}";
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
