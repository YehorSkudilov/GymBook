using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The level sheet: the user's level and league, the leagues to come, where their big lifts stand against famous
/// lifters' competition bests, the XP they earned and lost lately, and how XP works.
/// </summary>
public partial class LevelViewModel(DataStore store, LevelService levels, Units units) : BaseViewModel
{
    [ObservableProperty] string levelText = "";
    [ObservableProperty] string leagueText = "";
    [ObservableProperty] double progress;
    [ObservableProperty] string progressText = "";
    [ObservableProperty] string xpText = "";
    public List<LeagueItem> Leagues { get; private set; } = [];
    public List<FamousLiftItem> Lifts { get; private set; } = [];
    public List<XpItem> Recent { get; private set; } = [];
    public bool HasRecent => Recent.Count > 0;

    public string HowItWorks =>
        $"Finish a workout: +{Levels.WorkoutXp} XP, and +{Levels.SetXp} for each working set.\n" +
        $"Do every workout your plan has in a week: +{Levels.WeekGoalXp} XP.\n" +
        $"Beat your best estimated 1RM on an exercise: +{Levels.RecordXp} XP. More reps than ever at a weight: +{Levels.RepRecordXp} XP.\n" +
        $"Miss a planned workout in a week: {Levels.MissedXp} XP each (not while on a break of over two weeks).\n" +
        $"Go up in weight but fall short of the minimum reps: {Levels.EgoLiftXp} XP, and no record. Earn the weight first.\n" +
        "Lighter weeks, deloads and easier days never cost XP.";

    public override Task OnAppearingAsync()
    {
        var status = levels.Status;
        LevelText = $"Level {status.Level}";
        LeagueText = $"{status.League.Name} league";
        Progress = status.Progress;
        ProgressText = $"{status.IntoLevel:N0} / {status.LevelSize:N0} XP · {status.ToNext:N0} to level {status.Level + 1}";
        XpText = $"{status.Xp:N0} XP in all";

        Leagues = [.. Models.Levels.Leagues.Select((l, i) =>
        {
            var to = i + 1 < Models.Levels.Leagues.Length ? Models.Levels.Leagues[i + 1].FromLevel - 1 : (int?)null;
            return new LeagueItem(l.Name, to is { } last ? $"Levels {l.FromLevel}–{last}" : $"Level {l.FromLevel} and up", l == status.League);
        })];
        Lifts = [.. new[] { RankLift.Bench, RankLift.Squat, RankLift.Deadlift, RankLift.Ohp }.Select(LiftItem)];
        Recent = [.. levels.Events.Reverse().Take(20).Select(e => new XpItem(
            e.At.ToString("ddd d MMM"), LevelService.Describe(e, Name), LevelService.Signed(e.Amount), e.Amount >= 0))];

        OnPropertyChanged(nameof(Leagues));
        OnPropertyChanged(nameof(Lifts));
        OnPropertyChanged(nameof(Recent));
        OnPropertyChanged(nameof(HasRecent));
        return Task.CompletedTask;
    }

    string Name(string exerciseId) => store.GetExercise(exerciseId)?.Name ?? "an exercise";

    FamousLiftItem LiftItem(RankLift lift)
    {
        var best = levels.BestE1Rm(lift);
        var title = char.ToUpper(FamousLifts.LiftName(lift)[0]) + FamousLifts.LiftName(lift)[1..];
        if (best <= 0)
            return new(title, "Log a set of it to see how you compare.", "", "", null);
        var (passed, next) = FamousLifts.Compare(lift, best);
        return new(title, $"Your best: ≈{units.FormatWithUnit(best)} (estimated 1RM)",
            passed is { } p ? $"Past {Famous.Describe(p, units)}" : "",
            next is { } n ? $"Next: {Famous.Describe(n, units)} · you're at {best / n.Kg:P0}" : "Past every famous lift here. Legendary.",
            next ?? passed);
    }

    [RelayCommand]
    Task Close() => GoBack();
}

/// <summary>Famous lifts in words, for the level sheet and the workout.</summary>
public static class Famous
{
    /// <summary>"Arnold Schwarzenegger's 165 kg bench press (1966)".</summary>
    public static string Describe(FamousLift f, Units units) =>
        $"{f.Person}'s {units.FormatWithUnit(f.Kg)} {(f.Movement ?? FamousLifts.LiftName(f.Lift)).ToLowerInvariant()} ({f.Date.Year})";

    /// <summary>Who they are and where the lift was, for the line under it.</summary>
    public static string Detail(FamousLift f, Units units) =>
        $"{f.Known} · {f.Event}{(f.BodyweightKg is { } bw ? $" · body weight {units.FormatWithUnit(bw)}" : "")}";
}

public record LeagueItem(string Name, string Levels, bool IsCurrent)
{
    public Color Background => IsCurrent ? Color.FromArgb("#263F7DFF") : Colors.Transparent;
    public Color TextColor => IsCurrent ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#9AA3B5");
}

public record FamousLiftItem(string Title, string Best, string Passed, string Next, FamousLift? Source)
{
    public bool HasPassed => Passed.Length > 0;
    public bool HasNext => Next.Length > 0;
    public bool HasSource => Source != null;
    public string SourceText => Source is { } s ? $"Source: {new Uri(s.Source).Host}" : "";

    /// <summary>The meet results (or the report) the famous lift is from.</summary>
    public IAsyncRelayCommand OpenSourceCommand => new AsyncRelayCommand(() => Source == null ? Task.CompletedTask : Launcher.OpenAsync(Source.Source));
}

public record XpItem(string Date, string Text, string Amount, bool IsGain)
{
    public Color AmountColor => IsGain ? Color.FromArgb("#2ED47A") : Color.FromArgb("#FF4D5E");
}
