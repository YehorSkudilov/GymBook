using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public partial class WorkoutViewModel(
    DataStore store,
    WorkoutService workouts,
    ProgressionEngine engine,
    Units units,
    DialogService dialogs,
    ExercisePickerService picker,
    WatchLink watch,
    IWorkoutNotifier notifier) : BaseViewModel
{
    static bool _askedNotifications;
    IDispatcherTimer? _timer;
    WorkoutSession? _session;
    DateTime _restEndsAt;
    int _restTotal;
    /// <summary>The set the running rest comes after, which holds its times (see <see cref="SetTimes"/>).</summary>
    SetEntry? _restSet;

    public ObservableCollection<WorkoutExerciseViewModel> Exercises { get; } = [];

    [ObservableProperty] string name = "";
    [ObservableProperty] string elapsed = "00:00";
    [ObservableProperty] string progressText = "";
    /// <summary>Every set ticked: the Finish button shows in the header. Until then finishing is in the ··· menu.</summary>
    [ObservableProperty] bool canFinish;
    [ObservableProperty] double progress;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRestButton))]
    bool isResting;
    [ObservableProperty] string restText = "";
    [ObservableProperty] double restProgress;
    /// <summary>The rest ended (ran out or skipped) and the next set isn't done yet: how long since, counting up.</summary>
    [ObservableProperty] bool isOverRest;
    [ObservableProperty] string overRestText = "";
    /// <summary>The set whose time since rest was hidden; it shows again after the next rest.</summary>
    SetEntry? _overRestHidden;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRestButton))]
    bool isEmpty;

    /// <summary>The floating rest timer button: not while resting, and not before there's an exercise to rest between.</summary>
    public bool ShowRestButton => !IsResting && !IsEmpty;

    /// <summary>The exercise on screen; the page and the photo strip both follow it.</summary>
    [ObservableProperty] int currentIndex;
    // How long the finished exercise stays on screen after its last tick, so the tick is seen before moving on.
    const int AdvanceDelayMs = 700;

    public WorkoutExerciseViewModel? CurrentExercise => Exercises.ElementAtOrDefault(CurrentIndex);

    partial void OnCurrentIndexChanged(int value)
    {
        for (var i = 0; i < Exercises.Count; i++)
            Exercises[i].IsSelected = i == value;
        OnPropertyChanged(nameof(CurrentExercise));
    }

    internal Task SelectExercise(WorkoutExerciseViewModel vm)
    {
        var index = Exercises.IndexOf(vm);
        if (index >= 0)
            CurrentIndex = index;
        return Task.CompletedTask;
    }

    /// <summary>The next exercise after <paramref name="index"/> with sets still to do, wrapping round to earlier ones; null when all are done.</summary>
    int? NextOpen(int index)
    {
        for (var step = 1; step < Exercises.Count; step++)
        {
            var i = (index + step) % Exercises.Count;
            if (!Exercises[i].IsDone)
                return i;
        }
        return null;
    }

    /// <summary>
    /// After an exercise's last set is ticked: on to the next one with sets left (when that was the last one, the Finish
    /// button has appeared in the header). Nothing happens if the user moved or un-ticked meanwhile.
    /// </summary>
    async Task AdvanceAfterAsync(WorkoutExerciseViewModel finished)
    {
        await Task.Delay(AdvanceDelayMs);
        var index = Exercises.IndexOf(finished);
        if (index != CurrentIndex || !finished.IsDone)
            return;
        if (NextOpen(index) is { } next)
            CurrentIndex = next;
    }

    public string UnitLabel => units.Label.ToUpperInvariant();
    /// <summary>RIR is logged when the profile asks for it, except in workouts of a plan that doesn't use RIR.</summary>
    public bool TrackRir => store.Profile.TrackRir && store.GetPlan(_session?.PlanId)?.UseRir != false;

    internal Units Units => units;
    /// <summary>The warm-up settings of this workout's plan, or the profile's.</summary>
    internal WarmupSettings Warmups => workouts.WarmupsFor(_session);
    internal ProgressionEngine Engine => engine;
    internal DialogService Dialogs => dialogs;
    /// <summary>The goal the exercises are coached for: the plan's, or the profile's for a workout outside a plan.</summary>
    internal Goal Goal => store.GetPlan(_session?.PlanId)?.Goal ?? store.Profile.Goal;

    /// <summary>A workout of a plan day: its exercises' notes can be pinned, for the plan update offered on finishing.</summary>
    internal bool IsPlanWorkout => _session is { } session
        && store.GetPlan(session.PlanId)?.Workouts.Any(w => w.Id == session.PlanWorkoutId) == true;

    /// <summary>
    /// The note an exercise of this workout has in its plan day; null outside a plan, or for an exercise the day doesn't
    /// have. The same exercise twice in a day pairs up in order, as the plan update does.
    /// </summary>
    internal string? PlannedNote(SessionExercise se)
    {
        if (_session is not { } session || store.GetPlan(session.PlanId)?.Workouts.FirstOrDefault(w => w.Id == session.PlanWorkoutId) is not { } day)
            return null;
        var nth = session.Exercises.TakeWhile(e => e != se).Count(e => e.ExerciseId == se.ExerciseId);
        return day.Exercises.Where(e => e.ExerciseId == se.ExerciseId).ElementAtOrDefault(nth)?.Note;
    }

    internal void Save() => workouts.Save();

    IDispatcherTimer? _saveTimer;

    /// <summary>
    /// A weight, reps, RIR or note was typed: saved a moment after the typing stops, so it survives the app being closed
    /// (or a sync) without writing on every keystroke.
    /// </summary>
    internal void SaveSoon()
    {
        if (_saveTimer == null)
        {
            _saveTimer = Application.Current!.Dispatcher.CreateTimer();
            _saveTimer.Interval = TimeSpan.FromMilliseconds(700);
            _saveTimer.IsRepeating = false;
            _saveTimer.Tick += (_, _) =>
            {
                if (_session != null && workouts.Active == _session)
                    workouts.Save();
            };
        }
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public override async Task OnAppearingAsync()
    {
        // For the workout notification with the rest timer (Android 13+ asks); once per run of the app.
        if (!_askedNotifications)
        {
            _askedNotifications = true;
            try
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
            catch (Exception)
            {
            }
        }
        var active = workouts.Active;
        if (active == null)
        {
            // Finishing closes the page itself; a dialog closing on the way (the plan update) mustn't close it as well.
            if (!_finishing)
                await GoBack();
            return;
        }
        _finishing = false;
        if (_session != active)
        {
            _session = active;
            Name = active.Name;
            Exercises.Clear();
            foreach (var se in active.Exercises)
                AddExerciseVm(se);
            // Started empty (a quick workout) or everything removed: the empty state, not a blank exercise.
            IsEmpty = Exercises.Count == 0;
            // Pick up where the workout left off: the first exercise with sets still to do.
            var open = Exercises.ToList().FindIndex(e => !e.IsDone);
            CurrentIndex = Math.Max(0, open);
            OnCurrentIndexChanged(CurrentIndex);
            // A rest that was running when the app closed carries on until it's due.
            IsResting = false;
            if (SetTimes.RunningRest(active, DateTime.Now) is { RestStartedAt: { } restStarted, RestEndedAt: { } restEnds } resting)
            {
                _restSet = resting;
                _restEndsAt = restEnds;
                _restTotal = Math.Max(1, (int)Math.Round((restEnds - restStarted).TotalSeconds));
                IsResting = true;
            }
        }
        OnPropertyChanged(nameof(UnitLabel));
        OnPropertyChanged(nameof(TrackRir));
        UpdateProgress();
        Tick();

        _timer ??= Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
        // Ticks from the Wear OS app go through this page while it's open, so they work like a tap here.
        watch.LiveCompleteSet = CompleteFromWatch;
    }

    public override void OnDisappearing()
    {
        _timer?.Stop();
        if (watch.LiveCompleteSet == (Func<SetEntry, bool>)CompleteFromWatch)
            watch.LiveCompleteSet = null;
        if (workouts.Active != null)
            workouts.Save();
    }

    /// <summary>The Wear OS app ticked <paramref name="set"/>: the same as tapping its tick here, then showing its exercise.</summary>
    bool CompleteFromWatch(SetEntry set)
    {
        for (var i = 0; i < Exercises.Count; i++)
        {
            if (Exercises[i].Sets.FirstOrDefault(r => r.Model == set) is not { } row)
                continue;
            CurrentIndex = i;
            if (!row.IsCompleted)
                row.ToggleCommand.Execute(null);
            return row.IsCompleted;
        }
        return false;
    }

    void OnTick(object? sender, EventArgs e) => Tick();

    void Tick()
    {
        if (_session != null)
            Elapsed = Units.Clock(DateTime.Now - _session.StartedAt);
        UpdateNotification();
        UpdateOverRest();
        if (!IsResting)
            return;
        var left = _restEndsAt - DateTime.Now;
        if (left <= TimeSpan.Zero)
        {
            IsResting = false;
            UpdateOverRest();
            RestFinished?.Invoke();
            notifier.RestOver(NextSetText());
            UpdateNotification();
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(600));
            }
            catch
            {
                // Not every platform supports vibration.
            }
            return;
        }
        RestText = Units.Rest((int)Math.Ceiling(left.TotalSeconds));
        RestProgress = _restTotal <= 0 ? 0 : left.TotalSeconds / _restTotal;
    }

    /// <summary>The workout notification (Android): where it's at, and the rest timer while resting. Redrawn only on change.</summary>
    void UpdateNotification()
    {
        if (_session == null || workouts.Active != _session)
            return;
        notifier.Show(new WorkoutStatus(Name, _session.StartedAt, NextSetText(), ProgressText,
            IsResting ? _restEndsAt : null, _restTotal));
    }

    /// <summary>"Bench Press · set 2 of 4": the exercise on screen and its next set to do.</summary>
    string NextSetText()
    {
        if (CurrentExercise is not { } exercise)
            return IsEmpty ? "No exercises yet" : "Workout in progress";
        var working = exercise.Model.Sets.Where(s => !s.IsWarmup).ToList();
        var next = working.FindIndex(s => !s.IsCompleted);
        if (exercise.Model.Sets.FirstOrDefault(s => !s.IsCompleted) is { IsWarmup: true })
            return $"{exercise.Name} · warm-up";
        return next < 0 ? $"{exercise.Name} · done" : $"{exercise.Name} · set {next + 1} of {working.Count}";
    }

    WorkoutExerciseViewModel AddExerciseVm(SessionExercise se)
    {
        var ex = store.GetExercise(se.ExerciseId) ?? new Exercise { Id = se.ExerciseId, Name = "Unknown exercise" };
        var vm = new WorkoutExerciseViewModel(this, ex, se, _session!.Id);
        Exercises.Add(vm);
        IsEmpty = Exercises.Count == 0;
        return vm;
    }

    /// <summary>An exercise's last set was just done: the page celebrates it (the whole workout, when it was the last one).</summary>
    public event Action<WorkoutExerciseViewModel, bool>? ExerciseFinished;

    /// <summary>The rest timer ran out (not skipped): the page says it's time for the next set.</summary>
    public event Action? RestFinished;

    internal void OnSetToggled(WorkoutExerciseViewModel exercise, SetRowViewModel set)
    {
        // When it began and ended (and the rest before it, if one was running, ends now).
        if (_session != null && set.IsCompleted)
        {
            SetTimes.Complete(_session, set.Model, set.Model.CompletedAt ?? DateTime.Now);
            if (IsResting && _restSet != set.Model)
                IsResting = false;
        }
        else
        {
            SetTimes.Uncomplete(set.Model);
            if (_restSet == set.Model)
                IsResting = false;
        }
        if (set.IsCompleted && store.Profile.AutoRestTimer)
            StartRest(set.Model.IsWarmup ? Warmups.RestSeconds : exercise.Model.RestSeconds, set.Model);
        workouts.Save();
        UpdateProgress();
        UpdateOverRest();
        if (set.IsCompleted && !set.Model.IsWarmup && exercise.IsDone)
            ExerciseFinished?.Invoke(exercise, CanFinish);
        // Its last set done: move straight on to the next exercise (the rest timer keeps running over it).
        if (set.IsCompleted && exercise.IsDone && Exercises.IndexOf(exercise) == CurrentIndex)
            _ = AdvanceAfterAsync(exercise);
    }

    /// <summary>Sets of <paramref name="exercise"/> were skipped (or brought back): with none left to do, on to the next exercise.</summary>
    internal void OnSkipped(WorkoutExerciseViewModel exercise)
    {
        workouts.Save();
        UpdateProgress();
        if (exercise.IsDone && Exercises.IndexOf(exercise) == CurrentIndex && NextOpen(CurrentIndex) is { } next)
            CurrentIndex = next;
    }

    internal void OnStructureChanged()
    {
        workouts.Save();
        UpdateProgress();
        IsEmpty = Exercises.Count == 0;
    }

    /// <summary>
    /// Swaps an exercise for another from the catalogue, with the sets, rest and warm-ups an added one gets. Sets already
    /// done stay in the workout as done (they happened); the new exercise then comes right after what's left of it.
    /// </summary>
    internal async Task Replace(WorkoutExerciseViewModel old)
    {
        if (_session == null || await picker.PickOneAsync($"Replace {old.Name}", old.Exercise) is not { } ex)
            return;
        var index = Exercises.IndexOf(old);
        if (index < 0)
            return;
        var se = workouts.CreateAdHoc(ex, _session);
        var replacement = new WorkoutExerciseViewModel(this, ex, se, _session.Id);
        if (old.Model.Sets.Any(s => s.IsCompleted))
        {
            old.Model.Sets.RemoveAll(s => !s.IsCompleted);
            _session.Exercises.Insert(index + 1, se);
            // Rebuilt from its model, now only the done sets.
            Exercises[index] = new WorkoutExerciseViewModel(this, old.Exercise, old.Model, _session.Id);
            Exercises.Insert(index + 1, replacement);
            CurrentIndex = index + 1;
        }
        else
        {
            _session.Exercises[index] = se;
            Exercises[index] = replacement;
            CurrentIndex = index;
        }
        OnCurrentIndexChanged(CurrentIndex);
        OnStructureChanged();
    }

    internal void Remove(WorkoutExerciseViewModel vm)
    {
        _session?.Exercises.Remove(vm.Model);
        Exercises.Remove(vm);
        CurrentIndex = Math.Clamp(CurrentIndex, 0, Math.Max(0, Exercises.Count - 1));
        OnCurrentIndexChanged(CurrentIndex);
        OnStructureChanged();
    }

    void UpdateProgress()
    {
        var sets = Exercises.SelectMany(e => e.Sets).Where(s => !s.Model.IsWarmup).ToList();
        // Skipped sets are out of the count, but settled: with the rest done, the workout can be finished.
        var planned = sets.Where(s => !s.IsSkipped).ToList();
        var done = planned.Count(s => s.IsCompleted);
        ProgressText = $"{done}/{planned.Count} sets";
        Progress = planned.Count == 0 ? 0 : (double)done / planned.Count;
        CanFinish = sets.Count > 0 && sets.All(s => s.IsSettled);
    }

    /// <summary>Rests for <paramref name="seconds"/>, logged as the rest after <paramref name="after"/> (when there's a set before it).</summary>
    void StartRest(int seconds, SetEntry? after)
    {
        var now = DateTime.Now;
        _restTotal = seconds;
        _restEndsAt = now.AddSeconds(seconds);
        _restSet = after;
        if (_session != null)
        {
            if (after != null)
                SetTimes.StartRest(_session, after, now, _restEndsAt);
            else
                SetTimes.EndRest(_session, now);
        }
        IsResting = true;
        Tick();
    }

    [RelayCommand]
    void AdjustRest(string delta)
    {
        var d = int.Parse(delta);
        _restEndsAt = _restEndsAt.AddSeconds(d);
        _restTotal = Math.Max(1, _restTotal + d);
        if (_restSet != null)
        {
            SetTimes.MoveRestEnd(_restSet, _restEndsAt);
            workouts.Save();
        }
        Tick();
    }

    [RelayCommand]
    void SkipRest()
    {
        IsResting = false;
        if (_session != null)
        {
            SetTimes.EndRest(_session, DateTime.Now);
            workouts.Save();
        }
        UpdateOverRest();
    }

    /// <summary>
    /// Time since the last rest ended, while the next set isn't done: from the times logged (the last set done and when
    /// its rest ended), so it carries on after the app was closed. Not while resting, or once hidden.
    /// </summary>
    void UpdateOverRest()
    {
        var now = DateTime.Now;
        var last = _session?.Exercises.SelectMany(e => e.Sets).Where(s => s.IsCompleted && s.CompletedAt != null).MaxBy(s => s.CompletedAt);
        if (IsResting || last is not { RestStartedAt: not null, RestEndedAt: { } ended } || ended > now || last == _overRestHidden)
        {
            IsOverRest = false;
            return;
        }
        OverRestText = "+" + Units.Clock(now - ended);
        IsOverRest = true;
    }

    /// <summary>The ✕ on the time since rest: hides it until the next rest.</summary>
    [RelayCommand]
    void HideOverRest()
    {
        _overRestHidden = _session?.Exercises.SelectMany(e => e.Sets).Where(s => s.IsCompleted && s.CompletedAt != null).MaxBy(s => s.CompletedAt);
        IsOverRest = false;
    }

    /// <summary>The round timer button: rests for the current exercise's rest time, after the last set done.</summary>
    [RelayCommand]
    void StartRestNow()
    {
        var rest = Exercises.ElementAtOrDefault(CurrentIndex)?.Model.RestSeconds ?? store.Profile.CompoundRestSeconds ?? 120;
        var last = _session?.Exercises.SelectMany(e => e.Sets).Where(s => s.IsCompleted && s.CompletedAt != null).MaxBy(s => s.CompletedAt);
        StartRest(rest, last);
        workouts.Save();
    }

    /// <summary>The ··· in the header: a sheet with the workout's name, timing, finishing it and the logging settings.</summary>
    [RelayCommand]
    Task WorkoutMenu() => GoTo(Routes.WorkoutMenu, new Dictionary<string, object> { ["workout"] = this });

    internal DateTime? StartedAt => _session?.StartedAt;

    /// <summary>
    /// Moves the workout's start to <paramref name="start"/>, and every time logged in it with it (a running rest too).
    /// Null when done, else why not.
    /// </summary>
    internal string? ChangeStart(DateTime start)
    {
        if (_session == null)
            return null;
        var delta = start - _session.StartedAt;
        if (WorkoutService.ChangeStart(_session, start) is { } error)
            return error;
        if (IsResting)
            _restEndsAt += delta;
        workouts.Save();
        Tick();
        return null;
    }

    internal void SetName(string value)
    {
        if (_session == null || string.IsNullOrWhiteSpace(value))
            return;
        Name = _session.Name = value.Trim();
        workouts.Save();
    }

    /// <summary>After the logging settings change: the RIR column comes and goes with them.</summary>
    internal void RefreshSettings()
    {
        OnPropertyChanged(nameof(TrackRir));
        foreach (var exercise in Exercises)
            exercise.RefreshSettings();
    }

    [RelayCommand]
    async Task AddExercise()
    {
        var picked = await picker.PickAsync();
        if (_session == null || picked.Count == 0)
            return;
        foreach (var ex in picked)
        {
            var se = workouts.CreateAdHoc(ex, _session);
            _session.Exercises.Add(se);
            AddExerciseVm(se);
        }
        // Show the first one just added.
        CurrentIndex = Exercises.Count - picked.Count;
        OnCurrentIndexChanged(CurrentIndex);
        OnStructureChanged();
    }

    [RelayCommand]
    async Task Rename()
    {
        var value = await dialogs.Prompt("Rename workout", "Workout name", Name);
        if (value != null)
            SetName(value);
    }

    bool _finishing;

    /// <summary>Finishes the workout straight away (no confirmation); unfinished sets are dropped.</summary>
    [RelayCommand]
    async Task Finish()
    {
        var all = Exercises.SelectMany(e => e.Sets).ToList();
        var done = all.Count(s => s.IsCompleted);
        if (done == 0)
        {
            if (await dialogs.Confirm("No sets completed", "Tick off at least one set to save this workout. Discard it instead?", "Discard", "Keep training"))
            {
                workouts.Discard();
                await GoBack();
            }
            return;
        }
        // Worked out before finishing, which drops the sets that weren't done (not getting to one isn't a plan change).
        var update = _session == null ? null : workouts.ProposePlanUpdate(_session);
        IsResting = false;
        _finishing = true;
        var session = workouts.Finish();
        _session = null;
        // Changes made during the workout stay in it; the plan only takes them over when asked to.
        if (session != null && update != null && await dialogs.Confirm($"Update {update.Workout.Name} in your plan?",
                "You changed this workout:\n• " + string.Join("\n• ", update.Changes) +
                "\n\nUse these changes next time too? This workout's record keeps what you did either way.",
                "Update plan", "Keep plan as is"))
            workouts.ApplyPlanUpdate(update);
        if (session != null)
            // A celebration first; its Continue opens the finished workout (what was done, how it compares, the fatigue).
            await GoTo($"../{Routes.WorkoutDone}?session={session.Id}");
        else
            await GoBack();
    }

    /// <summary>
    /// Starts the workout over: every set unticked, the clock from now, back to the first exercise. The exercises and the
    /// weights and reps filled in stay.
    /// </summary>
    [RelayCommand]
    async Task Reset()
    {
        if (_session == null || !await dialogs.Confirm("Reset workout?", "Every set is unticked, skipped sets come back and the clock starts again. Exercises, weights and reps stay.", "Reset", "Cancel"))
            return;
        IsResting = false;
        foreach (var set in _session.Exercises.SelectMany(e => e.Sets))
            SetTimes.Uncomplete(set);
        _restSet = null;
        // Skipped sets come back too (skipping is only on the rows, rebuilt below).
        foreach (var e in _session.Exercises)
            (e.SkippedWarmups, e.SkippedSets) = (0, 0);
        _session.StartedAt = DateTime.Now;
        workouts.Save();

        // Rebuilt from the session, so every row, warm-up fold and piece of advice starts fresh too.
        Exercises.Clear();
        foreach (var se in _session.Exercises)
            AddExerciseVm(se);
        IsEmpty = Exercises.Count == 0;
        CurrentIndex = 0;
        OnCurrentIndexChanged(CurrentIndex);
        UpdateProgress();
        Tick();
    }

    [RelayCommand]
    async Task Discard()
    {
        if (!await dialogs.Confirm("Discard workout?", "All sets logged in this workout will be lost.", "Discard", "Cancel"))
            return;
        IsResting = false;
        workouts.Discard();
        _session = null;
        await GoBack();
    }

    [RelayCommand]
    Task Minimize() => GoBack();
}

