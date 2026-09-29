using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public partial class StatsViewModel(DataStore store, StatsService stats, Units units, DialogService dialogs) : BaseViewModel
{
    static readonly Color Accent = Color.FromArgb("#3F7DFF");
    static readonly Color Violet = Color.FromArgb("#7C5CFF");
    static readonly Color Green = Color.FromArgb("#2ED47A");
    static readonly Color Orange = Color.FromArgb("#FF8A3D");

    [ObservableProperty] string totalWorkouts = "0";
    [ObservableProperty] string thisMonth = "0";
    [ObservableProperty] string totalVolume = "0";
    [ObservableProperty] string streak = "0";
    [ObservableProperty] IDrawable? volumeChart;
    [ObservableProperty] IDrawable? workoutsChart;
    [ObservableProperty] IDrawable? bodyWeightChart;
    [ObservableProperty] string bodyWeightText = "";
    [ObservableProperty] string bodyWeightChange = "";
    [ObservableProperty] IDrawable muscleMap = MuscleMapDrawable.Empty;
    [ObservableProperty] List<MuscleBarItem> muscleSets = [];
    [ObservableProperty] List<LineItem> records = [];
    [ObservableProperty] bool hasRecords;

    // Strength: the overall trend, one lift at a time, and the biggest recent gains.
    [ObservableProperty] IDrawable? strengthTrendChart;
    [ObservableProperty] string strengthTrendText = "";
    [ObservableProperty] bool hasStrengthTrend;
    [ObservableProperty] IDrawable? liftChart;
    [ObservableProperty] string liftSummary = "";
    [ObservableProperty] bool hasLifts;
    [ObservableProperty] List<LineItem> gains = [];
    [ObservableProperty] bool hasGains;

    /// <summary>The tab is on screen: icons animate, and charts, numbers and cards play in each time it comes into view.</summary>
    [ObservableProperty] bool isShowing;

    [ObservableProperty] IDrawable? consistencyChart;
    [ObservableProperty] string consistencyText = "";
    [ObservableProperty] IDrawable? durationChart;
    [ObservableProperty] string durationText = "";
    [ObservableProperty] bool hasDuration;
    [ObservableProperty] IDrawable? splitChart;
    [ObservableProperty] List<SplitItem> split = [];
    [ObservableProperty] bool hasSplit;

    const int ConsistencyWeeks = 16;

    // The training split: muscles grouped the way plans are.
    static readonly (string Name, Color Color, MuscleGroup[] Muscles)[] SplitGroups =
    [
        ("Push", Color.FromArgb("#3F7DFF"), [MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps]),
        ("Pull", Color.FromArgb("#7C5CFF"), [MuscleGroup.Back, MuscleGroup.Traps, MuscleGroup.Biceps, MuscleGroup.Forearms]),
        ("Legs", Color.FromArgb("#2ED47A"), [MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves]),
        ("Core", Color.FromArgb("#FF8A3D"), [MuscleGroup.Abs, MuscleGroup.LowerBack, MuscleGroup.Neck]),
    ];

    public override void OnDisappearing() => IsShowing = false;
    public System.Collections.ObjectModel.ObservableCollection<ChipItem> LiftChips { get; } = [];
    string? _liftId;

    public override Task OnAppearingAsync()
    {
        IsShowing = false;
        var history = store.History.ToList();
        TotalWorkouts = history.Count.ToString();
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ThisMonth = history.Count(s => s.StartedAt >= monthStart).ToString();
        TotalVolume = units.FormatVolume(history.Sum(stats.SessionVolume));
        Streak = stats.WeekStreak().ToString();

        VolumeChart = new BarChartDrawable(stats.WeeklyVolume(8).Select(p => p with { Value = units.ToDisplay(p.Value) }).ToList(), Accent,
            v => v >= 1000 ? $"{v / 1000:0.#}k" : $"{v:0}");
        WorkoutsChart = new BarChartDrawable(stats.WorkoutsPerWeek(8), Violet, v => $"{v:0}");

        var weights = store.Data.BodyWeights.OrderBy(b => b.Date).ToList();
        BodyWeightChart = new LineChartDrawable(
            weights.TakeLast(12).Select(b => new ChartPoint(b.Date.ToString("d MMM", CultureInfo.CurrentCulture), units.ToDisplay(b.WeightKg))).ToList(),
            Green, v => $"{v:0.#}");
        BodyWeightText = weights.Count > 0 ? units.FormatWithUnit(weights[^1].WeightKg) : units.FormatWithUnit(store.Profile.BodyWeightKg);
        if (weights.Count > 1)
        {
            var diff = units.ToDisplay(weights[^1].WeightKg) - units.ToDisplay(weights[0].WeightKg);
            BodyWeightChange = $"{(diff >= 0 ? "+" : "−")}{Math.Abs(diff):0.#} {units.Label} since {weights[0].Date:d MMM}";
        }
        else
        {
            BodyWeightChange = "Log your weight regularly to see the trend";
        }

        var sets = stats.SetsPerMuscleSince(StatsService.WeekStart(DateTime.Today));
        MuscleMap = MuscleMapDrawable.ForVolume(sets);
        MuscleSets = sets.OrderByDescending(s => s.Value).Select(s => new MuscleBarItem
        {
            Name = s.Key.Display(),
            Value = $"{s.Value:0.#} sets",
            Progress = Math.Clamp(s.Value / 20, 0, 1),
            Color = s.Value >= 10 ? Green : s.Value > 0 ? Accent : Color.FromArgb("#272C39"),
        }).ToList();

        Records = stats.PersonalRecords().Take(10).Select(r => new LineItem
        {
            Title = r.Exercise.Name,
            Detail = $"{units.Format(r.WeightKg)} {units.Label} × {r.Reps} · {r.Date:d MMM}",
            Value = units.FormatWithUnit(r.E1RmKg),
            OpenCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Exercise}?id={r.Exercise.Id}")),
        }).ToList();
        HasRecords = Records.Count > 0;
        ShowStrength();
        ShowHabits(history);
        // Last, so everything bound above plays in (charts draw, numbers count, cards rise).
        IsShowing = true;
        return Task.CompletedTask;
    }

    const int TrendWeeks = 12;

    /// <summary>How often and how long you train, and how the work is split across the body.</summary>
    void ShowHabits(List<WorkoutSession> history)
    {
        var (monday, days) = stats.DailyVolume(ConsistencyWeeks);
        ConsistencyChart = new HeatmapDrawable(monday, days, Accent);
        var trained = days.Count(d => d > 0);
        var weeks = Math.Max(1, days.Count / 7.0);
        ConsistencyText = trained == 0
            ? "Every workout lights up a day here."
            : $"{trained} training day{(trained == 1 ? "" : "s")} in {ConsistencyWeeks} weeks · {trained / weeks:0.#} a week";

        var minutes = stats.MinutesPerWeek(TrendWeeks);
        HasDuration = minutes.Count > 0;
        DurationChart = new LineChartDrawable(minutes, Violet, v => $"{v:0} min");
        DurationText = HasDuration ? $"About {minutes.Average(p => p.Value):0} minutes a workout lately" : "";

        var sets = stats.SetsPerMuscleSince(DateTime.Today.AddDays(-30));
        var slices = SplitGroups.Select(g => new DonutSlice(g.Name, g.Muscles.Sum(m => sets.GetValueOrDefault(m)), g.Color)).ToList();
        var total = slices.Sum(s => s.Value);
        HasSplit = total > 0;
        SplitChart = new DonutDrawable(slices.OrderByDescending(s => s.Value).ToList(), $"{total:0}", "sets");
        Split = [.. slices.Select(s => new SplitItem(s.Label, s.Color, total > 0 ? s.Value / total : 0, $"{s.Value:0} sets"))];
    }

    void ShowStrength()
    {
        var lifts = stats.TrackedLifts(8);
        HasLifts = lifts.Count > 0;

        var trend = stats.StrengthTrend(lifts.Take(5).ToList(), TrendWeeks);
        HasStrengthTrend = trend.Count >= 2;
        StrengthTrendChart = new LineChartDrawable(trend, Orange, v => $"{v - 100:+0;−0;0}%");
        var change = trend.Count >= 2 ? trend[^1].Value - trend[0].Value : 0;
        StrengthTrendText = !HasStrengthTrend
            ? "Train your main lifts for a couple of weeks to see the trend."
            : $"{(change >= 0 ? "Up" : "Down")} {Math.Abs(change):0.#}% across your top {Math.Min(5, lifts.Count)} lifts since {trend[0].Label}";

        LiftChips.Clear();
        if (_liftId == null || lifts.All(l => l.Id != _liftId))
            _liftId = lifts.FirstOrDefault()?.Id;
        foreach (var lift in lifts)
            LiftChips.Add(new ChipItem(lift.Name, lift.Id, SelectLift) { IsSelected = lift.Id == _liftId });
        ShowLift();

        Gains = [.. stats.Gains(90, 5).Select(g => new LineItem
        {
            Title = g.Exercise.Name,
            Detail = $"{units.FormatWithUnit(g.FromKg)} → {units.FormatWithUnit(g.ToKg)} · {g.Sessions} sessions",
            Value = $"{g.Percent:+0;−0;0}%",
            OpenCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Exercise}?id={g.Exercise.Id}")),
        })];
        HasGains = Gains.Count > 0;
    }

    void SelectLift(ChipItem chip)
    {
        _liftId = chip.Value as string;
        foreach (var c in LiftChips)
            c.IsSelected = c == chip;
        ShowLift();
    }

    /// <summary>The chosen lift's estimated 1RM per session, with the gain since its first one and its best.</summary>
    void ShowLift()
    {
        if (_liftId == null)
        {
            LiftChart = null;
            LiftSummary = "";
            return;
        }
        var history = stats.E1RmHistory(_liftId);
        LiftChart = new LineChartDrawable(history.TakeLast(20).Select(p => p with { Value = units.ToDisplay(p.Value) }).ToList(), Accent, v => $"{v:0.#}");
        if (history.Count == 0)
        {
            LiftSummary = "";
            return;
        }
        var first = history[0].Value;
        var latest = history[^1].Value;
        var best = history.Max(p => p.Value);
        var gain = history.Count > 1
            ? $" · {(latest >= first ? "+" : "−")}{units.FormatWithUnit(Math.Abs(latest - first))} ({(latest / first - 1) * 100:+0;−0;0}%) since {history[0].Label}"
            : "";
        LiftSummary = $"Estimated 1RM {units.FormatWithUnit(latest)}{gain} · best {units.FormatWithUnit(best)}";
    }

    [RelayCommand]
    async Task LogWeight()
    {
        var value = await dialogs.Prompt("Log body weight", $"Today's weight in {units.Label}", units.Format(store.Profile.BodyWeightKg), Keyboard.Numeric);
        if (value == null)
            return;
        if (!units.TryParse(value, out var kg) || kg <= 0)
        {
            await dialogs.Alert("Invalid weight", "Please enter a number.");
            return;
        }
        store.Data.BodyWeights.RemoveAll(b => b.Date == DateTime.Today);
        store.Data.BodyWeights.Add(new BodyWeightEntry { Date = DateTime.Today, WeightKg = kg });
        store.Profile.BodyWeightKg = kg;
        store.Save();
        await OnAppearingAsync();
    }

    [RelayCommand]
    Task OpenHistory() => GoTo(Routes.History);
}
