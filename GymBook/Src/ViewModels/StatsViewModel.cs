using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>The Progress tab: the stats from this device's workouts, and (its Ranks tab) the strength ranks.</summary>
public partial class StatsViewModel(DataStore store, StatsService stats, Units units, DialogService dialogs, NutritionService nutrition, RanksViewModel ranks)
    : BaseViewModel
{
    /// <summary>The Ranks tab's own view model (see RanksView).</summary>
    public RanksViewModel Ranks => ranks;

    /// <summary>
    /// Day | Trends | Ranks, at the top of the tab, the chosen one lit. Made afresh on each choice rather than recoloured
    /// in place, as Nutrition's Day | Trends is: recolouring didn't always redraw in this switch.
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<ChipItem> SectionTabs
    {
        get
        {
            if (_sectionTabs.Count == 0)
                ShowSectionTabs();
            return _sectionTabs;
        }
    }

    readonly System.Collections.ObjectModel.ObservableCollection<ChipItem> _sectionTabs = [];

    void ShowSectionTabs()
    {
        _sectionTabs.Clear();
        foreach (var (title, icon, section) in new[] { ("Day", "today", ProgressSection.Day), ("Trends", "trending_up", ProgressSection.Trends), ("Ranks", "leaderboard", ProgressSection.Ranks) })
            _sectionTabs.Add(new ChipItem(title, section, SelectSection) { Icon = icon, IsSelected = _section == section });
    }

    ProgressSection _section = ProgressSection.Day;

    /// <summary>One day's training (see <see cref="ShowDay"/>).</summary>
    public bool IsDayTab => _section == ProgressSection.Day;
    /// <summary>The trends: totals, charts, records over time.</summary>
    public bool IsStatsTab => _section == ProgressSection.Trends;
    public bool IsRanksTab => _section == ProgressSection.Ranks;

    void SelectSection(ChipItem chip)
    {
        _section = (ProgressSection)chip.Value!;
        OnPropertyChanged(nameof(IsDayTab));
        OnPropertyChanged(nameof(IsStatsTab));
        OnPropertyChanged(nameof(IsRanksTab));
        ShowSectionTabs();
        // The trends play in again when they come back; the ranks load from the server each time they're opened.
        _ = OnAppearingAsync();
    }

    // ---- Day: one day's training, picked like Nutrition's day ----

    DateTime _date = DateTime.Today;

    [ObservableProperty] string dayTitle = "";
    [ObservableProperty] string daySubtitle = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DayNextOpacity))] bool canGoNextDay;
    public double DayNextOpacity => CanGoNextDay ? 1 : 0.3;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasNoDayWorkouts))] bool hasDayWorkouts;
    public bool HasNoDayWorkouts => !HasDayWorkouts;
    [ObservableProperty] string dayEmptyText = "";
    [ObservableProperty] List<StatItem> dayTotals = [];
    [ObservableProperty] List<SessionItem> daySessions = [];
    [ObservableProperty] IDrawable dayMuscleMap = MuscleMapDrawable.Empty;
    [ObservableProperty] List<MuscleBarItem> dayMuscles = [];
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasDayRecords))] List<LineItem> dayRecords = [];
    public bool HasDayRecords => DayRecords.Count > 0;

    void ShowDay()
    {
        DayTitle = _date == DateTime.Today ? "Today" : _date == DateTime.Today.AddDays(-1) ? "Yesterday" : _date.ToString("dddd", CultureInfo.CurrentCulture);
        DaySubtitle = _date.ToString("d MMMM", CultureInfo.CurrentCulture);
        CanGoNextDay = _date < DateTime.Today;

        var sessions = store.History.Where(s => s.StartedAt.Date == _date).OrderBy(s => s.StartedAt).ToList();
        HasDayWorkouts = sessions.Count > 0;
        DayEmptyText = _date == DateTime.Today ? "No workout yet today." : "No workout this day: a rest day.";
        DayTotals =
        [
            new("Workouts", sessions.Count.ToString(CultureInfo.CurrentCulture)),
            new("Time", Units.Duration(TimeSpan.FromTicks(sessions.Sum(s => s.Duration.Ticks)))),
            new("Volume", units.FormatVolume(sessions.Sum(stats.SessionVolume))),
            new("Sets", sessions.Sum(s => s.WorkingSets.Count()).ToString(CultureInfo.CurrentCulture)),
        ];
        DaySessions = [.. sessions.Select(s => SessionItem.Create(s, store, stats, units))];

        // A day's sets per muscle: about 10 hard sets is a full session's worth for one muscle.
        var sets = stats.SetsPerPart(sessions);
        DayMuscleMap = MuscleMapDrawable.ForVolume(sets);
        DayMuscles = [.. PartTotals(sets).Where(s => s.Value > 0).OrderByDescending(s => s.Value).Select(s => new MuscleBarItem
        {
            Name = s.Name,
            Value = $"{s.Value:0.#} sets",
            Progress = Math.Clamp(s.Value / 10, 0, 1),
            Color = Accent,
        })];

        DayRecords = [.. sessions.SelectMany(stats.RecordsIn).Select(r => new LineItem
        {
            Title = r.Exercise.Name,
            Detail = $"{units.Format(r.WeightKg)} {units.Label} × {r.Reps}",
            Value = units.FormatWithUnit(r.E1RmKg),
            OpenCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Exercise}?id={r.Exercise.Id}")),
        })];
    }

    [RelayCommand]
    void PreviousDay()
    {
        _date = _date.AddDays(-1);
        ShowDay();
    }

    [RelayCommand]
    void NextDay()
    {
        if (_date >= DateTime.Today)
            return;
        _date = _date.AddDays(1);
        ShowDay();
    }

    /// <summary>A calendar with a dot on each day with a workout.</summary>
    [RelayCommand]
    async Task PickDay()
    {
        var days = store.History.Select(s => s.StartedAt.Date).ToHashSet();
        if (await dialogs.Calendar("Pick a day", _date, DateTime.Today, days.Contains) is not { } day || day.Date == _date)
            return;
        _date = day.Date;
        ShowDay();
    }

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

    // Health data (connected in the Profile tab): today's activity, and body composition.
    [ObservableProperty] bool hasActivity;
    [ObservableProperty] List<StatItem> activity = [];
    [ObservableProperty] string activityCaption = "";
    [ObservableProperty] List<StatItem> body = [];
    [ObservableProperty] string bodyCaption = "";
    [ObservableProperty] IDrawable? bodyFatChart;
    [ObservableProperty] bool hasBodyFatChart;

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

    /// <summary>Today's calories burned and steps from the health apps (all four always; N/A for what they haven't recorded).</summary>
    void ShowActivity(DayNutrition day)
    {
        var h = day.Health;
        ActivityCaption = h?.Source is { } source ? $"Today so far · {source}" : "Today · connect your health data in the Profile tab";
        static string Kcal(double? v) => v is { } k ? $"{NutritionService.Kcal(k)} kcal" : "N/A";
        Activity =
        [
            new("Total burned", Kcal(day.BurnSource == BurnSource.Measured ? day.BurnedKcal : null)),
            new("Active", Kcal(h?.ActiveBurnedKcal)),
            // Samsung Health's own total (read directly) comes without a resting figure: what isn't activity.
            new("Resting", Kcal(h?.BasalBurnedKcal ?? (h is { TotalBurnedKcal: { } total, ActiveBurnedKcal: { } active } ? total - active : null))),
            new("Steps", h?.Steps is { } steps ? steps.ToString("#,0", CultureInfo.CurrentCulture) : "N/A"),
        ];
        HasActivity = true;
    }

    void ShowBody()
    {
        // Only measurements read from the health apps (not weights logged by hand, nothing estimated); N/A for what
        // they haven't measured. Fat mass and BMI are worked out from measured values only.
        var entries = store.Data.BodyWeights.Where(b => b.Source != null).OrderBy(b => b.Date).ToList();
        var latest = entries.LastOrDefault();
        double? Latest(Func<BodyWeightEntry, double?> pick) => entries.Select(pick).LastOrDefault(v => v != null);
        var weight = latest?.WeightKg;
        var fat = Latest(b => b.BodyFatPercent);
        var height = store.Profile.HeightCm;
        string Mass(double? kg) => kg is { } v ? units.FormatWithUnit(v) : "N/A";

        Body =
        [
            new("Weight", Mass(weight)),
            new("Body fat", fat is { } f ? $"{f:0.#}%" : "N/A"),
            new("Fat mass", Mass(weight * fat / 100)),
            new("Lean mass", Mass(Latest(b => b.LeanMassKg))),
            new("Bone mass", Mass(Latest(b => b.BoneMassKg))),
            new("Body water", Mass(Latest(b => b.BodyWaterKg))),
            new("BMR", Latest(b => b.BmrKcal) is { } bmr ? $"{NutritionService.Kcal(bmr)} kcal" : "N/A"),
            new("Height", height is { } cm ? Height(cm) : "N/A"),
            new("BMI", weight is { } w && height is { } h ? $"{w / Math.Pow(h / 100, 2):0.0}" : "N/A"),
        ];

        BodyCaption = latest == null
            ? "No measurements from your health apps yet. Connect your health data in the Profile tab to bring in your scale's and watch's."
            : $"Last measured {latest.Date:d MMM} · {latest.Source}";

        var fats = entries.Where(b => b.BodyFatPercent != null).TakeLast(12).ToList();
        HasBodyFatChart = fats.Count >= 2;
        BodyFatChart = new LineChartDrawable(
            [.. fats.Select(b => new ChartPoint(b.Date.ToString("d MMM", CultureInfo.CurrentCulture), b.BodyFatPercent!.Value))],
            Violet, v => $"{v:0.#}%");
    }

    string Height(double cm)
    {
        if (units.Unit == WeightUnit.Kg)
            return $"{cm:0} cm";
        var inches = (int)Math.Round(cm / 2.54);
        return $"{inches / 12}′ {inches % 12}″";
    }


    public override void OnDisappearing() => IsShowing = false;
    public System.Collections.ObjectModel.ObservableCollection<ChipItem> LiftChips { get; } = [];
    string? _liftId;

    public override Task OnAppearingAsync()
    {
        IsShowing = false;
        // The trends are only hidden under the other tabs, so they stay off there: their icons would otherwise redraw
        // every frame unseen. They're worked out again when the Trends tab comes back.
        if (IsRanksTab)
        {
            _ = ranks.OnAppearingAsync();
            return Task.CompletedTask;
        }
        if (IsDayTab)
        {
            ShowDay();
            return Task.CompletedTask;
        }
        // Worked out again only when the data changed (or the day did) since last time: it goes through all the history.
        // Either way it plays in again.
        if (_trendsVersion != store.Version || _trendsDay != DateTime.Today)
        {
            _trendsVersion = store.Version;
            _trendsDay = DateTime.Today;
            ShowTrends();
        }
        // Last, so everything bound plays in (charts draw, numbers count, cards rise).
        IsShowing = true;
        return Task.CompletedTask;
    }

    int _trendsVersion = -1;
    DateTime _trendsDay;

    void ShowTrends()
    {
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

        var sets = stats.SetsPerPartSince(StatsService.WeekStart(DateTime.Today));
        MuscleMap = MuscleMapDrawable.ForVolume(sets);
        MuscleSets = PartTotals(sets).OrderByDescending(s => s.Value).Select(s => new MuscleBarItem
        {
            Name = s.Name,
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
        ShowActivity(nutrition.Day(DateTime.Today));
        ShowBody();
    }

    /// <summary>
    /// Sets for each worked part of each muscle (front, side and rear delts, …), and one line for each muscle with
    /// none at all, so the list doesn't fill with every untrained part.
    /// </summary>
    static IEnumerable<(string Name, double Value)> PartTotals(Dictionary<SubMuscle, double> parts) =>
        parts.GroupBy(p => p.Key.Group()).SelectMany(g => g.Sum(p => p.Value) > 0
            ? g.Where(p => p.Value > 0).Select(p => (p.Key.Display(), p.Value))
            : [(g.Key.Display(), 0.0)]);

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

/// <summary>Progress's tabs: one day's training, the trends over time, and the strength ranks.</summary>
public enum ProgressSection { Day, Trends, Ranks }
