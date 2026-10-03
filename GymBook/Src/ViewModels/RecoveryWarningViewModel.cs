using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>What the user chose on the recovery warning.</summary>
public enum RecoveryChoice { NotNow, StartAnyway, Alternative }

/// <summary>
/// Shown before a planned workout whose muscles are still recovering: which ones and how far along they are, when they'll
/// be ready, what in the workout hits them, and a suggestion (a fresher workout from the plan, or resting / going lighter).
/// </summary>
public partial class RecoveryWarningViewModel(DataStore store, RecoveryService recovery) : BaseViewModel, IQueryAttributable
{
    TaskCompletionSource<RecoveryChoice>? _result;
    // Set while a button's choice closes the sheet, so closing doesn't count as "not now".
    bool _choosing;

    [ObservableProperty] string title = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] IDrawable map = MuscleMapDrawable.Empty;
    [ObservableProperty] List<TiredMuscleItem> muscles = [];
    [ObservableProperty] bool hasAlternative;
    [ObservableProperty] string alternativeName = "";
    [ObservableProperty] string alternativeDetail = "";
    [ObservableProperty] string alternativeText = "";
    [ObservableProperty] string advice = "";
    [ObservableProperty] string startAnywayText = "";
    // The battery in the header: filled and coloured by the most tired muscle's recovery.
    [ObservableProperty] string batteryGlyph = "battery_0_bar";
    [ObservableProperty] Color batteryColor = Colors.Transparent;
    [ObservableProperty] string levelText = "";

    // The time slider: hours from now, previewing how the muscles recover.
    [ObservableProperty] double hours;
    [ObservableProperty] double maxHours = 24;
    [ObservableProperty] string when = "Now";
    [ObservableProperty] bool isPreview;
    List<MuscleRecovery> _tired = [];
    List<Exercise> _exercises = [];

    /// <summary>Shows the warning and waits for the choice; the sheet has closed by the time it returns.</summary>
    public static Task<RecoveryChoice> ShowAsync(PlanWorkout workout, PlanWorkout? alternative, List<MuscleRecovery> tired)
    {
        var result = new TaskCompletionSource<RecoveryChoice>();
        _ = Shell.Current.GoToAsync(Routes.RecoveryWarning, new Dictionary<string, object>
        {
            ["workout"] = workout,
            ["alternative"] = (object?)alternative ?? "",
            ["tired"] = tired,
            ["result"] = result,
        });
        return result.Task;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_result != null)
            return;
        _result = query["result"] as TaskCompletionSource<RecoveryChoice>;
        var workout = (PlanWorkout)query["workout"];
        var alternative = query["alternative"] as PlanWorkout;
        var tired = (List<MuscleRecovery>)query["tired"];
        var now = DateTime.Now;

        Title = tired.Count == 1 ? $"{tired[0].Muscle.Display()} is still recovering" : $"{tired.Count} muscles are still recovering";
        var readyAt = tired.Max(t => t.ReadyAt);
        Summary = $"{workout.Name} works {(tired.Count == 1 ? "it" : "them")} hard" +
            (readyAt is { } at ? $", and they'll be fully recovered {ReadyIn(at)}." : ".") +
            " Training a muscle before it has recovered adds fatigue without adding much growth.";
        _tired = tired;
        _exercises = workout.Exercises.Select(pe => store.GetExercise(pe.ExerciseId)).OfType<Exercise>().ToList();
        // The slider runs from now to when the last of them is fully recovered.
        MaxHours = readyAt is { } last ? Math.Max(1, Math.Ceiling((last - now).TotalHours)) : 24;
        Update();

        HasAlternative = alternative != null;
        if (alternative != null)
        {
            var fresh = recovery.Compute(now);
            var worked = alternative.Exercises.Select(pe => store.GetExercise(pe.ExerciseId)).OfType<Exercise>()
                .Select(e => e.PrimaryMuscle).Distinct().ToList();
            AlternativeName = alternative.Name;
            AlternativeDetail = $"{alternative.Exercises.Count} exercises · {string.Join(", ", worked.Take(4).Select(m => m.Display().ToLowerInvariant()))}" +
                (worked.Count > 0 ? $" · {worked.Average(m => fresh[m]):P0} recovered" : "");
            AlternativeText = $"Do {alternative.Name} instead";
            Advice = $"Or rest today, or start {workout.Name} lighter: drop a set per exercise and keep a couple more reps in reserve.";
        }
        else
        {
            Advice = "Nothing else in your plan is fresher. Rest until then, or go lighter today: drop a set per exercise and keep a couple more reps in reserve.";
        }
        StartAnywayText = $"Start {workout.Name} anyway";
    }

    partial void OnHoursChanged(double value)
    {
        var snapped = Math.Round(value);
        if (Math.Abs(snapped - value) > 0.001)
        {
            Hours = snapped;
            return;
        }
        Update();
    }

    [RelayCommand]
    void Now() => Hours = 0;

    /// <summary>The map, each muscle and the battery at the moment the slider shows.</summary>
    void Update()
    {
        if (_tired.Count == 0)
            return;
        var at = DateTime.Now.AddHours(Hours);
        var rec = recovery.Compute(at);
        Map = MuscleMapDrawable.ForRecovery(rec, recovery.PartRecovery(at));
        When = RecoveryService.PreviewLabel(Hours);
        IsPreview = Hours != 0;

        var lowest = _tired.Min(t => rec[t.Muscle]);
        BatteryGlyph = BatteryFor(lowest);
        BatteryColor = RecoveryService.ColorFor(lowest);
        LevelText = $"RECOVERY {lowest:P0}";

        Muscles = [.. _tired.Select(t => new TiredMuscleItem
        {
            Name = t.Muscle.Display(),
            Percent = $"{rec[t.Muscle]:P0}",
            Progress = rec[t.Muscle],
            Color = RecoveryService.ColorFor(rec[t.Muscle]),
            Status = RecoveryService.StatusFor(rec[t.Muscle]),
            Detail = Detail(t, _exercises),
        })];
    }

    /// <summary>The Material battery icon for a level: 0 to 6 bars, full once fresh.</summary>
    static string BatteryFor(double recovery) =>
        recovery >= 0.95 ? "battery_full" : $"battery_{Math.Clamp((int)Math.Round(recovery * 6), 0, 6)}_bar";

    /// <summary>"Worked in Push A 1 day ago · ready tomorrow at 18:00 · Bench press, Dips".</summary>
    static string Detail(MuscleRecovery t, List<Exercise> exercises)
    {
        var parts = new List<string>();
        if (t.LimitingSession is { EndedAt: { } ended } session)
        {
            var ago = DateTime.Now - ended;
            parts.Add($"Worked in {session.Name} {(ago.TotalHours < 24 ? $"{Math.Max(1, Math.Round(ago.TotalHours)):0} h" : $"{(int)ago.TotalDays} d")} ago");
        }
        if (t.ReadyAt is { } at)
            parts.Add($"ready {ReadyIn(at)}");
        var hitting = exercises.Where(e => e.PrimaryMuscle == t.Muscle || e.SecondaryMuscles.Contains(t.Muscle)).Select(e => e.Name).Take(3).ToList();
        if (hitting.Count > 0)
            parts.Add(string.Join(", ", hitting));
        return string.Join(" · ", parts);
    }

    /// <summary>"in 5 h", "today at 18:00", "tomorrow at 18:00" or "on Tue at 09:00".</summary>
    static string ReadyIn(DateTime at)
    {
        var left = at - DateTime.Now;
        if (left < TimeSpan.FromHours(12))
            return $"in {Math.Max(1, Math.Round(left.TotalHours)):0} h";
        return at.Date == DateTime.Today ? $"today at {at:t}"
            : at.Date == DateTime.Today.AddDays(1) ? $"tomorrow at {at:t}"
            : $"on {at:ddd} at {at:t}";
    }

    [RelayCommand] Task Alternative() => Choose(RecoveryChoice.Alternative);
    [RelayCommand] Task StartAnyway() => Choose(RecoveryChoice.StartAnyway);
    [RelayCommand] Task NotNow() => Choose(RecoveryChoice.NotNow);

    // Closes the sheet first, so starting a workout doesn't collide with it sliding away.
    async Task Choose(RecoveryChoice choice)
    {
        if (_result == null || _result.Task.IsCompleted)
            return;
        var result = _result;
        _choosing = true;
        await GoBack();
        result.TrySetResult(choice);
    }

    // Closed some other way (the back button): don't train now.
    public override void OnDisappearing()
    {
        if (!_choosing)
            _result?.TrySetResult(RecoveryChoice.NotNow);
    }
}

public class TiredMuscleItem
{
    public required string Name { get; init; }
    public required string Percent { get; init; }
    public required double Progress { get; init; }
    public required Color Color { get; init; }
    public required string Status { get; init; }
    public required string Detail { get; init; }
}