/// <summary>One page of the workout: an exercise with its sets, advice, and what was lifted last time.</summary>
public partial class WorkoutExerciseViewModel : ObservableObject
{
    readonly WorkoutViewModel _parent;

    public WorkoutExerciseViewModel(WorkoutViewModel parent, Exercise exercise, SessionExercise model, string sessionId)
    {
        _parent = parent;
        Exercise = exercise;
        Model = model;
        Thumb = ExerciseThumb.For(exercise);

        if (parent.Engine.LastSession(exercise.Id, sessionId) is { } last)
        {
            var work = last.Exercise.Sets.Where(s => s.IsCompleted && !s.IsWarmup).ToList();
            PreviousTitle = $"{last.Session.StartedAt:ddd, d MMM} · {last.Session.Name}";
            // Its skipped sets too, dimmed, as everywhere else.
            PreviousRows = [.. work.Select((s, i) => new PlanDaySetRow($"{i + 1}", WeightText(s), $"{s.Reps}", E1RmText(s))),
                .. Enumerable.Range(work.Count + 1, last.Exercise.SkippedSets).Select(n => new PlanDaySetRow($"{n}", "Skipped", "–", "N/A") { IsSkipped = true })];
        }
        foreach (var set in model.Sets)
            Sets.Add(new SetRowViewModel(this, set));
        // Warm-ups start folded away once they're done, and open while there are some left to do.
        showWarmups = model.Sets.Any(s => s.IsWarmup && !s.IsCompleted);
        note = model.Note ?? "";
        Renumber();
    }

