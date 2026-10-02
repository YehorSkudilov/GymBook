using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The ··· sheet of the workout in progress: its name, when it started and how long it has run, finishing or
/// discarding it, and the logging settings. Every change is saved as it's made.
/// </summary>
public partial class WorkoutMenuViewModel(DataStore store, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    WorkoutViewModel? _workout;
    IDispatcherTimer? _timer;
    bool _loading;

    [ObservableProperty] string name = "";
    [ObservableProperty] string startText = "";
    [ObservableProperty] string durationText = "";
    [ObservableProperty] bool trackRir;
    /// <summary>False in a workout of a plan with RIR switched off: the switch would do nothing there.</summary>
    [ObservableProperty] bool canTrackRir = true;

    public void ApplyQueryAttributes(IDictionary<string, object> query) => _workout = query.TryGetValue("workout", out var w) ? w as WorkoutViewModel : null;

    public override async Task OnAppearingAsync()
    {
        if (_workout?.StartedAt is not { } started)
        {
            await GoBack();
            return;
        }
        _loading = true;
        Name = _workout.Name;
        ShowStart(started);
        TrackRir = store.Profile.TrackRir;
        // Only a plan with its own training settings and RIR off rules it out; one on the defaults follows this switch.
        CanTrackRir = store.GetPlan(store.Data.ActiveSession?.PlanId) is not { OwnTraining: true, UseRir: false };
        _loading = false;
        Tick();

        _timer ??= Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    void ShowStart(DateTime started) =>
        StartText = started.Date == DateTime.Today ? $"Today, {started:t}" : $"{started:ddd d MMM}, {started:t}";

    public override void OnDisappearing() => _timer?.Stop();

    /// <summary>Tap Start: when the workout really began. Every set and rest logged in it moves along.</summary>
    [RelayCommand]
    async Task EditStart()
    {
        if (_workout?.StartedAt is not { } started
            || await dialogs.DateAndTime("Start time", "Every set and rest logged moves with it.", started) is not { } value)
            return;
        if (_workout.ChangeStart(value) is { } error)
        {
            await dialogs.Alert("Can't start then", error);
            return;
        }
        ShowStart(value);
        Tick();
    }

    void OnTick(object? sender, EventArgs e) => Tick();

    void Tick()
    {
        if (_workout?.StartedAt is { } started)
            DurationText = Units.Clock(DateTime.Now - started);
    }

    // The default for plans' RIR too: plans following the defaults follow it.
    partial void OnTrackRirChanged(bool value) => UpdateProfile(p =>
    {
        p.TrackRir = value;
        Services.PlanTraining.ApplyToPlans(store);
    });


    void UpdateProfile(Action<Models.UserProfile> change)
    {
        if (_loading || _workout == null)
            return;
        change(store.Profile);
        store.Save();
        _workout.RefreshSettings();
    }

    [RelayCommand]
    async Task Rename()
    {
        var value = await dialogs.Prompt("Rename workout", "Workout name", Name);
        if (string.IsNullOrWhiteSpace(value) || _workout == null)
            return;
        _workout.SetName(value);
        Name = _workout.Name;
    }

    [RelayCommand]
    Task Close() => GoBack();

    /// <summary>Closes the sheet, then finishes the workout as the Finish button does.</summary>
    [RelayCommand]
    async Task Finish()
    {
        var workout = _workout;
        await GoBack();
        if (workout != null)
            await workout.FinishCommand.ExecuteAsync(null);
    }

    /// <summary>Closes the sheet, then starts the workout over (after its own confirmation).</summary>
    [RelayCommand]
    async Task Reset()
    {
        var workout = _workout;
        await GoBack();
        if (workout != null)
            await workout.ResetCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    async Task Discard()
    {
        var workout = _workout;
        await GoBack();
        if (workout != null)
            await workout.DiscardCommand.ExecuteAsync(null);
    }
}
