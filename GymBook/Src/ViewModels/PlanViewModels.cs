using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

public class PlanWorkoutItem
{
    public required string Name { get; init; }
    public required string Summary { get; init; }
    public required string Meta { get; init; }
    public required bool IsNext { get; init; }
    public required IAsyncRelayCommand StartCommand { get; init; }
    public required IAsyncRelayCommand EditCommand { get; init; }
}

public partial class PlansViewModel(DataStore store, DialogService dialogs) : BaseViewModel
{
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
        return Task.CompletedTask;
    }

    [RelayCommand]
    Task Generate() => GoTo(Routes.Wizard);

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
        };
        store.Data.Plans.Add(plan);
        store.Data.ActivePlanId ??= plan.Id;
        store.Save();
        await GoTo($"{Routes.Plan}?id={plan.Id}");
    }
}

public partial class PlanDetailViewModel(DataStore store, WorkoutService workouts, DialogService dialogs)
    : BaseViewModel, IQueryAttributable
{
    string? _id;

    [ObservableProperty] string name = "";
    [ObservableProperty] string description = "";
    [ObservableProperty] string meta = "";
    [ObservableProperty] bool isActive;
    [ObservableProperty] List<PlanWorkoutItem> workoutItems = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query) => _id = query["id"]?.ToString();

    public override Task OnAppearingAsync()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return GoBack();
        Name = plan.Name;
        Description = plan.Description;
        Meta = $"{plan.Goal.Display()} · {plan.DaysPerWeek} days per week · {plan.Workouts.Count} workouts";
        IsActive = plan.Id == store.Data.ActivePlanId;
        WorkoutItems = plan.Workouts.Select((w, i) => new PlanWorkoutItem
        {
            Name = w.Name,
            Summary = w.Exercises.Count == 0
                ? "No exercises yet. Tap Edit to add some."
                : string.Join("\n", w.Exercises.Select(e => $"{e.Sets} × {store.GetExercise(e.ExerciseId)?.Name}")),
            Meta = $"{w.Exercises.Count} exercises · {w.Exercises.Sum(e => e.Sets)} sets",
            IsNext = IsActive && i == plan.NextWorkoutIndex % Math.Max(1, plan.Workouts.Count),
            StartCommand = new AsyncRelayCommand(() => Start(plan, w)),
            EditCommand = new AsyncRelayCommand(() => GoTo($"{Routes.PlanWorkout}?plan={plan.Id}&workout={w.Id}")),
        }).ToList();
        return Task.CompletedTask;
    }

    Task Start(WorkoutPlan plan, PlanWorkout w)
    {
        if (w.Exercises.Count == 0)
            return dialogs.Alert("Empty workout", "Add exercises to this workout first.");
        return StartWorkoutAsync(workouts, dialogs, () => workouts.StartFromPlan(plan, w));
    }

    [RelayCommand]
    async Task SetActive()
    {
        store.Data.ActivePlanId = _id;
        store.Save();
        await OnAppearingAsync();
    }

    [RelayCommand]
    async Task AddWorkout()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return;
        var name = await dialogs.Prompt("New workout", "Workout name", $"Workout {(char)('A' + plan.Workouts.Count)}", accept: "Add");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var w = new PlanWorkout { Name = name.Trim() };
        plan.Workouts.Add(w);
        store.Save();
        await GoTo($"{Routes.PlanWorkout}?plan={plan.Id}&workout={w.Id}");
    }

    [RelayCommand]
    async Task More()
    {
        var plan = store.GetPlan(_id);
        if (plan == null)
            return;
        var choice = await dialogs.ActionSheet(plan.Name, "Delete plan", "Rename plan", "Duplicate plan");
        switch (choice)
        {
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
                var json = System.Text.Json.JsonSerializer.Serialize(new AppData { Plans = [plan] }, AppJsonContext.Default.AppData);
                var copy = System.Text.Json.JsonSerializer.Deserialize(json, AppJsonContext.Default.AppData)!.Plans[0];
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
            var pe = PlanGenerator.Prescription(store.Profile, ex);
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
            plan?.Workouts.Remove(_workout);
            if (plan != null && plan.Workouts.Count > 0)
                plan.NextWorkoutIndex %= plan.Workouts.Count;
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
