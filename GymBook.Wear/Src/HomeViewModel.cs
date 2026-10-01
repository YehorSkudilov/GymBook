using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.Wear;

/// <summary>A plan workout to start from the watch.</summary>
public record PlanWorkoutItem(string Name, string Detail, ICommand StartCommand);

/// <summary>
/// The watch's home. Two modes, one screen: a workout running on the phone opens the companion page (the phone owns
/// it); otherwise the watch works on its own: resume its workout, or start one of the active plan's (the plan's next
/// one first), stored on the watch and synced to the account whenever there's a connection. Plans arrive by syncing,
/// so starting from the watch needs it signed in; the companion mode doesn't.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    readonly IServiceProvider _services;
    readonly DataStore _store;
    readonly WorkoutService _workouts;
    readonly SyncService _sync;
    readonly WatchAccount _account;
    readonly PhoneLink _phone;
    bool _opened;
    bool _askedNotifications;

    public HomeViewModel(IServiceProvider services, DataStore store, WorkoutService workouts, SyncService sync, WatchAccount account, PhoneLink phone)
    {
        (_services, _store, _workouts, _sync, _account, _phone) = (services, store, workouts, sync, account, phone);
        _phone.WorkoutChanged += _ => OnPhoneWorkoutChanged();
        _sync.StatusChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        _store.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
    }

    [ObservableProperty] bool isSignedIn;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string message = "";
    [ObservableProperty] string syncStatus = "";
    [ObservableProperty] string account = "";

    [ObservableProperty] bool hasPhoneWorkout;
    [ObservableProperty] string phoneWorkoutName = "";
    [ObservableProperty] bool hasWatchWorkout;
    [ObservableProperty] string watchWorkoutName = "";

    [ObservableProperty] bool canStart;
    [ObservableProperty] bool canQuickStart;
    [ObservableProperty] string planName = "";
    [ObservableProperty] PlanWorkoutItem? nextWorkout;
    [ObservableProperty] IReadOnlyList<PlanWorkoutItem> otherWorkouts = [];
    [ObservableProperty] bool hasNoPlan;

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

    string? _lastPhoneSession;

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

        // The phone's workout, when it's running one; the watch's own otherwise (both can show: its own wins below).
        HasPhoneWorkout = PhoneOwnsWorkout;
        PhoneWorkoutName = _phone.Workout.Name;
        var active = _workouts.Active;
        HasWatchWorkout = HasOwnWorkout;
        WatchWorkoutName = active?.Name ?? "";

        // Starting one: signed in (plans come from the account), with nothing running on either.
        var plan = _store.ActivePlan;
        var idle = active == null && !HasPhoneWorkout;
        CanStart = IsSignedIn && plan is { Workouts.Count: > 0 } && idle;
        // A quick workout needs no plan, nor an account: made before signing in, it joins the account at sign-in.
        CanQuickStart = idle;
        HasNoPlan = IsSignedIn && plan is not { Workouts.Count: > 0 } && idle;
        PlanName = plan?.Name ?? "";
        if (plan is { Workouts.Count: > 0 })
        {
            var next = Math.Clamp(plan.NextWorkoutIndex, 0, plan.Workouts.Count - 1);
            NextWorkout = Item(plan, plan.Workouts[next]);
            OtherWorkouts = plan.Workouts.Where((_, i) => i != next).Select(w => Item(plan, w)).ToList();
        }
        else
        {
            NextWorkout = null;
            OtherWorkouts = [];
        }
    }

    PlanWorkoutItem Item(WorkoutPlan plan, PlanWorkout workout) =>
        new(workout.Name, $"{workout.Exercises.Count} exercises", new AsyncRelayCommand(() => StartAsync(plan, workout)));

    async Task StartAsync(WorkoutPlan plan, PlanWorkout workout)
    {
        if (_workouts.Active != null || PhoneOwnsWorkout)
            return;
        WatchOwnership.SessionId = _workouts.StartFromPlan(plan, workout).Id;
        await ResumeWatchWorkout();
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

    [RelayCommand]
    void SyncNow() => _sync.Schedule(TimeSpan.Zero);

    [RelayCommand]
    async Task SignOut()
    {
        var page = Application.Current!.Windows[0].Page!;
        var warning = _store.Local.HasPendingChanges
            ? "Some workouts haven't synced yet and will be lost."
            : "Your workouts stay in your account.";
        if (!await page.DisplayAlertAsync("Sign out?", warning, "Sign out", "Cancel"))
            return;
        await _account.SignOutAsync();
        Refresh();
    }

    static INavigation Navigation => Application.Current!.Windows[0].Page!.Navigation;
}
