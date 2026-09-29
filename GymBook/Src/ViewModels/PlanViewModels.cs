using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public class PlanItem
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Meta { get; init; }
    public required bool IsActive { get; init; }
    public required IAsyncRelayCommand OpenCommand { get; init; }
}

/// <summary>A day in the plan page's day strip: a workout or a rest day.</summary>
public class PlanDayChip
{
    public required string Title { get; init; }
    public required bool IsSelected { get; init; }
    public bool IsRest { get; init; }
    public required IRelayCommand SelectCommand { get; init; }

    public Color Background => IsSelected ? Color.FromArgb("#272C39") : Colors.Transparent;
    public Color TextColor => IsSelected ? Color.FromArgb("#F4F6FB") : IsRest ? Color.FromArgb("#626B7E") : Color.FromArgb("#9AA3B5");
}

/// <summary>An exercise of the selected day on the plan page.</summary>
public class PlanDayExercise
{
    public required ExerciseThumb Thumb { get; init; }
    public required string Name { get; init; }
    public required string Equipment { get; init; }
    public required string Sets { get; init; }
    public required string Reps { get; init; }
    public required IAsyncRelayCommand OpenCommand { get; init; }
}

public partial class PlansViewModel(DataStore store, DialogService dialogs, AiPlanService ai) : BaseViewModel
{
    /// <summary>How many AI plans are left, under "Build a new plan"; empty when signed out.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAiQuota))]
    string aiQuotaText = "";
    public bool HasAiQuota => AiQuotaText.Length > 0;

    [ObservableProperty] PlanItem? activePlan;
    [ObservableProperty] bool hasActivePlan;
    [ObservableProperty] List<PlanItem> otherPlans = [];
    [ObservableProperty] bool hasOtherPlans;

    public override Task OnAppearingAsync()
    {
        var items = store.Data.Plans.OrderByDescending(p => p.CreatedAt).Select(p => new PlanItem
        {
            Name = p.Name,
            Description = p.Description,
            Meta = $"{p.Workouts.Count} workouts · {p.Workouts.Sum(w => w.Exercises.Count)} exercises · {p.DaysPerWeek}x/week",
            IsActive = p.Id == store.Data.ActivePlanId,
            OpenCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Plan}?id={p.Id}")),
        }).ToList();
        ActivePlan = items.FirstOrDefault(i => i.IsActive);
        HasActivePlan = ActivePlan != null;
        OtherPlans = items.Where(i => !i.IsActive).ToList();
        HasOtherPlans = OtherPlans.Count > 0;
        ShowQuota();
        _ = RefreshQuotaAsync();
        return Task.CompletedTask;
    }

    async Task RefreshQuotaAsync()
    {
        await ai.RefreshQuotaAsync();
        ShowQuota();
    }

    void ShowQuota() => AiQuotaText = !ai.IsAvailable ? "" : ai.Quota is { } q ? AiPlanService.Describe(q) : "";

    [RelayCommand]
    Task Generate() => GoTo(Routes.Wizard);

    /// <summary>A plan from text, an image or a file, read by AI; needs an account.</summary>
    [RelayCommand]
    async Task Import()
    {
        if (!ai.IsAvailable)
        {
            await dialogs.Alert("Sign in to import plans", "Importing reads the plan with AI, which needs an account. Sign in from the Profile tab.");
            return;
        }
        await GoTo(Routes.ImportPlan);
    }

    [RelayCommand]
    async Task CreateEmpty()
    {
        var name = await dialogs.Prompt("New plan", "Give your plan a name", "My plan", accept: "Create");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var plan = new WorkoutPlan
        {
            Name = name.Trim(),
            Description = "Custom plan",
            Goal = store.Profile.Goal,
            DaysPerWeek = store.Profile.DaysPerWeek,
            Workouts = [new PlanWorkout { Name = "Workout A" }],
            RestDays = [],
        };
        store.Data.Plans.Add(plan);
        store.Data.ActivePlanId ??= plan.Id;
        store.Save();
        await GoTo($"{Routes.Plan}?id={plan.Id}");
    }
}

