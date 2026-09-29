using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// A plan's settings, from its ··· menu: its rest times, warm-ups and training. Each part either uses the profile's
/// defaults, following them as they change, or is the plan's own, starting from them. And for each, a way to put every
/// exercise that has its own back on the plan's. Edits go into the Edit plan page's draft, like any other edit there.
/// </summary>
public partial class PlanSettingsViewModel : ObservableObject
{
    readonly WorkoutPlan _plan;
    readonly DataStore _store;
    readonly DialogService _dialogs;
    readonly Units _units;
    readonly Action _edited;
    bool _loading;

    public PlanSettingsViewModel(WorkoutPlan plan, DataStore store, DialogService dialogs, Units units, Action edited)
    {
        (_plan, _store, _dialogs, _units, _edited) = (plan, store, dialogs, units, edited);
        Refresh();
    }

    UserProfile P => _store.Profile;
    List<PlanExercise> Exercises => [.. _plan.Workouts.SelectMany(w => w.Exercises)];

    // ---- Rest ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RestOpacity))]
    bool ownRest;
    public double RestOpacity => OwnRest ? 1 : 0.5;
    [ObservableProperty] string compoundRestText = "";
    [ObservableProperty] string isolationRestText = "";
    [ObservableProperty] string warmupRestText = "";
    [ObservableProperty] string ownRestExercisesText = "";
    [ObservableProperty] bool hasOwnRestExercises;

    // ---- Warm-ups ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WarmupsOpacity))]
    bool ownWarmups;
    public double WarmupsOpacity => OwnWarmups ? 1 : 0.5;
    [ObservableProperty] WarmupSettingsViewModel? warmups;
    [ObservableProperty] string ownWarmupsExercisesText = "";
    [ObservableProperty] bool hasOwnWarmupsExercises;

    // ---- Training ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrainingOpacity))]
    bool ownTraining;
    public double TrainingOpacity => OwnTraining ? 1 : 0.5;
    [ObservableProperty] bool useRir;
    [ObservableProperty] bool deloads;
    [ObservableProperty] bool periodization;
    [ObservableProperty] string targetRirText = "";
    [ObservableProperty] string ownRirExercisesText = "";
    [ObservableProperty] bool hasOwnRirExercises;
    [ObservableProperty] string ownWeeksExercisesText = "";
    [ObservableProperty] bool hasOwnWeeksExercises;

    static string Count(int n, string what) => n == 1 ? $"1 exercise has its own {what}" : $"{n} exercises have their own {what}";

    void Refresh()
    {
        _loading = true;
        var exercises = Exercises;

        OwnRest = _plan.OwnRest;
        string Rest(Mechanic m) => Units.Rest(_plan.OwnRest && (m == Mechanic.Compound ? _plan.CompoundRestSeconds : _plan.IsolationRestSeconds) is { } own
            ? own : PlanRest.Typical(_plan.Goal, P, m));
        CompoundRestText = Rest(Mechanic.Compound);
        IsolationRestText = Rest(Mechanic.Isolation);
        WarmupRestText = Units.Rest(PlanRest.Warmup(_plan, P));
        var ownRest = exercises.Count(e => e.CustomRest);
        (HasOwnRestExercises, OwnRestExercisesText) = (ownRest > 0, Count(ownRest, "rest"));

        OwnWarmups = _plan.Warmups != null;
        Warmups = new WarmupSettingsViewModel(
            () => WarmupSettings.For(_plan, P),
            () => WarmupSettings.Global(P),
            // Kept as the plan's own even when the same as the defaults, so it stays put when they change.
            s =>
            {
                _plan.Warmups = (s ?? WarmupSettings.Global(P)).ToJson();
                _edited();
            },
            // No reset row: Use defaults above is the way back.
            () => false,
            "", OwnWarmups ? "this plan's own warm-ups" : "your default warm-ups", _dialogs, _units);
        var ownWarmups = exercises.Count(e => e.Warmups != null);
        (HasOwnWarmupsExercises, OwnWarmupsExercisesText) = (ownWarmups > 0, Count(ownWarmups, "warm-ups"));

        OwnTraining = _plan.OwnTraining;
        (UseRir, Deloads, Periodization) = (_plan.UseRir, _plan.Deloads, _plan.Periodization);
        TargetRirText = _plan is { OwnTraining: true, TargetRir: { } rir } ? $"{rir} in reserve" : "By goal";
        var ownRir = exercises.Count(e => e.CustomRir);
        (HasOwnRirExercises, OwnRirExercisesText) = (ownRir > 0, Count(ownRir, "target RIR"));
        var ownWeeks = exercises.Count(e => e.Deloads != null || e.Periodization != null);
        (HasOwnWeeksExercises, OwnWeeksExercisesText) = (ownWeeks > 0, Count(ownWeeks, "deloads or periodization"));
        _loading = false;
    }

    void Changed()
    {
        _edited();
        Refresh();
    }

    // ---- Rest ----

