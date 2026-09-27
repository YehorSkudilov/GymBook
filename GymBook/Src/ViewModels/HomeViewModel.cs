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
    DialogService dialogs,
    SyncIndicator syncIndicator) : BaseViewModel
{
    public SyncIndicator Sync => syncIndicator;

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
    [ObservableProperty] string planWeekDone = "";
    [ObservableProperty] string nextLabel = "UP NEXT";
    [ObservableProperty] List<PlanDayItem> planDays = [];

    // The plan week picked from the week menu, for as long as that plan stays active; otherwise the current week.
    string? _chosenPlanId;
    int _chosenWeek;
    int _week;
    // What "Start" in the Up next card starts: the first workout left in the shown week, else in the week after.
    int _nextWeek;
    PlanWorkout? _next;

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
            var progress = new PlanProgress(plan, store.History);
            _week = _chosenPlanId == plan.Id ? Math.Min(_chosenWeek, progress.LastUnlockedWeek) : progress.CurrentWeek;
            (_nextWeek, _next) = progress.NextWorkout(_week) is { } inWeek
                ? (_week, inWeek)
                : (_week + 1, progress.NextWorkout(_week + 1) ?? progress.Days.OfType<PlanWorkout>().First());
            var next = _next;
            NextLabel = _nextWeek == _week ? "UP NEXT" : $"UP NEXT · WEEK {_nextWeek}";
            BuildPlanWeek(plan, progress);

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
        else
        {
            _next = null;
            PlanDays = [];
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
        WeekWorkouts = $"{thisWeek.Count}/{stats.WeeklyTarget}";
        WeekVolume = units.FormatVolume(thisWeek.Sum(stats.SessionVolume));
        WeekSets = thisWeek.Sum(s => s.WorkingSets.Count()).ToString();
        var streakWeeks = stats.WeekStreak();
        Streak = streakWeeks == 1 ? "1 week streak" : $"{streakWeeks} week streak";
    }

    /// <summary>The shown plan week's days in order. Tapping a day opens it in the day sheet, where it can be started or marked finished.</summary>
    void BuildPlanWeek(WorkoutPlan plan, PlanProgress progress)
    {
        var week = _week;
        PlanWeek = $"Week {week}";
        PlanWeekDone = $"{progress.WorkoutsDone(week)}/{plan.Workouts.Count} done";
        var next = progress.NextWorkout(week);
        PlanDays = progress.Days.Select((w, day) =>
        {
            var open = new AsyncRelayCommand(() => GoTo($"{Routes.PlanDay}?id={plan.Id}&day={day}&week={week}"));
            if (w == null)
                return new PlanDayItem { Name = "Rest", Number = "–", IsRest = true, IsDone = progress.IsRestDone(day, week), Thumbnails = [], More = "", OpenCommand = open };
            var photos = w.Exercises.Select(e => ExerciseLibrary.Details(e.ExerciseId)?.Images.FirstOrDefault()).OfType<string>().ToList();
            return new PlanDayItem
            {
                Name = w.Name,
                Number = (plan.Workouts.IndexOf(w) + 1).ToString(),
                IsDone = progress.SessionFor(w, week) != null,
                IsNext = w == next,
                Thumbnails = photos.Take(3).ToList(),
                More = w.Exercises.Count > 3 ? $"+{w.Exercises.Count - 3}" : "",
                OpenCommand = open,
            };
        }).ToList();
    }

    /// <summary>The "Week N" beside the plan name: shows any unlocked week.</summary>
    [RelayCommand]
    async Task ChooseWeek()
    {
        var plan = store.ActivePlan;
        if (plan is not { Workouts.Count: > 0 })
            return;
        var progress = new PlanProgress(plan, store.History);
        var last = progress.LastUnlockedWeek;
        var labels = Enumerable.Range(1, last)
            .Select(w => $"Week {w} · {progress.WorkoutsDone(w)}/{plan.Workouts.Count} done{(progress.IsComplete(w) ? " ✓" : "")}")
            .ToList();
        var pick = await dialogs.ActionSheet($"Finish a workout in week {last} to unlock week {last + 1}", null, [.. labels]);
        var index = pick == null ? -1 : labels.IndexOf(pick);
        if (index < 0)
            return;
        _chosenPlanId = plan.Id;
        _chosenWeek = index + 1;
        Refresh();
    }

    [RelayCommand]
    async Task StartNext()
    {
        var plan = store.ActivePlan;
        if (plan == null || _next is not { } next)
            return;
        var week = _nextWeek;
        await StartWorkoutAsync(workouts, dialogs, () => workouts.StartFromPlan(plan, next, week));
    }

    /// <summary>Tapping the Up next card previews that day, like tapping it in the week below.</summary>
    [RelayCommand]
    Task OpenNext()
    {
        var plan = store.ActivePlan;
        if (plan == null || _next is not { } next)
            return Task.CompletedTask;
        var day = PlanSchedule.Days(plan).IndexOf(next);
        return day < 0 ? Task.CompletedTask : GoTo($"{Routes.PlanDay}?id={plan.Id}&day={day}&week={_nextWeek}");
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