public partial class PlanDetailViewModel(DataStore store, WorkoutService workouts, DialogService dialogs, WorkoutEstimator estimator, AiPlanService ai, RecoveryService recovery)
    : BaseViewModel, IQueryAttributable
{
    string? _id;
    int _selected;
    // Set when opened from a plan week on Home: the day is shown, started and marked finished for that week.
    int? _week;

    [ObservableProperty] string name = "";
    [ObservableProperty] string meta = "";
    [ObservableProperty] bool isActive;
    [ObservableProperty] List<PlanDayChip> days = [];

    // The selected day
    [ObservableProperty] string dayName = "";
    [ObservableProperty] string dayLabel = "";
    [ObservableProperty] string dayMeta = "";
    [ObservableProperty] bool isRestDay;
    [ObservableProperty] bool isWorkoutDay;
    [ObservableProperty] bool isNextDay;
    [ObservableProperty] bool isEmptyDay;
    [ObservableProperty] IDrawable dayMap = MuscleMapDrawable.Empty;
    [ObservableProperty] List<PlanDayExercise> dayExercises = [];
    [ObservableProperty] bool isDayDone;
    [ObservableProperty] string dayActionText = "";
    [ObservableProperty] bool hasDayAction;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = query["id"]?.ToString();
        if (query.TryGetValue("day", out var day) && int.TryParse(day?.ToString(), out var d))
            _selected = d;
        _week = query.TryGetValue("week", out var week) && int.TryParse(week?.ToString(), out var w) ? w : null;
    }

    public override Task OnAppearingAsync()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return GoBack();
        Name = plan.Name;
        IsActive = plan.Id == store.Data.ActivePlanId;
        var progress = new PlanProgress(plan, store.History);
        var schedule = progress.Days;
        Meta = $"{plan.Goal.Display()} · {plan.Workouts.Count} training days · {schedule.Count - plan.Workouts.Count} rest";
        _selected = Math.Clamp(_selected, 0, Math.Max(0, schedule.Count - 1));

        Days = schedule.Select((w, i) => new PlanDayChip
        {
            Title = (_week is { } week && progress.IsDayDone(i, week) ? "✓ " : "") + (w?.Name ?? "Rest"),
            IsRest = w == null,
            IsSelected = i == _selected,
            SelectCommand = new RelayCommand(() => Select(i)),
        }).ToList();

        var day = schedule.ElementAtOrDefault(_selected);
        var shownWeek = _week ?? progress.CurrentWeek;
        var next = progress.NextWorkout(shownWeek);
        DayLabel = _week == null ? $"Day {_selected + 1}" : $"Week {_week} · Day {_selected + 1}";
        IsDayDone = _week != null && schedule.Count > 0 && progress.IsDayDone(_selected, shownWeek);
        // Workouts start (or show the finished session); in a week, rest days are marked finished instead.
        DayActionText = day != null
            ? IsDayDone ? "View finished workout" : $"▶  Start {day.Name}"
            : IsDayDone ? "Mark as not finished" : "✓  Mark rest day finished";
        HasDayAction = day != null || (_week != null && schedule.Count > 0);
        IsRestDay = day == null;
        IsWorkoutDay = day != null;
        IsNextDay = IsActive && day != null && day == next && !IsDayDone;
        DayName = day?.Name ?? "Rest";

        var exercises = day?.Exercises.Select(pe => (pe, ex: store.GetExercise(pe.ExerciseId))).ToList() ?? [];
        DayMeta = day == null
            ? "Recovery day. Muscles grow between sessions."
            : $"{exercises.Count} exercises · {exercises.Sum(x => x.pe.Sets)} sets · {WorkoutEstimator.Format(estimator.Minutes(day, plan.Goal))}";
        IsEmptyDay = day != null && exercises.Count == 0;
        DayMap = MuscleMapDrawable.ForWorkout(exercises.Select(x => x.ex).OfType<Exercise>());
        DayExercises = exercises.Select(x => new PlanDayExercise
        {
            Thumb = x.ex == null ? new ExerciseThumb(null, "?", Colors.Gray, Colors.Gray.WithAlpha(0.16f)) : ExerciseThumb.For(x.ex),
            Name = x.ex?.Name ?? "Unknown exercise",
            Equipment = x.ex?.Equipment.Display() ?? "",
            Sets = x.pe.Sets == 1 ? "1 set" : $"{x.pe.Sets} sets",
            Reps = x.pe.RepMin == x.pe.RepMax ? $"{x.pe.RepMin} reps" : $"{x.pe.RepMin}–{x.pe.RepMax} reps",
            OpenCommand = new AsyncRelayCommand(() => x.ex == null ? Task.CompletedTask : GoTo($"{Routes.Exercise}?id={x.ex.Id}")),
        }).ToList();
        return Task.CompletedTask;
    }

    void Select(int index)
    {
        _selected = index;
        _ = OnAppearingAsync();
    }

    PlanWorkout? SelectedWorkout(WorkoutPlan plan) => PlanSchedule.Days(plan).ElementAtOrDefault(_selected);

    /// <summary>The button under the day: start the workout or open its finished session, or mark the rest day.</summary>
    [RelayCommand]
    Task DayAction()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return Task.CompletedTask;
        if (SelectedWorkout(plan) is not PlanWorkout w)
        {
            if (_week is { } week)
            {
                PlanProgress.SetRestDone(plan, _selected, week, !IsDayDone);
                store.Save();
            }
            return OnAppearingAsync();
        }
        if (_week is { } shown && new PlanProgress(plan, store.History).SessionFor(w, shown) is { } done)
            return GoTo($"{Routes.Session}?id={done.Id}");
        if (w.Exercises.Count == 0)
            return dialogs.Alert("Empty workout", "Add exercises to this workout first.");
        return StartPlannedWorkoutAsync(workouts, dialogs, recovery, plan, w, _week);
    }

    /// <summary>Opens the muscle breakdown for the selected day, with a switch to the whole plan.</summary>
    [RelayCommand]
    Task OpenMuscles()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return Task.CompletedTask;
        var page = new Views.MuscleBreakdownPage(new MuscleBreakdownViewModel(store, plan, SelectedWorkout(plan)));
        return Shell.Current.Navigation.PushModalAsync(page, false);
    }

    [RelayCommand]
    Task EditDay()
    {
        var plan = store.GetPlan(_id);
        return plan != null && SelectedWorkout(plan) is PlanWorkout w
            ? GoTo($"{Routes.PlanWorkout}?plan={plan.Id}&workout={w.Id}")
            : Task.CompletedTask;
    }

    [RelayCommand]
    async Task DayOptions()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return;
        var days = PlanSchedule.Days(plan);
        var day = days.ElementAtOrDefault(_selected);
        var options = new List<string>();
        if (day != null)
            options.Add("Edit exercises");
        if (_selected > 0)
            options.Add("Move earlier");
        if (_selected < days.Count - 1)
            options.Add("Move later");
        var remove = day == null ? "Remove rest day" : null;
        switch (await dialogs.ActionSheet(DayName, remove, [.. options]))
        {
            case "Edit exercises":
                await EditDay();
                break;
            case "Move earlier":
                MoveSelected(plan, days, -1);
                break;
            case "Move later":
                MoveSelected(plan, days, 1);
                break;
            case "Remove rest day":
                days.RemoveAt(_selected);
                SaveDays(plan, days);
                break;
        }
    }

    void MoveSelected(WorkoutPlan plan, List<PlanWorkout?> days, int delta)
    {
        var target = _selected + delta;
        (days[_selected], days[target]) = (days[target], days[_selected]);
        _selected = target;
        SaveDays(plan, days);
    }

    /// <summary>The "+" at the end of the day strip.</summary>
    [RelayCommand]
    async Task AddDay()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return;
        switch (await dialogs.ActionSheet("Add a day", null, "Workout", "Rest day"))
        {
            case "Workout":
                await AddWorkout(plan);
                break;
            case "Rest day":
                var days = PlanSchedule.Days(plan);
                if (days.Count >= 7)
                {
                    await dialogs.Alert("Week is full", "This plan already fills all 7 days. Remove a day or a workout first.");
                    return;
                }
                days.Add(null);
                _selected = days.Count - 1;
                SaveDays(plan, days);
                break;
        }
    }

    void SaveDays(WorkoutPlan plan, List<PlanWorkout?> days)
    {
        PlanSchedule.SetDays(plan, days);
        store.Save();
        _ = OnAppearingAsync();
    }

    [RelayCommand]
    async Task SetActive()
    {
        store.Data.ActivePlanId = _id;
        store.Save();
        await OnAppearingAsync();
    }

    async Task AddWorkout(WorkoutPlan plan)
    {
        var name = await dialogs.Prompt("New workout", "Workout name", $"Workout {(char)('A' + plan.Workouts.Count)}", accept: "Add");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var w = new PlanWorkout { Name = name.Trim() };
        var days = PlanSchedule.Days(plan);
        days.Add(w);
        _selected = days.Count - 1;
        PlanSchedule.SetDays(plan, days);
        store.Save();
        await GoTo($"{Routes.PlanWorkout}?plan={plan.Id}&workout={w.Id}");
    }

    /// <summary>
    /// Switches the plan's goal and offers to re-apply it to every exercise, so rep ranges, sets, effort and rest
    /// don't have to be set up one by one.
    /// </summary>
    async Task ChangeGoal(WorkoutPlan plan)
    {
        var labels = TrainingGoals.All.Select(g => g == plan.Goal ? $"{g.Display()} ✓" : g.Display()).ToList();
        var pick = await dialogs.ActionSheet("Training goal", null, [.. labels]);
        var index = pick == null ? -1 : labels.IndexOf(pick);
        if (index < 0)
            return;
        var goal = TrainingGoals.All[index];
        var apply = await dialogs.Confirm($"Use {goal.Display()} for every exercise?",
            $"{goal.Description()}. This resets each exercise's rep range, sets, reps in reserve and rest to suit it.", "Apply to all", "Only change the goal");
        plan.Goal = goal;
        if (apply)
        {
            foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
            {
                if (store.GetExercise(pe.ExerciseId) is not { } ex)
                    continue;
                var p = TrainingGoals.Prescription(goal, store.Profile.Experience, ex, store.Profile);
                (pe.Sets, pe.RepMin, pe.RepMax, pe.TargetRir, pe.RestSeconds) = (p.Sets, p.RepMin, p.RepMax, p.TargetRir, p.RestSeconds);
            }
        }
        store.Save();
        await OnAppearingAsync();
    }

    [RelayCommand]
    async Task More()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return;
        var choice = await dialogs.ActionSheet(plan.Name, "Delete plan", "Change with AI", "Regenerate plan", "Training goal", "Rename plan", "Duplicate plan");
        switch (choice)
        {
            case "Change with AI":
                if (!ai.IsAvailable)
                {
                    await dialogs.Alert("Sign in to use AI", "Changing a plan with AI needs an account. Sign in from the Profile tab.");
                    break;
                }
                // Changes are saved as the AI makes them; this page reloads when the chat closes.
                await PlanChatViewModel.OpenAsync(plan, ai.AnswersFor(plan), save: true);
                break;
            case "Regenerate plan":
                // The questionnaire, filled in from this plan; saving replaces its workouts.
                await GoTo($"{Routes.Wizard}?regenerate={plan.Id}");
                break;
            case "Training goal":
                await ChangeGoal(plan);
                break;
            case "Rename plan":
                var name = await dialogs.Prompt("Rename plan", "Plan name", plan.Name);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    plan.Name = name.Trim();
                    store.Save();
                    await OnAppearingAsync();
                }
                break;
            case "Duplicate plan":
                var copy = LocalJson.Clone(plan);
                copy.Id = Guid.NewGuid().ToString("N");
                copy.Name += " (copy)";
                copy.CreatedAt = DateTime.Now;
                copy.Workouts.ForEach(w => w.Id = Guid.NewGuid().ToString("N"));
                store.Data.Plans.Add(copy);
                store.Save();
                await GoBack();
                break;
            case "Delete plan":
                if (await dialogs.Confirm("Delete plan?", $"\"{plan.Name}\" will be deleted. Your workout history is kept.", "Delete"))
                {
                    store.Data.Plans.Remove(plan);
                    if (store.Data.ActivePlanId == plan.Id)
                        store.Data.ActivePlanId = store.Data.Plans.FirstOrDefault()?.Id;
                    store.Save();
                    await GoBack();
                }
                break;
        }
    }
}

