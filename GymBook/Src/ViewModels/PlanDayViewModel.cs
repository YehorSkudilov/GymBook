using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>One row of an exercise's set table on the day sheet.</summary>
public record PlanDaySetRow(string Number, string First, string Second, string Third)
{
    /// <summary>A set that was skipped: dimmed, "Skipped" for its weight and N/A for its E1RM.</summary>
    public bool IsSkipped { get; init; }
    public double RowOpacity => IsSkipped ? 0.45 : 1;
}

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
    /// <summary>Tapping the exercise: its info page (how it's done, the video). Nothing for an unknown exercise.</summary>
    public required IAsyncRelayCommand OpenCommand { get; init; }
    /// <summary>The note typed in during the workout; empty for a plan day.</summary>
    public string Note { get; init; } = "";
    public bool HasNote => Note.Length > 0;
}

/// <summary>
/// A single day of a plan week, opened from Home: its exercises with their sets, and the action to start it,
/// view it or mark the rest day finished. Also a finished workout on its own (?session=, from the calendar), plan or
/// not: what was done, its stats, and how fatigued each muscle was right after it. Open workout shows it in the workout
/// view to change what was logged; Resume makes it the workout in progress again.
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
    // The finished workout on show, however it was opened; null for a day still to do.
    WorkoutSession? _finished;

    // A finished workout: its numbers, and the fatigue it left.
    [ObservableProperty] bool hasStats;
    /// <summary>A finished workout is on show: the Open workout and Resume buttons.</summary>
    [ObservableProperty] bool canOpen;
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
    [ObservableProperty] bool isSkipped;
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
        CanOpen = false;
        var session = workout == null ? null : progress.SessionFor(workout, _week);
        Subtitle = PlanCycle.Describe(plan, _week) is { } phase ? $"{plan.Name} · Week {_week} · {phase}" : $"{plan.Name} · Week {_week}";
        // A skipped workout isn't finished: it has its own pill.
        IsDone = workout == null ? progress.IsRestDone(_day, _week) : session != null;
        IsSkipped = workout != null && session == null && progress.IsSkipped(_day, _week);
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
            // Skipped this week: no time spent, and every set shows as skipped, as in a workout. It can still be done.
            When = IsSkipped ? "0 min" : WorkoutEstimator.Format(estimator.Minutes(workout, plan.Goal));
            // Its warm-ups as a workout of it would have them: lighter once an earlier exercise worked the same muscle.
            var warmups = WarmupSettings.For(plan, store.Profile);
            var worked = new HashSet<MuscleGroup>();
            Exercises = planned.Select(x =>
            {
                var steps = x.ex == null ? [] : warmups.StepsFor(x.pe, x.ex, worked.Contains(x.ex.PrimaryMuscle));
                if (x.ex != null)
                    worked.Add(x.ex.PrimaryMuscle);
                return IsSkipped ? SkippedExercise(x.pe, x.ex, steps) : Planned(x.pe, x.ex, steps);
            }).ToList();
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
        const string startTime = "Change start time", open = "Open workout", resume = "Resume", rename = "Rename", length = "Change length",
            link = "Link to a plan", relink = "Change plan link", unlink = "Unlink from plan";
        var options = new List<string> { open, resume, rename, startTime, length, plan == null ? link : relink };
        if (plan != null)
            options.Add(unlink);
        if (day >= 0)
            options.Add("Edit in plan");
        switch (await dialogs.ActionSheet(DayName, "Discard workout", [.. options]))
        {
            case link or relink:
                await LinkToPlan(session);
                break;
            case unlink:
                store.LinkSession(session, null, null, null);
                ShowFinishedWorkout(session);
                break;
            case open:
                await OpenWorkout();
                break;
            case resume:
                await Resume();
                break;
            case rename:
                await Rename(session);
                break;
            case length:
                await ChangeLength(session);
                break;
            case startTime:
                // Its end and every set and rest logged move with it, so its length stays the same.
                if (await dialogs.DateAndTime("Start time", "The end and every set and rest logged move with it.", session.StartedAt) is not { } start)
                    break;
                if (WorkoutService.ChangeStart(session, start) is { } error)
                {
                    await dialogs.Alert("Can't start then", error);
                    break;
                }
                store.Save();
                ShowFinishedWorkout(session);
                break;
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
        store.CompactPlanWeeks();
        store.Save();
        await Close();
    }

    /// <summary>
    /// Open workout: the finished workout in the workout view, without the clock, to change its sets and exercises (its
    /// start, length and name are in its ···). In place of this sheet.
    /// </summary>
    [RelayCommand]
    Task OpenWorkout() => _finished is { } session ? GoTo($"../{Routes.Workout}?session={session.Id}") : Task.CompletedTask;

    /// <summary>Resume: the finished workout becomes the workout in progress again, its clock carrying on from where it ended.</summary>
    [RelayCommand]
    async Task Resume()
    {
        if (_finished is not { } session)
            return;
        await Close();
        await ResumeWorkoutAsync(workouts, dialogs, session);
    }

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
        CanOpen = true;
        var plan = store.GetPlan(session.PlanId);
        var logged = session.Exercises.Where(e => e.Sets.Count > 0).ToList();
        DayName = session.Name;
        // The plan it counts toward, or that it's linked to none (linking is in the ···).
        Subtitle = plan == null ? SessionItem.PlanLink(session, null) : session.PlanWeek is { } week ? $"{plan.Name} · Week {week}" : plan.Name;
        IsDone = true;
        IsSkipped = false;
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
        FatigueMap = MuscleMapDrawable.ForRecovery(details.ToDictionary(d => d.Muscle, d => d.Recovery), recovery.PartRecovery(at));
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
        var sets = se.Sets.Where(s => s.IsCompleted).ToList();
        if (sets.Count == 0)
            sets = se.Sets;
        var reps = sets.Select(s => s.Reps).Distinct().Count() == 1 ? $"{sets[0].Reps}" : $"{sets.Min(s => s.Reps)}–{sets.Max(s => s.Reps)}";
        return new PlanDaySheetExercise
        {
            OpenCommand = OpenInfo(ex),
            Thumb = Thumb(ex),
            Name = ex?.Name ?? "Unknown exercise",
            Detail = $"{ex?.Equipment.Display() ?? ""} · {sets.Count}×{reps} reps",
            Note = se.Note ?? "",
            IsDone = true,
            FirstHeader = units.Label.ToUpperInvariant(),
            SecondHeader = "REPS",
            ThirdHeader = "E1RM",
            Rows = SessionRows(se, ex),
        };
    }

    /// <summary>An exercise of a workout skipped this week: each of its sets, warm-ups first, as skipped.</summary>
    PlanDaySheetExercise SkippedExercise(PlanExercise pe, Exercise? ex, IReadOnlyList<WarmupStep> warmups)
    {
        var range = pe.RepMin == pe.RepMax ? $"{pe.RepMin}" : $"{pe.RepMin}–{pe.RepMax}";
        return new PlanDaySheetExercise
        {
            OpenCommand = OpenInfo(ex),
            Thumb = Thumb(ex),
            Name = ex?.Name ?? "Unknown exercise",
            Detail = $"{ex?.Equipment.Display() ?? ""} · {pe.Sets}×{range} reps · skipped",
            Note = pe.Note ?? "",
            IsDone = false,
            FirstHeader = units.Label.ToUpperInvariant(),
            SecondHeader = "REPS",
            ThirdHeader = "E1RM",
            Rows =
            [
                .. warmups.Select((_, i) => new PlanDaySetRow($"W{i + 1}", "Skipped", "–", "N/A") { IsSkipped = true }),
                .. Enumerable.Range(1, pe.Sets).Select(n => new PlanDaySetRow($"{n}", "Skipped", "–", "N/A") { IsSkipped = true }),
            ],
        };
    }

    /// <summary>
    /// A planned exercise: its warm-ups (percent of the working weight × reps), then each set's target reps beside what
    /// was lifted for that set last time.
    /// </summary>
    PlanDaySheetExercise Planned(PlanExercise pe, Exercise? ex, IReadOnlyList<WarmupStep> warmups)
    {
        var range = pe.RepMin == pe.RepMax ? $"{pe.RepMin}" : $"{pe.RepMin}–{pe.RepMax}";
        var last = ex == null ? [] : progression.LastPerformance(ex.Id)?.Sets.Where(s => s.IsCompleted && !s.IsWarmup).ToList() ?? [];
        return new PlanDaySheetExercise
        {
            OpenCommand = OpenInfo(ex),
            Thumb = Thumb(ex),
            Name = ex?.Name ?? "Unknown exercise",
            Detail = $"{ex?.Equipment.Display() ?? ""} · {pe.Sets}×{range} reps",
            IsDone = false,
            FirstHeader = "TARGET",
            SecondHeader = $"LAST {units.Label.ToUpperInvariant()}",
            ThirdHeader = "LAST REPS",
            Rows =
            [
                .. warmups.Select((w, i) => new PlanDaySetRow($"W{i + 1}", w.ToString(), "–", "–")),
                .. Enumerable.Range(0, pe.Sets).Select(i => new PlanDaySetRow(
                    $"{i + 1}",
                    range,
                    last.ElementAtOrDefault(i) is { } s ? units.Format(s.WeightKg) : "–",
                    last.ElementAtOrDefault(i) is { } r ? $"{r.Reps}" : "–")),
            ],
        };
    }

    /// <summary>
    /// A finished exercise's sets: the warm-ups done and a row for each one skipped, then the working sets likewise (only
    /// how many were skipped is kept), the way the workout showed them. A set not done (the workout was open in the
    /// workout view when the app closed) shows as skipped too.
    /// </summary>
    List<PlanDaySetRow> SessionRows(SessionExercise se, Exercise? ex)
    {
        var (working, warm) = (0, 0);
        PlanDaySetRow Row(SetEntry s) => new(
            s.IsWarmup ? $"W{++warm}" : $"{++working}",
            ex?.IsBodyweight == true && s.WeightKg <= 0 ? "BW" : units.Format(s.WeightKg),
            $"{s.Reps}",
            !s.IsWarmup && ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir) is > 0 and var e1 ? units.Format(e1) : "–");
        PlanDaySetRow Skipped(bool warmup) => new(warmup ? $"W{++warm}" : $"{++working}", "Skipped", "–", "N/A") { IsSkipped = true };
        return
        [
            .. se.Sets.Where(s => s.IsWarmup && s.IsCompleted).Select(Row),
            .. Enumerable.Range(0, se.SkippedWarmups + se.Sets.Count(s => s.IsWarmup && !s.IsCompleted)).Select(_ => Skipped(true)),
            .. se.Sets.Where(s => !s.IsWarmup && s.IsCompleted).Select(Row),
            .. Enumerable.Range(0, se.SkippedSets + se.Sets.Count(s => !s.IsWarmup && !s.IsCompleted)).Select(_ => Skipped(false)),
        ];
    }

    // ---------- Linking a finished workout to a plan ----------

    /// <summary>
    /// Links a finished workout to a plan (or another one): the plan, then the day of it the workout was, then the week it
    /// counts toward (by default the first that day isn't done in). Unlinking is in the menu too.
    /// </summary>
    async Task LinkToPlan(WorkoutSession session)
    {
        var plans = store.Data.Plans.Where(p => p.Workouts.Count > 0).ToList();
        if (plans.Count == 0)
        {
            await dialogs.Alert("No plans", "Make or import a plan first, then link workouts to its days.");
            return;
        }
        const string unlink = "Unlink from plan";
        var current = store.GetPlan(session.PlanId);
        // Numbered so plans with the same name stay distinguishable; the one it's linked to marked.
        var labels = plans.Select((p, i) => $"{i + 1}. {p.Name}{(p == current ? " (linked now)" : "")}").ToList();
        var pick = await dialogs.ActionSheet("Link to plan", current == null ? null : unlink, [.. labels]);
        if (pick == null)
            return;
        if (pick == unlink)
        {
            store.LinkSession(session, null, null, null);
            ShowFinishedWorkout(session);
            return;
        }
        var plan = plans[labels.IndexOf(pick)];

        // Its days, in order; the one with the workout's name first, as the likely one.
        var days = plan.Workouts.ToList();
        var likely = days.FirstOrDefault(w => w.Id == session.PlanWorkoutId && plan == current)
            ?? days.FirstOrDefault(w => w.Name.Equals(session.Name, StringComparison.OrdinalIgnoreCase));
        if (likely != null)
            days = [likely, .. days.Where(w => w != likely)];
        var dayLabels = days.Select((w, i) => $"{i + 1}. {w.Name}").ToList();
        var dayPick = await dialogs.ActionSheet($"Which day of {plan.Name}?", null, [.. dayLabels]);
        if (dayPick == null)
            return;
        var workout = days[dayLabels.IndexOf(dayPick)];

        // The week: as if this workout weren't done yet, the first week that day is open in.
        var others = store.History.Where(s => s.Id != session.Id).ToList();
        var progress = new PlanProgress(plan, others);
        var suggested = session.PlanId == plan.Id && session.PlanWorkoutId == workout.Id && session.PlanWeek is { } linkedWeek ? linkedWeek : progress.FirstOpenWeek(workout);
        if (await dialogs.Numbers("Week", $"The week of {plan.Name} this {workout.Name} counts toward.", "Link",
                new Views.NumberField("Week", suggested, 1, Views.NumberField.NoLimit)) is not [var week])
            return;
        if (progress.SessionFor(workout, week) is { } taken && !await dialogs.Confirm($"Week {week} already has {workout.Name}",
                $"It was done on {taken.StartedAt:d MMM yyyy}. Link this one too? The later of the two counts for the week.", "Link anyway"))
            return;
        store.LinkSession(session, plan, workout, week);
        ShowFinishedWorkout(session);
    }

    // ---------- Changing a finished workout ----------

    /// <summary>Keeps a change to the finished workout and shows it, with its stats worked out again.</summary>
    void Edited(WorkoutSession session)
    {
        store.Save();
        ShowFinishedWorkout(session);
    }

    /// <summary>Renames a finished workout.</summary>
    async Task Rename(WorkoutSession session)
    {
        if (await dialogs.Prompt("Rename workout", "", session.Name) is not { } name || (name = name.Trim()).Length == 0)
            return;
        session.Name = name.Length > SyncLimits.NameLength ? name[..SyncLimits.NameLength] : name;
        Edited(session);
    }

    /// <summary>How long a finished workout took: its end moves, its start stays.</summary>
    async Task ChangeLength(WorkoutSession session)
    {
        var minutes = (int)Math.Round(session.Duration.TotalMinutes);
        if (await dialogs.Numbers("Workout length", "Minutes from start to finish. The start stays where it is.", "Save",
                new Views.NumberField("Minutes", Math.Clamp(minutes, 1, 600), 1, 600)) is not [var value])
            return;
        var end = session.StartedAt.AddMinutes(value);
        if (end > DateTime.Now)
        {
            await dialogs.Alert("Too long", "The workout would end in the future. Pick fewer minutes, or an earlier start time.");
            return;
        }
        session.EndedAt = end;
        Edited(session);
    }

    static ExerciseThumb Thumb(Exercise? ex) =>
        ex == null ? new ExerciseThumb(null, "?", Colors.Gray, Colors.Gray.WithAlpha(0.16f)) : ExerciseThumb.For(ex);

    // Over this sheet, like Help on the workout sheet.
    IAsyncRelayCommand OpenInfo(Exercise? ex) =>
        new AsyncRelayCommand(() => ex == null ? Task.CompletedTask : GoTo($"{Routes.Exercise}?id={ex.Id}"));

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
        // A workout not done this week can be skipped (Up next moves past it), or a skip undone. It can still be done either way.
        const string skip = "Skip workout", unskip = "Undo skip", edit = "Edit in plan";
        var progress = new PlanProgress(plan, store.History);
        var options = new List<string>();
        if (progress.Days.ElementAtOrDefault(_day) is { } workout && progress.SessionFor(workout, _week) == null)
            options.Add(progress.IsSkipped(_day, _week) ? unskip : skip);
        options.Add(edit);
        switch (await dialogs.ActionSheet(DayName, null, [.. options]))
        {
            case skip or unskip:
                PlanProgress.SetSkipped(plan, _day, _week, !progress.IsSkipped(_day, _week));
                store.Save();
                await OnAppearingAsync();
                break;
            case edit:
                // One navigation: the sheet slides away as the plan comes in, with nothing in between.
                await GoTo($"../{Routes.Plan}?id={plan.Id}&day={_day}");
                break;
        }
    }
}
