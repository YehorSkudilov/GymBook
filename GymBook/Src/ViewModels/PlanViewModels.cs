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
    /// <summary>Waiting suggestions from the weekly AI check (the active plan only); 0 when none.</summary>
    public int Suggestions { get; init; }
    public bool HasSuggestions => Suggestions > 0;
    public string SuggestionsText => Suggestions == 1 ? "1 AI suggestion" : $"{Suggestions} AI suggestions";
    public required IAsyncRelayCommand OpenCommand { get; init; }
}

/// <summary>A day in the plan page's day strip: a workout or a rest day. Hold and drag it along the strip to move it.</summary>
public class PlanDayChip
{
    public required string Title { get; init; }
    public required bool IsSelected { get; init; }
    public bool IsRest { get; init; }
    public required IRelayCommand SelectCommand { get; init; }
    /// <summary>Moves the day to the position it was dragged to (see <see cref="Controls.ReorderItem"/>).</summary>
    public required IRelayCommand<int> MoveCommand { get; init; }

    public Color Background => IsSelected ? Color.FromArgb("#272C39") : Colors.Transparent;
    public Color TextColor => IsSelected ? Color.FromArgb("#F4F6FB") : IsRest ? Color.FromArgb("#626B7E") : Color.FromArgb("#9AA3B5");
}

/// <summary>
/// An exercise of the selected day on the plan page. Tap it to change its sets, rep range, reps in reserve and rest
/// right there; long-press and drag it onto another exercise to move it there.
/// </summary>
public partial class PlanDayExercise(PlanExercise model, Exercise? exercise, bool showRir, Action changed) : ObservableObject
{
    public PlanExercise Model { get; } = model;
    public ExerciseThumb Thumb { get; } = exercise == null ? new ExerciseThumb(null, "?", Colors.Gray, Colors.Gray.WithAlpha(0.16f)) : ExerciseThumb.For(exercise);
    public string Name => exercise?.Name ?? "Unknown exercise";
    public string Equipment => exercise?.Equipment.Display() ?? "";
    public bool HasInfo => exercise != null;
    /// <summary>The plan uses reps in reserve; otherwise its target isn't shown or changed.</summary>
    public bool ShowRir { get; } = showRir;

    public string Sets => Model.Sets == 1 ? "1 set" : $"{Model.Sets} sets";
    public string Reps => Model.RepMin == Model.RepMax ? $"{Model.RepMin} reps" : $"{Model.RepMin}–{Model.RepMax} reps";
    public string SetsText => Model.Sets.ToString();
    public string RepsText => $"{Model.RepMin}–{Model.RepMax}";
    public string RirText => Model.TargetRir.ToString();
    public string RestText => Units.Rest(Model.RestSeconds);

    public required IAsyncRelayCommand OpenCommand { get; init; }
    public required IRelayCommand RemoveCommand { get; init; }
    /// <summary>Moves the exercise to the position it was dragged to (see <see cref="Controls.ReorderItem"/>).</summary>
    public required IRelayCommand<int> MoveCommand { get; init; }

    [ObservableProperty] bool isExpanded;

    [RelayCommand] void Toggle() => IsExpanded = !IsExpanded;

    void Changed()
    {
        foreach (var p in new[] { nameof(Sets), nameof(Reps), nameof(SetsText), nameof(RepsText), nameof(RirText), nameof(RestText) })
            OnPropertyChanged(p);
        changed();
    }