public partial class PlanWorkoutEditViewModel(DataStore store, DialogService dialogs, ExercisePickerService picker)
    : BaseViewModel, IQueryAttributable
{
    string? _planId, _workoutId;
    PlanWorkout? _workout;

    [ObservableProperty] string name = "";
    [ObservableProperty] bool isEmpty;
    public ObservableCollection<PlanExerciseItem> Exercises { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _planId = query["plan"]?.ToString();
        _workoutId = query["workout"]?.ToString();
    }

    public override Task OnAppearingAsync()
    {
        _workout = store.GetPlan(_planId)?.Workouts.FirstOrDefault(w => w.Id == _workoutId);
        if (_workout == null)
            return GoBack();
        Name = _workout.Name;
        Exercises.Clear();
        foreach (var pe in _workout.Exercises)
            Exercises.Add(new PlanExerciseItem(this, pe, store.GetExercise(pe.ExerciseId)));
        IsEmpty = Exercises.Count == 0;
        return Task.CompletedTask;
    }

    internal void Save() => store.Save();

    internal void Remove(PlanExerciseItem item)
    {
        _workout?.Exercises.Remove(item.Model);
        Exercises.Remove(item);
        IsEmpty = Exercises.Count == 0;
        Save();
    }

    internal void Move(PlanExerciseItem item, int delta)
    {
        var i = Exercises.IndexOf(item);
        var j = i + delta;
        if (_workout == null || i < 0 || j < 0 || j >= Exercises.Count)
            return;
        Exercises.Move(i, j);
        _workout.Exercises.RemoveAt(i);
        _workout.Exercises.Insert(j, item.Model);
        Save();
    }

    [RelayCommand]
    async Task AddExercise()
    {
        var picked = await picker.PickAsync();
        if (_workout == null)
            return;
        foreach (var ex in picked)
        {
            // Set up for the plan's goal straight away.
            var goal = store.GetPlan(_planId)?.Goal ?? store.Profile.Goal;
            var pe = TrainingGoals.Prescription(goal, store.Profile.Experience, ex, store.Profile);
            _workout.Exercises.Add(pe);
            Exercises.Add(new PlanExerciseItem(this, pe, ex));
        }
        IsEmpty = Exercises.Count == 0;
        Save();
    }

    [RelayCommand]
    async Task More()
    {
        if (_workout == null)
            return;
        var choice = await dialogs.ActionSheet(_workout.Name, "Delete workout", "Rename workout");
        if (choice == "Rename workout")
        {
            var name = await dialogs.Prompt("Rename workout", "Workout name", _workout.Name);
            if (!string.IsNullOrWhiteSpace(name))
            {
                Name = _workout.Name = name.Trim();
                Save();
            }
        }
        else if (choice == "Delete workout" && await dialogs.Confirm("Delete workout?", $"Remove \"{_workout.Name}\" from the plan?", "Delete"))
        {
            var plan = store.GetPlan(_planId);
            if (plan != null)
            {
                // Through the schedule so the rest days around it stay where they were.
                var days = PlanSchedule.Days(plan);
                days.Remove(_workout);
                PlanSchedule.SetDays(plan, days);
            }
            Save();
            await GoBack();
        }
    }
}

