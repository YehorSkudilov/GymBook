using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;

namespace GymBook.Wear;

/// <summary>
/// The phone's workout, one set at a time: the set to do next, a big Done that asks the phone to tick it, the rest timer
/// after it, and the heart rate. Everything is worked out from the workout the phone last sent, so the watch and the
/// phone always agree; a tick shows straight away and is confirmed when the phone sends the workout back.
/// </summary>
public partial class CompanionViewModel : ObservableObject
{
    // How long a tick may wait for the phone before the watch gives up on it.
    static readonly TimeSpan PendingTimeout = TimeSpan.FromSeconds(6);
    // Warm-ups rest at most this long (the phone's warm-up rest setting isn't sent).
    const int WarmupRestSeconds = 60;

    readonly PhoneLink _phone;
    readonly HeartRateMonitor _heart;
    IDispatcherTimer? _timer;
    WearWorkout _workout = WearWorkout.None;
    (string Session, int Exercise, int Set, DateTimeOffset At)? _pending;
    DateTimeOffset? _restSkippedFor;
    DateTimeOffset? _restAlertedFor;

    public CompanionViewModel(PhoneLink phone, HeartRateMonitor heart)
    {
        _phone = phone;
        _heart = heart;
    }

    // What's on screen: exactly one of these is true.
    [ObservableProperty] bool isWaiting = true;
    [ObservableProperty] bool isLifting;
    [ObservableProperty] bool isResting;
    [ObservableProperty] bool isFinished;

    [ObservableProperty] string status = "The workout on your phone has ended.";
    [ObservableProperty] string workoutName = "";
    [ObservableProperty] string elapsed = "";
    [ObservableProperty] string heartRate = "";
    [ObservableProperty] string progress = "";
    [ObservableProperty] string exerciseName = "";
    [ObservableProperty] string setLabel = "";
    [ObservableProperty] string target = "";
    [ObservableProperty] string next = "";
    [ObservableProperty] string doneText = "Done";
    [ObservableProperty] bool canComplete;
    [ObservableProperty] string restText = "";
    [ObservableProperty] string restUntil = "";

    /// <summary>Ambient mode (see <see cref="Ambient"/>): buttons hidden, times that hold for a minute.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInteractive))]
    bool isAmbient;

    public bool IsInteractive => !IsAmbient;
    [ObservableProperty] double restProgress;
    [ObservableProperty] string message = "";

    /// <summary>The phone's workout ended (finished or discarded there): the page goes back to the watch's home.</summary>
    public event Action? Ended;

    /// <summary>While the page is on screen: the clock, the heart rate, and the workout the phone last sent.</summary>
    public async Task StartAsync()
    {
        // Followed only while on screen: these services outlive the page.
        _phone.WorkoutChanged -= OnWorkoutChanged;
        _phone.WorkoutChanged += OnWorkoutChanged;
        _heart.Changed -= OnHeartRate;
        _heart.Changed += OnHeartRate;
        Ambient.Changed -= OnAmbientChanged;
        Ambient.Changed += OnAmbientChanged;
        Ambient.Tick -= Refresh;
        Ambient.Tick += Refresh;
        IsAmbient = Ambient.IsActive;
        _workout = _phone.Workout;
        _timer ??= Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        if (!IsAmbient)
            _timer.Start();
        Refresh();
        try
        {
            if (!await _heart.StartAsync())
                HeartRate = "";
        }
        catch (Exception e)
        {
            // No sensor, or the permission can't be asked for on this watch: just no heart rate.
            System.Diagnostics.Debug.WriteLine($"GymBook.Wear: heart rate failed: {e}");
            HeartRate = "";
        }
    }