    [RelayCommand] void SetsUp() { Model.Sets = Math.Min(10, Model.Sets + 1); Changed(); }
    [RelayCommand] void SetsDown() { Model.Sets = Math.Max(1, Model.Sets - 1); Changed(); }
    [RelayCommand] void RepsUp() { Model.RepMin = Math.Min(40, Model.RepMin + 1); Model.RepMax = Math.Max(Model.RepMax, Model.RepMin); Changed(); }
    [RelayCommand] void RepsDown() { Model.RepMin = Math.Max(1, Model.RepMin - 1); Changed(); }
    [RelayCommand] void RangeUp() { Model.RepMax = Math.Min(50, Model.RepMax + 1); Changed(); }
    [RelayCommand] void RangeDown() { Model.RepMax = Math.Max(Model.RepMin, Model.RepMax - 1); Changed(); }
    [RelayCommand] void RirUp() { Model.TargetRir = Math.Min(5, Model.TargetRir + 1); Changed(); }
    [RelayCommand] void RirDown() { Model.TargetRir = Math.Max(0, Model.TargetRir - 1); Changed(); }
    [RelayCommand] void RestUp() { Model.RestSeconds = Math.Min(600, Model.RestSeconds + 15); Changed(); }
    [RelayCommand] void RestDown() { Model.RestSeconds = Math.Max(15, Model.RestSeconds - 15); Changed(); }
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
        // The weekly AI check can finish while the tab is showing.
        ai.SuggestionsChanged -= OnSuggestionsChanged;
        ai.SuggestionsChanged += OnSuggestionsChanged;
        var items = store.Data.Plans.OrderByDescending(p => p.CreatedAt).Select(p => new PlanItem
        {
            Name = p.Name,
            Description = p.Description,
            // Training days are the workouts; rest days don't count.
            Meta = $"{p.Workouts.Count} workouts a week · {p.Workouts.Sum(w => w.Exercises.Count)} exercises",
            IsActive = p.Id == store.Data.ActivePlanId,
            Suggestions = ai.PendingSuggestions(p),
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

    void OnSuggestionsChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(() => _ = OnAppearingAsync());

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

/// <summary>
/// A plan: its days, and the selected day's exercises, all edited right here. Tap an exercise to change it, drag
/// exercises and days to reorder them, add exercises under the list, and rename or delete a day from its ··· menu.
/// Edits go into a draft copy: nothing reaches the saved plan until Save (in the plan's ··· menu), and leaving with
/// unsaved changes asks whether to keep them.
/// </summary>
public partial class PlanDetailViewModel(DataStore store, DialogService dialogs, WorkoutEstimator estimator, AiPlanService ai,
    ExercisePickerService picker)
    : BaseViewModel, IQueryAttributable
{
    string? _id;
    int _selected;
    // Set when opened from a plan week on Home: the day is shown, started and marked finished for that week.
    int? _week;
    // Exercises opened for editing stay open when the list is rebuilt (e.g. after a drag).
    readonly HashSet<PlanExercise> _expanded = [];
    // The plan as being edited. The page always shows this; Save copies it into the saved plan.
    WorkoutPlan? _draft;

    [ObservableProperty] string name = "";
    [ObservableProperty] string meta = "";
    [ObservableProperty] bool isActive;
    [ObservableProperty] List<PlanDayChip> days = [];
    /// <summary>The draft differs from the saved plan.</summary>
    [ObservableProperty] bool hasChanges;

    // The selected day
    [ObservableProperty] string dayName = "";
    [ObservableProperty] string dayLabel = "";
    [ObservableProperty] string dayMeta = "";
    [ObservableProperty] bool isRestDay;
    [ObservableProperty] bool isWorkoutDay;
    [ObservableProperty] bool isEmptyDay;
    [ObservableProperty] IDrawable dayMap = MuscleMapDrawable.Empty;
    [ObservableProperty] List<PlanDayExercise> dayExercises = [];
    [ObservableProperty] bool isDayDone;

    // The weekly AI check found ways to improve this (active) plan.
    [ObservableProperty] bool hasSuggestions;
    [ObservableProperty] string suggestionsTitle = "";

    public override void OnDisappearing() => ai.SuggestionsChanged -= OnSuggestionsChanged;

    void OnSuggestionsChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (store.GetPlan(_id) is { } plan)
            ShowSuggestions(plan);
    });

    void ShowSuggestions(WorkoutPlan plan)
    {
        var count = ai.PendingSuggestions(plan);
        HasSuggestions = count > 0;
        SuggestionsTitle = count == 1 ? "AI found a way to improve this plan" : $"AI found {count} ways to improve this plan";
    }

    /// <summary>"Not now" on the suggestions card: hidden until the next weekly check finds something.</summary>
    [RelayCommand]
    void DismissSuggestions()
    {
        if (store.GetPlan(_id) is { } plan)
            ai.Dismiss(plan);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = query["id"]?.ToString();
        if (query.TryGetValue("day", out var day) && int.TryParse(day?.ToString(), out var d))
            _selected = d;
        _week = query.TryGetValue("week", out var week) && int.TryParse(week?.ToString(), out var w) ? w : null;
    }

    public override Task OnAppearingAsync()
    {
        var saved = store.GetPlan(_id);
        if (saved == null)
            return GoBack();
        // Without unsaved edits, pick up whatever changed the saved plan meanwhile (the AI, another device). Only when
        // it did change: the page also "appears" again each time a dialog on it closes, and an edit that dialog was
        // asking about (e.g. "Delete workout?") is about to go into this same draft.
        if (_draft == null || (!HasChanges && !SameEdits(_draft, saved)))
            _draft = LocalJson.Clone(saved);
        else
            CopyProgress(saved, _draft);
        Refresh();
        return Task.CompletedTask;
    }

    /// <summary>Rebuilds the page from the draft.</summary>
    void Refresh()
    {
        if (_draft is not { } plan || store.GetPlan(_id) is not { } saved)
            return;
        HasChanges = !SameEdits(plan, saved);
        Name = plan.Name;
        IsActive = plan.Id == store.Data.ActivePlanId;
        ai.SuggestionsChanged -= OnSuggestionsChanged;
        ai.SuggestionsChanged += OnSuggestionsChanged;
        ShowSuggestions(saved);
        var progress = new PlanProgress(plan, store.History);
        var schedule = progress.Days;
        Meta = $"{plan.Goal.Display()} · {plan.Workouts.Count} training days · {schedule.Count - plan.Workouts.Count} rest";
        // Where the plan is in its cycle this week (deload, block week); nothing for a plan run the same every week.
        if (PlanCycle.Describe(plan, _week ?? progress.CurrentWeek) is { } phase)
            Meta += $"\nWeek {_week ?? progress.CurrentWeek} · {phase}";
        _selected = Math.Clamp(_selected, 0, Math.Max(0, schedule.Count - 1));

        Days = schedule.Select((w, i) => new PlanDayChip
        {
            Title = (_week is { } week && progress.IsDayDone(i, week) ? "✓ " : "") + (w?.Name ?? "Rest"),
            IsRest = w == null,
            IsSelected = i == _selected,
            SelectCommand = new RelayCommand(() => Select(i)),
            MoveCommand = new RelayCommand<int>(to => MoveDay(i, to)),
        }).ToList();

        var day = schedule.ElementAtOrDefault(_selected);
        var shownWeek = _week ?? progress.CurrentWeek;
        DayLabel = _week == null ? $"Day {_selected + 1}" : $"Week {_week} · Day {_selected + 1}";
        IsDayDone = _week != null && schedule.Count > 0 && progress.IsDayDone(_selected, shownWeek);
        IsRestDay = day == null;
        IsWorkoutDay = day != null;
        DayName = day?.Name ?? "Rest";

        var exercises = day?.Exercises.Select(pe => (pe, ex: store.GetExercise(pe.ExerciseId))).ToList() ?? [];
        UpdateDayMeta(plan, day);
        DayMap = MuscleMapDrawable.ForWorkout(exercises.Select(x => x.ex).OfType<Exercise>());
        _expanded.IntersectWith(exercises.Select(x => x.pe));
        DayExercises = exercises.Select(x =>
        {
            PlanDayExercise? item = null;
            item = new PlanDayExercise(x.pe, x.ex, plan.UseRir, () => ExerciseChanged(plan, day))
            {
                IsExpanded = _expanded.Contains(x.pe),
                OpenCommand = new AsyncRelayCommand(() => x.ex == null ? Task.CompletedTask : GoTo($"{Routes.Exercise}?id={x.ex.Id}")),
                RemoveCommand = new RelayCommand(() => RemoveExercise(item!)),
                MoveCommand = new RelayCommand<int>(to => MoveExercise(item!, to)),
            };
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(PlanDayExercise.IsExpanded))
                    return;
                if (item.IsExpanded)
                    _expanded.Add(item.Model);
                else
                    _expanded.Remove(item.Model);
            };
            return item;
        }).ToList();
    }

    // ---- The draft ----

    /// <summary>What an edit changes: everything but progress (which week, rest days done) and bookkeeping.</summary>
    static string Edits(WorkoutPlan p) => LocalJson.Serialize(new WorkoutPlan
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Goal = p.Goal,
        DaysPerWeek = p.DaysPerWeek,
        Workouts = p.Workouts,
        RestDays = p.RestDays,
        UseRir = p.UseRir,
        Deloads = p.Deloads,
        Periodization = p.Periodization,
        CreatedAt = default,
    });

    static bool SameEdits(WorkoutPlan a, WorkoutPlan b) => Edits(a) == Edits(b);

    static string WorkoutJson(PlanWorkout w) => LocalJson.Serialize(new WorkoutPlan { Id = "", CreatedAt = default, Workouts = [w] });

    // Progress isn't edited here, so the draft always follows the saved plan's.
    static void CopyProgress(WorkoutPlan from, WorkoutPlan to)
    {
        to.NextWorkoutIndex = from.NextWorkoutIndex;
        to.RestDaysDone = from.RestDaysDone == null ? null : [.. from.RestDaysDone];
    }

    /// <summary>After any edit: nothing is saved, the page just shows it (and that there are unsaved changes).</summary>
    void Edited() => Refresh();

    /// <summary>"Save changes" in the plan's ··· menu: the draft becomes the plan.</summary>
    [RelayCommand]
    void Save()
    {
        if (_draft is not { } draft || store.GetPlan(_id) is not { } saved)
            return;
        var copy = LocalJson.Clone(draft);
        saved.Name = copy.Name;
        saved.Description = copy.Description;
        saved.Goal = copy.Goal;
        // Training days per week: its workouts, whatever edits did to its days.
        saved.DaysPerWeek = Math.Max(1, copy.Workouts.Count);
        saved.Workouts = copy.Workouts;
        saved.RestDays = copy.RestDays;
        saved.UseRir = copy.UseRir;
        saved.Deloads = copy.Deloads;
        saved.Periodization = copy.Periodization;
        store.Save();
        Refresh();
    }

    /// <summary>"Discard changes": back to the saved plan.</summary>
    void Discard()
    {
        if (store.GetPlan(_id) is not { } saved)
            return;
        _draft = LocalJson.Clone(saved);
        _expanded.Clear();
        Refresh();
    }

    /// <summary>
    /// With unsaved changes, asks what to do with them first (before leaving, starting a workout, or anything that works
    /// on the saved plan). False when the user backed out, so whatever was about to happen shouldn't.
    /// </summary>
    async Task<bool> SettleChangesAsync(string title = "Save your changes?")
    {
        if (!HasChanges)
            return true;
        switch (await dialogs.ActionSheet(title, "Discard changes", "Save changes"))
        {
            case "Save changes":
                Save();
                return true;
            case "Discard changes":
                Discard();
                return true;
            default:
                return false;
        }
    }

    /// <summary>The back arrow and the back button: unsaved changes are saved or discarded first.</summary>
    [RelayCommand]
    async Task Back()
    {
        if (await SettleChangesAsync("Save changes to this plan?"))
        {
            HasChanges = false;
            await GoBack();
        }
    }

    // ---- Editing ----

    void UpdateDayMeta(WorkoutPlan plan, PlanWorkout? day)
    {
        DayMeta = day == null
            ? "Recovery day. Muscles grow between sessions."
            : $"{day.Exercises.Count} exercises · {day.Exercises.Sum(e => e.Sets)} sets · {WorkoutEstimator.Format(estimator.Minutes(day, plan.Goal))}";
        IsEmptyDay = day != null && day.Exercises.Count == 0;
    }

    // A stepper on an open exercise: the totals under the day name follow; the list itself stays as it is.
    void ExerciseChanged(WorkoutPlan plan, PlanWorkout? day)
    {
        UpdateDayMeta(plan, day);
        if (store.GetPlan(_id) is { } saved)
            HasChanges = !SameEdits(plan, saved);
    }

    void RemoveExercise(PlanDayExercise item)
    {
        if (_draft is not { } plan || SelectedWorkout(plan) is not { } workout)
            return;
        workout.Exercises.Remove(item.Model);
        Edited();
    }

    /// <summary>An exercise dragged to position <paramref name="to"/>; the ones in between shift over.</summary>
    void MoveExercise(PlanDayExercise item, int to)
    {
        if (_draft is not { } plan || SelectedWorkout(plan) is not { } workout)
            return;
        var from = workout.Exercises.IndexOf(item.Model);
        if (from < 0 || to < 0 || to >= workout.Exercises.Count || from == to)
            return;
        workout.Exercises.RemoveAt(from);
        workout.Exercises.Insert(to, item.Model);
        Edited();
    }

    /// <summary>A day dragged from position <paramref name="from"/> to <paramref name="to"/>; it stays selected if it was.</summary>
    void MoveDay(int from, int to)
    {
        if (_draft is not { } plan)
            return;
        var days = PlanSchedule.Days(plan);
        if (from == to || from >= days.Count || to < 0 || to >= days.Count)
            return;
        var day = days[from];
        days.RemoveAt(from);
        days.Insert(to, day);
        // The selection follows whichever day it was on.
        if (_selected == from)
            _selected = to;
        else if (from < _selected && to >= _selected)
            _selected--;
        else if (from > _selected && to <= _selected)
            _selected++;
        SetDays(plan, days);
    }

    /// <summary>The button under the selected day's exercises.</summary>
    [RelayCommand]
    async Task AddExercise()
    {
        if (_draft is not { } plan || SelectedWorkout(plan) is not { } workout)
            return;
        var picked = await picker.PickAsync();
        // Set up for the plan's goal straight away; tap one to adjust. The same exercise can be in a workout more than
        // once (e.g. a heavy and a light block).
        foreach (var ex in picked)
            workout.Exercises.Add(TrainingGoals.Prescription(plan.Goal, store.Profile.Experience, ex, store.Profile));
        if (picked.Count > 0)
            Edited();
    }

    void Select(int index)
    {
        _selected = index;
        Refresh();
    }

    PlanWorkout? SelectedWorkout(WorkoutPlan plan) => PlanSchedule.Days(plan).ElementAtOrDefault(_selected);

    void SetDays(WorkoutPlan plan, List<PlanWorkout?> days)
    {
        PlanSchedule.SetDays(plan, days);
        Edited();
    }

    /// <summary>Opens the muscle breakdown for the selected day, with a switch to the whole plan.</summary>
    [RelayCommand]
    Task OpenMuscles()
    {
        if (_draft is not { } plan)
            return Task.CompletedTask;
        var page = new Views.MuscleBreakdownPage(new MuscleBreakdownViewModel(store, plan, SelectedWorkout(plan)));
        return Shell.Current.Navigation.PushModalAsync(page, false);
    }

    /// <summary>
    /// The ··· beside the day name: rename or delete the workout, remove the rest day, or undo this day's unsaved
    /// changes. Days move by dragging.
    /// </summary>
    [RelayCommand]
    async Task DayOptions()
    {
        if (_draft is not { } plan)
            return;
        var days = PlanSchedule.Days(plan);
        if (_selected >= days.Count)
            return;
        var day = days[_selected];
        var saved = store.GetPlan(_id)?.Workouts.FirstOrDefault(w => w.Id == day?.Id);
        // A workout edited since the last save, or added since (then discarding removes it).
        var dayChanged = day != null && (saved == null || WorkoutJson(saved) != WorkoutJson(day));
        var options = new List<string>();
        if (dayChanged)
            options.Add("Discard changes to this day");
        if (day != null)
            options.Add("Rename workout");
        switch (await dialogs.ActionSheet(DayName, day == null ? "Remove rest day" : "Delete workout", [.. options]))
        {
            case "Discard changes to this day":
                if (saved == null)
                {
                    days.RemoveAt(_selected);
                    SetDays(plan, days);
                }
                else
                {
                    var original = LocalJson.Clone(new WorkoutPlan { Workouts = [saved] }).Workouts[0];
                    day!.Name = original.Name;
                    day.Exercises = original.Exercises;
                    Edited();
                }
                break;
            case "Rename workout":
                var name = await dialogs.Prompt("Rename workout", "Workout name", day!.Name);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    day.Name = name.Trim();
                    Edited();
                }
                break;
            case "Delete workout":
                if (await dialogs.Confirm("Delete workout?", $"Remove \"{day!.Name}\" from the plan? Your workout history is kept.", "Delete"))
                {
                    // Through the schedule, so the rest days around it stay where they were.
                    days.RemoveAt(_selected);
                    SetDays(plan, days);
                }
                break;
            case "Remove rest day":
                days.RemoveAt(_selected);
                SetDays(plan, days);
                break;
        }
    }

    /// <summary>The "+" at the end of the day strip.</summary>
    [RelayCommand]
    async Task AddDay()
    {
        if (_draft is not { } plan)
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
                SetDays(plan, days);
                break;
        }
    }

    async Task AddWorkout(WorkoutPlan plan)
    {
        var name = await dialogs.Prompt("New workout", "Workout name", $"Workout {(char)('A' + plan.Workouts.Count)}", accept: "Add");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var days = PlanSchedule.Days(plan);
        days.Add(new PlanWorkout { Name = name.Trim() });
        // Shown straight away, empty, with the button to add its exercises.
        _selected = days.Count - 1;
        SetDays(plan, days);
        await AddExercise();
    }

    /// <summary>Not an edit of the plan: which plan is active applies straight away.</summary>
    [RelayCommand]
    void SetActive()
    {
        store.Data.ActivePlanId = _id;
        store.Save();
        Refresh();
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
        Edited();
    }

    /// <summary>RIR, deloads and periodization for this plan, switched on a sheet; part of the draft like any edit.</summary>
    Task TrainingOptions(WorkoutPlan plan) =>
        Shell.Current.Navigation.PushModalAsync(new Views.PlanOptionsPage(new PlanOptionsViewModel(plan, Edited)), false);

    /// <summary>The AI's suggestions from the workouts logged on the plan; applied ones are saved, and this page reloads when it closes.</summary>
    [RelayCommand]
    async Task ImproveWithAi()
    {
        if (store.GetPlan(_id) is not { } plan)
            return;
        if (!ai.IsAvailable)
        {
            await dialogs.Alert("Sign in to use AI", "AI suggestions need an account. Sign in from the Profile tab.");
            return;
        }
        // Suggestions are made for, and applied to, the saved plan.
        if (!await SettleChangesAsync("Save your changes first?"))
            return;
        await PlanReviewViewModel.OpenAsync(plan);
    }

    [RelayCommand]
    async Task More()
    {
        if (_draft is not { } plan || store.GetPlan(_id) is not { } saved)
            return;
        var options = new List<string>();
        if (HasChanges)
            options.AddRange(["Save changes", "Discard changes"]);
        options.AddRange(["Training options", "AI suggestions", "Change with AI", "Regenerate plan", "Training goal", "Rename plan", "Duplicate plan"]);
        switch (await dialogs.ActionSheet(plan.Name, "Delete plan", [.. options]))
        {
            case "Save changes":
                Save();
                break;
            case "Discard changes":
                if (await dialogs.Confirm("Discard changes?", "Your unsaved changes to this plan will be lost.", "Discard"))
                    Discard();
                break;
            case "Training options":
                await TrainingOptions(plan);
                break;
            case "AI suggestions":
                await ImproveWithAi();
                break;
            case "Change with AI":
                if (!ai.IsAvailable)
                {
                    await dialogs.Alert("Sign in to use AI", "Changing a plan with AI needs an account. Sign in from the Profile tab.");
                    break;
                }
                if (!await SettleChangesAsync("Save your changes first?"))
                    break;
                // Changes are saved as the AI makes them; this page reloads when the chat closes.
                await PlanChatViewModel.OpenAsync(saved, ai.AnswersFor(saved), save: true);
                break;
            case "Regenerate plan":
                if (!await SettleChangesAsync("Save your changes first?"))
                    break;
                // The questionnaire, filled in from this plan; saving replaces its workouts.
                await GoTo($"{Routes.Wizard}?regenerate={saved.Id}");
                break;
            case "Training goal":
                await ChangeGoal(plan);
                break;
            case "Rename plan":
                var name = await dialogs.Prompt("Rename plan", "Plan name", plan.Name);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    plan.Name = name.Trim();
                    Edited();
                }
                break;
            case "Duplicate plan":
                if (!await SettleChangesAsync("Save your changes first?"))
                    break;
                var copy = LocalJson.Clone(saved);
                copy.Id = Guid.NewGuid().ToString("N");
                copy.Name += " (copy)";
                copy.CreatedAt = DateTime.Now;
                copy.Workouts.ForEach(w => w.Id = Guid.NewGuid().ToString("N"));
                store.Data.Plans.Add(copy);
                store.Save();
                await GoBack();
                break;
            case "Delete plan":
                if (await dialogs.Confirm("Delete plan?", $"\"{saved.Name}\" will be deleted. Your workout history is kept.", "Delete"))
                {
                    store.Data.Plans.Remove(saved);
                    if (store.Data.ActivePlanId == saved.Id)
                        store.Data.ActivePlanId = store.Data.Plans.FirstOrDefault()?.Id;
                    store.Save();
                    HasChanges = false;
                    await GoBack();
                }
                break;
        }
    }
}

/// <summary>
/// The plan's training options: reps in reserve, deload weeks and periodization (see <see cref="PlanCycle"/>). They
/// change the plan being edited, which is saved with the rest of its changes.
/// </summary>
public partial class PlanOptionsViewModel : ObservableObject
{
    readonly WorkoutPlan _plan;
    readonly Action _changed;

    public PlanOptionsViewModel(WorkoutPlan plan, Action changed)
    {
        (_plan, _changed) = (plan, changed);
        useRir = plan.UseRir;
        deloads = plan.Deloads;
        periodization = plan.Periodization;
    }

    [ObservableProperty] bool useRir;
    [ObservableProperty] bool deloads;
    [ObservableProperty] bool periodization;

    partial void OnUseRirChanged(bool value) => Set(() => _plan.UseRir = value);
    partial void OnDeloadsChanged(bool value) => Set(() => _plan.Deloads = value);
    partial void OnPeriodizationChanged(bool value) => Set(() => _plan.Periodization = value);

    void Set(Action apply)
    {
        apply();
        _changed();
    }
}
