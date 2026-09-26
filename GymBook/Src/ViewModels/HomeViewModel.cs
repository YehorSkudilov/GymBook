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
    [ObservableProperty] bool hasPlan;
    [ObservableProperty] string planName = "";
    [ObservableProperty] string nextWorkoutName = "";
    [ObservableProperty] string nextWorkoutMeta = "";
    [ObservableProperty] string nextWorkoutMuscles = "";
    [ObservableProperty] List<ExerciseThumb> nextThumbs = [];
    [ObservableProperty] string nextMore = "";
    [ObservableProperty] IDrawable muscleMap = MuscleMapDrawable.Empty;
    [ObservableProperty] string recoverySummary = "";
    [ObservableProperty] List<DayItem> weekDays = [];
    [ObservableProperty] string weekWorkouts = "";
    [ObservableProperty] string weekVolume = "";
    [ObservableProperty] string weekSets = "";
    [ObservableProperty] string streak = "";
    [ObservableProperty] string planWeek = "";
    [ObservableProperty] List<PlanDayItem> planDays = [];

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
            NextWorkoutMeta = $"{exercises.Count} exercises · {sets} sets\n~{minutes} min";
            NextWorkoutMuscles = string.Join(" · ", exercises.Select(x => x.ex!.PrimaryMuscle).Distinct().Select(m => m.Display()));
            NextThumbs = exercises.Take(5).Select(x => ExerciseThumb.For(x.ex!)).ToList();
            NextMore = exercises.Count > 5 ? $"+{exercises.Count - 5}" : "";
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
        BuildPlanWeek(plan, weekStart, thisWeek);
        WeekWorkouts = $"{thisWeek.Count}/{stats.WeeklyTarget}";
        WeekVolume = units.FormatVolume(thisWeek.Sum(stats.SessionVolume));
        WeekSets = thisWeek.Sum(s => s.WorkingSets.Count()).ToString();
        var streakWeeks = stats.WeekStreak();
        Streak = streakWeeks == 1 ? "1 week streak" : $"{streakWeeks} week streak";
    }

    /// <summary>The plan's days in order: workouts (marked done when this week has a session from them) and rest days.</summary>
    void BuildPlanWeek(WorkoutPlan? plan, DateTime weekStart, List<WorkoutSession> thisWeek)
    {
        if (plan is not { Workouts.Count: > 0 })
        {
            PlanDays = [];
            return;
        }

        PlanWeek = $"Week {(weekStart - StatsService.WeekStart(plan.CreatedAt)).Days / 7 + 1}";
        var nextIndex = plan.NextWorkoutIndex % plan.Workouts.Count;
        var workoutItems = plan.Workouts.Select((w, i) =>
        {
            var done = thisWeek.FirstOrDefault(s => s.PlanWorkoutId == w.Id);
            var photos = w.Exercises.Select(e => ExerciseLibrary.Details(e.ExerciseId)?.Images.FirstOrDefault()).OfType<string>().ToList();
            return (Workout: w, Item: new PlanDayItem
            {
                Name = w.Name,
                Number = (i + 1).ToString(),
                IsDone = done != null,
                IsNext = done == null && i == nextIndex,
                Thumbnails = photos.Take(3).ToList(),
                More = w.Exercises.Count > 3 ? $"+{w.Exercises.Count - 3}" : "",
                OpenCommand = done != null
                    ? new AsyncRelayCommand(() => GoTo($"{Routes.Session}?id={done.Id}"))
                    : new AsyncRelayCommand(() => StartWorkoutAsync(workouts, dialogs, () => workouts.StartFromPlan(plan, w))),
            });
        }).ToDictionary(x => x.Workout);

        PlanDays = PlanSchedule.Days(plan)
            .Select(w => w == null
                ? new PlanDayItem { Name = "Rest", Number = "–", IsRest = true, Thumbnails = [], More = "" }
                : workoutItems[w].Item)
            .ToList();
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
    Task StartEmpty() => StartWorkoutAsync(workouts, dialogs, workouts.StartEmpty);

    /// <summary>The ··· beside the week's plan: options for the plan itself.</summary>
    [RelayCommand]
    async Task PlanOptions()
    {
        var plan = store.ActivePlan;
        if (plan == null)
            return;
        var others = store.Data.Plans.Where(p => p != plan).ToList();
        var options = new List<string> { "View plan" };
        if (others.Count > 0)
            options.Add("Switch plan");

        switch (await dialogs.ActionSheet(plan.Name, null, [.. options]))
        {
            case "View plan":
                await GoTo($"{Routes.Plan}?id={plan.Id}");
                break;
            case "Switch plan":
                // Numbered so plans with the same name stay distinguishable.
                var labels = others.Select((p, i) => $"{i + 1}. {p.Name}").ToList();
                var pick = await dialogs.ActionSheet("Switch to", null, [.. labels]);
                var index = pick == null ? -1 : labels.IndexOf(pick);
                if (index >= 0)
                {
                    store.Data.ActivePlanId = others[index].Id;
                    store.Save();
                    Refresh();
                }
                break;
        }
    }

    [RelayCommand]
    Task CreatePlan() => GoTo(Routes.Wizard);

    [RelayCommand]
    Task OpenCalendar() => GoTo(Routes.Calendar);
}
