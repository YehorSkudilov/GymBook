using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.Wear;

/// <summary>
/// A workout started on the watch (from the plan, or a quick one built as it goes), one set at a time: the set to do,
/// its weight and reps (adjustable), Done, the rest timer after it, and the heart rate. It's the watch's own copy (the
/// phone app's WorkoutService and on-device database), saved after every change so it survives the app closing, and
/// synced to the account whenever the watch is online. In ambient mode it stays on screen dimmed, without buttons.
/// </summary>
public partial class WorkoutViewModel : ObservableObject
{
    readonly WorkoutService _workouts;
    readonly DataStore _store;
    readonly Units _units;
    readonly HeartRateMonitor _heart;
    IDispatcherTimer? _timer;
    int _exercise;
    DateTime? _restEndsAt;
    int _restTotal;

    public WorkoutViewModel(WorkoutService workouts, DataStore store, Units units, HeartRateMonitor heart)
    {
        _workouts = workouts;
        _store = store;
        _units = units;
        _heart = heart;
    }

    /// <summary>The workout was finished or discarded: the page goes back to the watch's home.</summary>
    public event Action? Ended;

    /// <summary>Minimized: back to the home, where a pill keeps it a tap away; the workout carries on.</summary>
    public event Action? Minimized;

    // What's on screen: one of these (an empty quick workout shows none of the first three).
    [ObservableProperty] bool isLifting;
    [ObservableProperty] bool isResting;
    [ObservableProperty] bool isAllDone;
    [ObservableProperty] bool isEmpty;

    /// <summary>Ambient mode: buttons hidden, times that hold for a minute.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInteractive))]
    bool isAmbient;

    public bool IsInteractive => !IsAmbient;

    [ObservableProperty] string name = "";
    [ObservableProperty] string elapsed = "";
    [ObservableProperty] string heartRate = "";
    [ObservableProperty] string progress = "";
    [ObservableProperty] string exerciseName = "";
    [ObservableProperty] string exercisePosition = "";
    [ObservableProperty] string setLabel = "";
    [ObservableProperty] string weight = "";
    [ObservableProperty] string reps = "";
    [ObservableProperty] bool hasWeight;
    [ObservableProperty] bool canComplete;
    [ObservableProperty] string restText = "";
    [ObservableProperty] string restUntil = "";
    [ObservableProperty] double restProgress;
    [ObservableProperty] bool canGoBack;
    [ObservableProperty] bool canGoNext;
    [ObservableProperty] bool hasExercises;

    WorkoutSession? Session => _workouts.Active;
    SessionExercise? CurrentExercise => Session?.Exercises.ElementAtOrDefault(_exercise);
    SetEntry? CurrentSet => CurrentExercise?.Sets.FirstOrDefault(s => !s.IsCompleted);
    Exercise? Info(SessionExercise se) => _store.GetExercise(se.ExerciseId);

    public async Task StartAsync()
    {
        if (Session is not { } session)
        {
            Ended?.Invoke();
            return;
        }
        // Pick up where it left off: the first exercise with sets still to do (kept when coming back from adding one).
        if (CurrentExercise == null || CurrentSet == null)
            _exercise = Math.Max(0, session.Exercises.FindIndex(e => e.Sets.Any(s => !s.IsCompleted)));
        Name = session.Name;
        // Followed only while on screen: these outlive the page.
        _heart.Changed -= OnHeartRate;
        _heart.Changed += OnHeartRate;
        Ambient.Changed -= OnAmbientChanged;
        Ambient.Changed += OnAmbientChanged;
        Ambient.Tick -= Refresh;
        Ambient.Tick += Refresh;
        IsAmbient = Ambient.IsActive;
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
        catch (Exception)
        {
            HeartRate = "";
        }
    }