    partial void OnOwnRestChanged(bool value)
    {
        if (_loading)
            return;
        if (value)
            PlanRest.MakeOwn(_plan, P);
        else
            PlanRest.UseDefaults(_plan);
        // Exercises with a rest of their own keep it.
        PlanRest.Apply(_plan, P, _store.GetExercise);
        Changed();
    }

    static readonly int[] RestChoices = [30, 45, 60, 75, 90, 120, 150, 180, 240, 300];
    static readonly int[] WarmupRestChoices = [30, 45, 60, 75, 90, 120, 150];

    async Task<int?> PickRest(string title, int[] choices)
    {
        var labels = choices.Select(Units.Rest).ToList();
        var pick = await _dialogs.ActionSheet(title, null, [.. labels]);
        var index = pick == null ? -1 : labels.IndexOf(pick);
        return index < 0 ? null : choices[index];
    }

    [RelayCommand]
    async Task EditCompoundRest()
    {
        if (OwnRest && await PickRest("Rest for compound lifts", RestChoices) is { } seconds)
        {
            _plan.CompoundRestSeconds = seconds;
            PlanRest.Apply(_plan, P, _store.GetExercise);
            Changed();
        }
    }

    [RelayCommand]
    async Task EditIsolationRest()
    {
        if (OwnRest && await PickRest("Rest for isolation exercises", RestChoices) is { } seconds)
        {
            _plan.IsolationRestSeconds = seconds;
            PlanRest.Apply(_plan, P, _store.GetExercise);
            Changed();
        }
    }

    [RelayCommand]
    async Task EditWarmupRest()
    {
        if (OwnRest && await PickRest("Rest after a warm-up set", WarmupRestChoices) is { } seconds)
        {
            _plan.WarmupRestSeconds = seconds;
            Changed();
        }
    }

    [RelayCommand]
    async Task RestToAll()
    {
        if (!await _dialogs.Confirm("Use the plan's rest for every exercise?", $"{OwnRestExercisesText}; it goes back to the plan's.", "Use the plan's"))
            return;
        PlanRest.ApplyToAll(_plan, P, _store.GetExercise);
        Changed();
    }

    // ---- Warm-ups ----

    partial void OnOwnWarmupsChanged(bool value)
    {
        if (_loading)
            return;
        // Its own start as a copy of the defaults.
        _plan.Warmups = value ? WarmupSettings.Global(P).ToJson() : null;
        Changed();
    }

    [RelayCommand]
    async Task WarmupsToAll()
    {
        if (!await _dialogs.Confirm("Use the plan's warm-ups for every exercise?", $"{OwnWarmupsExercisesText}; they go back to the plan's.", "Use the plan's"))
            return;
        foreach (var pe in Exercises)
            pe.Warmups = null;
        Changed();
    }

    // ---- Training ----

    partial void OnOwnTrainingChanged(bool value)
    {
        if (_loading)
            return;
        var target = _plan.OwnTraining ? _plan.TargetRir : null;
        if (value)
            PlanTraining.MakeOwn(_plan);
        else
            PlanTraining.UseDefaults(_plan, P);
        // Only when the plan's target RIR changed does it reach the exercises; otherwise they stay as they are.
        if ((_plan.OwnTraining ? _plan.TargetRir : null) != target)
            PlanTraining.ApplyRir(_plan, P, _store.GetExercise);
        Changed();
    }

    partial void OnUseRirChanged(bool value) => SetTraining(() => _plan.UseRir = value);
    partial void OnDeloadsChanged(bool value) => SetTraining(() => _plan.Deloads = value);
    partial void OnPeriodizationChanged(bool value) => SetTraining(() => _plan.Periodization = value);

    void SetTraining(Action change)
    {
        if (_loading || !OwnTraining)
            return;
        change();
        Changed();
    }

    [RelayCommand]
    async Task EditTargetRir()
    {
        if (!OwnTraining)
            return;
        List<int?> values = [null, 0, 1, 2, 3, 4, 5];
        var labels = values.Select(v => v is { } r ? $"{r} in reserve" : "By goal (each exercise's usual)").ToList();
        var pick = await _dialogs.ActionSheet("Target reps in reserve, for exercises without their own", null, [.. labels]);
        var index = pick == null ? -1 : labels.IndexOf(pick);
        if (index < 0)
            return;
        _plan.TargetRir = values[index];
        PlanTraining.ApplyRir(_plan, P, _store.GetExercise);
        Changed();
    }

    [RelayCommand]
    async Task RirToAll()
    {
        if (!await _dialogs.Confirm("Use the plan's target RIR for every exercise?", $"{OwnRirExercisesText}; it goes back to the plan's.", "Use the plan's"))
            return;
        PlanTraining.ApplyRirToAll(_plan, P, _store.GetExercise);
        Changed();
    }

    [RelayCommand]
    async Task WeeksToAll()
    {
        if (!await _dialogs.Confirm("Use the plan's deloads and periodization for every exercise?", $"{OwnWeeksExercisesText}; they go back to the plan's.", "Use the plan's"))
            return;
        PlanTraining.WeeksToAll(_plan);
        Changed();
    }
}
