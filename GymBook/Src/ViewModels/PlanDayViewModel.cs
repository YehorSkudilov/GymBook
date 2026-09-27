using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>One row of an exercise's set table on the day sheet.</summary>
public record PlanDaySetRow(string Number, string First, string Second, string Third);

/// <summary>An exercise on the day sheet: what was logged once the day is finished, otherwise the plan's targets.</summary>
public class PlanDaySheetExercise
{
    public required ExerciseThumb Thumb { get; init; }
    public required string Name { get; init; }
    public required string Detail { get; init; }
    public required bool IsDone { get; init; }
    public required string FirstHeader { get; init; }
    public required string SecondHeader { get; init; }
    public required string ThirdHeader { get; init; }
    public required List<PlanDaySetRow> Rows { get; init; }
}

/// <summary>
/// A single day of a plan week, opened from Home: its exercises with their sets, and the action to start it,
/// view it or mark the rest day finished.
/// </summary>
public partial class PlanDayViewModel(DataStore store, WorkoutService workouts, DialogService dialogs, Units units, ProgressionEngine progression, WorkoutEstimator estimator)
    : BaseViewModel, IQueryAttributable
{
    string? _planId;
    int _day;
    int _week = 1;

    [ObservableProperty] string dayName = "";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] string meta = "";
    [ObservableProperty] string when = "";
    [ObservableProperty] bool hasWhen;
    [ObservableProperty] bool isNext;
    [ObservableProperty] bool isDone;
    [ObservableProperty] bool isRestDay;
    [ObservableProperty] bool isWorkoutDay;
    [ObservableProperty] bool isEmptyDay;
    [ObservableProperty] IDrawable dayMap = MuscleMapDrawable.Empty;
    [ObservableProperty] List<PlanDaySheetExercise> exercises = [];
    [ObservableProperty] string actionText = "";
    [ObservableProperty] bool hasAction;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _planId = query["id"]?.ToString();
        _day = query.TryGetValue("day", out var day) && int.TryParse(day?.ToString(), out var d) ? d : 0;
        _week = query.TryGetValue("week", out var week) && int.TryParse(week?.ToString(), out var w) ? w : 1;
    }

    public override Task OnAppearingAsync()
    {
        var plan = store.GetPlan(_planId);
        var progress = plan == null ? null : new PlanProgress(plan, store.History);
        if (plan == null || progress == null || _day >= progress.Days.Count)
            return Close();

        var workout = progress.Days[_day];
        var session = workout == null ? null : progress.SessionFor(workout, _week);
        Subtitle = $"{plan.Name} · Week {_week}";
        IsDone = progress.IsDayDone(_day, _week);
        IsNext = workout != null && !IsDone && plan.Id == store.Data.ActivePlanId && workout == progress.NextWorkout(_week);
        IsRestDay = workout == null;
        IsWorkoutDay = workout != null;
        DayName = workout?.Name ?? "Rest";

        if (workout == null)
        {
            Meta = "Recovery day. Muscles grow between sessions.";
            When = "";
            Exercises = [];
            IsEmptyDay = false;
            DayMap = MuscleMapDrawable.Empty;
            ActionText = IsDone ? "Mark as not finished" : "✓  Mark rest day finished";
            HasAction = true;
            HasWhen = false;
            return Task.CompletedTask;
        }

        var planned = workout.Exercises.Select(pe => (pe, ex: store.GetExercise(pe.ExerciseId))).ToList();
        DayMap = MuscleMapDrawable.ForWorkout(planned.Select(x => x.ex).OfType<Exercise>());
        IsEmptyDay = planned.Count == 0;

        if (session != null)
        {
            // A finished session only keeps completed sets; warm-ups are shown too, like on the session page.
            var logged = session.Exercises.Where(e => e.Sets.Count > 0).ToList();
            Meta = $"{logged.Count} exercises · {logged.Sum(e => e.Sets.Count)} sets";
            var minutes = session.EndedAt is { } end ? (int)Math.Round((end - session.StartedAt).TotalMinutes) : (int?)null;
            When = session.StartedAt.ToString("dddd, h:mm tt") + (minutes is { } m ? $" · {m} min" : "");
            Exercises = logged.Select(Logged).ToList();
            ActionText = "";
            HasAction = false;
        }
        else
        {
            Meta = $"{planned.Count} exercises · {planned.Sum(x => x.pe.Sets)} sets";
            When = WorkoutEstimator.Format(estimator.Minutes(workout, plan.Goal));
            Exercises = planned.Select(x => Planned(x.pe, x.ex)).ToList();
            ActionText = $"▶  Start {workout.Name}";
            HasAction = true;
        }
        HasWhen = When.Length > 0;
        return Task.CompletedTask;
    }

    /// <summary>A finished exercise: every logged set with its weight, reps and, for working sets, estimated one-rep max.</summary>
    PlanDaySheetExercise Logged(SessionExercise se)
    {
        var ex = store.GetExercise(se.ExerciseId);
        var sets = se.Sets;
        var reps = sets.Select(s => s.Reps).Distinct().Count() == 1 ? $"{sets[0].Reps}" : $"{sets.Min(s => s.Reps)}–{sets.Max(s => s.Reps)}";
        var working = 0;
        return new PlanDaySheetExercise
        {
            Thumb = Thumb(ex),
            Name = ex?.Name ?? "Unknown exercise",
            Detail = $"{ex?.Equipment.Display() ?? ""} · {sets.Count}×{reps} reps",
            IsDone = true,
            FirstHeader = units.Label.ToUpperInvariant(),
            SecondHeader = "REPS",
            ThirdHeader = "E1RM",
            Rows = sets.Select(s => new PlanDaySetRow(
                s.IsWarmup ? "W" : $"{++working}",
                ex?.IsBodyweight == true && s.WeightKg <= 0 ? "BW" : units.Format(s.WeightKg),
                $"{s.Reps}",
                !s.IsWarmup && ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir) is > 0 and var e1 ? units.Format(e1) : "–")).ToList(),
        };
    }

    /// <summary>A planned exercise: each set's target reps beside what was lifted for that set last time.</summary>
    PlanDaySheetExercise Planned(PlanExercise pe, Exercise? ex)
    {
        var range = pe.RepMin == pe.RepMax ? $"{pe.RepMin}" : $"{pe.RepMin}–{pe.RepMax}";
        var last = ex == null ? [] : progression.LastPerformance(ex.Id)?.Sets.Where(s => s.IsCompleted && !s.IsWarmup).ToList() ?? [];
        return new PlanDaySheetExercise
        {
            Thumb = Thumb(ex),
            Name = ex?.Name ?? "Unknown exercise",
            Detail = $"{ex?.Equipment.Display() ?? ""} · {pe.Sets}×{range} reps",
            IsDone = false,
            FirstHeader = "TARGET",
            SecondHeader = $"LAST {units.Label.ToUpperInvariant()}",
            ThirdHeader = "LAST REPS",
            Rows = Enumerable.Range(0, pe.Sets).Select(i => new PlanDaySetRow(
                $"{i + 1}",
                range,
                last.ElementAtOrDefault(i) is { } s ? units.Format(s.WeightKg) : "–",
                last.ElementAtOrDefault(i) is { } r ? $"{r.Reps}" : "–")).ToList(),
        };
    }

    static ExerciseThumb Thumb(Exercise? ex) =>
        ex == null ? new ExerciseThumb(null, "?", Colors.Gray, Colors.Gray.WithAlpha(0.16f)) : ExerciseThumb.For(ex);

    [RelayCommand]
    Task Close() => GoBack();

    /// <summary>Starts the workout (after closing the sheet), or marks the rest day.</summary>
    [RelayCommand]
    async Task DayAction()
    {
        var plan = store.GetPlan(_planId);
        if (plan == null)
            return;
        var workout = PlanSchedule.Days(plan).ElementAtOrDefault(_day);
        if (workout == null)
        {
            PlanProgress.SetRestDone(plan, _day, _week, !IsDone);
            store.Save();
            await OnAppearingAsync();
            return;
        }
        if (workout.Exercises.Count == 0)
        {
            await dialogs.Alert("Empty workout", "Add exercises to this workout first.");
            return;
        }
        await Close();
        await StartWorkoutAsync(workouts, dialogs, () => workouts.StartFromPlan(plan, workout, _week));
    }

    /// <summary>Opens the muscle breakdown for this day, with a switch to the whole plan.</summary>
    [RelayCommand]
    Task OpenMuscles()
    {
        var plan = store.GetPlan(_planId);
        if (plan == null || PlanSchedule.Days(plan).ElementAtOrDefault(_day) is not PlanWorkout workout)
            return Task.CompletedTask;
        return Shell.Current.Navigation.PushModalAsync(new Views.MuscleBreakdownPage(new MuscleBreakdownViewModel(store, plan, workout)));
    }

    /// <summary>The ··· beside the day name. Other pages open after the sheet closes.</summary>
    [RelayCommand]
    async Task Options()
    {
        var plan = store.GetPlan(_planId);
        if (plan == null)
            return;
        var workout = PlanSchedule.Days(plan).ElementAtOrDefault(_day);
        var session = workout == null ? null : new PlanProgress(plan, store.History).SessionFor(workout, _week);
        var options = new List<string>();
        if (session != null)
            options.Add("View workout summary");
        if (workout != null)
            options.Add("Edit exercises");
        options.Add("View plan");

        switch (await dialogs.ActionSheet(DayName, null, [.. options]))
        {
            case "View workout summary":
                await Close();
                await GoTo($"{Routes.Session}?id={session!.Id}");
                break;
            case "Edit exercises":
                await Close();
                await GoTo($"{Routes.PlanWorkout}?plan={plan.Id}&workout={workout!.Id}");
                break;
            case "View plan":
                await Close();
                await GoTo($"{Routes.Plan}?id={plan.Id}&day={_day}");
                break;
        }
    }
}