    /// <summary>The user's note on the exercise in this workout; kept with it like the sets typed in.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinStatus), nameof(HasPinStatus))]
    string note = "";

    /// <summary>
    /// The pin: marks the note to become the exercise's note in the plan (or, unpinning one the plan gave it, to come
    /// off). Nothing changes in the plan until finishing, when it's among the changes offered for the plan.
    /// </summary>
    public bool IsPinned => Model.NotePinned;
    public bool CanPin => _parent.IsPlanWorkout;
    public Color PinColor => IsPinned ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#626B7E");

    /// <summary>What the pin will do to the plan, under the note; empty when it changes nothing.</summary>
    public string PinStatus
    {
        get
        {
            var planned = _parent.PlannedNote(Model);
            return IsPinned && Model.Note != null && Model.Note == planned ? "Pinned: this note is in your plan."
                : IsPinned && Model.Note != null ? "Pinned: when you finish, choose Update plan to keep this note on the exercise in your plan."
                : planned != null && Model.Note != planned ? "When you finish, choose Update plan to take this note off the exercise in your plan."
                : "";
        }
    }
    public bool HasPinStatus => PinStatus.Length > 0;

    partial void OnNoteChanged(string value)
    {
        Model.Note = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        _parent.SaveSoon();
    }

    internal void SaveSoon() => _parent.SaveSoon();