    public void Stop()
    {
        _timer?.Stop();
        _heart.Changed -= OnHeartRate;
        Ambient.Changed -= OnAmbientChanged;
        Ambient.Tick -= Refresh;
        try
        {
            _heart.Stop();
        }
        catch (Exception)
        {
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

    void Refresh()
    {
        if (Session is not { } session)
            return;
        var elapsed = DateTime.Now - session.StartedAt;
        // Seconds would be wrong for most of the minute between ambient redraws.
        Elapsed = IsAmbient ? $"{(int)elapsed.TotalMinutes} min" : Clock(elapsed);
        var working = session.Exercises.SelectMany(e => e.Sets).Where(s => !s.IsWarmup).ToList();
        Progress = $"{working.Count(s => s.IsCompleted)}/{working.Count} sets";
        HasExercises = session.Exercises.Count > 0;
        CanGoBack = _exercise > 0;
        CanGoNext = _exercise < session.Exercises.Count - 1;

        if (session.Exercises.Count == 0)
        {
            Show(empty: true);
            return;
        }

        if (_restEndsAt is { } ends)
        {
            var left = ends - DateTime.Now;
            if (left > TimeSpan.Zero)
            {
                RestText = Clock(left);
                RestUntil = $"until {ends:HH:mm}";
                RestProgress = _restTotal <= 0 ? 0 : left.TotalSeconds / _restTotal;
                ShowSet();
                Show(resting: true);
                return;
            }
            // Rest is over: buzz, so the wrist says when to lift again.
            _restEndsAt = null;
            try
            {
                if (WatchSettings.RestBuzz)
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
            }
            catch (Exception)
            {
            }
        }

        if (session.Exercises.All(e => e.Sets.All(s => s.IsCompleted)))
        {
            Show(allDone: true);
            return;
        }
        ShowSet();
        Show(lifting: true);
    }

    void ShowSet()
    {
        if (CurrentExercise is not { } se)
            return;
        var info = Info(se);
        ExerciseName = info?.Name ?? "Exercise";
        ExercisePosition = $"{_exercise + 1} of {Session!.Exercises.Count}";
        if (CurrentSet is not { } set)
        {
            // This exercise is done; the arrows lead to the others.
            SetLabel = "All sets done";
            Weight = Reps = "";
            HasWeight = CanComplete = false;
            return;
        }
        var ofKind = se.Sets.Where(s => s.IsWarmup == set.IsWarmup).ToList();
        SetLabel = $"{(set.IsWarmup ? "Warm-up" : "Set")} {ofKind.IndexOf(set) + 1} of {ofKind.Count}";
        HasWeight = info?.Equipment != Equipment.Bodyweight || set.WeightKg > 0;
        Weight = $"{_units.ToDisplay(set.WeightKg).ToString("0.#", CultureInfo.CurrentCulture)} {_units.Label}";
        Reps = $"{set.Reps} reps";
        CanComplete = set.Reps > 0;
    }

    void Show(bool lifting = false, bool resting = false, bool allDone = false, bool empty = false)
    {
        IsLifting = lifting;
        IsResting = resting;
        IsAllDone = allDone;
        IsEmpty = empty;
    }

    [RelayCommand]
    void ChangeWeight(string steps)
    {
        if (CurrentSet is not { } set || CurrentExercise is not { } se || Info(se) is not { } info)
            return;
        var weight = _units.Step(set.WeightKg, info, int.Parse(steps, CultureInfo.InvariantCulture));
        // The sets after it of the same kind follow, as they usually share the weight.
        foreach (var later in se.Sets.SkipWhile(s => s != set).Where(s => !s.IsCompleted && s.IsWarmup == set.IsWarmup))
            later.WeightKg = weight;
        _workouts.Save();
        Refresh();
    }

    [RelayCommand]
    void ChangeReps(string delta)
    {
        if (CurrentSet is not { } set)
            return;
        set.Reps = Math.Clamp(set.Reps + int.Parse(delta, CultureInfo.InvariantCulture), 0, 100);
        _workouts.Save();
        Refresh();
    }

    [RelayCommand]
    void Complete()
    {
        if (CurrentSet is not { Reps: > 0 } set || CurrentExercise is not { } se)
            return;
        var now = DateTime.Now;
        SetTimes.Complete(Session!, set, now);
        try
        {
            if (WatchSettings.TapFeedback)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception)
        {
        }
        // Rest as the phone would: the warm-up rest after a warm-up, the exercise's own after a working set.
        _restTotal = set.IsWarmup ? _workouts.WarmupsFor(Session).RestSeconds : se.RestSeconds;
        _restEndsAt = _store.Profile.AutoRestTimer && _restTotal > 0 ? now.AddSeconds(_restTotal) : null;
        if (_restEndsAt is { } restEnds)
            SetTimes.StartRest(Session!, set, now, restEnds);
        _workouts.Save();
        // This exercise done: on to the next one with sets left (the rest timer runs over it).
        if (!se.Sets.Any(s => !s.IsCompleted) && Session!.Exercises.FindIndex(_exercise + 1, e => e.Sets.Any(s => !s.IsCompleted)) is >= 0 and var next)
            _exercise = next;
        Refresh();
    }

    [RelayCommand]
    void SkipRest()
    {
        _restEndsAt = null;
        if (Session is { } session)
        {
            SetTimes.EndRest(session, DateTime.Now);
            _workouts.Save();
        }
        Refresh();
    }

    [RelayCommand]
    void PreviousExercise()
    {
        if (_exercise > 0)
            _exercise--;
        Refresh();
    }

    [RelayCommand]
    void NextExercise()
    {
        if (Session != null && _exercise < Session.Exercises.Count - 1)
            _exercise++;
        Refresh();
    }

    /// <summary>Adds an exercise from the catalogue, with the sets, rest and warm-ups the phone would give it, and shows it.</summary>
    [RelayCommand]
    async Task AddExercise()
    {
        if (Session is not { } session || await ExercisePickerPage.PickAsync(Page.Navigation) is not { } exercise)
            return;
        session.Exercises.Add(_workouts.CreateAdHoc(exercise, session));
        _workouts.Save();
        _exercise = session.Exercises.Count - 1;
        Refresh();
    }

    [RelayCommand]
    async Task Finish()
    {
        if (Session is not { } session)
            return;
        var done = session.Exercises.SelectMany(e => e.Sets).Count(s => s.IsCompleted && !s.IsWarmup);
        if (done == 0)
        {
            if (!await Page.DisplayAlertAsync("Discard workout?", "No sets are done yet.", "Discard", "Keep going"))
                return;
            _workouts.Discard();
        }
        else
        {
            if (!await Page.DisplayAlertAsync("Finish workout?", $"{done} sets done. Sets not done are left out.", "Finish", "Keep going"))
                return;
            // Worked out before finishing, which drops the sets that weren't done (as the phone does).
            var update = _workouts.ProposePlanUpdate(session);
            var finished = _workouts.Finish();
            // Exercises added, removed or changed: offer to keep them in the plan, as the phone does.
            if (finished != null && update != null && await Page.DisplayAlertAsync($"Update {update.Workout.Name}?",
                    "You changed this workout:\n• " + string.Join("\n• ", update.Changes.Take(4)) + (update.Changes.Count > 4 ? "\n…" : "") +
                    "\n\nUse these changes next time too?", "Update plan", "Keep plan"))
                _workouts.ApplyPlanUpdate(update);
        }
        WatchOwnership.SessionId = null;
        Ended?.Invoke();
    }

    /// <summary>The workout's options, as on the phone: minimize it, start it over, or throw it away.</summary>
    [RelayCommand]
    async Task Options()
    {
        if (Session is not { } session)
            return;
        const string minimize = "Minimize", reset = "Reset workout", discard = "Discard workout";
        switch (await Page.DisplayActionSheetAsync(session.Name, "Cancel", discard, minimize, reset))
        {
            case minimize:
                Minimized?.Invoke();
                break;
            case reset:
                if (!await Page.DisplayAlertAsync("Reset workout?", "Every set is unticked and the clock starts again. Exercises, weights and reps stay.", "Reset", "Cancel"))
                    return;
                foreach (var set in session.Exercises.SelectMany(e => e.Sets))
                    SetTimes.Uncomplete(set);
                session.StartedAt = DateTime.Now;
                _restEndsAt = null;
                _exercise = 0;
                _workouts.Save();
                Refresh();
                break;
            case discard:
                if (!await Page.DisplayAlertAsync("Discard workout?", "It's deleted, with every set done in it.", "Discard", "Keep"))
                    return;
                _workouts.Discard();
                WatchOwnership.SessionId = null;
                Ended?.Invoke();
                break;
        }
    }

    [RelayCommand]
    void Minimize() => Minimized?.Invoke();

    static Page Page => Application.Current!.Windows[0].Page!;

    static string Clock(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{(int)t.TotalMinutes}:{t.Seconds:00}";
}