public partial class PlanExerciseItem(PlanWorkoutEditViewModel parent, PlanExercise model, Exercise? exercise) : ObservableObject
{
    public PlanExercise Model { get; } = model;
    public string Name => exercise?.Name ?? "Unknown exercise";
    public string Subtitle => exercise?.Subtitle ?? "";
    public string SetsText => Model.Sets.ToString();
    public string RepsText => $"{Model.RepMin}–{Model.RepMax}";
    public string RirText => Model.TargetRir.ToString();
    public string RestText => Units.Rest(Model.RestSeconds);

    void Changed()
    {
        OnPropertyChanged(nameof(SetsText));
        OnPropertyChanged(nameof(RepsText));
        OnPropertyChanged(nameof(RirText));
        OnPropertyChanged(nameof(RestText));
        parent.Save();
    }

    [RelayCommand] void SetsUp() { Model.Sets = Math.Min(10, Model.Sets + 1); Changed(); }
    [RelayCommand] void SetsDown() { Model.Sets = Math.Max(1, Model.Sets - 1); Changed(); }
    [RelayCommand] void RepsUp() { Model.RepMin = Math.Min(40, Model.RepMin + 1); Model.RepMax = Math.Max(Model.RepMax + 1, Model.RepMin); Changed(); }
    [RelayCommand] void RepsDown() { Model.RepMin = Math.Max(1, Model.RepMin - 1); Model.RepMax = Math.Max(Model.RepMin, Model.RepMax - 1); Changed(); }
    [RelayCommand] void RangeUp() { Model.RepMax = Math.Min(50, Model.RepMax + 1); Changed(); }
    [RelayCommand] void RangeDown() { Model.RepMax = Math.Max(Model.RepMin, Model.RepMax - 1); Changed(); }
    [RelayCommand] void RirUp() { Model.TargetRir = Math.Min(5, Model.TargetRir + 1); Changed(); }
    [RelayCommand] void RirDown() { Model.TargetRir = Math.Max(0, Model.TargetRir - 1); Changed(); }
    [RelayCommand] void RestUp() { Model.RestSeconds = Math.Min(600, Model.RestSeconds + 15); Changed(); }
    [RelayCommand] void RestDown() { Model.RestSeconds = Math.Max(15, Model.RestSeconds - 15); Changed(); }
    [RelayCommand] void Remove() => parent.Remove(this);
    [RelayCommand] void MoveUp() => parent.Move(this, -1);
    [RelayCommand] void MoveDown() => parent.Move(this, 1);
}