    [RelayCommand]
    async Task TogglePin()
    {
        if (!CanPin)
            return;
        if (!IsPinned && Model.Note == null)
        {
            await _parent.Dialogs.Alert("Write a note first",
                "Pin a note to keep it on this exercise in your plan: when you finish, choose Update plan and it'll be there each time you do this workout.");
            return;
        }
        Model.NotePinned = !Model.NotePinned;
        _parent.Save();
        OnPropertyChanged(nameof(IsPinned));
        OnPropertyChanged(nameof(PinColor));
        OnPropertyChanged(nameof(PinStatus));
        OnPropertyChanged(nameof(HasPinStatus));
    }

    public Exercise Exercise { get; }
    public SessionExercise Model { get; }
    public ExerciseThumb Thumb { get; }
    public ObservableCollection<SetRowViewModel> Sets { get; } = [];
    internal Units Units => _parent.Units;

    public string Name => Exercise.Name;
    public string TargetText => TrackRir
        ? $"{Exercise.Equipment.Display()} · {Model.RepMin}–{Model.RepMax} reps · {Model.TargetRir} RIR · rest {Units.Rest(Model.RestSeconds)}"
        : $"{Exercise.Equipment.Display()} · {Model.RepMin}–{Model.RepMax} reps · rest {Units.Rest(Model.RestSeconds)}";
    public string Recommendation => Model.Recommendation ?? "";
    public bool HasRecommendation => !string.IsNullOrEmpty(Model.Recommendation);
    /// <summary>The main coaching cue for this exercise and goal; the rest are on the exercise's page.</summary>
    public string Tip => TrainingGoals.Tips(_parent.Goal, Exercise, Model.RepMin, Model.RepMax, Model.RestSeconds).FirstOrDefault() ?? "";
    public bool HasTip => Tip.Length > 0;
    public bool TrackRir => _parent.TrackRir;
    public string UnitLabel => _parent.UnitLabel;

