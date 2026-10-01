using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.Wear;

/// <summary>A day of the shown plan week, as on the phone's Workout tab: ✓ done, ▶ running, its number, or – for rest.</summary>
public record WatchDayItem(string Badge, Color BadgeColor, string Name, Color NameColor, string Status, Color StatusColor, ICommand OpenCommand)
{
    public bool HasStatus => Status.Length > 0;
}

/// <summary>
/// The watch's home, laid out like the phone's Workout tab: an Up next card (the plan's next workout, how recovered its
/// muscles are, Start), and the plan's week below it (pick any unlocked week, see which days are done, start one), with
/// the active plan switchable. A workout in progress takes the card's place: the watch's own (Resume), or the phone's,
/// which opens the companion page (the phone owns it). Workouts started here are stored on the watch and synced to the
/// account whenever there's a connection; plans arrive by syncing, so the plan part needs the watch signed in.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    static readonly Color Green = Color.FromArgb("#2ED47A"), Blue = Color.FromArgb("#3F7DFF"), Amber = Color.FromArgb("#FFB020");
    static readonly Color Grey = Color.FromArgb("#626B7E"), TextPrimary = Color.FromArgb("#F4F6FB"), TextSecondary = Color.FromArgb("#9AA3B5");

    readonly IServiceProvider _services;
    readonly DataStore _store;
    readonly WorkoutService _workouts;
    readonly RecoveryService _recovery;
    readonly WorkoutEstimator _estimator;
    readonly SyncService _sync;
    readonly WatchAccount _account;
    readonly PhoneLink _phone;
    bool _opened;
    bool _askedNotifications;
    string? _lastPhoneSession;
    // The week picked with "Week N ▾" (for that plan); otherwise the plan's current week.
    string? _chosenPlanId;
    int _chosenWeek;
    int _week;
    Func<Task>? _cardAction;

    public HomeViewModel(IServiceProvider services, DataStore store, WorkoutService workouts, RecoveryService recovery, WorkoutEstimator estimator,
        SyncService sync, WatchAccount account, PhoneLink phone)
    {
        (_services, _store, _workouts, _recovery, _estimator, _sync, _account, _phone) = (services, store, workouts, recovery, estimator, sync, account, phone);
        _phone.WorkoutChanged += _ => OnPhoneWorkoutChanged();
        _sync.StatusChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        _store.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
    }

    [ObservableProperty] bool isSignedIn;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string message = "";
    [ObservableProperty] string syncStatus = "";
    [ObservableProperty] string account = "";

    // The card: Up next, or the workout in progress.
    [ObservableProperty] bool hasCard;
    [ObservableProperty] string cardLabel = "";
    [ObservableProperty] Color cardColor = Blue;
    [ObservableProperty] string cardName = "";
    [ObservableProperty] string cardMuscles = "";
    [ObservableProperty] string cardStatus = "";
    [ObservableProperty] string cardMeta = "";
    [ObservableProperty] string cardButton = "▶  Start";

    // The plan's week.
    [ObservableProperty] bool hasPlan;
    [ObservableProperty] string planName = "";
    [ObservableProperty] string weekText = "";
    [ObservableProperty] IReadOnlyList<WatchDayItem> days = [];
    [ObservableProperty] bool hasNoPlan;
    [ObservableProperty] bool canChangePlan;
    [ObservableProperty] bool canChoosePlan;
    [ObservableProperty] bool canQuickStart;

    // The pill floating at the bottom while a workout runs (minimized), like the phone's: tap to go back to it.
    [ObservableProperty] bool hasPill;
    [ObservableProperty] string pillName = "";
    [ObservableProperty] string pillTime = "";
    IDispatcherTimer? _pillTimer;

    /// <summary>The phone is running a workout, other than the watch's own one that it picked up by syncing.</summary>
    bool PhoneOwnsWorkout => _phone.Workout.IsActive && _phone.Workout.SessionId != WatchOwnership.SessionId;

    /// <summary>
    /// The watch's own workout in progress. A workout in progress that came in by syncing (the phone's) counts too once
    /// the phone isn't running it any more, e.g. it was started on the phone and the phone is far away.
    /// </summary>
    bool HasOwnWorkout => _workouts.Active is { } active && (active.Id == WatchOwnership.SessionId || !PhoneOwnsWorkout);

    public async Task OnAppearingAsync()
    {
        await _account.EnsureLoadedAsync();
        // For the Ongoing Activity on the watch face during workouts (Wear OS 4+ asks).
        if (!_askedNotifications)
        {
            _askedNotifications = true;
            try
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
            catch (Exception)
            {
            }
        }
        Refresh();
        // Catch up: plans and workouts from the phone or the website, and anything done on the watch offline.
        _sync.Schedule(TimeSpan.Zero);
        // The first time: straight into whichever workout is running, like opening the phone app mid-workout.
        if (!_opened)
        {
            _opened = true;
            if (HasOwnWorkout && _workouts.Active?.Id == WatchOwnership.SessionId)
                await ResumeWatchWorkout();
            else if (PhoneOwnsWorkout)
                await OpenPhoneWorkout();
            else if (HasOwnWorkout)
                await ResumeWatchWorkout();
        }
    }

    // A workout just started on the phone while the watch shows its home: show it, as the phone app would. Only when
    // it's a new one, so backing out of it to the home doesn't bring it straight back at the next tick.
    void OnPhoneWorkoutChanged()
    {
        Refresh();
        var started = PhoneOwnsWorkout && _phone.Workout.SessionId != _lastPhoneSession;
        _lastPhoneSession = _phone.Workout.IsActive ? _phone.Workout.SessionId : null;
        if (started && Navigation.NavigationStack.Count == 1)
            _ = OpenPhoneWorkout();
    }

    void Refresh()
    {
        IsSignedIn = _account.IsSignedIn;
        Account = _account.Email ?? "";
        SyncStatus = !IsSignedIn ? "" : _sync.State switch
        {
            SyncState.Syncing => "Syncing…",
            SyncState.UpToDate => _sync.LastSyncedAt is { } at ? $"Synced {at:HH:mm}" : "Up to date",
            SyncState.Offline => "Offline · syncs later",
            SyncState.SignInRequired => "Sign in again to sync",
            SyncState.Failed => _sync.Status,
            _ => "",
        };

        var plan = IsSignedIn ? _store.ActivePlan : null;
        CanQuickStart = !HasOwnWorkout && !PhoneOwnsWorkout;
        CanChangePlan = IsSignedIn && _store.Data.Plans.Count > (plan == null ? 0 : 1);
        HasPlan = plan is { Workouts.Count: > 0 };
        HasNoPlan = IsSignedIn && !HasPlan;
        CanChoosePlan = HasNoPlan && CanChangePlan;
        PlanProgress? progress = null;
        if (plan is { Workouts.Count: > 0 })
        {
            progress = new PlanProgress(plan, _store.History);
            _week = _chosenPlanId == plan.Id ? Math.Min(_chosenWeek, progress.LastUnlockedWeek) : progress.CurrentWeek;
            PlanName = plan.Name;
            WeekText = $"Week {_week} ▾";
            BuildDays(plan, progress);
        }
        else
            Days = [];

        UpdatePill();

        // The card: the workout in progress first, the plan's next one otherwise.
        if (HasOwnWorkout)
            ShowRunning("IN PROGRESS", _workouts.Active!.Name, _workouts.Active);
        else if (PhoneOwnsWorkout)
            ShowRunning("ON YOUR PHONE", _phone.Workout.Name, null);
        else if (plan != null && progress != null)
            ShowUpNext(plan, progress);
        else
            HasCard = false;
    }

    /// <summary>Up next: the plan's next workout (in the shown week, or the week after once it's done), and how recovered its muscles are.</summary>
    void ShowUpNext(WorkoutPlan plan, PlanProgress progress)
    {
        var (week, next) = progress.NextWorkout(_week) is { } inWeek
            ? (_week, inWeek)
            : (_week + 1, progress.NextWorkout(_week + 1) ?? progress.Days.OfType<PlanWorkout>().First());
        var now = DateTime.Now;
        var readiness = Math.Clamp(_recovery.Readiness(next, now), 0, 1);
        var tired = _recovery.NotReady(next, now);
        var exercises = next.Exercises.Select(e => _store.GetExercise(e.ExerciseId)).OfType<Exercise>().ToList();
        HasCard = true;
        CardLabel = week == _week ? "UP NEXT" : $"UP NEXT · WEEK {week}";
        CardColor = readiness < 0.6 ? Amber : readiness < 0.85 ? Blue : Green;
        CardName = next.Name;
        CardMuscles = string.Join(" · ", exercises.Select(e => e.PrimaryMuscle).Distinct().Take(3).Select(m => m.Display()));
        CardStatus = tired.Count == 0
            ? $"{readiness:P0} recovered · ready"
            : $"{readiness:P0} · {string.Join(", ", tired.Take(2).Select(t => t.Muscle.Display()))} recovering";
        CardMeta = $"{exercises.Count} exercises · {next.Exercises.Sum(e => e.Sets)} sets · {WorkoutEstimator.Format(_estimator.Minutes(next, plan.Goal))}";
        CardButton = "▶  Start";
        _cardAction = () => StartAsync(plan, next, week);
    }

    /// <summary>A workout in progress in the card's place: how far along it is, and Resume.</summary>
    void ShowRunning(string label, string name, WorkoutSession? session)
    {
        HasCard = true;
        CardLabel = label;
        CardColor = Green;
        CardName = name;
        if (session != null)
        {
            var sets = session.Exercises.SelectMany(e => e.Sets).Where(s => !s.IsWarmup).ToList();
            CardMuscles = string.Join(" · ", session.Exercises.Select(e => _store.GetExercise(e.ExerciseId)?.PrimaryMuscle).OfType<MuscleGroup>()
                .Distinct().Take(3).Select(m => m.Display()));
            CardStatus = $"{sets.Count(s => s.IsCompleted)}/{sets.Count} sets done";
            CardMeta = $"Started {session.StartedAt:HH:mm} · {(int)(DateTime.Now - session.StartedAt).TotalMinutes} min in";
            _cardAction = ResumeWatchWorkout;
        }
        else
        {
            var sets = _phone.Workout.Exercises.SelectMany(e => e.Sets).Where(s => !s.IsWarmup).ToList();
            CardMuscles = "";
            CardStatus = $"{sets.Count(s => s.IsCompleted)}/{sets.Count} sets done";
            CardMeta = $"Started {_phone.Workout.StartedAt.ToLocalTime():HH:mm}";
            _cardAction = OpenPhoneWorkout;
        }
        CardButton = "▶  Resume";
    }

    [RelayCommand]
    Task OpenCard() => _cardAction?.Invoke() ?? Task.CompletedTask;

    /// <summary>The pill: the workout in progress and its running time, ticking while the home is on screen.</summary>
    void UpdatePill()
    {
        DateTime? started = HasOwnWorkout ? _workouts.Active!.StartedAt : PhoneOwnsWorkout ? _phone.Workout.StartedAt.LocalDateTime : null;
        HasPill = started != null;
        PillName = HasOwnWorkout ? _workouts.Active!.Name : _phone.Workout.Name;
        if (started is { } at)
        {
            var t = DateTime.Now - at;
            PillTime = t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{(int)t.TotalMinutes}:{t.Seconds:00}";
        }
    }

    /// <summary>While the home is on screen: the pill's clock.</summary>
    public void StartTicking()
    {
        _pillTimer ??= Application.Current!.Dispatcher.CreateTimer();
        _pillTimer.Interval = TimeSpan.FromSeconds(1);
        _pillTimer.Tick -= OnPillTick;
        _pillTimer.Tick += OnPillTick;
        _pillTimer.Start();
    }

    public void StopTicking() => _pillTimer?.Stop();

    void OnPillTick(object? sender, EventArgs e) => UpdatePill();

    /// <summary>The pill tapped: back into the workout, the watch's own or the phone's.</summary>
    [RelayCommand]
    Task OpenRunning() => HasOwnWorkout ? ResumeWatchWorkout() : PhoneOwnsWorkout ? OpenPhoneWorkout() : Task.CompletedTask;

    [RelayCommand]
    async Task OpenSettings() => await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

    /// <summary>The shown week's days, as on the phone: done, running, up next, or rest.</summary>
    void BuildDays(WorkoutPlan plan, PlanProgress progress)
    {
        var week = _week;
        var next = progress.NextWorkout(week);
        var active = _workouts.Active;
        var running = HasOwnWorkout && active?.PlanId == plan.Id && (active.PlanWeek ?? week) == week ? active.PlanWorkoutId : null;
        Days = progress.Days.Select((workout, day) =>
        {
            if (workout == null)
            {
                var restDone = progress.IsRestDone(day, week);
                return new WatchDayItem(restDone ? "✓" : "–", restDone ? Green : Grey, "Rest", TextSecondary, restDone ? "Done" : "", Green,
                    new AsyncRelayCommand(() => ToggleRestAsync(plan, day, week, !restDone)));
            }
            var isRunning = workout.Id == running;
            var done = progress.SessionFor(workout, week) != null;
            var isNext = workout == next && !isRunning && running == null;
            var badge = isRunning ? "▶" : done ? "✓" : (plan.Workouts.IndexOf(workout) + 1).ToString();
            var badgeColor = isRunning || done ? Green : isNext ? Blue : Grey;
            var status = isRunning ? "In progress" : done ? "Done" : isNext ? "Up next" : "";
            var statusColor = isRunning || done ? Green : Blue;
            ICommand open = isRunning
                ? new AsyncRelayCommand(ResumeWatchWorkout)
                : new AsyncRelayCommand(() => StartAsync(plan, workout, week, confirm: true, again: done));
            return new WatchDayItem(badge, badgeColor, workout.Name, TextPrimary, status, statusColor, open);
        }).ToList();
    }

    /// <summary>"Week N ▾": any unlocked week, with how much of it is done.</summary>
    [RelayCommand]
    async Task ChooseWeek()
    {
        if (_store.ActivePlan is not { Workouts.Count: > 0 } plan)
            return;
        var progress = new PlanProgress(plan, _store.History);
        var labels = Enumerable.Range(1, progress.LastUnlockedWeek)
            .Select(w => $"Week {w} · {progress.WorkoutsDone(w)}/{plan.Workouts.Count}{(progress.IsComplete(w) ? " ✓" : "")}")
            .ToList();
        var pick = await Page.DisplayActionSheetAsync("Week", "Cancel", null, [.. labels]);
        var index = pick == null ? -1 : labels.IndexOf(pick);
        if (index < 0)
            return;
        _chosenPlanId = plan.Id;
        _chosenWeek = index + 1;
        Refresh();
    }

    /// <summary>The ⋯ beside the plan's name, as on the phone: switch to another plan, or look at another week.</summary>
    [RelayCommand]
    async Task PlanOptions()
    {
        if (_store.ActivePlan is not { } plan)
            return;
        const string change = "Change plan", week = "Choose week";
        string[] options = CanChangePlan ? [change, week] : [week];
        switch (await Page.DisplayActionSheetAsync(plan.Name, "Cancel", null, options))
        {
            case change:
                await ChangePlan();
                break;
            case week:
                await ChooseWeek();
                break;
        }
    }

    /// <summary>Switches the active plan, as the phone's plan ··· menu does. It syncs, so the phone follows.</summary>
    [RelayCommand]
    async Task ChangePlan()
    {
        var current = _store.ActivePlan;
        var plans = _store.Data.Plans.Where(p => p != current).ToList();
        if (plans.Count == 0)
            return;
        // Numbered so plans with the same name stay distinguishable.
        var labels = plans.Select((p, i) => $"{i + 1}. {p.Name}").ToList();
        var pick = await Page.DisplayActionSheetAsync(current == null ? "Choose a plan" : "Switch plan", "Cancel", null, [.. labels]);
        var index = pick == null ? -1 : labels.IndexOf(pick);
        if (index < 0)
            return;
        _store.Data.ActivePlanId = plans[index].Id;
        _chosenPlanId = null;
        _store.Save();
        Refresh();
    }

    async Task StartAsync(WorkoutPlan plan, PlanWorkout workout, int week, bool confirm = false, bool again = false)
    {
        if (_workouts.Active != null || PhoneOwnsWorkout)
        {
            Message = "Finish the workout in progress first.";
            return;
        }
        if (confirm && !await Page.DisplayAlertAsync(again ? $"Do {workout.Name} again?" : $"Start {workout.Name}?",
                again ? $"It's already done in week {week}." : $"Week {week} · {workout.Exercises.Count} exercises", "Start", "Cancel"))
            return;
        Message = "";
        WatchOwnership.SessionId = _workouts.StartFromPlan(plan, workout, week).Id;
        await ResumeWatchWorkout();
    }

    /// <summary>A rest day ticked off (or not), as on the phone, so the week can be complete.</summary>
    async Task ToggleRestAsync(WorkoutPlan plan, int day, int week, bool done)
    {
        if (!await Page.DisplayAlertAsync(done ? "Rest day done?" : "Undo rest day?", $"Week {week}", done ? "Mark done" : "Undo", "Cancel"))
            return;
        PlanProgress.SetRestDone(plan, day, week, done);
        _store.Save();
        Refresh();
    }

    /// <summary>An empty workout, built on the watch by adding exercises from the catalogue.</summary>
    [RelayCommand]
    async Task QuickWorkout()
    {
        if (_workouts.Active != null || PhoneOwnsWorkout)
            return;
        WatchOwnership.SessionId = _workouts.StartEmpty().Id;
        await ResumeWatchWorkout();
    }

    [RelayCommand]
    async Task OpenPhoneWorkout() => await Navigation.PushAsync(_services.GetRequiredService<CompanionPage>());

    [RelayCommand]
    async Task ResumeWatchWorkout() => await Navigation.PushAsync(_services.GetRequiredService<WorkoutPage>());

    [RelayCommand]
    async Task SignInWithPhone()
    {
        if (IsBusy)
            return;
        IsBusy = true;
        Message = "Check your phone…";
        try
        {
            await _account.SignInWithPhoneAsync();
            Message = "";
        }
        catch (InvalidOperationException e)
        {
            Message = e.Message;
        }
        catch (Exception e) when (e is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = e is ApiException ? e.Message : "Couldn't reach the server. Try again.";
        }
        catch (Exception)
        {
            Message = "Couldn't reach your phone. Try again, or sign in with email.";
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }

    [RelayCommand]
    async Task SignInWithEmail() => await Navigation.PushAsync(_services.GetRequiredService<SignInPage>());

    static Page Page => Application.Current!.Windows[0].Page!;

    static INavigation Navigation => Page.Navigation;
}
