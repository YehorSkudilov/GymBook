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
    ExercisePickerService picker) : BaseViewModel
{
    IDispatcherTimer? _timer;
    WorkoutSession? _session;
    DateTime _restEndsAt;
    int _restTotal;

    public ObservableCollection<WorkoutExerciseViewModel> Exercises { get; } = [];

    [ObservableProperty] string name = "";
    [ObservableProperty] string elapsed = "00:00";
    [ObservableProperty] string progressText = "";
    [ObservableProperty] double progress;
    [ObservableProperty] bool isResting;
    [ObservableProperty] string restText = "";
    [ObservableProperty] double restProgress;
    [ObservableProperty] bool isEmpty;

    /// <summary>The exercise on screen; the page and the photo strip both follow it.</summary>
    [ObservableProperty] int currentIndex;
    // Set when an exercise's last set is done: move on to the next one once the rest is over.
    int? _advanceTo;

    public WorkoutExerciseViewModel? CurrentExercise => Exercises.ElementAtOrDefault(CurrentIndex);

    partial void OnCurrentIndexChanged(int value)
    {
        for (var i = 0; i < Exercises.Count; i++)
            Exercises[i].IsSelected = i == value;
        OnPropertyChanged(nameof(CurrentExercise));
    }

    [RelayCommand]
    void NextExercise()
    {
        if (CurrentIndex + 1 < Exercises.Count)
            CurrentIndex++;
    }

    [RelayCommand]
    void PreviousExercise()
    {
        if (CurrentIndex > 0)
            CurrentIndex--;
    }

    internal Task SelectExercise(WorkoutExerciseViewModel vm)
    {
        var index = Exercises.IndexOf(vm);
        if (index >= 0)
            CurrentIndex = index;
        return Task.CompletedTask;
    }

    void AdvanceIfPending()
    {
        if (_advanceTo is { } next && next < Exercises.Count)
            CurrentIndex = next;
        _advanceTo = null;
    }

    public string UnitLabel => units.Label.ToUpperInvariant();
    public bool TrackRir => store.Profile.TrackRir;

    internal Units Units => units;
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
    }

    public override void OnDisappearing()
    {
        _timer?.Stop();
        if (workouts.Active != null)
            workouts.Save();
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
            AdvanceIfPending();
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

    internal void OnSetToggled(WorkoutExerciseViewModel exercise, SetRowViewModel set)
    {
        workouts.Save();
        UpdateProgress();
        var index = Exercises.IndexOf(exercise);
        _advanceTo = set.IsCompleted && exercise.IsDone && index == CurrentIndex && index + 1 < Exercises.Count ? index + 1 : null;
        if (set.IsCompleted && store.Profile.AutoRestTimer)
            StartRest(set.Model.IsWarmup ? store.Profile.WarmupRestSeconds : exercise.Model.RestSeconds);
        else
            AdvanceIfPending();
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

    internal void Move(WorkoutExerciseViewModel vm, int delta)
    {
        var i = Exercises.IndexOf(vm);
        var j = i + delta;
        if (_session == null || i < 0 || j < 0 || j >= Exercises.Count)
            return;
        Exercises.Move(i, j);
        _session.Exercises.RemoveAt(i);
        _session.Exercises.Insert(j, vm.Model);
        CurrentIndex = j;
        OnCurrentIndexChanged(j);
        OnStructureChanged();
    }

    void UpdateProgress()
    {
        var sets = Exercises.SelectMany(e => e.Sets).Where(s => !s.Model.IsWarmup).ToList();
        var done = sets.Count(s => s.IsCompleted);
        ProgressText = $"{done}/{sets.Count} sets";
        Progress = sets.Count == 0 ? 0 : (double)done / sets.Count;
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
    void SkipRest()
    {
        IsResting = false;
        AdvanceIfPending();
    }

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
            var se = workouts.CreateAdHoc(ex);
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
        var open = all.Count - done;
        var message = open > 0
            ? $"{open} unfinished set{(open == 1 ? "" : "s")} will be removed. Finish the workout?"
            : "Great work! Save this workout?";
        if (!await dialogs.Confirm("Finish workout", message, "Finish", "Cancel"))
            return;

        IsResting = false;
        var session = workouts.Finish();
        _session = null;
        if (session != null)
            await GoTo($"../{Routes.Session}?id={session.Id}&finished=true");
        else
            await GoBack();
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
    public string TargetText => $"{Exercise.Equipment.Display()} · {Model.RepMin}–{Model.RepMax} reps · {Model.TargetRir} RIR · rest {Units.Rest(Model.RestSeconds)}";
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
    public bool HasOpenWarmups => Model.Sets.Any(s => s.IsWarmup && !s.IsCompleted);
    public string WarmupText
    {
        get
        {
            var count = Model.Sets.Count(s => s.IsWarmup);
            return $"{count} warm-up set{(count == 1 ? "" : "s")} {(ShowWarmups ? "▴" : "▾")}";
        }
    }

    public bool IsDone => Model.Sets.Where(s => !s.IsWarmup).All(s => s.IsCompleted) && Model.Sets.Any(s => !s.IsWarmup);

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
        foreach (var s in Sets)
            s.RefreshVisibility();
        UpdateCurrent();
    }

    /// <summary>The set being logged: <paramref name="chosen"/> when the user tapped one, otherwise the first open one shown.</summary>
    internal void UpdateCurrent(SetRowViewModel? chosen = null)
    {
        var current = chosen ?? Sets.FirstOrDefault(s => s.IsVisible && !s.IsCompleted);
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
        UpdateCurrent();
        _parent.OnSetToggled(this, row);
    }

    internal async Task SetMenu(SetRowViewModel row)
    {
        var toggle = row.Model.IsWarmup ? "Mark as working set" : "Mark as warm-up";
        string[] options = row.Model.IsWarmup && !row.IsCompleted ? ["Skip warm-up", toggle] : [toggle];
        var choice = await _parent.Dialogs.ActionSheet($"Set {row.Label}", "Delete set", options);
        if (choice == "Skip warm-up")
        {
            SkipWarmup(row);
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

    /// <summary>Drops a warm-up that won't be done; "Add warm-up sets" in the menu brings them back.</summary>
    internal void SkipWarmup(SetRowViewModel row) => RemoveWarmups([row]);

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
        var choice = await _parent.Dialogs.ActionSheet(Name, "Remove exercise", "Add warm-up sets", "Exercise details", "Change rest time", "Move earlier", "Move later");
        switch (choice)
        {
            case "Remove exercise":
                if (await _parent.Dialogs.Confirm("Remove exercise?", $"Remove {Name} and its sets from this workout?", "Remove"))
                    _parent.Remove(this);
                break;
            case "Add warm-up sets":
                AddWarmups();
                break;
            case "Exercise details":
                await Help();
                break;
            case "Change rest time":
                var pick = await _parent.Dialogs.ActionSheet("Rest between sets", null, "0:45", "1:00", "1:30", "2:00", "2:30", "3:00", "4:00", "5:00");
                if (pick != null)
                {
                    var parts = pick.Split(':');
                    Model.RestSeconds = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
                    OnPropertyChanged(nameof(TargetText));
                    OnPropertyChanged(nameof(Tip));
                    _parent.OnStructureChanged();
                }
                break;
            case "Move earlier":
                _parent.Move(this, -1);
                break;
            case "Move later":
                _parent.Move(this, 1);
                break;
        }
    }

    void AddWarmups()
    {
        var working = Model.Sets.FirstOrDefault(s => !s.IsWarmup)?.WeightKg ?? 0;
        var warmups = _parent.Engine.Warmups(Exercise, working);
        if (warmups.Count == 0)
        {
            _ = _parent.Dialogs.Alert("No warm-ups needed", "Warm-up sets are suggested for compound lifts of 20 kg or more.");
            return;
        }
        Model.Sets.RemoveAll(s => s.IsWarmup && !s.IsCompleted);
        foreach (var row in Sets.Where(s => s.Model.IsWarmup && !s.IsCompleted).ToList())
            Sets.Remove(row);
        Model.Sets.InsertRange(0, warmups);
        for (var i = 0; i < warmups.Count; i++)
            Sets.Insert(i, new SetRowViewModel(this, warmups[i]));
        ShowWarmups = true;
        Renumber();
        _parent.OnStructureChanged();
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
    [NotifyPropertyChangedFor(nameof(LabelColor), nameof(ShowSkip), nameof(ShowE1Rm))]
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
    [NotifyPropertyChangedFor(nameof(CheckBackground), nameof(CheckIcon), nameof(ValueColor), nameof(ShowDoneTick), nameof(ShowSkip), nameof(ShowE1Rm))]
    bool isCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotCurrent), nameof(ShowRirInput), nameof(ShowRirValue), nameof(ShowDoneTick), nameof(NumberOpacity), nameof(ShowSkip), nameof(ShowE1Rm))]
    bool isCurrent;
    public bool IsNotCurrent => !IsCurrent;
    public bool ShowRirInput => IsCurrent && TrackRir;
    public bool ShowRirValue => !IsCurrent && TrackRir;
    public bool ShowDoneTick => !IsCurrent && IsCompleted;
    public double NumberOpacity => IsCurrent ? 1 : 0.6;
    /// <summary>The warm-up being logged offers a skip in place of its E1RM, which warm-ups don't have.</summary>
    public bool ShowSkip => IsCurrent && Model.IsWarmup && !IsCompleted;
    public bool ShowE1Rm => !ShowSkip;

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
        IsCompleted = !IsCompleted;
        Model.IsCompleted = IsCompleted;
        Model.CompletedAt = IsCompleted ? DateTime.Now : null;
        _parent.OnToggled(this);
    }

    /// <summary>Tapping a row other than the current one opens it for editing.</summary>
    [RelayCommand]
    void Edit() => _parent.UpdateCurrent(this);

    [RelayCommand]
    void Skip() => _parent.SkipWarmup(this);

    [RelayCommand]
    Task Menu() => _parent.SetMenu(this);
}