    internal void RefreshSettings()
    {
        OnPropertyChanged(nameof(TrackRir));
        OnPropertyChanged(nameof(TargetText));
        foreach (var s in Sets)
            s.RefreshSettings();
    }

    /// <summary>The set table of the last session with this exercise.</summary>
    public string PreviousTitle { get; } = "";
    public List<PlanDaySetRow> PreviousRows { get; } = [];
    public bool HasPrevious => PreviousRows.Count > 0;

    /// <summary>Feedback on the last working set: under the minimum or over the maximum, and what the next set changed to.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveAdvice))]
    string liveAdvice = "";
    public bool HasLiveAdvice => LiveAdvice.Length > 0;

    /// <summary>Whether this is the exercise on screen; its photo is highlighted in the strip.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbStroke))]
    bool isSelected;
    public Color ThumbStroke => IsSelected ? Color.FromArgb("#3F7DFF") : Colors.Transparent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WarmupText))]
    bool showWarmups;
    public bool HasWarmups => Model.Sets.Any(s => s.IsWarmup);
    public bool HasOpenWarmups => Sets.Any(s => s.Model.IsWarmup && !s.IsSettled);
    /// <summary>Warm-ups not done yet, skipped or not: the Skip warm-ups button skips them, or brings them back.</summary>
    public bool HasUndoneWarmups => Sets.Any(s => s.Model.IsWarmup && !s.IsCompleted);
    public string SkipWarmupsText => HasUndoneWarmups && !HasOpenWarmups ? "Don't skip warm-ups" : "Skip warm-ups";
    public string WarmupText
    {
        get
        {
            var count = Model.Sets.Count(s => s.IsWarmup);
            return $"{count} warm-up set{(count == 1 ? "" : "s")} {(ShowWarmups ? "▴" : "▾")}";
        }
    }

    /// <summary>Every working set done or skipped.</summary>
    public bool IsDone => Sets.Where(s => !s.Model.IsWarmup).All(s => s.IsSettled) && Sets.Any(s => !s.Model.IsWarmup);
    /// <summary>Every working set left was skipped: the whole exercise is.</summary>
    public bool IsSkipped => Sets.Where(s => !s.Model.IsWarmup && !s.IsCompleted).Any() && Sets.Where(s => !s.Model.IsWarmup && !s.IsCompleted).All(s => s.IsSkipped);
    public string SkipExerciseText => IsSkipped ? "Don't skip exercise" : "Skip exercise";
    public bool CanSkipExercise => Sets.Any(s => !s.Model.IsWarmup && !s.IsCompleted);

    partial void OnShowWarmupsChanged(bool value)
    {
        foreach (var s in Sets)
            s.RefreshVisibility();
        UpdateCurrent();
    }

    [RelayCommand]
    void ToggleWarmups() => ShowWarmups = !ShowWarmups;

    [RelayCommand]
    Task Select() => _parent.SelectExercise(this);

    [RelayCommand]
    Task Help() => Shell.Current.GoToAsync($"{Routes.Exercise}?id={Exercise.Id}&tab=instructions");

    internal string WeightText(SetEntry s) => Exercise.IsBodyweight && s.WeightKg <= 0 ? "BW" : Units.Format(s.WeightKg);

    internal string E1RmText(SetEntry s)
    {
        var e1 = s.IsWarmup ? 0 : ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir);
        return e1 > 0 ? Units.Format(e1) : "–";
    }

    internal void Renumber()
    {
        // Warm-ups W1, W2…, working sets 1, 2…
        var (n, w) = (0, 0);
        foreach (var s in Sets)
            s.Label = s.Model.IsWarmup ? $"W{++w}" : (++n).ToString();
        OnPropertyChanged(nameof(HasWarmups));
        OnWarmupsChanged();
        OnPropertyChanged(nameof(WarmupText));
        OnPropertyChanged(nameof(IsSkipped));
        OnPropertyChanged(nameof(SkipExerciseText));
        OnPropertyChanged(nameof(CanSkipExercise));
        foreach (var s in Sets)
            s.RefreshVisibility();
        UpdateCurrent();
    }

    /// <summary>The set being logged: <paramref name="chosen"/> when the user tapped one, otherwise the first open one shown.</summary>
    internal void UpdateCurrent(SetRowViewModel? chosen = null)
    {
        var current = chosen ?? Sets.FirstOrDefault(s => s.IsVisible && !s.IsSettled);
        foreach (var s in Sets)
            s.IsCurrent = s == current;
    }

    internal void OnToggled(SetRowViewModel row)
    {
        // Carry the completed values into the next open set, as a starting point.
        if (row.IsCompleted)
        {
            var next = Sets.SkipWhile(s => s != row).Skip(1).FirstOrDefault(s => !s.IsCompleted && s.Model.IsWarmup == row.Model.IsWarmup);
            if (next != null && next.Model.WeightKg <= 0 && row.Model.WeightKg > 0)
                next.WeightText = row.WeightText;

            // Out of the rep range: say so. Only advice: the working sets keep the weights and reps from history (or as
            // typed), and the user changes them.
            if (!row.Model.IsWarmup)
                LiveAdvice = _parent.Engine.AfterSet(Exercise, row.Model, Model.RepMin, Model.RepMax, Model.TargetRir)?.Advice ?? "";
            // Warm-ups done: fold them away and move on to the working sets.
            if (row.Model.IsWarmup && Model.Sets.Where(s => s.IsWarmup).All(s => s.IsCompleted))
                ShowWarmups = false;
        }
        OnWarmupsChanged();
        OnPropertyChanged(nameof(IsSkipped));
        OnPropertyChanged(nameof(SkipExerciseText));
        OnPropertyChanged(nameof(CanSkipExercise));
        UpdateCurrent();
        _parent.OnSetToggled(this, row);
    }

    internal async Task SetMenu(SetRowViewModel row)
    {
        var toggle = row.Model.IsWarmup ? "Mark as working set" : "Mark as warm-up";
        var skip = row.Model.IsWarmup ? (row.IsSkipped ? "Don't skip warm-up" : "Skip warm-up") : row.IsSkipped ? "Don't skip set" : "Skip set";
        string[] options = !row.IsCompleted ? [skip, toggle] : [toggle];
        var choice = await _parent.Dialogs.ActionSheet($"Set {row.Label}", "Delete set", options);
        if (choice == skip)
        {
            Skip(row);
            return;
        }
        if (choice == "Delete set")
        {
            Model.Sets.Remove(row.Model);
            Sets.Remove(row);
        }
        else if (choice == toggle)
        {
            row.Model.IsWarmup = !row.Model.IsWarmup;
        }
        else
        {
            return;
        }
        Renumber();
        _parent.OnStructureChanged();
    }

    /// <summary>
    /// Skips a set, or brings a skipped one back. It stays in the table, dimmed and marked skipped, and is left out of
    /// the workout when it's finished (a skipped warm-up is counted for the exercise's history).
    /// </summary>
    internal void Skip(SetRowViewModel row)
    {
        if (row.IsCompleted)
            return;
        row.IsSkipped = !row.IsSkipped;
        OnSkipsChanged();
    }

    /// <summary>Skips every set still to do, warm-ups included, or, when the working sets are all skipped already, brings them all back.</summary>
    [RelayCommand]
    void SkipExercise()
    {
        if (!Sets.Any(s => !s.Model.IsWarmup && !s.IsCompleted))
            return;
        var skip = !IsSkipped;
        // Warm-ups too: they stay in the table, marked skipped, like the working sets.
        foreach (var s in Sets.Where(s => !s.IsCompleted))
            s.IsSkipped = skip;
        OnSkipsChanged();
    }

    void OnSkipsChanged()
    {
        OnWarmupsChanged();
        OnPropertyChanged(nameof(IsSkipped));
        OnPropertyChanged(nameof(SkipExerciseText));
        OnPropertyChanged(nameof(CanSkipExercise));
        UpdateCurrent();
        _parent.OnSkipped(this);
    }

    /// <summary>
    /// Skips every warm-up still to do, going straight to the working sets; they stay in the table, marked skipped. When
    /// they're all skipped already, brings them back.
    /// </summary>
    [RelayCommand]
    void SkipWarmups()
    {
        var undone = Sets.Where(s => s.Model.IsWarmup && !s.IsCompleted).ToList();
        if (undone.Count == 0)
            return;
        var skip = HasOpenWarmups;
        foreach (var s in undone)
            s.IsSkipped = skip;
        OnSkipsChanged();
    }

    void OnWarmupsChanged()
    {
        OnPropertyChanged(nameof(HasOpenWarmups));
        OnPropertyChanged(nameof(HasUndoneWarmups));
        OnPropertyChanged(nameof(SkipWarmupsText));
    }

    // The column headings: one change for every set still to do (working sets not done or skipped; warm-ups keep theirs).
    List<SetRowViewModel> OpenSets => [.. Sets.Where(s => !s.Model.IsWarmup && !s.IsSettled)];

    /// <summary>The # heading: skip, bring back or delete the sets still to do, or add one.</summary>
    [RelayCommand]
    async Task SetsHeader()
    {
        var open = OpenSets;
        var skipped = Sets.Where(s => s.IsSkipped).ToList();
        var options = new List<string>();
        if (open.Count > 0)
            options.Add(open.Count == 1 ? "Skip the set left" : $"Skip the {open.Count} sets left");
        if (skipped.Count > 0)
            options.Add(skipped.Count == 1 ? "Don't skip the skipped set" : $"Don't skip the {skipped.Count} skipped sets");
        options.Add("Add a set");
        var remove = open.Count + skipped.Count > 0 ? "Delete sets not done" : null;
        var choice = await _parent.Dialogs.ActionSheet("All sets", remove, [.. options]);
        if (choice == null)
            return;
        if (choice.StartsWith("Skip"))
            SkipExercise();
        else if (choice.StartsWith("Don't"))
        {
            foreach (var s in skipped)
                s.IsSkipped = false;
            OnSkipsChanged();
        }
        else if (choice == "Add a set")
            AddSet();
        else if (choice == remove && await _parent.Dialogs.Confirm("Delete sets not done?",
                     "Every working set that isn't ticked is removed from this exercise.", "Delete"))
        {
            foreach (var s in open.Concat(skipped))
            {
                Model.Sets.Remove(s.Model);
                Sets.Remove(s);
            }
            Renumber();
            _parent.OnStructureChanged();
        }
    }

    /// <summary>The weight heading: one weight for all the sets still to do, or nudged up or down together.</summary>
    [RelayCommand]
    async Task WeightHeader()
    {
        var open = OpenSets;
        if (open.Count == 0)
        {
            await NothingOpen();
            return;
        }
        const string change = "Add or subtract…";
        var choice = await _parent.Dialogs.ActionSheet($"Weight for the {Count(open)} left", null,
            "Set one weight for all", change, "Same as the last set done");
        switch (choice)
        {
            case "Set one weight for all":
                var text = await _parent.Dialogs.Prompt("Weight for all", $"In {Units.Label}, for every set still to do", open[0].WeightText, Keyboard.Numeric, "Apply");
                if (text != null && Units.TryParse(text, out var kg))
                    foreach (var s in open)
                        s.WeightText = Units.Format(kg);
                break;
            case "Same as the last set done":
                if (Sets.LastOrDefault(s => s.IsCompleted && !s.Model.IsWarmup) is { } last)
                    foreach (var s in open)
                        s.WeightText = last.WeightText;
                break;
            case change:
                // Typed in the unit shown, stepped by the exercise's usual increment (e.g. 2.5 kg for a barbell).
                if (await _parent.Dialogs.Change($"Change the {Count(open)} left", "The same amount on or off each set still to do",
                        Units.Label, Units.Increment(Exercise)) is not { } delta || delta == 0)
                    return;
                var deltaKg = Units.FromDisplay(delta);
                foreach (var s in open)
                    s.WeightText = Units.Format(Math.Max(0, s.Model.WeightKg + deltaKg));
                break;
            default:
                return;
        }
        _parent.OnStructureChanged();
    }

    /// <summary>The reps heading: one rep count for all the sets still to do.</summary>
    [RelayCommand]
    async Task RepsHeader()
    {
        var open = OpenSets;
        if (open.Count == 0)
        {
            await NothingOpen();
            return;
        }
        var choice = await _parent.Dialogs.ActionSheet($"Reps for the {Count(open)} left", null,
            $"Bottom of the range ({Model.RepMin})", $"Top of the range ({Model.RepMax})", "Set one rep count for all");
        int? reps = choice switch
        {
            null => null,
            _ when choice.StartsWith("Bottom") => Model.RepMin,
            _ when choice.StartsWith("Top") => Model.RepMax,
            _ => int.TryParse(await _parent.Dialogs.Prompt("Reps for all", "For every set still to do", open[0].RepsText, Keyboard.Numeric, "Apply"), out var r) && r > 0 ? r : null,
        };
        if (reps is not { } value)
            return;
        foreach (var s in open)
            s.RepsText = value.ToString();
        _parent.OnStructureChanged();
    }

    /// <summary>The RIR heading: one effort for all the sets still to do.</summary>
    [RelayCommand]
    async Task RirHeader()
    {
        var open = OpenSets;
        if (open.Count == 0)
        {
            await NothingOpen();
            return;
        }
        var choice = await _parent.Dialogs.ActionSheet($"RIR for the {Count(open)} left", null,
            $"The target ({Model.TargetRir} RIR)", "0 · to failure", "1", "2", "3", "4", "Clear");
        if (choice == null)
            return;
        var text = choice.StartsWith("The target") ? Model.TargetRir.ToString() : choice == "Clear" ? "" : choice[..1];
        foreach (var s in open)
            s.RirText = text;
        _parent.OnStructureChanged();
    }

    /// <summary>The E1RM heading: what the number is, and this exercise's best.</summary>
    [RelayCommand]
    async Task E1RmHeader()
    {
        var best = Sets.Where(s => s.IsCompleted && !s.Model.IsWarmup)
            .Select(s => ProgressionEngine.E1Rm(s.Model.WeightKg, s.Model.Reps, s.Model.Rir)).DefaultIfEmpty(0).Max();
        var choice = await _parent.Dialogs.ActionSheet("Estimated 1RM", null,
            best > 0 ? [$"Best today: {Units.FormatWithUnit(best)}", "What's this?", "Exercise details"] : ["What's this?", "Exercise details"]);
        if (choice == "What's this?")
            await _parent.Dialogs.Alert("Estimated one-rep max",
                "The most you could lift once, worked out from each set's weight, reps and RIR. It's how progress is compared across rep ranges.");
        else if (choice == "Exercise details")
            await Help();
    }

    static string Count(List<SetRowViewModel> sets) => sets.Count == 1 ? "set" : $"{sets.Count} sets";

    Task NothingOpen() => _parent.Dialogs.Alert("No sets left to do", "Every set of this exercise is done or skipped. Add a set to log another.");

    [RelayCommand]
    void AddSet()
    {
        var last = Model.Sets.LastOrDefault(s => !s.IsWarmup) ?? Model.Sets.LastOrDefault();
        var set = new SetEntry { WeightKg = last?.WeightKg ?? 0, Reps = last?.Reps ?? Model.RepMin };
        Model.Sets.Add(set);
        Sets.Add(new SetRowViewModel(this, set));
        Renumber();
        _parent.OnStructureChanged();
    }

    [RelayCommand]
    async Task Menu()
    {
        string[] options = CanSkipExercise
            ? [SkipExerciseText, "Replace exercise", "Exercise details", "Change rest time"]
            : ["Replace exercise", "Exercise details", "Change rest time"];
        var choice = await _parent.Dialogs.ActionSheet(Name, "Remove exercise", options);
        if (choice == SkipExerciseText)
        {
            SkipExercise();
            return;
        }
        switch (choice)
        {
            case "Remove exercise":
                if (await _parent.Dialogs.Confirm("Remove exercise?", $"Remove {Name} and its sets from this workout?", "Remove"))
                    _parent.Remove(this);
                break;
            case "Replace exercise":
                await _parent.Replace(this);
                break;
            case "Exercise details":
                await Help();
                break;
            case "Change rest time":
                if (await _parent.Dialogs.RestTime($"{Name} · rest between sets", Model.RestSeconds) is { } seconds)
                {
                    Model.RestSeconds = seconds;
                    OnPropertyChanged(nameof(TargetText));
                    OnPropertyChanged(nameof(Tip));
                    _parent.OnStructureChanged();
                }
                break;
        }
    }

}

