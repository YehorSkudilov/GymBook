using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>One row of an exercise's set table on the day sheet.</summary>
public record PlanDaySetRow(string Number, string First, string Second, string Third);

/// <summary>A number on a finished workout's sheet, with how it compares with last time (<paramref name="Note"/>, empty the first time).</summary>
public record StatTile(string Value, string Label, string Note, Color NoteColor)
{
    public static StatTile Empty { get; } = new("", "", "", Colors.Transparent);
    public bool HasNote => Note.Length > 0;
}

/// <summary>An exercise of a finished workout against the last time it was done, with its recent trend.</summary>
public class ExerciseProgressItem
{
    public required string Name { get; init; }
    public required string Headline { get; init; }
    public required string Detail { get; init; }
    /// <summary>Green when it went up, red when it went down.</summary>
    public required Color Color { get; init; }
    /// <summary>Its estimated 1RM (or reps, for bodyweight) over the last sessions; null with fewer than two.</summary>
    public IDrawable? Chart { get; init; }
    public bool HasChart => Chart != null;
}

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
/// view it or mark the rest day finished. Also a finished workout on its own (?session=, from the calendar), plan or
/// not: what was done, its stats, and how fatigued each muscle was right after it.
/// </summary>
public partial class PlanDayViewModel(DataStore store, WorkoutService workouts, DialogService dialogs, Units units, ProgressionEngine progression,
    WorkoutEstimator estimator, RecoveryService recovery, StatsService stats)
    : BaseViewModel, IQueryAttributable
{
    string? _planId;
    int _day;
    int _week = 1;
    // Opened for one finished workout rather than a plan day.
    string? _sessionId;
    // Opened right after finishing the workout (from the celebration): it was just saved, so no Discard.
    bool _justFinished;
    // The finished workout on show, however it was opened; null for a day still to do.
    WorkoutSession? _finished;

    // A finished workout: its numbers, and the fatigue it left.
    [ObservableProperty] bool hasStats;
    /// <summary>A finished workout is on show: the Discard workout button.</summary>
    [ObservableProperty] bool canDiscard;
    [ObservableProperty] StatTile statTime = StatTile.Empty;
    [ObservableProperty] StatTile statVolume = StatTile.Empty;
    [ObservableProperty] StatTile statSets = StatTile.Empty;
    [ObservableProperty] StatTile statRecords = StatTile.Empty;
    /// <summary>Each exercise against the last time it was done.</summary>
    [ObservableProperty] List<ExerciseProgressItem> improvements = [];
    [ObservableProperty] bool hasImprovements;
    /// <summary>This workout's volume and length over its last sessions, up to this one.</summary>
    [ObservableProperty] IDrawable? volumeTrend;
    [ObservableProperty] IDrawable? durationTrend;
    [ObservableProperty] bool hasTrends;
    [ObservableProperty] IDrawable fatigueMap = MuscleMapDrawable.Empty;
    [ObservableProperty] string fatigueSummary = "";
    /// <summary>The muscles it worked, most fatigued first, with when each is fresh again.</summary>
    [ObservableProperty] List<MuscleRecoveryItem> fatigueMuscles = [];

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
        _planId = query.TryGetValue("id", out var id) ? id?.ToString() : null;
        _sessionId = query.TryGetValue("session", out var session) ? session?.ToString() : null;
        _justFinished = query.TryGetValue("finished", out var finished) && finished?.ToString() == "1";
        _day = query.TryGetValue("day", out var day) && int.TryParse(day?.ToString(), out var d) ? d : 0;
        _week = query.TryGetValue("week", out var week) && int.TryParse(week?.ToString(), out var w) ? w : 1;
    }

    public override Task OnAppearingAsync()
    {
        if (_sessionId != null)
            return ShowSession();
        var plan = store.GetPlan(_planId);
        var progress = plan == null ? null : new PlanProgress(plan, store.History);
        if (plan == null || progress == null || _day >= progress.Days.Count)
            return Close();

        var workout = progress.Days[_day];
        HasStats = false;
        _finished = null;
        CanDiscard = false;
        var session = workout == null ? null : progress.SessionFor(workout, _week);
        Subtitle = PlanCycle.Describe(plan, _week) is { } phase ? $"{plan.Name} · Week {_week} · {phase}" : $"{plan.Name} · Week {_week}";
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

        // As it will be done this week: a deload has fewer sets, a block week may have more.
        var planned = workout.Exercises.Select(pe => (pe: PlanCycle.ForWeek(plan, pe, _week), ex: store.GetExercise(pe.ExerciseId))).ToList();
        DayMap = MuscleMapDrawable.ForWorkout(planned.Select(x => x.ex).OfType<Exercise>());
        IsEmptyDay = planned.Count == 0;

        if (session != null)
        {
            // Exactly what the calendar shows for it: one way to show a finished workout.
            ShowFinishedWorkout(session);
            return Task.CompletedTask;
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

    /// <summary>The ··· of a finished workout: the plan it came from, or discarding it.</summary>
    async Task SessionOptions(WorkoutSession session)
    {
        var plan = store.GetPlan(session.PlanId);
        var day = plan == null ? -1 : PlanSchedule.Days(plan).FindIndex(w => w?.Id == session.PlanWorkoutId);
        var options = day >= 0 ? new[] { "Edit in plan" } : Array.Empty<string>();
        switch (await dialogs.ActionSheet(DayName, "Discard workout", options))
        {
            case "Edit in plan":
                // One navigation: the sheet slides away as the plan comes in, with nothing in between.
                await GoTo($"../{Routes.Plan}?id={plan!.Id}&day={day}");
                break;
            case "Discard workout":
                await DeleteSession(session);
                break;
        }
    }

    /// <summary>Removes a finished workout from the history and statistics, after asking.</summary>
    async Task DeleteSession(WorkoutSession session)
    {
        if (!await dialogs.Confirm("Discard workout?", "It's removed from your history and statistics. This can't be undone.", "Discard"))
            return;
        store.Data.Sessions.Remove(session);
        store.Save();
        await Close();
    }

    /// <summary>The Discard workout button under a finished workout.</summary>
    [RelayCommand]
    Task Discard() => _finished is { } session ? DeleteSession(session) : Task.CompletedTask;

    /// <summary>One finished workout (from the calendar), plan or not: what was done, its stats and the fatigue it left.</summary>
    Task ShowSession()
    {
        var session = store.History.FirstOrDefault(s => s.Id == _sessionId);
        if (session == null)
            return Close();
        ShowFinishedWorkout(session);
        return Task.CompletedTask;
    }

    /// <summary>
    /// A finished workout, the same wherever it's opened from (the Workout tab's plan week, the calendar, History, or
    /// right after finishing it): what was done, its stats against last time, and the fatigue it left.
    /// </summary>
    void ShowFinishedWorkout(WorkoutSession session)
    {
        _finished = session;
        CanDiscard = !_justFinished;
        var plan = store.GetPlan(session.PlanId);
        var logged = session.Exercises.Where(e => e.Sets.Count > 0).ToList();
        DayName = session.Name;
        Subtitle = plan == null ? "Workout" : session.PlanWeek is { } week ? $"{plan.Name} · Week {week}" : plan.Name;
        IsDone = true;
        IsNext = false;
        IsRestDay = false;
        IsWorkoutDay = true;
        IsEmptyDay = false;
        DayMap = MuscleMapDrawable.ForWorkout(logged.Select(e => store.GetExercise(e.ExerciseId)).OfType<Exercise>());
        Meta = $"{logged.Count} exercises · {logged.Sum(e => e.Sets.Count)} sets";
        When = session.StartedAt.ToString("dddd d MMM, h:mm tt");
        HasWhen = true;
        Exercises = logged.Select(Logged).ToList();
        ActionText = "";
        HasAction = false;
        ShowFinished(session);
    }

    /// <summary>The stats of a finished workout, and how fatigued each muscle was the moment it ended.</summary>
    void ShowFinished(WorkoutSession session)
    {
        HasStats = true;
        // The same workout the time before: a plan workout by its id, a free one by its name.
        var earlier = store.History
            .Where(s => s.Id != session.Id && s.StartedAt < session.StartedAt)
            .Where(s => session.PlanWorkoutId != null ? s.PlanWorkoutId == session.PlanWorkoutId : s.PlanWorkoutId == null && s.Name == session.Name)
            .ToList();
        var previous = earlier.FirstOrDefault();

        // Tiles: this workout, and how it compares with last time (shorter is better for time).
        var minutes = session.Duration.TotalMinutes;
        var volume = stats.SessionVolume(session);
        var sets = session.WorkingSets.Count();
        var records = stats.RecordsIn(session).Count;
        StatTime = new StatTile(Units.Duration(session.Duration), "TIME", previous == null ? "" : Change(previous.Duration.TotalMinutes - minutes, 1,
            d => $"{Math.Abs(d):0} min {(d > 0 ? "faster" : "slower")}"), Better(previous == null ? 0 : previous.Duration.TotalMinutes - minutes, 1));
        StatVolume = new StatTile(units.FormatVolume(volume), "VOLUME", previous == null ? "" : Change(volume - stats.SessionVolume(previous), 1,
            d => $"{(d > 0 ? "+" : "−")}{units.FormatVolume(Math.Abs(d))}"), Better(previous == null ? 0 : volume - stats.SessionVolume(previous), 1));
        StatSets = new StatTile(sets.ToString(), "WORKING SETS", previous == null ? "" : Change(sets - previous.WorkingSets.Count(), 0.5,
            d => $"{d:+0;−0} vs last time"), Better(previous == null ? 0 : sets - previous.WorkingSets.Count(), 0.5));
        StatRecords = new StatTile(records.ToString(), records == 1 ? "PR" : "PRS", records > 0 ? "New personal bests" : "", records > 0 ? Up : Neutral);

        Improvements = session.Exercises.Where(e => e.Sets.Any(s => !s.IsWarmup))
            .DistinctBy(e => e.ExerciseId).Select(e => Progress(session, e)).OfType<ExerciseProgressItem>().ToList();
        HasImprovements = Improvements.Count > 0;

        // The last few times this workout was done, up to this one: volume and how long it took.
        var trend = earlier.Take(7).Reverse().Append(session).ToList();
        HasTrends = trend.Count >= 2;
        VolumeTrend = HasTrends ? new BarChartDrawable(trend.Select(s => new ChartPoint(s.StartedAt.ToString("d MMM"), units.ToDisplay(stats.SessionVolume(s)))).ToList(),
            Color.FromArgb("#3F7DFF"), v => units.FormatVolume(units.FromDisplay(v))) : null;
        DurationTrend = HasTrends ? new BarChartDrawable(trend.Select(s => new ChartPoint(s.StartedAt.ToString("d MMM"), Math.Round(s.Duration.TotalMinutes))).ToList(),
            Color.FromArgb("#8B5CF6"), v => $"{v:0} min") : null;

        // Fatigue the moment it ended: the map, and each muscle it worked with when it'll be fresh.
        var at = session.EndedAt ?? session.StartedAt;
        var details = recovery.Details(at);
        FatigueMap = MuscleMapDrawable.ForRecovery(details.ToDictionary(d => d.Muscle, d => d.Recovery));
        var worked = session.Exercises.Select(e => store.GetExercise(e.ExerciseId)).OfType<Exercise>()
            .SelectMany(ex => ex.SecondaryMuscles.Prepend(ex.PrimaryMuscle)).ToHashSet();
        FatigueMuscles = [.. details.Where(d => worked.Contains(d.Muscle) && d.Recovery < 1).OrderBy(d => d.Recovery).Select(d => new MuscleRecoveryItem
        {
            Name = d.Muscle.Display(),
            Progress = d.Recovery,
            Percent = $"{d.Recovery:P0}",
            Status = RecoveryService.StatusFor(d.Recovery),
            Color = RecoveryService.ColorFor(d.Recovery),
            Detail = d.ReadyAt is { } ready ? $"Fresh {Relative(ready - at)} after it · {d.Sets:0.#} sets" : $"{d.Sets:0.#} sets",
        })];
        var tired = details.Where(d => d.Recovery < 0.6).OrderBy(d => d.Recovery).Select(d => d.Muscle.Display()).ToList();
        FatigueSummary = tired.Count == 0 ? "No muscle group was worked hard." : $"Fatigued: {string.Join(", ", tired)}";
    }

    static readonly Color Up = Color.FromArgb("#2ED47A"), Down = Color.FromArgb("#FF4D5E"), Neutral = Color.FromArgb("#9AA3B5");

    /// <summary>The change in words, or "Same as last time" when it's within <paramref name="noise"/>.</summary>
    static string Change(double delta, double noise, Func<double, string> text) => Math.Abs(delta) < noise ? "Same as last time" : text(delta);

    static Color Better(double delta, double noise) => delta >= noise ? Up : delta <= -noise ? Down : Neutral;

    static string Relative(TimeSpan span)
    {
        var h = Math.Max(1, (int)Math.Ceiling(span.TotalHours));
        return h >= 24 ? $"in {h / 24} d {h % 24} h" : $"in {h} h";
    }

    /// <summary>
    /// How an exercise went against the last time it was done (in any workout): the best set's estimated 1RM, total
    /// reps and volume, with its 1RM over the last sessions for a small chart.
    /// </summary>
    ExerciseProgressItem? Progress(WorkoutSession session, SessionExercise se)
    {
        var ex = store.GetExercise(se.ExerciseId);
        if (ex == null)
            return null;
        static IEnumerable<SetEntry> Working(IEnumerable<SessionExercise> es) => es.SelectMany(e => e.Sets).Where(s => s.IsCompleted && !s.IsWarmup);
        var now = Working(session.Exercises.Where(e => e.ExerciseId == ex.Id)).ToList();
        if (now.Count == 0)
            return null;
        // Sessions with this exercise, newest first, up to and including this one.
        var history = store.History.Where(s => s.StartedAt <= session.StartedAt && s.Exercises.Any(e => e.ExerciseId == ex.Id)).ToList();
        var before = history.FirstOrDefault(s => s.Id != session.Id);
        var then = before == null ? [] : Working(before.Exercises.Where(e => e.ExerciseId == ex.Id)).ToList();

        double Best(List<SetEntry> sets) => sets.Count == 0 ? 0 : sets.Max(s => ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir));
        var top = now.MaxBy(s => ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir))!;
        var bestNow = Best(now);
        var reps = now.Sum(s => s.Reps);
        var volume = now.Sum(s => stats.SetVolume(ex, s));
        var topText = ex.IsBodyweight && top.WeightKg <= 0 ? $"{top.Reps} reps" : $"{units.FormatWithUnit(top.WeightKg)} × {top.Reps}";

        string headline, detail;
        Color color;
        if (then.Count == 0)
        {
            headline = $"Best set {topText}";
            detail = $"{reps} reps · {units.FormatVolume(volume)} · first time";
            color = Neutral;
        }
        else
        {
            var e1Change = bestNow - Best(then);
            var repsChange = reps - then.Sum(s => s.Reps);
            var volumeChange = volume - then.Sum(s => stats.SetVolume(ex, s));
            headline = ex.IsBodyweight && bestNow <= 0
                ? $"Best set {topText} · {repsChange:+0;−0;±0} reps"
                : $"Best set {topText} · est. 1RM {units.FormatWithUnit(bestNow)} ({(e1Change >= 0 ? "+" : "−")}{units.Format(Math.Abs(e1Change))})";
            detail = $"{reps} reps ({repsChange:+0;−0;±0}) · {units.FormatVolume(volume)} ({(volumeChange >= 0 ? "+" : "−")}{units.FormatVolume(Math.Abs(volumeChange))}) vs last time";
            var score = ex.IsBodyweight && bestNow <= 0 ? repsChange : e1Change;
            color = score > 0.05 ? Up : score < -0.05 ? Down : Neutral;
        }

        // The estimated 1RM (or reps, for bodyweight) of its last few sessions, oldest first.
        var points = history.Take(8).Reverse().Select(s =>
        {
            var sets = Working(s.Exercises.Where(e => e.ExerciseId == ex.Id)).ToList();
            var value = ex.IsBodyweight && Best(sets) <= 0 ? sets.Sum(x => x.Reps) : units.ToDisplay(Best(sets));
            return new ChartPoint(s.StartedAt.ToString("d MMM"), Math.Round(value, 1));
        }).ToList();
        return new ExerciseProgressItem
        {
            Name = ex.Name,
            Headline = headline,
            Detail = detail,
            Color = color,
            Chart = points.Count >= 2 ? new LineChartDrawable(points, color == Neutral ? Color.FromArgb("#3F7DFF") : color, v => $"{v:0.#}") : null,
        };
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
        await StartPlannedWorkoutAsync(workouts, dialogs, recovery, plan, workout, _week);
    }

    /// <summary>Opens the muscle breakdown for this day, with a switch to the whole plan.</summary>
    [RelayCommand]
    Task OpenMuscles()
    {
        var plan = store.GetPlan(_planId);
        if (plan == null || PlanSchedule.Days(plan).ElementAtOrDefault(_day) is not PlanWorkout workout)
            return Task.CompletedTask;
        return Shell.Current.Navigation.PushModalAsync(new Views.MuscleBreakdownPage(new MuscleBreakdownViewModel(store, plan, workout)), false);
    }

    /// <summary>The ··· beside the day name. Other pages open after the sheet closes.</summary>
    [RelayCommand]
    async Task Options()
    {
        // A finished workout has the same menu wherever it was opened from.
        if (_finished is { } finished)
        {
            await SessionOptions(finished);
            return;
        }
        var plan = store.GetPlan(_planId);
        if (plan == null)
            return;
        // Opens the plan page on this day.
        if (await dialogs.ActionSheet(DayName, null, "Edit in plan") == "Edit in plan")
            // One navigation: the sheet slides away as the plan comes in, with nothing in between.
            await GoTo($"../{Routes.Plan}?id={plan.Id}&day={_day}");
    }
}
