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

    public string UnitLabel => units.Label.ToUpperInvariant();
    public bool TrackRir => store.Profile.TrackRir;

    internal Units Units => units;
    internal ProgressionEngine Engine => engine;
    internal DialogService Dialogs => dialogs;

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
        if (set.IsCompleted && store.Profile.AutoRestTimer)
            StartRest(set.Model.IsWarmup ? Math.Min(60, exercise.Model.RestSeconds) : exercise.Model.RestSeconds);
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
    void SkipRest() => IsResting = false;

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
        OnStructureChanged();
    }

    [RelayCommand]
    async Task Rename()
    {
        var value = await dialogs.Prompt("Rename workout", "Workout name", Name);
        if (string.IsNullOrWhiteSpace(value) || _session == null)
            return;
        Name = _session.Name = value.Trim();
        workouts.Save();
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

public partial class WorkoutExerciseViewModel : ObservableObject
{
    readonly WorkoutViewModel _parent;
    readonly List<SetEntry> _previous;

    public WorkoutExerciseViewModel(WorkoutViewModel parent, Exercise exercise, SessionExercise model, string sessionId)
    {
        _parent = parent;
        Exercise = exercise;
        Model = model;
        _previous = parent.Engine.LastPerformance(exercise.Id, sessionId)?.Sets.Where(s => !s.IsWarmup).ToList() ?? [];
        foreach (var set in model.Sets)
            Sets.Add(new SetRowViewModel(this, set));
        Renumber();
    }

    public Exercise Exercise { get; }
    public SessionExercise Model { get; }
    public ObservableCollection<SetRowViewModel> Sets { get; } = [];
    internal Units Units => _parent.Units;

    public string Name => Exercise.Name;
    public string Subtitle => Exercise.Subtitle;
    public string TargetText => $"{Model.RepMin}–{Model.RepMax} reps · {Model.TargetRir} RIR · rest {Units.Rest(Model.RestSeconds)}";
    public string Recommendation => Model.Recommendation ?? "";
    public bool HasRecommendation => !string.IsNullOrEmpty(Model.Recommendation);
    public bool TrackRir => _parent.TrackRir;
    public string UnitLabel => _parent.UnitLabel;

    internal void Renumber()
    {
        var n = 0;
        var w = 0;
        foreach (var s in Sets)
        {
            if (s.Model.IsWarmup)
            {
                s.Label = "W";
                s.Previous = "—";
            }
            else
            {
                s.Label = (++n).ToString();
                var prev = w < _previous.Count ? _previous[w] : null;
                s.Previous = prev == null ? "—" : $"{Units.Format(prev.WeightKg)} × {prev.Reps}";
                w++;
            }
        }
    }

    internal void OnToggled(SetRowViewModel row)
    {
        // Carry the completed values into the next open set, as a starting point.
        if (row.IsCompleted)
        {
            var next = Sets.SkipWhile(s => s != row).Skip(1).FirstOrDefault(s => !s.IsCompleted && s.Model.IsWarmup == row.Model.IsWarmup);
            if (next != null && next.Model.WeightKg <= 0 && row.Model.WeightKg > 0)
                next.WeightText = row.WeightText;
        }
        _parent.OnSetToggled(this, row);
    }

    internal async Task SetMenu(SetRowViewModel row)
    {
        var toggle = row.Model.IsWarmup ? "Mark as working set" : "Mark as warm-up";
        var choice = await _parent.Dialogs.ActionSheet($"Set {row.Label}", "Delete set", toggle);
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
        var choice = await _parent.Dialogs.ActionSheet(Name, "Remove exercise", "Add warm-up sets", "Exercise details", "Change rest time", "Move up", "Move down");
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
                await Shell.Current.GoToAsync($"{Routes.Exercise}?id={Exercise.Id}");
                break;
            case "Change rest time":
                var pick = await _parent.Dialogs.ActionSheet("Rest between sets", null, "0:45", "1:00", "1:30", "2:00", "2:30", "3:00", "4:00", "5:00");
                if (pick != null)
                {
                    var parts = pick.Split(':');
                    Model.RestSeconds = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
                    OnPropertyChanged(nameof(TargetText));
                    _parent.OnStructureChanged();
                }
                break;
            case "Move up":
                _parent.Move(this, -1);
                break;
            case "Move down":
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
        Renumber();
        _parent.OnStructureChanged();
    }
}

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

    [ObservableProperty] string label = "";
    [ObservableProperty] string previous = "—";
    [ObservableProperty] string weightText;
    [ObservableProperty] string repsText;
    [ObservableProperty] string rirText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground), nameof(CheckBackground), nameof(CheckIcon))]
    bool isCompleted;

    public Color RowBackground => IsCompleted ? Color.FromArgb("#133526") : Colors.Transparent;
    public Color CheckBackground => IsCompleted ? Color.FromArgb("#2ED47A") : Color.FromArgb("#272C39");
    public string CheckIcon => IsCompleted ? "ic_check.png" : "ic_check_muted.png";
    public Color LabelColor => Model.IsWarmup ? Color.FromArgb("#FFB020") : Color.FromArgb("#9AA3B5");
    public bool TrackRir => _parent.TrackRir;

    partial void OnLabelChanged(string value) => OnPropertyChanged(nameof(LabelColor));

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

    [RelayCommand]
    Task Menu() => _parent.SetMenu(this);
}