/// <summary>A row of the set table. Only the current row is edited in place; tapping another row makes it current.</summary>
public partial class SetRowViewModel : ObservableObject
{
    readonly WorkoutExerciseViewModel _parent;

    public SetRowViewModel(WorkoutExerciseViewModel parent, SetEntry model)
    {
        _parent = parent;
        Model = model;
        weightText = model.WeightKg > 0 || !parent.Exercise.IsBodyweight ? parent.Units.Format(model.WeightKg) : "";
        repsText = model.Reps > 0 ? model.Reps.ToString() : "";
        rirText = model.Rir?.ToString() ?? "";
        isCompleted = model.IsCompleted;
    }

    public SetEntry Model { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabelColor), nameof(ShowE1Rm))]
    string label = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(E1Rm), nameof(WeightDisplay))]
    string weightText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(E1Rm), nameof(RepsDisplay))]
    string repsText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(E1Rm), nameof(RirDisplay))]
    string rirText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckBackground), nameof(CheckIcon), nameof(CheckStroke), nameof(ValueColor), nameof(ShowDoneTick), nameof(ShowE1Rm),
        nameof(IsSettled), nameof(RowBackground), nameof(RowStroke), nameof(NumberBackground), nameof(LabelColor), nameof(NumberOpacity))]
    bool isCompleted;

    /// <summary>Won't be done this time: dimmed, not counted, and left out of the workout when it's finished. Not a plan change.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowOpacity), nameof(ShowE1Rm), nameof(ShowSkipped), nameof(IsSettled), nameof(RowBackground), nameof(RowStroke))]
    bool isSkipped;

    /// <summary>Nothing left to do on it: done or skipped.</summary>
    public bool IsSettled => IsCompleted || IsSkipped;
    public double RowOpacity => IsSkipped ? 0.4 : 1;
    public bool ShowSkipped => IsSkipped && !IsCurrent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotCurrent), nameof(ShowRirInput), nameof(ShowRirValue), nameof(ShowDoneTick), nameof(NumberOpacity), nameof(ShowE1Rm), nameof(ShowSkipped),
        nameof(RowBackground), nameof(RowStroke), nameof(NumberBackground), nameof(ValueColor))]
    bool isCurrent;
    public bool IsNotCurrent => !IsCurrent;
    public bool ShowRirInput => IsCurrent && TrackRir;
    public bool ShowRirValue => !IsCurrent && TrackRir;
    public bool ShowDoneTick => !IsCurrent && IsCompleted;
    public double NumberOpacity => IsCurrent || IsCompleted ? 1 : 0.6;
    /// <summary>A skipped set says "Skipped" where its tick would be, and N/A for its E1RM. (Skipping is in the set number's menu.)</summary>
    public bool ShowE1Rm => !ShowSkipped;

    [ObservableProperty] bool isVisible = true;

    public string WeightDisplay => string.IsNullOrWhiteSpace(WeightText) ? (_parent.Exercise.IsBodyweight ? "BW" : "–") : WeightText;
    public string RepsDisplay => string.IsNullOrWhiteSpace(RepsText) ? "–" : RepsText;
    public string RirDisplay => string.IsNullOrWhiteSpace(RirText) ? "–" : RirText;
    public string E1Rm => _parent.E1RmText(Model);
    // Each set's state at a glance: done rows are tinted green with a green number and tick; the one being logged is
    // outlined in blue with a solid green button to tick it; the ones still to do are dimmed; skipped ones fade out.
    static readonly Color Green = Color.FromArgb("#2ED47A"), GreenSoft = Color.FromArgb("#133526"), GreenTint = Color.FromArgb("#0F2ED47A"),
        Blue = Color.FromArgb("#3F7DFF"), Pending = Color.FromArgb("#626B7E");

    /// <summary>The current set's button: solid green to tick it; once done, a quiet green tick (tap to undo).</summary>
    public Color CheckBackground => IsCompleted ? GreenSoft : Green;
    public Color CheckStroke => IsCompleted ? Green : Colors.Transparent;
    public ImageSource CheckIcon => (ImageSource)Application.Current!.Resources[IsCompleted ? "IconCheckGreen" : "IconCheck"];
    public Color RowBackground => IsCompleted && !IsCurrent ? GreenTint : IsCurrent ? Color.FromArgb("#151821") : Colors.Transparent;
    public Color RowStroke => IsCurrent ? Blue : Colors.Transparent;
    public Color NumberBackground => IsCompleted ? GreenSoft : IsCurrent ? Color.FromArgb("#1A2A4F") : Color.FromArgb("#1D212C");
    public Color LabelColor => IsCompleted ? Green : Model.IsWarmup ? Color.FromArgb("#FFB020") : IsCurrent ? Color.FromArgb("#F4F6FB") : Color.FromArgb("#9AA3B5");
    public Color ValueColor => IsCompleted ? Green : Pending;
    public bool TrackRir => _parent.TrackRir;

    internal void RefreshSettings()
    {
        OnPropertyChanged(nameof(TrackRir));
        OnPropertyChanged(nameof(ShowRirInput));
        OnPropertyChanged(nameof(ShowRirValue));
    }

    internal void RefreshVisibility() => IsVisible = !Model.IsWarmup || _parent.ShowWarmups;

    // Each edit is kept straight away (see WorkoutViewModel.SaveSoon), not only when the set is ticked.
    partial void OnWeightTextChanged(string value)
    {
        if (_parent.Units.TryParse(value, out var kg))
            Model.WeightKg = kg;
        else if (string.IsNullOrWhiteSpace(value))
            Model.WeightKg = 0;
        _parent.SaveSoon();
    }

    partial void OnRepsTextChanged(string value)
    {
        if (int.TryParse(value, out var reps) && reps >= 0)
            Model.Reps = reps;
        _parent.SaveSoon();
    }

    partial void OnRirTextChanged(string value)
    {
        Model.Rir = int.TryParse(value, out var rir) && rir >= 0 ? rir : null;
        _parent.SaveSoon();
    }

    [RelayCommand]
    void Toggle()
    {
        if (!IsCompleted && Model.Reps <= 0)
            return;
        IsSkipped = false;
        IsCompleted = !IsCompleted;
        Model.IsCompleted = IsCompleted;
        Model.CompletedAt = IsCompleted ? DateTime.Now : null;
        _parent.OnToggled(this);
    }

    /// <summary>Tapping a row other than the current one opens it for editing.</summary>
    [RelayCommand]
    void Edit() => _parent.UpdateCurrent(this);

    [RelayCommand]
    Task Menu() => _parent.SetMenu(this);
}
