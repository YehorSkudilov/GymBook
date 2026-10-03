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
    /// <summary>Editing a finished workout: tapping the row changes the set. Null otherwise.</summary>
    public IAsyncRelayCommand? TapCommand { get; init; }
    public Color ValueColor => TapCommand != null ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#9AA3B5");
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
    /// <summary>Editing a finished workout: its ··· (note, replace, move, remove) and Add set show.</summary>
    public bool IsEditing { get; init; }
    public IAsyncRelayCommand? MenuCommand { get; init; }
    public IAsyncRelayCommand? AddSetCommand { get; init; }
}

/// <summary>
/// A single day of a plan week, opened from Home: its exercises with their sets, and the action to start it,
/// view it or mark the rest day finished. Also a finished workout on its own (?session=, from the calendar), plan or
/// not: what was done, its stats, and how fatigued each muscle was right after it; and editing it (Edit workout).
/// </summary>
public partial class PlanDayViewModel(DataStore store, WorkoutService workouts, DialogService dialogs, Units units, ProgressionEngine progression,
    WorkoutEstimator estimator, RecoveryService recovery, StatsService stats, ExercisePickerService picker)
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
    /// <summary>A finished workout is on show and not being edited: the Edit workout button.</summary>
    [ObservableProperty] bool canEdit;
    /// <summary>
    /// The finished workout is being edited: tapping a set changes it, each exercise has a ··· and Add set, and exercises
    /// can be added. Each change is saved as it's made (and syncs like any other).
    /// </summary>
    [ObservableProperty] bool isEditing;
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
        _justFinished = query.TryGetValue("finished", out var finished) && finished?.ToString() == "1";
        _day = query.TryGetValue("day", out var day) && int.TryParse(day?.ToString(), out var d) ? d : 0;
        _week = query.TryGetValue("week", out var week) && int.TryParse(week?.ToString(), out var w) ? w : 1;
        IsEditing = false;
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
        CanEdit = false;
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
        const string startTime = "Change start time", edit = "Edit workout", rename = "Rename", length = "Change length";
        var options = new List<string> { IsEditing ? "Done editing" : edit, rename, startTime, length };
        if (day >= 0)
            options.Add("Edit in plan");
        switch (await dialogs.ActionSheet(DayName, "Discard workout", [.. options]))
        {
            case edit:
                StartEditing();
                break;
            case "Done editing":
                DoneEditing();
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
        CanDiscard = !_justFinished && !IsEditing;
        CanEdit = !IsEditing;
        var plan = store.GetPlan(session.PlanId);
        var logged = session.Exercises.Where(e => e.Sets.Count > 0).ToList();
        DayName = session.Name;
        Subtitle = plan == null ? "Workout" : session.PlanWeek is { } week ? $"{plan.Name} · Week {week}" : plan.Name;
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
        Exercises = logged.Select((e, i) => Logged(e, i, logged.Count)).ToList();
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

    /// <summary>
    /// A finished exercise (the <paramref name="index"/>th of <paramref name="count"/> shown): every logged set with its
    /// weight, reps and, for working sets, estimated one-rep max; while editing, with what changes them.
    /// </summary>
    PlanDaySheetExercise Logged(SessionExercise se, int index, int count)
    {
        var ex = store.GetExercise(se.ExerciseId);
        var sets = se.Sets;
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
            IsEditing = IsEditing,
            MenuCommand = IsEditing ? new AsyncRelayCommand(() => ExerciseMenu(se, ex, index, count)) : null,
            AddSetCommand = IsEditing ? new AsyncRelayCommand(() => AddSet(se)) : null,
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
    /// how many were skipped is kept), the way the workout showed them.
    /// </summary>
    List<PlanDaySetRow> SessionRows(SessionExercise se, Exercise? ex)
    {
        var (working, warm) = (0, 0);
        PlanDaySetRow Row(SetEntry s)
        {
            var number = s.IsWarmup ? $"W{++warm}" : $"{++working}";
            return new(
                number,
                ex?.IsBodyweight == true && s.WeightKg <= 0 ? "BW" : units.Format(s.WeightKg),
                $"{s.Reps}",
                !s.IsWarmup && ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir) is > 0 and var e1 ? units.Format(e1) : "–")
            {
                TapCommand = IsEditing ? new AsyncRelayCommand(() => EditSet(se, s, ex, number)) : null,
            };
        }
        PlanDaySetRow Skipped(bool warmup) => new(warmup ? $"W{++warm}" : $"{++working}", "Skipped", "–", "N/A")
        {
            IsSkipped = true,
            TapCommand = IsEditing ? new AsyncRelayCommand(() => EditSkipped(se, warmup)) : null,
        };
        return
        [
            .. se.Sets.Where(s => s.IsWarmup).Select(Row),
            .. Enumerable.Range(0, se.SkippedWarmups).Select(_ => Skipped(true)),
            .. se.Sets.Where(s => !s.IsWarmup).Select(Row),
            .. Enumerable.Range(0, se.SkippedSets).Select(_ => Skipped(false)),
        ];
    }

    // ---------- Editing a finished workout ----------

    /// <summary>The Edit workout button under a finished workout: its sets and exercises become editable.</summary>
    [RelayCommand]
    void StartEditing()
    {
        if (_finished is not { } session)
            return;
        IsEditing = true;
        ShowFinishedWorkout(session);
    }

    /// <summary>The Done button while editing: back to the finished workout as it now is (every change is already saved).</summary>
    [RelayCommand]
    void DoneEditing()
    {
        IsEditing = false;
        if (_finished is { } session)
            ShowFinishedWorkout(session);
    }

    /// <summary>Keeps a change to the finished workout and shows it, with its stats worked out again.</summary>
    void Edited(WorkoutSession session)
    {
        store.Save();
        ShowFinishedWorkout(session);
    }

    /// <summary>Uses RIR: the profile tracks it, and the workout's plan (if any) doesn't turn it off.</summary>
    bool TrackRir(WorkoutSession session) => store.Profile.TrackRir && store.GetPlan(session.PlanId)?.UseRir != false;

    /// <summary>A logged set, tapped while editing: its weight, reps or RIR, warm-up or working, or deleting it.</summary>
    async Task EditSet(SessionExercise se, SetEntry set, Exercise? ex, string number)
    {
        if (_finished is not { } session)
            return;
        var bodyweight = ex?.IsBodyweight == true && set.WeightKg <= 0;
        string weight = $"Weight · {(bodyweight ? "bodyweight" : units.FormatWithUnit(set.WeightKg))}", reps = $"Reps · {set.Reps}",
            rir = $"RIR · {(set.Rir is { } r ? r.ToString() : "none")}", kind = set.IsWarmup ? "Make it a working set" : "Make it a warm-up";
        var options = new List<string> { weight, reps };
        if (TrackRir(session) || set.Rir != null)
            options.Add(rir);
        options.Add(kind);
        var title = $"{ex?.Name ?? "Set"} · {(set.IsWarmup ? "warm-up " : "set ")}{number.TrimStart('W')}";
        var choice = await dialogs.ActionSheet(title, "Delete set", [.. options]);
        if (choice == weight)
        {
            var max = units.Unit == WeightUnit.Kg ? 500 : 1100;
            var step = ex == null ? (units.Unit == WeightUnit.Kg ? 2.5 : 5) : units.Increment(ex);
            if (await Views.NumberPadSheet.Show(units.Format(set.WeightKg), units.ToDisplay(set.WeightKg), step, 0, max, units.Label, decimals: true) is not { } v)
                return;
            set.WeightKg = units.FromDisplay(v);
        }
        else if (choice == reps)
        {
            if (await Views.NumberPadSheet.Show($"{set.Reps}", set.Reps, 1, 0, 100, "reps") is not { } v)
                return;
            set.Reps = (int)v;
        }
        else if (choice == rir)
        {
            if (await Views.NumberPadSheet.Show(set.Rir?.ToString(), set.Rir ?? se.TargetRir, 1, 0, 10, "RIR", allowEmpty: true) is not { } v)
                return;
            set.Rir = double.IsNaN(v) ? null : (int)Math.Min(v, 10);
        }
        else if (choice == kind)
        {
            set.IsWarmup = !set.IsWarmup;
            // Kept in the order they're shown: warm-ups, then working sets.
            se.Sets = [.. se.Sets.Where(s => s.IsWarmup), .. se.Sets.Where(s => !s.IsWarmup)];
        }
        else if (choice == "Delete set")
        {
            await DeleteSet(session, se, set);
            return;
        }
        else
            return;
        Edited(session);
    }

    /// <summary>
    /// Deletes a logged set. The exercise's last goes with it (after asking); the workout's very last means discarding it.
    /// </summary>
    async Task DeleteSet(WorkoutSession session, SessionExercise se, SetEntry set)
    {
        if (se.Sets.Count == 1)
        {
            if (session.Exercises.Count(e => e.Sets.Count > 0) == 1)
            {
                await DeleteSession(session);
                return;
            }
            var name = store.GetExercise(se.ExerciseId)?.Name ?? "this exercise";
            if (!await dialogs.Confirm("Remove the exercise?", $"It's the last set of {name}, so {name} is taken out of this workout.", "Remove"))
                return;
            session.Exercises.Remove(se);
        }
        else
            se.Sets.Remove(set);
        Edited(session);
    }

    /// <summary>A set that was skipped, tapped while editing: logged after all (like the one before it), or taken out.</summary>
    async Task EditSkipped(SessionExercise se, bool warmup)
    {
        if (_finished is not { } session)
            return;
        const string log = "Log it as done";
        switch (await dialogs.ActionSheet(warmup ? "Skipped warm-up" : "Skipped set", "Remove it", log))
        {
            case log:
                if (warmup)
                    se.SkippedWarmups = Math.Max(0, se.SkippedWarmups - 1);
                else
                    se.SkippedSets = Math.Max(0, se.SkippedSets - 1);
                AddLoggedSet(se, warmup);
                break;
            case "Remove it":
                if (warmup)
                    se.SkippedWarmups = Math.Max(0, se.SkippedWarmups - 1);
                else
                    se.SkippedSets = Math.Max(0, se.SkippedSets - 1);
                break;
            default:
                return;
        }
        Edited(session);
    }

    /// <summary>Add set under an exercise while editing: another working set like its last one, to change from there.</summary>
    Task AddSet(SessionExercise se)
    {
        if (_finished is { } session)
        {
            AddLoggedSet(se, warmup: false);
            Edited(session);
        }
        return Task.CompletedTask;
    }

    /// <summary>A done set, a copy of the last one of its kind (or of any), in its place: warm-ups first, then working sets.</summary>
    static void AddLoggedSet(SessionExercise se, bool warmup)
    {
        var like = se.Sets.LastOrDefault(s => s.IsWarmup == warmup) ?? se.Sets.LastOrDefault();
        var set = new SetEntry
        {
            WeightKg = like?.WeightKg ?? 0,
            Reps = like?.Reps ?? se.RepMin,
            Rir = warmup ? null : like?.Rir,
            IsWarmup = warmup,
            IsCompleted = true,
        };
        var at = warmup ? se.Sets.FindLastIndex(s => s.IsWarmup) + 1 : se.Sets.Count;
        se.Sets.Insert(at, set);
    }

    /// <summary>An exercise's ··· while editing: its note, replacing it, moving it, or taking it out.</summary>
    async Task ExerciseMenu(SessionExercise se, Exercise? ex, int index, int count)
    {
        if (_finished is not { } session)
            return;
        const string note = "Edit note", replace = "Replace exercise", up = "Move up", down = "Move down";
        var options = new List<string> { note, replace };
        if (index > 0)
            options.Add(up);
        if (index < count - 1)
            options.Add(down);
        var choice = await dialogs.ActionSheet(ex?.Name ?? "Exercise", "Remove exercise", [.. options]);
        switch (choice)
        {
            case note:
                if (await dialogs.Prompt("Note", "Your note on this exercise in this workout.", se.Note) is not { } text)
                    return;
                text = text.Trim();
                se.Note = text.Length == 0 ? null : text.Length > SyncLimits.TextLength ? text[..SyncLimits.TextLength] : text;
                break;
            case replace:
                if (await picker.PickOneAsync($"Replace {ex?.Name ?? "exercise"}", ex) is not { } replacement)
                    return;
                se.ExerciseId = replacement.Id;
                break;
            case up or down:
                // Among the exercises shown (those with sets logged), so it swaps with the one beside it on screen.
                var shown = session.Exercises.Where(e => e.Sets.Count > 0).ToList();
                var neighbour = shown[choice == up ? index - 1 : index + 1];
                var (at, to) = (session.Exercises.IndexOf(se), session.Exercises.IndexOf(neighbour));
                (session.Exercises[at], session.Exercises[to]) = (session.Exercises[to], session.Exercises[at]);
                break;
            case "Remove exercise":
                if (session.Exercises.Count(e => e.Sets.Count > 0) == 1)
                {
                    await DeleteSession(session);
                    return;
                }
                if (!await dialogs.Confirm("Remove the exercise?", $"{ex?.Name ?? "It"} and its sets are taken out of this workout.", "Remove"))
                    return;
                session.Exercises.Remove(se);
                break;
            default:
                return;
        }
        Edited(session);
    }

    /// <summary>Add exercise while editing: each picked goes at the end, with one set to change from there.</summary>
    [RelayCommand]
    async Task AddExercise()
    {
        if (_finished is not { } session)
            return;
        var picked = await picker.PickAsync();
        if (picked.Count == 0)
            return;
        foreach (var ex in picked)
        {
            // The weight and reps it would be suggested now, as a starting point.
            var se = workouts.CreateAdHoc(ex, session);
            var first = se.Sets.FirstOrDefault(s => !s.IsWarmup);
            se.Sets = [new SetEntry { WeightKg = first?.WeightKg ?? 0, Reps = first?.Reps ?? se.RepMin, IsCompleted = true }];
            se.Recommendation = null;
            se.Note = null;
            se.NotePinned = false;
            session.Exercises.Add(se);
        }
        Edited(session);
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
