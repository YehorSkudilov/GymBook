using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The celebration right after a workout is finished: what was done, counted up as the screen comes in, and any new
/// personal bests. Continue opens the finished workout itself.
/// </summary>
public partial class WorkoutDoneViewModel(DataStore store, StatsService stats, Units units) : BaseViewModel, IQueryAttributable
{
    string? _sessionId;
    TimeSpan _duration;
    int _sets, _exercises;
    double _volume;

    [ObservableProperty] string title = "Workout complete!";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] int records;
    [ObservableProperty] string recordsText = "";
    public bool HasRecords => Records > 0;

    /// <summary>0 to 1 as the numbers count up (driven by the page's animation).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText), nameof(SetsText), nameof(VolumeText), nameof(ExercisesText))]
    double reveal;

    public string DurationText => Units.Duration(_duration * Reveal);
    public string SetsText => $"{Math.Round(_sets * Reveal)}";
    public string VolumeText => units.FormatVolume(_volume * Reveal);
    public string ExercisesText => $"{Math.Round(_exercises * Reveal)}";

    partial void OnRecordsChanged(int value) => OnPropertyChanged(nameof(HasRecords));

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _sessionId = query.TryGetValue("session", out var id) ? id?.ToString() : null;

    public override Task OnAppearingAsync()
    {
        if (store.History.FirstOrDefault(s => s.Id == _sessionId) is not { } session)
            return Task.CompletedTask;
        _duration = session.Duration;
        _sets = session.WorkingSets.Count();
        _volume = stats.SessionVolume(session);
        _exercises = session.Exercises.Count(e => e.Sets.Any(s => s.IsCompleted && !s.IsWarmup));
        var name = store.Profile.Name.Trim();
        Title = Pick(name.Length > 0 ? [$"Great work, {name}!", $"Nailed it, {name}!", $"That's a wrap, {name}!"] : ["Great work!", "Nailed it!", "Workout complete!"]);
        Subtitle = $"{session.Name} is done. Rest up and recover.";
        Records = PersonalRecords(session);
        RecordsText = Records == 1 ? "New personal best" : $"{Records} new personal bests";
        OnPropertyChanged(nameof(DurationText));
        OnPropertyChanged(nameof(SetsText));
        OnPropertyChanged(nameof(VolumeText));
        OnPropertyChanged(nameof(ExercisesText));
        return Task.CompletedTask;
    }

    static string Pick(string[] options) => options[Random.Shared.Next(options.Length)];

    /// <summary>Exercises whose best estimated 1RM today beats every earlier workout's (only ones done before).</summary>
    int PersonalRecords(WorkoutSession session)
    {
        var earlier = store.History.Where(s => s.Id != session.Id && s.StartedAt < session.StartedAt).ToList();
        var count = 0;
        foreach (var e in session.Exercises)
        {
            var best = Best(e.Sets);
            var before = earlier.SelectMany(s => s.Exercises).Where(x => x.ExerciseId == e.ExerciseId).Select(x => Best(x.Sets)).DefaultIfEmpty(-1).Max();
            if (before > 0 && best > before + 0.01)
                count++;
        }
        return count;
    }

    static double Best(IEnumerable<SetEntry> sets) =>
        sets.Where(s => s.IsCompleted && !s.IsWarmup).Select(s => ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir)).DefaultIfEmpty(0).Max();

    /// <summary>On to the finished workout: what was done, how it compares, and the fatigue it left.</summary>
    [RelayCommand]
    Task Continue() => _sessionId == null ? GoBack() : GoTo($"../{Routes.PlanDay}?session={_sessionId}&finished=1");
}
