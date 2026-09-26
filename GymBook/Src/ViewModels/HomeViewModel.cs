using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public partial class HomeViewModel(
    DataStore store,
    WorkoutService workouts,
    RecoveryService recovery,
    StatsService stats,
    Units units,
    DialogService dialogs) : BaseViewModel
{
    [ObservableProperty] string greeting = "";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] bool hasActiveSession;
    [ObservableProperty] string activeSessionText = "";
    [ObservableProperty] bool hasPlan;
    [ObservableProperty] string planName = "";
    [ObservableProperty] string nextWorkoutName = "";
    [ObservableProperty] string nextWorkoutMeta = "";
    [ObservableProperty] string nextWorkoutMuscles = "";
    [ObservableProperty] List<LineItem> nextExercises = [];
    [ObservableProperty] IDrawable muscleMap = MuscleMapDrawable.Empty;
    [ObservableProperty] string recoverySummary = "";
    [ObservableProperty] List<DayItem> weekDays = [];
    [ObservableProperty] string weekWorkouts = "";
    [ObservableProperty] string weekVolume = "";
    [ObservableProperty] string weekSets = "";
    [ObservableProperty] string streak = "";
    [ObservableProperty] List<SessionItem> recent = [];
    [ObservableProperty] bool hasRecent;

    public override Task OnAppearingAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    void Refresh()
    {
        var profile = store.Profile;
        var hour = DateTime.Now.Hour;
        var part = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
        Greeting = string.IsNullOrWhiteSpace(profile.Name) ? part : $"{part}, {profile.Name}";
        DateText = DateTime.Today.ToString("dddd, d MMMM");

        var active = workouts.Active;
        HasActiveSession = active != null;
        ActiveSessionText = active == null ? "" : $"{active.Name} · started {Units.Duration(active.Duration)} ago";

        var plan = store.ActivePlan;
        HasPlan = plan is { Workouts.Count: > 0 };
        if (plan is { Workouts.Count: > 0 })
        {
            var next = plan.Workouts[plan.NextWorkoutIndex % plan.Workouts.Count];
            var exercises = next.Exercises.Select(e => (pe: e, ex: store.GetExercise(e.ExerciseId))).Where(x => x.ex != null).ToList();
            var sets = next.Exercises.Sum(e => e.Sets);
            var minutes = next.Exercises.Sum(e => e.Sets * (45 + e.RestSeconds)) / 60;
            PlanName = plan.Name;
            NextWorkoutName = next.Name;
            NextWorkoutMeta = $"{exercises.Count} exercises · {sets} sets · ~{minutes} min";
            NextWorkoutMuscles = string.Join(" · ", exercises.Select(x => x.ex!.PrimaryMuscle).Distinct().Select(m => m.Display()));
            NextExercises = exercises.Take(5).Select(x => new LineItem
            {
                Title = x.ex!.Name,
                Value = $"{x.pe.Sets} × {x.pe.RepMin}–{x.pe.RepMax}",
            }).ToList();
        }

        var rec = recovery.Compute(DateTime.Now);
        MuscleMap = MuscleMapDrawable.ForRecovery(rec);
        var tired = rec.Where(r => r.Value < 0.6).OrderBy(r => r.Value).Select(r => r.Key.Display()).ToList();
        RecoverySummary = tired.Count == 0
            ? "All muscle groups are recovered and ready to train."
            : $"Still recovering: {string.Join(", ", tired)}";

        var weekStart = StatsService.WeekStart(DateTime.Today);
        var history = store.History.ToList();
        var trainedDays = history.Select(s => s.StartedAt.Date).ToHashSet();
        WeekDays = Enumerable.Range(0, 7).Select(i =>
        {
            var d = weekStart.AddDays(i);
            return new DayItem { Letter = d.ToString("ddd")[..1], Day = d.Day.ToString(), IsToday = d == DateTime.Today, Done = trainedDays.Contains(d) };
        }).ToList();

        var thisWeek = history.Where(s => s.StartedAt >= weekStart).ToList();
        WeekWorkouts = $"{thisWeek.Count}/{Math.Max(profile.DaysPerWeek, plan?.DaysPerWeek ?? 0)}";
        WeekVolume = units.FormatVolume(thisWeek.Sum(stats.SessionVolume));
        WeekSets = thisWeek.Sum(s => s.WorkingSets.Count()).ToString();
        var streakWeeks = stats.WeekStreak();
        Streak = streakWeeks == 1 ? "1 week streak" : $"{streakWeeks} week streak";

        Recent = history.Take(3).Select(s => SessionItem.Create(s, store, stats, units)).ToList();
        HasRecent = Recent.Count > 0;
    }

    [RelayCommand]
    async Task StartNext()
    {
        var plan = store.ActivePlan;
        if (plan is not { Workouts.Count: > 0 })
            return;
        var next = plan.Workouts[plan.NextWorkoutIndex % plan.Workouts.Count];
        await StartWorkoutAsync(workouts, dialogs, () => workouts.StartFromPlan(plan, next));
    }

    [RelayCommand]
    Task Resume() => GoTo(Routes.Workout);

    [RelayCommand]
    Task StartEmpty() => StartWorkoutAsync(workouts, dialogs, workouts.StartEmpty);

    [RelayCommand]
    async Task WorkoutOptions()
    {
        var plan = store.ActivePlan;
        if (plan is not { Workouts.Count: > 0 })
            return;
        var choice = await dialogs.ActionSheet(NextWorkoutName, null, "Choose another workout", "Skip this workout", "View plan");
        switch (choice)
        {
            case "Choose another workout":
                var name = await dialogs.ActionSheet("Start which workout?", null, [.. plan.Workouts.Select(w => w.Name)]);
                var pick = plan.Workouts.FirstOrDefault(w => w.Name == name);
                if (pick != null)
                    await StartWorkoutAsync(workouts, dialogs, () => workouts.StartFromPlan(plan, pick));
                break;
            case "Skip this workout":
                plan.NextWorkoutIndex = (plan.NextWorkoutIndex + 1) % plan.Workouts.Count;
                store.Save();
                Refresh();
                break;
            case "View plan":
                await GoTo($"{Routes.Plan}?id={plan.Id}");
                break;
        }
    }

    [RelayCommand]
    Task CreatePlan() => GoTo(Routes.Wizard);

    [RelayCommand]
    Task SeeHistory() => GoTo(Routes.History);
}
