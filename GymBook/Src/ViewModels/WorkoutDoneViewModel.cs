using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The celebration right after a workout is finished: what was done, counted up as the screen comes in, and any new
/// personal bests. Continue opens the finished workout itself.
/// </summary>
public partial class WorkoutDoneViewModel(DataStore store, StatsService stats, Units units, GymBook.Services.Sync.AccountService account,
    LevelService levels) : BaseViewModel, IQueryAttributable
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

    // ---- XP: what the workout earned (or cost), and the level bar filling from where it was ----
    [ObservableProperty] string xpText = "";
    [ObservableProperty] string xpLines = "";
    [ObservableProperty] string levelText = "";
    /// <summary>"Level up! You're level 13" (or into a new league), empty otherwise.</summary>
    [ObservableProperty] string levelUpText = "";
    public bool HasLevelUp => LevelUpText.Length > 0;
    /// <summary>The level bar, animated by the page from <see cref="LevelProgressFrom"/> to <see cref="LevelProgressTo"/>.</summary>
    [ObservableProperty] double levelProgress;
    public double LevelProgressFrom { get; private set; }
    public double LevelProgressTo { get; private set; }

    partial void OnLevelUpTextChanged(string value) => OnPropertyChanged(nameof(HasLevelUp));

    void ShowXp(WorkoutSession session)
    {
        var earned = levels.EarnedBy(session);
        var (before, after) = (levels.StatusBefore(session), levels.Status);
        XpText = LevelService.Signed(earned.Sum(e => e.Amount));
        XpLines = string.Join("\n", earned.Select(e => $"{LevelService.Signed(e.Amount)}  {LevelService.Describe(e, id => store.GetExercise(id)?.Name ?? "an exercise")}"));
        LevelText = $"Level {after.Level} · {after.League.Name} league · {after.ToNext:N0} XP to go";
        LevelUpText = after.League != before.League && after.Level > before.Level ? $"Welcome to the {after.League.Name} league!"
            : after.Level > before.Level ? $"Level up! You're level {after.Level}"
            : "";
        LevelProgressFrom = after.Level > before.Level ? 0 : before.Progress;
        LevelProgressTo = after.Progress;
        LevelProgress = LevelProgressFrom;
    }

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
        var name = account.Username ?? "";
        Title = Pick(name.Length > 0 ? [$"Great work, {name}!", $"Nailed it, {name}!", $"That's a wrap, {name}!"] : ["Great work!", "Nailed it!", "Workout complete!"]);
        Subtitle = $"{session.Name} is done. Rest up and recover.";
        Records = PersonalRecords(session);
        RecordsText = Records == 1 ? "New personal best" : $"{Records} new personal bests";
        ShowXp(session);
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

    /// <summary>The level sheet: leagues, famous lifts and every XP earned.</summary>
    [RelayCommand]
    Task OpenLevel() => GoTo(Routes.Level);

    /// <summary>On to the finished workout: what was done, how it compares, and the fatigue it left.</summary>
    [RelayCommand]
    Task Continue() => _sessionId == null ? GoBack() : GoTo($"../{Routes.PlanDay}?session={_sessionId}&finished=1");
}
