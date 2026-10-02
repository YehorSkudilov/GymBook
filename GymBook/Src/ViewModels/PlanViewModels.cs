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
    /// <summary>For the active plan's card: where it is this week, e.g. "Week 3 · 2 of 5 workouts done".</summary>
    public string WeekText { get; init; } = "";
    /// <summary>This week's share of its workouts done, 0 to 1, for the card's ring.</summary>
    public double WeekProgress { get; init; }
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
/// One day on the Edit plan page, on its own page in the day pager: a workout with its exercises, or a rest day.
/// What it does (add an exercise, its ··· menu, the muscle map) is on <see cref="Plan"/>, for the day on screen.
/// </summary>
public class PlanDetailDay
{
    public required PlanDetailViewModel Plan { get; init; }
    /// <summary>The workout, or null for a rest day.</summary>
    public required PlanWorkout? Workout { get; init; }
    public required string DayName { get; init; }
    public required string DayLabel { get; init; }
    public required string DayMeta { get; init; }
    public bool IsRestDay => Workout == null;
    public bool IsWorkoutDay => Workout != null;
    public required bool IsEmptyDay { get; init; }
    /// <summary>Opened from a plan week on Home, and that week's session of it is logged.</summary>
    public required bool IsDayDone { get; init; }
    public required IDrawable DayMap { get; init; }
    public required List<PlanDayExercise> DayExercises { get; init; }
}

