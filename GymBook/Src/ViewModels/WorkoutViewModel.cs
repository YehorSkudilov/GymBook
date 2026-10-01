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
    WatchLink watch) : BaseViewModel
{
    IDispatcherTimer? _timer;
    WorkoutSession? _session;
    DateTime _restEndsAt;
    int _restTotal;

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

    public override async Task OnAppearingAsync()
    {
        var active = workouts.Active;
        if (active == null)
        {
            await GoBack();
            return;
        }
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
        if (!IsResting)
            return;
        var left = _restEndsAt - DateTime.Now;
        if (left <= TimeSpan.Zero)
        {
            IsResting = false;
            RestFinished?.Invoke();
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
        workouts.Save();
        UpdateProgress();
        if (set.IsCompleted && !set.Model.IsWarmup && exercise.IsDone)
            ExerciseFinished?.Invoke(exercise, CanFinish);
        if (set.IsCompleted && store.Profile.AutoRestTimer)
            StartRest(set.Model.IsWarmup ? Warmups.RestSeconds : exercise.Model.RestSeconds);
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

    void StartRest(int seconds)
    {
        _restTotal = seconds;
        _restEndsAt = DateTime.Now.AddSeconds(seconds);
        IsResting = true;
        Tick();
    }

    [RelayCommand]
    void AdjustRest(string delta)
    {
        var d = int.Parse(delta);
        _restEndsAt = _restEndsAt.AddSeconds(d);
        _restTotal = Math.Max(1, _restTotal + d);
        Tick();
    }

    [RelayCommand]
    void SkipRest() => IsResting = false;

    /// <summary>The round timer button: rests for the current exercise's rest time.</summary>
    [RelayCommand]
    void StartRestNow()
    {
        var rest = Exercises.ElementAtOrDefault(CurrentIndex)?.Model.RestSeconds ?? store.Profile.CompoundRestSeconds ?? 120;
        StartRest(rest);
    }

    /// <summary>The ··· in the header: a sheet with the workout's name, timing, finishing it and the logging settings.</summary>
    [RelayCommand]
    Task WorkoutMenu() => GoTo(Routes.WorkoutMenu, new Dictionary<string, object> { ["workout"] = this });

    internal DateTime? StartedAt => _session?.StartedAt;

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
        if (_session == null || !await dialogs.Confirm("Reset workout?", "Every set is unticked and the clock starts again. Exercises, weights and reps stay.", "Reset", "Cancel"))
            return;
        IsResting = false;
        foreach (var set in _session.Exercises.SelectMany(e => e.Sets))
        {
            set.IsCompleted = false;
            set.CompletedAt = null;
        }
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
            PreviousRows = [.. work.Select((s, i) => new PlanDaySetRow($"{i + 1}", WeightText(s), $"{s.Reps}", E1RmText(s)))];
        }
        foreach (var set in model.Sets)
            Sets.Add(new SetRowViewModel(this, set));
        // Warm-ups start folded away once they're done, and open while there are some left to do.
        showWarmups = model.Sets.Any(s => s.IsWarmup && !s.IsCompleted);
        Renumber();
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
    [NotifyPropertyChangedFor(nameof(ThumbOpacity), nameof(ThumbStroke))]
    bool isSelected;
    public double ThumbOpacity => IsSelected ? 1 : 0.45;
    public Color ThumbStroke => IsSelected ? Color.FromArgb("#3F7DFF") : Colors.Transparent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WarmupText))]
    bool showWarmups;
    public bool HasWarmups => Model.Sets.Any(s => s.IsWarmup);
    public bool HasOpenWarmups => Sets.Any(s => s.Model.IsWarmup && !s.IsSettled);
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
    Task Help() => Shell.Current.GoToAsync($"{Routes.Exercise}?id={Exercise.Id}");

    internal string WeightText(SetEntry s) => Exercise.IsBodyweight && s.WeightKg <= 0 ? "BW" : Units.Format(s.WeightKg);

    internal string E1RmText(SetEntry s)
    {
        var e1 = s.IsWarmup ? 0 : ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir);
        return e1 > 0 ? Units.Format(e1) : "–";
    }

    internal void Renumber()
    {
        var n = 0;
        foreach (var s in Sets)
            s.Label = s.Model.IsWarmup ? "W" : (++n).ToString();
        OnPropertyChanged(nameof(HasWarmups));
        OnPropertyChanged(nameof(HasOpenWarmups));
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

            // Out of the rep range: say so, and adjust the next set unless its weight was already changed by hand.
            if (!row.Model.IsWarmup)
            {
                var advice = _parent.Engine.AfterSet(Exercise, row.Model, Model.RepMin, Model.RepMax, Model.TargetRir);
                LiveAdvice = advice?.Advice ?? "";
                if (advice?.NextKg is { } kg && next != null && Math.Abs(next.Model.WeightKg - row.Model.WeightKg) < 0.01)
                    next.WeightText = Units.Format(kg);
            }
            // Warm-ups done: fold them away and move on to the working sets.
            if (row.Model.IsWarmup && Model.Sets.Where(s => s.IsWarmup).All(s => s.IsCompleted))
                ShowWarmups = false;
        }
        OnPropertyChanged(nameof(HasOpenWarmups));
        OnPropertyChanged(nameof(IsSkipped));
        OnPropertyChanged(nameof(SkipExerciseText));
        OnPropertyChanged(nameof(CanSkipExercise));
        UpdateCurrent();
        _parent.OnSetToggled(this, row);
    }

    internal async Task SetMenu(SetRowViewModel row)
    {
        var toggle = row.Model.IsWarmup ? "Mark as working set" : "Mark as warm-up";
        var skip = row.Model.IsWarmup ? "Skip warm-up" : row.IsSkipped ? "Don't skip set" : "Skip set";
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
    /// Skips a set, or brings a skipped one back. A warm-up is dropped ("Add warm-up sets" in the menu brings them
    /// back); a working set stays in the table, dimmed, and is left out of the workout when it's finished.
    /// </summary>
    internal void Skip(SetRowViewModel row)
    {
        if (row.Model.IsWarmup)
        {
            RemoveWarmups([row]);
            return;
        }
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
        OnPropertyChanged(nameof(HasOpenWarmups));
        OnPropertyChanged(nameof(IsSkipped));
        OnPropertyChanged(nameof(SkipExerciseText));
        OnPropertyChanged(nameof(CanSkipExercise));
        UpdateCurrent();
        _parent.OnSkipped(this);
    }

    /// <summary>Drops every warm-up still to do and goes straight to the working sets.</summary>
    [RelayCommand]
    void SkipWarmups() => RemoveWarmups(Sets.Where(s => s.Model.IsWarmup && !s.IsCompleted).ToList());

    void RemoveWarmups(List<SetRowViewModel> rows)
    {
        if (rows.Count == 0)
            return;
        foreach (var row in rows)
        {
            Model.Sets.Remove(row.Model);
            Sets.Remove(row);
        }
        // Whatever warm-ups are left are done: fold them away.
        if (!HasOpenWarmups)
            ShowWarmups = false;
        Renumber();
        _parent.OnStructureChanged();
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
            ? [SkipExerciseText, "Exercise details", "Change rest time"]
            : ["Exercise details", "Change rest time"];
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
    [NotifyPropertyChangedFor(nameof(CheckBackground), nameof(CheckIcon), nameof(ValueColor), nameof(ShowDoneTick), nameof(ShowE1Rm), nameof(IsSettled))]
    bool isCompleted;

    /// <summary>Won't be done this time: dimmed, not counted, and left out of the workout when it's finished. Not a plan change.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowOpacity), nameof(ShowE1Rm), nameof(ShowSkipped), nameof(IsSettled))]
    bool isSkipped;

    /// <summary>Nothing left to do on it: done or skipped.</summary>
    public bool IsSettled => IsCompleted || IsSkipped;
    public double RowOpacity => IsSkipped ? 0.4 : 1;
    public bool ShowSkipped => IsSkipped && !IsCurrent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotCurrent), nameof(ShowRirInput), nameof(ShowRirValue), nameof(ShowDoneTick), nameof(NumberOpacity), nameof(ShowE1Rm), nameof(ShowSkipped))]
    bool isCurrent;
    public bool IsNotCurrent => !IsCurrent;
    public bool ShowRirInput => IsCurrent && TrackRir;
    public bool ShowRirValue => !IsCurrent && TrackRir;
    public bool ShowDoneTick => !IsCurrent && IsCompleted;
    public double NumberOpacity => IsCurrent ? 1 : 0.6;
    /// <summary>A skipped set says so where its E1RM would be. (Skipping is in the set number's menu.)</summary>
    public bool ShowE1Rm => !ShowSkipped;

    [ObservableProperty] bool isVisible = true;

    public string WeightDisplay => string.IsNullOrWhiteSpace(WeightText) ? (_parent.Exercise.IsBodyweight ? "BW" : "–") : WeightText;
    public string RepsDisplay => string.IsNullOrWhiteSpace(RepsText) ? "–" : RepsText;
    public string RirDisplay => string.IsNullOrWhiteSpace(RirText) ? "–" : RirText;
    public string E1Rm => _parent.E1RmText(Model);
    public Color CheckBackground => IsCompleted ? Color.FromArgb("#2ED47A") : Color.FromArgb("#272C39");
    public ImageSource CheckIcon => (ImageSource)Application.Current!.Resources[IsCompleted ? "IconCheck" : "IconCheckMuted"];
    public Color LabelColor => Model.IsWarmup ? Color.FromArgb("#FFB020") : Color.FromArgb("#9AA3B5");
    // Done sets stand out in green; the ones still to do are dimmed.
    public Color ValueColor => IsCompleted ? Color.FromArgb("#2ED47A") : Color.FromArgb("#9AA3B5");
    public bool TrackRir => _parent.TrackRir;

    internal void RefreshSettings()
    {
        OnPropertyChanged(nameof(TrackRir));
        OnPropertyChanged(nameof(ShowRirInput));
        OnPropertyChanged(nameof(ShowRirValue));
    }

    internal void RefreshVisibility() => IsVisible = !Model.IsWarmup || _parent.ShowWarmups;

    partial void OnWeightTextChanged(string value)
    {
        if (_parent.Units.TryParse(value, out var kg))
            Model.WeightKg = kg;
        else if (string.IsNullOrWhiteSpace(value))
            Model.WeightKg = 0;
    }

    partial void OnRepsTextChanged(string value)
    {
        if (int.TryParse(value, out var reps) && reps >= 0)
            Model.Reps = reps;
    }

    partial void OnRirTextChanged(string value) =>
        Model.Rir = int.TryParse(value, out var rir) && rir >= 0 ? rir : null;

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
