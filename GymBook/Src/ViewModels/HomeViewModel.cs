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
    WorkoutEstimator estimator,
    ProgressionEngine progression) : BaseViewModel
{

    [ObservableProperty] string greeting = "";
    [ObservableProperty] string dateText = "";
    /// <summary>Which little scene sits by the greeting.</summary>
    [ObservableProperty] DayPart dayPart;
    /// <summary>The connection, at the end of the date line; kept up to date while Home is showing.</summary>
    [ObservableProperty] bool isOnline;
    [ObservableProperty] string connectionText = "";
    [ObservableProperty] Color connectionColor = Colors.Transparent;

    // How ready the Up next workout's muscles are, 0 to 1, and what that does to the card: the ring, the words, and
    // a Start button (and background) that get brighter and livelier the fresher you are.
    [ObservableProperty] double nextReadiness = 1;
    [ObservableProperty] string nextReadyText = "";
    [ObservableProperty] Color nextTint = Color.FromArgb("#3F7DFF");
    [ObservableProperty] Color startColor = Color.FromArgb("#3F7DFF");
    [ObservableProperty] Color startTextColor = Colors.White;
    /// <summary>How lively the card's glow is: the readiness, or full while a workout is running.</summary>
    [ObservableProperty] double nextGlow = 1;

    /// <summary>
    /// A workout is in progress: the Up next card shows it instead (and its day in the week below), and tapping either
    /// opens it rather than starting anything new.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNextCard))]
    bool isWorkoutRunning;
    [ObservableProperty] string startText = "▶  Start";
    public bool ShowNextCard => HasPlan || IsWorkoutRunning;
    /// <summary>Workouts put aside unfinished, newest first, each with Resume.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPaused))]
    List<PausedWorkoutItem> pausedWorkouts = [];
    public bool HasPaused => PausedWorkouts.Count > 0;

    static readonly Color Muted = Color.FromArgb("#2C3240"), Amber = Color.FromArgb("#FFB020"),
        Blue = Color.FromArgb("#3F7DFF"), Green = Color.FromArgb("#2ED47A");

    void ShowReadiness(double readiness, List<MuscleRecovery> tired, PlanWorkout? fresher)
    {
        NextReadiness = Math.Clamp(readiness, 0, 1);
        NextGlow = NextReadiness;
        NextReadyText = tired.Count == 0
            ? $"{NextReadiness:P0} recovered · ready to go"
            : $"{NextReadiness:P0} recovered · {string.Join(", ", tired.Take(2).Select(t => t.Muscle.Display()))} still recovering"
              + (fresher != null ? $" · {fresher.Name} is fresher" : "");
        // Amber when tired, through the app's blue, to green when fresh.
        NextTint = NextReadiness < 0.75
            ? Lerp(Amber, Blue, (float)Math.Clamp((NextReadiness - 0.5) / 0.25, 0, 1))
            : Lerp(Blue, Green, (float)Math.Clamp((NextReadiness - 0.75) / 0.25, 0, 1));
        // The button starts out flat and grey, and takes on that colour as recovery goes up.
        var vivid = (float)Math.Clamp((NextReadiness - 0.4) / 0.5, 0, 1);
        StartColor = Lerp(Muted, NextTint, vivid);
        StartTextColor = vivid > 0.35 ? Colors.White : Color.FromArgb("#9AA3B5");
    }

    static Color Lerp(Color a, Color b, float t) =>
        new(a.Red + (b.Red - a.Red) * t, a.Green + (b.Green - a.Green) * t, a.Blue + (b.Blue - a.Blue) * t, a.Alpha + (b.Alpha - a.Alpha) * t);

    /// <summary>Home is the tab on screen: the greeting's scene animates only then.</summary>
    [ObservableProperty] bool isShowing;

    public override void OnDisappearing()
    {
        IsShowing = false;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }

    void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e) => MainThread.BeginInvokeOnMainThread(ShowConnection);

    void ShowConnection()
    {
        IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        ConnectionText = IsOnline ? "Online" : "Offline";
        ConnectionColor = IsOnline ? Color.FromArgb("#2ED47A") : Color.FromArgb("#FFB020");
    }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNextCard))]
    bool hasPlan;
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
    [ObservableProperty] string nextLabel = "UP NEXT";
    [ObservableProperty] List<PlanDayItem> planDays = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWelcomeBack))]
    string welcomeBack = "";
    public bool HasWelcomeBack => WelcomeBack.Length > 0;

    // The plan week picked from the week menu, for as long as that plan stays active; otherwise the current week.
    string? _chosenPlanId;
    int _chosenWeek;
    int _week;
    // What "Start" in the Up next card starts: the first workout left in the shown week, else in the week after.
    int _nextWeek;
    PlanWorkout? _next;

    public override Task OnAppearingAsync()
    {
        IsShowing = true;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        ShowConnection();
        Refresh();
        return Task.CompletedTask;
    }

    void Refresh()
    {
        var profile = store.Profile;
        DayPart = TimeOfDayView.For(DateTime.Now);
        var part = DayPart switch
        {
            DayPart.Morning => "Good morning",
            DayPart.Afternoon => "Good afternoon",
            DayPart.Evening => "Good evening",
            // After midnight and before dawn.
            _ => DateTime.Now.Hour < 5 ? "Up late" : "Good evening",
        };
        Greeting = string.IsNullOrWhiteSpace(profile.Name) ? part : $"{part}, {profile.Name}";
        DateText = DateTime.Today.ToString("dddd, d MMMM");

        // Close any gap in the plan weeks (an empty week before one with progress) before showing them, and move any
        // exercise ids from an earlier library to today's (both, so neither short-circuits the other).
        if (store.CompactPlanWeeks() | store.MigrateExerciseIds())
            store.Save();

        var plan = store.ActivePlan;
        HasPlan = plan is { Workouts.Count: > 0 };
        if (plan is { Workouts.Count: > 0 })
        {
            var progress = new PlanProgress(plan, store.History);
            _week = _chosenPlanId == plan.Id ? Math.Min(_chosenWeek, progress.LastUnlockedWeek) : progress.CurrentWeek;
            // Up next is always the plan's actual next workout, whichever week is shown below (picking another week in
            // the week menu doesn't change it). Recovery only colours the card, and when it would hit muscles still
            // recovering, names a fresher workout left in its week.
            var current = progress.CurrentWeek;
            (_nextWeek, _next) = progress.NextWorkoutFrom(current) ?? (current, progress.Days.OfType<PlanWorkout>().First());
            var now = DateTime.Now;
            var next = _next;
            NextLabel = _nextWeek == _week ? "UP NEXT" : $"UP NEXT · WEEK {_nextWeek}";
            var tired = recovery.NotReady(next, now);
            var fresher = tired.Count == 0 ? null : progress.Days.OfType<PlanWorkout>()
                .Where(w => w != next && w.Exercises.Count > 0 && !progress.IsHandled(w, _nextWeek) && recovery.NotReady(w, now).Count == 0)
                .MaxBy(w => recovery.Readiness(w, now));
            ShowReadiness(recovery.Readiness(next, now), tired, fresher);
            BuildPlanWeek(plan, progress);

            var exercises = next.Exercises.Select(e => (pe: e, ex: store.GetExercise(e.ExerciseId))).Where(x => x.ex != null).ToList();
            var sets = next.Exercises.Sum(e => e.Sets);
            var minutes = estimator.Minutes(next, plan.Goal);
            PlanName = plan.Name;
            NextWorkoutName = next.Name;
            NextWorkoutMeta = $"{exercises.Count} exercises · {sets} sets\n{WorkoutEstimator.Format(minutes)}";
            NextWorkoutMuscles = string.Join(" · ", exercises.Select(x => x.ex!.PrimaryMuscle).Distinct().Select(m => m.Display()));
            NextThumbs = exercises.Take(5).Select(x => ExerciseThumb.For(x.ex!)).ToList();
            NextMore = exercises.Count > 5 ? $"+{exercises.Count - 5}" : "";
        }
        else
        {
            _next = null;
            PlanDays = [];
        }

        ShowActiveWorkout();
        ShowPausedWorkouts();

        WelcomeBack = progression.DaysAway() is { } away
            ? $"It's been {away} days since your last workout. Your weights are set lighter for a safe return and build back up over the next few sessions."
            : "";

        UpdateRecovery();

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

    // The workout in progress, if any, in place of Up next: how far along it is, and Resume to jump back in.
    void ShowActiveWorkout()
    {
        var active = workouts.Active;
        IsWorkoutRunning = active != null;
        StartText = active != null ? "▶  Resume" : "▶  Start";
        if (active == null)
            return;
        var sets = active.Exercises.SelectMany(e => e.Sets).Where(s => !s.IsWarmup).ToList();
        var done = sets.Count(s => s.IsCompleted);
        var exercises = active.Exercises.Select(e => store.GetExercise(e.ExerciseId)).OfType<Exercise>().ToList();
        NextLabel = "IN PROGRESS";
        NextWorkoutName = active.Name;
        NextWorkoutMuscles = string.Join(" · ", exercises.Select(x => x.PrimaryMuscle).Distinct().Select(m => m.Display()));
        NextThumbs = exercises.Take(5).Select(x => ExerciseThumb.For(x)).ToList();
        NextMore = exercises.Count > 5 ? $"+{exercises.Count - 5}" : "";
        NextWorkoutMeta = $"{done}/{sets.Count} sets done\nStarted {active.StartedAt:HH:mm} · {Units.Duration(DateTime.Now - active.StartedAt)} in";
        // The ring shows how much of it is done; the card is lit up, it's happening now.
        NextReadiness = sets.Count == 0 ? 0 : (double)done / sets.Count;
        NextReadyText = "Running now · tap to jump back in";
        NextTint = Green;
        NextGlow = 1;
        StartColor = Green;
        StartTextColor = Color.FromArgb("#06200F");
    }

    void ShowPausedWorkouts() => PausedWorkouts = store.Data.PausedSessions.Select(session =>
    {
        var sets = session.Exercises.SelectMany(e => e.Sets).Where(s => !s.IsWarmup).ToList();
        var paused = session.PausedAt ?? session.StartedAt;
        var when = paused.Date == DateTime.Today ? $"{paused:t}" : paused.Date == DateTime.Today.AddDays(-1) ? $"yesterday {paused:t}" : $"{paused:ddd d MMM}";
        return new PausedWorkoutItem
        {
            Name = session.Name,
            Detail = $"Paused {when} · {sets.Count(s => s.IsCompleted)}/{sets.Count} sets · {Units.Duration(session.Duration)}",
            ResumeCommand = new AsyncRelayCommand(() => ResumeWorkoutAsync(workouts, dialogs, session)),
            MenuCommand = new AsyncRelayCommand(() => PausedMenu(session)),
        };
    }).ToList();

    /// <summary>Tapping a paused workout: resume it, or discard it (it can be brought back from Recently deleted).</summary>
    async Task PausedMenu(WorkoutSession session)
    {
        switch (await dialogs.ActionSheet(session.Name, "Discard workout", "Resume"))
        {
            case "Resume":
                await ResumeWorkoutAsync(workouts, dialogs, session);
                break;
            case "Discard workout":
                if (!await dialogs.Confirm("Discard workout?", "It's paused, not finished: everything logged in it goes. It can be brought back from Recently deleted for a while.", "Discard"))
                    return;
                store.Data.PausedSessions.Remove(session);
                store.Save();
                Refresh();
                break;
        }
    }

    /// <summary>The shown plan week's days in order. Tapping a day opens it in the day sheet, where it can be started or marked finished.</summary>
    void BuildPlanWeek(WorkoutPlan plan, PlanProgress progress)
    {
        var week = _week;
        PlanWeek = $"Week {week}";
        // Up next is marked only in its own week.
        var next = week == _nextWeek ? _next : null;
        // The day being done right now: marked as such, and it opens the running workout.
        var active = workouts.Active;
        var running = active?.PlanId == plan.Id && (active.PlanWeek ?? week) == week ? active.PlanWorkoutId : null;
        PlanDays = progress.Days.Select((w, day) =>
        {
            var isRunning = w != null && w.Id == running;
            var open = isRunning
                ? new AsyncRelayCommand(() => GoTo(Routes.Workout))
                : new AsyncRelayCommand(() => GoTo($"{Routes.PlanDay}?id={plan.Id}&day={day}&week={week}"));
            if (w == null)
                return new PlanDayItem { Name = "Rest", Number = "–", IsRest = true, IsDone = progress.IsRestDone(day, week), Thumbnails = [], More = "", OpenCommand = open };
            var thumbs = w.Exercises
                .Select(e => ExerciseLibrary.Find(e.ExerciseId))
                .OfType<Exercise>()
                .Select(ExerciseThumb.For)
                .ToList();
            return new PlanDayItem
            {
                Name = w.Name,
                Number = (plan.Workouts.IndexOf(w) + 1).ToString(),
                IsDone = progress.SessionFor(w, week) != null,
                IsSkipped = progress.SessionFor(w, week) == null && progress.IsSkipped(day, week),
                IsNext = w == next && !isRunning && running == null,
                IsRunning = isRunning,
                Thumbnails = thumbs.Take(3).ToList(),
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
        if (workouts.Active != null)
        {
            await GoTo(Routes.Workout);
            return;
        }
        var plan = store.ActivePlan;
        if (plan == null || _next is not { } next)
            return;
        var week = _nextWeek;
        await StartPlannedWorkoutAsync(workouts, dialogs, recovery, plan, next, week);
    }

    /// <summary>Skip on the Up next card: skips that workout for its week, so Up next moves on to the one after.</summary>
    [RelayCommand]
    async Task SkipNext()
    {
        var plan = store.ActivePlan;
        if (workouts.Active != null || plan == null || _next is not { } next)
            return;
        var day = PlanSchedule.Days(plan).IndexOf(next);
        if (day < 0 || !await dialogs.Confirm($"Skip {next.Name}?", $"It's marked skipped for week {_nextWeek} and Up next moves on. You can still do it, or undo the skip, from its day.", "Skip"))
            return;
        PlanProgress.SetSkipped(plan, day, _nextWeek, true);
        store.Save();
        Refresh();
    }

    /// <summary>Tapping the Up next card previews that day, like tapping it in the week below.</summary>
    [RelayCommand]
    Task OpenNext()
    {
        if (workouts.Active != null)
            return GoTo(Routes.Workout);
        var plan = store.ActivePlan;
        if (plan == null || _next is not { } next)
            return Task.CompletedTask;
        var day = PlanSchedule.Days(plan).IndexOf(next);
        return day < 0 ? Task.CompletedTask : GoTo($"{Routes.PlanDay}?id={plan.Id}&day={day}&week={_nextWeek}");
    }

    [RelayCommand]
    Task StartEmpty() => StartWorkoutAsync(workouts, dialogs, workouts.StartEmpty);

    /// <summary>
    /// Starts a plan week over: every day open again. The week is what's in the calendar, so its workouts are deleted
    /// (from the calendar, History and stats too) and its rest days unmarked.
    /// </summary>
    async Task ResetWeek(WorkoutPlan plan, PlanProgress progress, int week)
    {
        var sessions = progress.SessionsIn(week);
        var message = sessions.Count switch
        {
            0 => "Its rest days are marked not done again.",
            1 => "Its workout is deleted, from the calendar and your stats too. This can't be undone.",
            _ => $"Its {sessions.Count} workouts are deleted, from the calendar and your stats too. This can't be undone.",
        };
        if (!await dialogs.Confirm($"Reset week {week}?", message, "Reset"))
            return;
        foreach (var session in sessions)
            store.Data.Sessions.Remove(session);
        PlanProgress.ClearRestDays(plan, week);
        // The weeks after it move up, so the week just emptied doesn't sit before them.
        store.CompactPlanWeeks();
        store.Save();
        Refresh();
    }

    /// <summary>The ··· beside the week's plan: options for the plan itself.</summary>
    [RelayCommand]
    async Task PlanOptions()
    {
        var plan = store.ActivePlan;
        if (plan == null)
            return;
        var others = store.Data.Plans.Where(p => p != plan).ToList();
        var options = new List<string> { "Edit plan" };
        if (others.Count > 0)
            options.Add("Switch plan");
        // The week shown below: start it over.
        var week = _week;
        var progress = new PlanProgress(plan, store.History);
        var reset = progress.HasAnythingDone(week) ? $"Reset week {week}" : null;

        switch (await dialogs.ActionSheet(plan.Name, reset, [.. options]))
        {
            case { } choice when choice == reset:
                await ResetWeek(plan, progress, week);
                break;
            case "Edit plan":
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

    // Recovery preview: hours from now, negative for the past.
    [ObservableProperty] double recoveryHours;
    [ObservableProperty] string recoveryWhen = "Now";
    [ObservableProperty] bool isRecoveryPreview;
    [ObservableProperty] List<MuscleRecoveryItem> majorMuscles = [];
    [ObservableProperty] List<MuscleRecoveryItem> supportingMuscles = [];

    partial void OnRecoveryHoursChanged(double value)
    {
        // Snap to whole hours so the label and map move in clean steps.
        var snapped = Math.Round(value);
        if (Math.Abs(snapped - value) > 0.001)
        {
            RecoveryHours = snapped;
            return;
        }
        UpdateRecovery();
    }

    void UpdateRecovery()
    {
        var at = DateTime.Now.AddHours(RecoveryHours);
        var details = recovery.Details(at);
        var rec = details.ToDictionary(d => d.Muscle, d => d.Recovery);
        MuscleMap = MuscleMapDrawable.ForRecovery(rec, recovery.PartRecovery(at));
        (MajorMuscles, SupportingMuscles) = RecoveryViewModel.Lists(details, at);
        RecoveryWhen = RecoveryService.PreviewLabel(RecoveryHours);
        IsRecoveryPreview = RecoveryHours != 0;
        var tired = rec.Where(r => r.Value < 0.6).OrderBy(r => r.Value).Select(r => r.Key.Display()).ToList();
        var fresh = RecoveryHours == 0 ? "All muscle groups are recovered and ready to train." : "All muscle groups are recovered at this point.";
        RecoverySummary = tired.Count == 0 ? fresh : $"{(RecoveryHours == 0 ? "Still recovering" : "Recovering")}: {string.Join(", ", tired)}";
    }

    [RelayCommand]
    void RecoveryNow() => RecoveryHours = 0;
}

/// <summary>A workout put aside unfinished, on the Workout tab.</summary>
public class PausedWorkoutItem
{
    public required string Name { get; init; }
    /// <summary>"Paused 18:02 · 6/14 sets · 34 min".</summary>
    public required string Detail { get; init; }
    public required IAsyncRelayCommand ResumeCommand { get; init; }
    public required IAsyncRelayCommand MenuCommand { get; init; }
}
