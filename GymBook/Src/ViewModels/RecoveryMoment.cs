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
/// The moment a muscle recovery map shows, picked on the calendar's day card and on the recovery details page (see
/// <see cref="Views.RecoveryMomentView"/>): a timeline of the day zoomed in on its workouts, one stop per exercise, with
/// the moments around them (that morning, before and after each workout, that evening, the next morning). Slide along
/// it, or tap a stop.
/// </summary>
public partial class RecoveryMoment : ObservableObject
{
    // Asks SetDay for the stop right after the day's last workout.
    const int AfterLastWorkout = -1;

    DateTime _day = DateTime.Today;
    // Anchored at the current time rather than a day (the recovery page opened from the Workout tab).
    bool _fromNow;
    bool _loading;

    /// <summary>Something changed the moment: the map should be redrawn.</summary>
    public event EventHandler? Changed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxIndex), nameof(HasSteps))]
    List<RecoveryStep> steps = [];

    /// <summary>The stop picked, also the slider's value (whole numbers).</summary>
    [ObservableProperty] double index;

    [ObservableProperty] string when = "";

    public double MaxIndex => Math.Max(1, Steps.Count - 1);
    public bool HasSteps => Steps.Count > 1;
    public RecoveryStep? Current => Steps.ElementAtOrDefault((int)Math.Round(Index));

    /// <summary>The moment the map shows.</summary>
    public DateTime At => Current?.At ?? DateTime.Now;

    /// <summary>
    /// A day's timeline: that morning, then for each workout "Before", one stop per exercise (after its last set) and
    /// "After", then that evening and the next morning. <paramref name="stop"/> picks one by position; by default the
    /// moment before the first workout, or that morning.
    /// </summary>
    public void SetDay(DateTime day, IEnumerable<WorkoutSession> sessions, Func<string, Exercise?> exercise, int? stop = null)
    {
        _day = day.Date;
        _fromNow = false;
        var list = new List<RecoveryStep>();
        void Add(string title, DateTime at, bool exercise = false) => list.Add(new RecoveryStep(title, at.ToString("HH:mm"), at, exercise, Select));

        var workouts = sessions.Where(s => s.EndedAt != null).OrderBy(s => s.StartedAt).ToList();
        var morning = _day.AddHours(7);
        if (workouts.Count == 0 || workouts[0].StartedAt > morning)
            Add("Morning", morning);
        foreach (var s in workouts)
        {
            Add($"Before {s.Name}", s.StartedAt);
            // Each exercise once its last working set was done; without set times, spread evenly over the workout.
            var done = s.Exercises.Where(e => e.Sets.Any(x => x.IsCompleted && !x.IsWarmup)).ToList();
            for (var i = 0; i < done.Count; i++)
            {
                var times = done[i].Sets.Where(x => x.IsCompleted && !x.IsWarmup).Select(x => x.CompletedAt).OfType<DateTime>().ToList();
                var at = times.Count > 0 ? times.Max() : s.StartedAt + (s.EndedAt!.Value - s.StartedAt) * ((i + 1.0) / done.Count);
                var name = exercise(done[i].ExerciseId)?.Name ?? "Exercise";
                Add(name, at, exercise: true);
            }
            Add($"After {s.Name}", s.EndedAt!.Value);
        }
        var evening = _day.AddHours(21);
        if (workouts.Count == 0 || workouts[^1].EndedAt < evening)
            Add("Evening", evening);
        Add("Next morning", _day.AddDays(1).AddHours(7));
        // Today, "Now" goes in where it falls (and is where it starts, without workouts).
        var nowIndex = -1;
        if (_day == DateTime.Today)
        {
            nowIndex = list.FindIndex(x => x.At > DateTime.Now);
            nowIndex = nowIndex < 0 ? list.Count : nowIndex;
            list.Insert(nowIndex, new RecoveryStep("Now", DateTime.Now.ToString("HH:mm"), DateTime.Now, false, Select));
        }
        var last = workouts.Count > 0 ? list.FindLastIndex(x => x.Title == $"After {workouts[^1].Name}") : -1;
        var start = stop == AfterLastWorkout ? Math.Max(0, last) : stop ?? (workouts.Count > 0 ? list.FindIndex(x => x.Title == $"Before {workouts[0].Name}") : Math.Max(0, nowIndex));
        Load(list, start);
    }

    /// <summary>Around now, with no day to follow (the recovery page from the Workout tab): a few days back and ahead.</summary>
    public void SetFromNow(double hours)
    {
        _fromNow = true;
        var now = DateTime.Now;
        var offsets = new[] { -72, -48, -24, -12, 0, 6, 12, 24, 48, 72, 96 };
        var list = offsets.Select(h => new RecoveryStep(
            h == 0 ? "Now" : h % 24 == 0 ? $"{(h > 0 ? "+" : "−")}{Math.Abs(h) / 24} d" : $"{(h > 0 ? "+" : "−")}{Math.Abs(h)} h",
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

    /// <summary>For the recovery page's link: the same day and stop.</summary>
    public string Query => _fromNow
        ? $"hours={Math.Round((At - DateTime.Now).TotalHours).ToString(CultureInfo.InvariantCulture)}"
        : $"day={_day:yyyy-MM-dd}&stop={(int)Math.Round(Index)}";

    /// <summary>Reads <see cref="Query"/> back (or a plain "hours" from now).</summary>
    public void Apply(IDictionary<string, object> query, Func<DateTime, IEnumerable<WorkoutSession>> sessionsOn, Func<string, Exercise?> exercise)
    {
        if (query.TryGetValue("day", out var d) && DateTime.TryParseExact(d?.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            // A position, or "after": the stop right after the day's last workout.
            var text = query.TryGetValue("stop", out var s) ? s?.ToString() : null;
            int? stop = text == "after" ? AfterLastWorkout : int.TryParse(text, out var i) ? i : null;
            SetDay(day, sessionsOn(day), exercise, stop);
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
        var current = Current;
        foreach (var step in Steps)
            step.IsSelected = step == current;
        When = current == null ? "" : current.IsExercise ? $"After {current.Title} · {current.At:ddd d MMM, HH:mm}" : $"{current.Title} · {current.At:ddd d MMM, HH:mm}";
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