/// <summary>
/// An exercise of the day on the Edit plan page. Its sets and reps are picked from the pills on the row itself; opened,
/// it has its target RIR, rest, warm-ups, and deloads and periodization. Each of those is the plan's unless set here,
/// and can go back to it.
/// </summary>
public partial class PlanDayExercise(PlanExercise model, Exercise? exercise, WorkoutPlan plan, UserProfile profile, Action changed, DialogService dialogs)
    : ObservableObject
{
    public PlanExercise Model { get; } = model;
    public ExerciseThumb Thumb { get; } = exercise == null ? new ExerciseThumb(null, "?", Colors.Gray, Colors.Gray.WithAlpha(0.16f)) : ExerciseThumb.For(exercise);
    public string Name => exercise?.Name ?? "Unknown exercise";
    public string Equipment => exercise?.Equipment.Display() ?? "";
    public bool HasInfo => exercise != null;
    /// <summary>The plan uses reps in reserve; otherwise its target isn't shown or changed.</summary>
    public bool ShowRir { get; } = plan.UseRir;
    /// <summary>Bodyweight moves get no warm-up sets, so there's nothing to set.</summary>
    public bool ShowWarmups => exercise is { IsBodyweight: false };

    /// <summary>
    /// The note pinned to it: each workout of the day starts with it (pinned there, or written here). Goes into the draft
    /// as it's typed; the page only shows the change once the field is left, so typing isn't interrupted.
    /// </summary>
    public string Note
    {
        get => Model.Note ?? "";
        set
        {
            Model.Note = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged(nameof(HasNote));
        }
    }

    /// <summary>It has a note pinned: the pin beside it unpins it.</summary>
    public bool HasNote => Model.Note != null;

    internal void NoteDone() => changed();

    /// <summary>The pin: takes the note off the exercise in the plan. Workouts already done keep theirs.</summary>
    [RelayCommand]
    async Task Unpin()
    {
        if (Model.Note == null || !await dialogs.Confirm("Unpin the note?",
                $"\u201c{Model.Note}\u201d comes off {Name} in this plan, so workouts of this day no longer start with it. Workouts you've done keep it.", "Unpin"))
            return;
        Model.Note = null;
        changed();
    }

    // Where the opened row's cards go, two to a row: Rest, then RIR and Warm-ups when shown, then Week to week, which
    // takes the whole row when it's left on its own.
    int WarmupsIndex => ShowRir ? 2 : 1;
    int WeeksIndex => 1 + (ShowRir ? 1 : 0) + (ShowWarmups ? 1 : 0);
    public int WarmupsRow => WarmupsIndex / 2;
    public int WarmupsColumn => WarmupsIndex % 2;
    public int WeeksRow => WeeksIndex / 2;
    public int WeeksColumn => WeeksIndex % 2;
    public int WeeksSpan => WeeksIndex % 2 == 0 ? 2 : 1;
    /// <summary>No gap under the first row when there's no second.</summary>
    public double CardsRowSpacing => WeeksRow == 0 ? 0 : 10;

    // What the plan gives it, when it has none of its own.
    int PlanRest => exercise == null ? Model.RestSeconds : Services.PlanRest.DefaultFor(plan, profile, exercise);
    int PlanRir => exercise == null ? Model.TargetRir : PlanTraining.RirFor(plan, profile, exercise);
    IReadOnlyList<WarmupStep> PlanWarmups => exercise == null ? [] : WarmupSettings.For(plan, profile).StepsFor(null, exercise, false);

    public string Sets => Model.Sets == 1 ? "1 set" : $"{Model.Sets} sets";
    public string Reps => Model.RepMin == Model.RepMax ? $"{Model.RepMin} reps" : $"{Model.RepMin}–{Model.RepMax} reps";
    public string SetsText => Model.Sets.ToString();
    public string RepsText => $"{Model.RepMin}–{Model.RepMax}";

    public string RirText => Model.TargetRir.ToString();
    /// <summary>Set for this exercise itself, not the plan's: it can go back.</summary>
    public bool IsCustomRir => Model.CustomRir;
    public string RirSource => Model.CustomRir ? $"Plan: {PlanRir}" : "Plan's";

    public string RestText => Units.Rest(Model.RestSeconds);
    public bool IsCustomRest => Model.CustomRest;
    public string RestSource => Model.CustomRest ? $"Plan: {Units.Rest(PlanRest)}" : "Plan's";

    static string SetCount(int n) => n switch { 0 => "None", 1 => "1 set", _ => $"{n} sets" };
    public string WarmupsText => SetCount((WarmupSettings.ParseSteps(Model.Warmups) ?? PlanWarmups).Count);
    public bool IsCustomWarmups => Model.Warmups != null;
    public string WarmupsSource => WarmupSettings.ParseSteps(Model.Warmups) is { } steps ? WarmupSettings.Describe(steps) : "Plan's";

    public string WeeksText
    {
        get
        {
            var (deloads, periodization) = PlanTraining.Weeks(plan, Model);
            // A deload week only comes in a plan that has them.
            var parts = new List<string>();
            if (deloads && plan.Deloads)
                parts.Add("Deloads");
            if (periodization)
                parts.Add("Builds");
            return parts.Count == 0 ? "Same weekly" : string.Join(" · ", parts);
        }
    }
    public bool IsCustomWeeks => Model.Deloads != null || Model.Periodization != null;
    public string WeeksSource => IsCustomWeeks ? "Own" : "Plan's";

    public required IAsyncRelayCommand OpenCommand { get; init; }
    public required IRelayCommand RemoveCommand { get; init; }
    /// <summary>Swaps it for another exercise from the catalogue, in the same place.</summary>
    public required IAsyncRelayCommand ReplaceCommand { get; init; }
    /// <summary>Moves the exercise to the position it was dragged to (see <see cref="Controls.ReorderItem"/>).</summary>
    public required IRelayCommand<int> MoveCommand { get; init; }

    [ObservableProperty] bool isExpanded;

    [RelayCommand] void Toggle() => IsExpanded = !IsExpanded;

    void Changed()
    {
        foreach (var p in new[]
        {
            nameof(Sets), nameof(Reps), nameof(SetsText), nameof(RepsText), nameof(RirText), nameof(IsCustomRir), nameof(RirSource),
            nameof(RestText), nameof(IsCustomRest), nameof(RestSource), nameof(WarmupsText), nameof(IsCustomWarmups), nameof(WarmupsSource),
            nameof(WeeksText), nameof(IsCustomWeeks), nameof(WeeksSource),
        })
            OnPropertyChanged(p);
        changed();
    }

    // A rest changed here is this exercise's own (unless it lands back on the plan's), so the plan's no longer moves it.
    void SetRest(int seconds)
    {
        (Model.RestSeconds, Model.CustomRest) = (seconds, seconds != PlanRest);
        Changed();
    }

    /// <summary>Back to the plan's rest, following it from now on.</summary>
    [RelayCommand]
    void ResetRest()
    {
        (Model.RestSeconds, Model.CustomRest) = (PlanRest, false);
        Changed();
    }

    /// <summary>Back to the plan's target RIR, following it from now on.</summary>
    [RelayCommand]
    void ResetRir()
    {
        (Model.TargetRir, Model.CustomRir) = (PlanRir, false);
        Changed();
    }

    /// <summary>Back to the plan's warm-ups.</summary>
    [RelayCommand]
    void ResetWarmups()
    {
        Model.Warmups = null;
        Changed();
    }

    /// <summary>Back to the plan's deloads and periodization.</summary>
    [RelayCommand]
    void ResetWeeks()
    {
        (Model.Deloads, Model.Periodization) = (null, null);
        Changed();
    }

    /// <summary>The sets pill: how many working sets, typed or stepped.</summary>
    [RelayCommand]
    async Task PickSets()
    {
        if (await dialogs.Numbers(Name, "Working sets", "Save", new Views.NumberField("Sets", Model.Sets, 0, Views.NumberField.NoLimit)) is not [var sets])
            return;
        Model.Sets = sets;
        Changed();
    }

    /// <summary>The target RIR pill: reps in reserve, typed or stepped; its own unless it lands on the plan's.</summary>
    [RelayCommand]
    async Task PickRir()
    {
        if (await dialogs.Numbers(Name, "Target reps in reserve", "Save", new Views.NumberField("RIR", Model.TargetRir, 0, Views.NumberField.NoLimit)) is not [var rir])
            return;
        (Model.TargetRir, Model.CustomRir) = (rir, rir != PlanRir);
        Changed();
    }

    /// <summary>The rest pill: the time between sets, typed ("2:30") or stepped, the same popup as in a workout.</summary>
    [RelayCommand]
    async Task PickRest()
    {
        if (await dialogs.RestTime($"{Name} · rest between sets", Model.RestSeconds) is { } seconds)
            SetRest(seconds);
    }

    const string CustomWarmups = "Custom…";

    /// <summary>The warm-ups pill: the plan's, none, a common ramp, or typed in.</summary>
    [RelayCommand]
    async Task PickWarmups()
    {
        var planLabel = $"The plan's ({WarmupSettings.Describe(PlanWarmups)})";
        var presets = WarmupSettingsViewModel.Presets;
        var labels = presets.Select(p => p.Length == 0 ? "None" : $"{SetCount(p.Length)}: {WarmupSettings.Describe(p)}").ToList();
        var choice = await dialogs.ActionSheet($"{Name} · warm-ups, % of the working weight × reps", null, [planLabel, .. labels, CustomWarmups]);
        if (choice == null)
            return;
        if (choice == planLabel)
        {
            ResetWarmups();
            return;
        }
        List<WarmupStep>? steps;
        if (choice == CustomWarmups)
        {
            var current = WarmupSettings.ParseSteps(Model.Warmups) ?? PlanWarmups;
            var text = await dialogs.Prompt(Name, "Each warm-up set as percent × reps, lightest first, e.g. 50x8, 75x4",
                WarmupSettings.FormatSteps(current), Keyboard.Text, "Save");
            if (text == null)
                return;
            steps = WarmupSettings.ParseSteps(text);
            if (steps == null)
            {
                await dialogs.Alert("Couldn't read that", "Write each set as percent x reps (10–95% and 1–20 reps, up to 5 sets), e.g. 50x8, 75x4. Leave it empty for none.");
                return;
            }
        }
        else
        {
            steps = [.. presets[labels.IndexOf(choice)]];
        }
        Model.Warmups = WarmupSettings.FormatSteps(steps);
        Changed();
    }

    /// <summary>
    /// The deloads and periodization pill: switched in place, each becoming this exercise's own; or back to the plan's.
    /// </summary>
    [RelayCommand]
    async Task PickWeeks()
    {
        var (deloads, periodization) = PlanTraining.Weeks(plan, Model);
        const string usePlan = "Use the plan's";
        Views.MenuSwitch[] switches =
        [
            new("Deload weeks", plan.Deloads ? "Lighter in the plan's deload weeks" : "The plan has no deload weeks now",
                deloads, on => { Model.Deloads = on; Changed(); }),
            new("Periodization", "Builds over each 4-week block", periodization, on => { Model.Periodization = on; Changed(); }),
        ];
        if (await dialogs.ActionSheet($"{Name} · week to week", null, switches, IsCustomWeeks ? new[] { usePlan } : []) == usePlan)
            ResetWeeks();
    }

    /// <summary>
    /// The reps pill: an exact number of reps, or a range (lowest and highest), typed or stepped. Exact is kept as the
    /// same number twice.
    /// </summary>
    [RelayCommand]
    async Task PickReps()
    {
        if (await dialogs.Reps(Name, Model.RepMin, Model.RepMax) is not { } reps)
            return;
        (Model.RepMin, Model.RepMax) = reps;
        Changed();
    }
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

    /// <summary>The Plans tab is the one on screen: the active plan card animates only then.</summary>
    [ObservableProperty] bool isShowing;

    public override void OnDisappearing() => IsShowing = false;

    /// <summary>The plan's current week and how much of it is done.</summary>
    (string Text, double Progress) WeekOf(WorkoutPlan plan)
    {
        if (plan.Workouts.Count == 0)
            return ("", 0);
        var progress = new PlanProgress(plan, store.History);
        var week = progress.CurrentWeek;
        var done = progress.WorkoutsDone(week);
        return ($"Week {week} · {done} of {plan.Workouts.Count} workouts done", (double)done / plan.Workouts.Count);
    }

    public override Task OnAppearingAsync()
    {
        IsShowing = true;
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
            WeekText = WeekOf(p).Text,
            WeekProgress = WeekOf(p).Progress,
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

    /// <summary>The import sheet: a plan read by AI (needs an account), or another app's plans and workout history (CSV).</summary>
    [RelayCommand]
    Task Import() => GoTo(Routes.ImportPlan);

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
        // It follows the profile's defaults until given its own.
        PlanTraining.Apply(plan, store.Profile);
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
    ExercisePickerService picker, Units units)
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

    /// <summary>Every day of the plan, in order, each on its own page that the day strip and swiping move between.</summary>
    [ObservableProperty] List<PlanDetailDay> dayPages = [];
    /// <summary>The day on screen, an index into <see cref="DayPages"/>.</summary>
    [ObservableProperty] int selectedDay;

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

    /// <summary>"Next week" on the suggestions card: hidden until the next weekly check finds something.</summary>
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

        // Every day, not just the selected one: the page swipes between them, the neighbours sliding in beside it.
        var shownWeek = _week ?? progress.CurrentWeek;
        var everyExercise = schedule.OfType<PlanWorkout>().SelectMany(w => w.Exercises).ToList();
        _expanded.IntersectWith(everyExercise);
        // The selection first: the page moves its pager when the pages change, to the selected one.
        SelectedDay = _selected;
        DayPages = schedule.Select((day, i) => BuildDay(plan, progress, day, i, shownWeek)).ToList();
    }

    /// <summary>One day of the plan as its page shows it.</summary>
    PlanDetailDay BuildDay(WorkoutPlan plan, PlanProgress progress, PlanWorkout? day, int index, int shownWeek)
    {
        var exercises = day?.Exercises.Select(pe => (pe, ex: store.GetExercise(pe.ExerciseId))).ToList() ?? [];
        return new PlanDetailDay
        {
            Plan = this,
            Workout = day,
            DayName = day?.Name ?? "Rest",
            DayLabel = _week == null ? $"Day {index + 1}" : $"Week {_week} · Day {index + 1}",
            DayMeta = day == null
                ? "Recovery day. Muscles grow between sessions."
                : $"{day.Exercises.Count} exercises · {day.Exercises.Sum(e => e.Sets)} sets · {WorkoutEstimator.Format(estimator.Minutes(day, plan.Goal))}",
            IsDayDone = _week != null && progress.IsDayDone(index, shownWeek),
            IsEmptyDay = day != null && day.Exercises.Count == 0,
            DayMap = MuscleMapDrawable.ForWorkout(exercises.Select(x => x.ex).OfType<Exercise>()),
            DayExercises = day == null ? [] : exercises.Select(x =>
            {
                PlanDayExercise? item = null;
                // An edit rebuilds the list: its popup's closing already did (the page "appears" again), so this item may
                // no longer be the one on screen.
                item = new PlanDayExercise(x.pe, x.ex, plan, store.Profile, Edited, dialogs)
                {
                    IsExpanded = _expanded.Contains(x.pe),
                    OpenCommand = new AsyncRelayCommand(() => x.ex == null ? Task.CompletedTask : GoTo($"{Routes.Exercise}?id={x.ex.Id}")),
                    RemoveCommand = new RelayCommand(() => RemoveExercise(day!, item!)),
                    ReplaceCommand = new AsyncRelayCommand(() => ReplaceExercise(day!, item!)),
                    MoveCommand = new RelayCommand<int>(to => MoveExercise(day!, item!, to)),
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
            }).ToList(),
        };
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
        OwnTraining = p.OwnTraining,
        TargetRir = p.TargetRir,
        Warmups = p.Warmups,
        OwnRest = p.OwnRest,
        CompoundRestSeconds = p.CompoundRestSeconds,
        IsolationRestSeconds = p.IsolationRestSeconds,
        WarmupRestSeconds = p.WarmupRestSeconds,
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

    /// <summary>Save in the title bar, or "Save changes" in the plan's ··· menu: the draft becomes the plan.</summary>
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
        saved.OwnTraining = copy.OwnTraining;
        saved.TargetRir = copy.TargetRir;
        saved.Warmups = copy.Warmups;
        saved.OwnRest = copy.OwnRest;
        saved.CompoundRestSeconds = copy.CompoundRestSeconds;
        saved.IsolationRestSeconds = copy.IsolationRestSeconds;
        saved.WarmupRestSeconds = copy.WarmupRestSeconds;
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

    void RemoveExercise(PlanWorkout workout, PlanDayExercise item)
    {
        workout.Exercises.Remove(item.Model);
        Edited();
    }

    /// <summary>
    /// Swaps an exercise for another in the same place, set up for the plan's goal like an added one (reps and rest
    /// suit the new movement), keeping how many sets it had.
    /// </summary>
    async Task ReplaceExercise(PlanWorkout workout, PlanDayExercise item)
    {
        if (_draft is not { } plan || await picker.PickOneAsync($"Replace {item.Name}", store.GetExercise(item.Model.ExerciseId)) is not { } ex)
            return;
        var index = workout.Exercises.IndexOf(item.Model);
        if (index < 0)
            return;
        var pe = TrainingGoals.Prescription(plan.Goal, store.Profile.Experience, ex, store.Profile);
        pe.RestSeconds = PlanRest.DefaultFor(plan, store.Profile, ex);
        pe.TargetRir = PlanTraining.RirFor(plan, store.Profile, ex);
        pe.Sets = item.Model.Sets;
        workout.Exercises[index] = pe;
        Edited();
    }

    /// <summary>An exercise dragged to position <paramref name="to"/>; the ones in between shift over.</summary>
    void MoveExercise(PlanWorkout workout, PlanDayExercise item, int to)
    {
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
        {
            var pe = TrainingGoals.Prescription(plan.Goal, store.Profile.Experience, ex, store.Profile);
            pe.RestSeconds = PlanRest.DefaultFor(plan, store.Profile, ex);
            pe.TargetRir = PlanTraining.RirFor(plan, store.Profile, ex);
            workout.Exercises.Add(pe);
        }
        if (picked.Count > 0)
            Edited();
    }

    void Select(int index)
    {
        _selected = index;
        Refresh();
    }

    /// <summary>A day swiped to (the page is already showing it): it becomes the selected one.</summary>
    public void ShowDay(int index)
    {
        if (index != _selected && index >= 0 && index < DayPages.Count)
            Select(index);
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
        switch (await dialogs.ActionSheet(day?.Name ?? "Rest", day == null ? "Remove rest day" : "Delete workout", [.. options]))
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

    /// <summary>The plan's goal, rest times, warm-ups and training on a sheet; part of the draft like any edit.</summary>
    Task PlanSettings(WorkoutPlan plan) =>
        Shell.Current.Navigation.PushModalAsync(new Views.PlanSettingsPage(new PlanSettingsViewModel(plan, store, dialogs, units, Edited)), false);

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

    /// <summary>"AI" in the ··· menu: the AI's suggestions for the plan, or changing it in a chat.</summary>
    async Task Ai()
    {
        if (store.GetPlan(_id) is not { } saved)
            return;
        var count = ai.PendingSuggestions(saved);
        var suggestions = count > 0 ? $"AI suggestions ({count})" : "AI suggestions";
        var choice = await dialogs.ActionSheet("AI", null, suggestions, "Change with AI");
        if (choice == suggestions)
            await ImproveWithAi();
        else if (choice == "Change with AI")
            await ChangeWithAi();
    }

    async Task ChangeWithAi()
    {
        if (store.GetPlan(_id) is not { } saved)
            return;
        if (!ai.IsAvailable)
        {
            await dialogs.Alert("Sign in to use AI", "Changing a plan with AI needs an account. Sign in from the Profile tab.");
            return;
        }
        if (!await SettleChangesAsync("Save your changes first?"))
            return;
        // Changes are saved as the AI makes them; this page reloads when the chat closes.
        await PlanChatViewModel.OpenAsync(saved, ai.AnswersFor(saved), save: true);
    }

    [RelayCommand]
    async Task More()
    {
        if (_draft is not { } plan || store.GetPlan(_id) is not { } saved)
            return;
        var options = new List<string>();
        if (HasChanges)
            options.AddRange(["Save changes", "Discard changes"]);
        options.AddRange(["Plan settings", "AI", "Regenerate plan", "Rename plan", "Duplicate plan"]);
        switch (await dialogs.ActionSheet(plan.Name, "Delete plan", [.. options]))
        {
            case "Save changes":
                Save();
                break;
            case "Discard changes":
                if (await dialogs.Confirm("Discard changes?", "Your unsaved changes to this plan will be lost.", "Discard"))
                    Discard();
                break;
            case "Plan settings":
                await PlanSettings(plan);
                break;
            case "AI":
                await Ai();
                break;
            case "Regenerate plan":
                if (!await SettleChangesAsync("Save your changes first?"))
                    break;
                // The questionnaire, filled in from this plan; saving replaces its workouts.
                await GoTo($"{Routes.Wizard}?regenerate={saved.Id}");
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