    public void Stop()
    {
        _timer?.Stop();
        _phone.WorkoutChanged -= OnWorkoutChanged;
        _heart.Changed -= OnHeartRate;
        Ambient.Changed -= OnAmbientChanged;
        Ambient.Tick -= Refresh;
        try
        {
            _heart.Stop();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"GymBook.Wear: stopping failed: {e}");
        }
    }

    void OnTick(object? sender, EventArgs e) => Refresh();

    void OnHeartRate(int bpm) => HeartRate = $"♥ {bpm}";

    // Ambient: the per-second clock stops (the system redraws about once a minute instead), back to normal after.
    void OnAmbientChanged(bool ambient)
    {
        IsAmbient = ambient;
        if (ambient)
            _timer?.Stop();
        else
            _timer?.Start();
        Refresh();
    }

    void OnWorkoutChanged(WearWorkout workout)
    {
        _workout = workout;
        // The phone has it (or the workout moved on): the tick isn't pending any more.
        if (_pending is { } p && (p.Session != workout.SessionId || SetAt(p.Exercise, p.Set)?.IsCompleted != false))
            _pending = null;
        Message = "";
        Refresh();
    }

    WearSet? SetAt(int exercise, int set) => _workout.Exercises.ElementAtOrDefault(exercise)?.Sets.ElementAtOrDefault(set);

    bool IsDone(int exercise, int set) =>
        SetAt(exercise, set)?.IsCompleted == true || _pending is { } p && p.Exercise == exercise && p.Set == set;

    /// <summary>The set to do next: in the exercise of the last tick if it has sets left, otherwise the first open one.</summary>
    (int Exercise, int Set)? Current()
    {
        var exercises = _workout.Exercises;
        int? Open(int e)
        {
            var sets = exercises[e].Sets;
            for (var s = 0; s < sets.Count; s++)
                if (!IsDone(e, s))
                    return s;
            return null;
        }
        if (LastTick() is { } last && Open(last.Exercise) is { } same)
            return (last.Exercise, same);
        for (var e = 0; e < exercises.Count; e++)
            if (Open(e) is { } set)
                return (e, set);
        return null;
    }

    /// <summary>The newest tick, the phone's or a pending one, with how long to rest after it.</summary>
    (int Exercise, DateTimeOffset At, int RestSeconds)? LastTick()
    {
        (int, DateTimeOffset, int)? last = null;
        for (var e = 0; e < _workout.Exercises.Count; e++)
        {
            var exercise = _workout.Exercises[e];
            foreach (var set in exercise.Sets)
                if (set.IsCompleted && set.CompletedAt is { } at && (last == null || at > last.Value.Item2))
                    last = (e, at, RestFor(exercise, set));
        }
        if (_pending is { } p && (last == null || p.At > last.Value.Item2) && SetAt(p.Exercise, p.Set) is { } pendingSet)
            last = (p.Exercise, p.At, RestFor(_workout.Exercises[p.Exercise], pendingSet));
        return last;
    }

    static int RestFor(WearExercise exercise, WearSet set) => set.IsWarmup ? Math.Min(WarmupRestSeconds, exercise.RestSeconds) : exercise.RestSeconds;

    void Refresh()
    {
        if (_pending is { } stale && DateTimeOffset.Now - stale.At > PendingTimeout)
        {
            _pending = null;
            Message = "Your phone didn't answer. Try again.";
        }

        var active = _workout.IsActive;
        if (!active)
        {
            Show(waiting: true);
            Ended?.Invoke();
            return;
        }

        WorkoutName = _workout.Name;
        var elapsed = DateTimeOffset.Now - _workout.StartedAt;
        // Seconds would be wrong for most of the minute between ambient redraws.
        Elapsed = IsAmbient ? $"{(int)elapsed.TotalMinutes} min" : Clock(elapsed);
        var working = _workout.Exercises.SelectMany((e, i) => e.Sets.Select((s, j) => (s, i, j))).Where(x => !x.s.IsWarmup).ToList();
        Progress = $"{working.Count(x => IsDone(x.i, x.j))}/{working.Count} sets";

        if (Current() is not { } current)
        {
            Show(finished: true);
            return;
        }

        // Resting after the last tick, unless skipped or over.
        if (LastTick() is { } last && last.At != _restSkippedFor)
        {
            var left = last.At.AddSeconds(last.RestSeconds) - DateTimeOffset.Now;
            if (left > TimeSpan.Zero)
            {
                RestText = Clock(left);
                RestUntil = $"until {last.At.AddSeconds(last.RestSeconds).ToLocalTime():HH:mm}";
                RestProgress = last.RestSeconds <= 0 ? 0 : left.TotalSeconds / last.RestSeconds;
                ShowSet(current);
                Show(resting: true);
                return;
            }
            if (_restAlertedFor != last.At && left > TimeSpan.FromSeconds(-3))
            {
                // Rest just ran out: buzz, so the wrist says when to lift again.
                _restAlertedFor = last.At;
                try
                {
                    if (WatchSettings.RestBuzz)
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                }
                catch (Exception)
                {
                }
            }
        }

        ShowSet(current);
        Show(lifting: true);
    }

    void ShowSet((int Exercise, int Set) current)
    {
        var exercise = _workout.Exercises[current.Exercise];
        var set = exercise.Sets[current.Set];
        ExerciseName = exercise.Name;
        var ofKind = exercise.Sets.Where(s => s.IsWarmup == set.IsWarmup).ToList();
        var number = exercise.Sets.Take(current.Set + 1).Count(s => s.IsWarmup == set.IsWarmup);
        SetLabel = $"{(set.IsWarmup ? "Warm-up" : "Set")} {number} of {ofKind.Count}";
        var weight = set.Weight > 0 ? $"{set.Weight.ToString("0.#", CultureInfo.CurrentCulture)} {_workout.Unit}" : "Bodyweight";
        Target = set.Reps > 0 ? $"{weight} × {set.Reps}" : weight;
        var nextExercise = _workout.Exercises.Skip(current.Exercise + 1).FirstOrDefault(e => e.Sets.Any(s => !s.IsCompleted));
        Next = current.Set == exercise.Sets.Count - 1 && nextExercise != null ? $"Next: {nextExercise.Name}" : "";
        CanComplete = _pending == null && set.Reps > 0;
        DoneText = _pending != null ? "Saving…" : set.Reps > 0 ? "Done" : "Set reps on phone";
    }

    void Show(bool waiting = false, bool lifting = false, bool resting = false, bool finished = false)
    {
        IsWaiting = waiting;
        IsLifting = lifting;
        IsResting = resting;
        IsFinished = finished;
    }

    [RelayCommand]
    async Task Complete()
    {
        if (Current() is not { } current || _pending != null)
            return;
        var request = new WearCompleteSet(_workout.SessionId, current.Exercise, current.Set);
        _pending = (_workout.SessionId, current.Exercise, current.Set, DateTimeOffset.Now);
        _restSkippedFor = null;
        Message = "";
        try
        {
            if (WatchSettings.TapFeedback)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception)
        {
        }
        Refresh();
        try
        {
            if (!await _phone.CompleteSetAsync(request))
            {
                _pending = null;
                Message = "Your phone isn't connected.";
            }
        }
        catch (Exception)
        {
            _pending = null;
            Message = "Couldn't reach your phone.";
        }
        Refresh();
    }

    [RelayCommand]
    void SkipRest()
    {
        _restSkippedFor = LastTick()?.At;
        Refresh();
    }

    static string Clock(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{(int)t.TotalMinutes}:{t.Seconds:00}";
}
