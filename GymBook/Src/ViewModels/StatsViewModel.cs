using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>The Progress tab: the stats from this device's workouts, and (its Ranks tab) the strength ranks.</summary>
public partial class StatsViewModel(DataStore store, StatsService stats, Units units, DialogService dialogs, NutritionService nutrition, RanksViewModel ranks,
    RecoveryService recovery) : BaseViewModel
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

    // The day's own take on Trends' cards: its lifts against the time before, its split, recovery going into it, the
    // night's sleep, its activity and the body as last measured by then.
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasDayLifts))] List<LineItem> dayLifts = [];
    public bool HasDayLifts => DayLifts.Count > 0;
    [ObservableProperty] IDrawable? daySplitChart;
    [ObservableProperty] List<SplitItem> daySplit = [];
    [ObservableProperty] bool hasDaySplit;
    [ObservableProperty] string dayRecoveryTitle = "";
    [ObservableProperty] string dayRecoveryValue = "";
    [ObservableProperty] Color dayRecoveryColor = Green;
    [ObservableProperty] IDrawable dayRecoveryMap = MuscleMapDrawable.Empty;
    [ObservableProperty] RecoveryOverview dayRecoveryOverview = RecoveryOverview.Empty;
    [ObservableProperty] string dayRecoverySleep = "";
    [ObservableProperty] string daySleepValue = "";
    [ObservableProperty] string daySleepCaption = "";
    [ObservableProperty] List<StatItem> dayActivity = [];
    [ObservableProperty] string dayActivityCaption = "";
    [ObservableProperty] List<StatItem> dayBody = [];
    [ObservableProperty] string dayBodyCaption = "";

    void ShowDay()
    {
        DayTitle = _date == DateTime.Today ? "Today" : _date == DateTime.Today.AddDays(-1) ? "Yesterday" : _date.ToString("dddd", CultureInfo.CurrentCulture);
        DaySubtitle = _date.ToString("d MMMM", CultureInfo.CurrentCulture);
        CanGoNextDay = _date < DateTime.Today;

        var sessions = store.History.Where(s => s.StartedAt.Date == _date).OrderBy(s => s.StartedAt).ToList();
        HasDayWorkouts = sessions.Count > 0;
        DayEmptyText = _date == DateTime.Today ? "No workout yet today." : "No workout this day: a rest day.";
        // The week so far against the target, as Trends has the week streak.
        var week = StatsService.WeekStart(_date);
        var thisWeek = stats.SessionsIn(week, _date).Count;
        DayTotals =
        [
            new("Workouts", sessions.Count.ToString(CultureInfo.CurrentCulture)),
            new("Time", Units.Duration(TimeSpan.FromTicks(sessions.Sum(s => s.Duration.Ticks)))),
            new("Volume", units.FormatVolume(sessions.Sum(stats.SessionVolume))),
            new("Sets", sessions.Sum(s => s.WorkingSets.Count()).ToString(CultureInfo.CurrentCulture)),
            new("Records", sessions.Sum(s => stats.RecordsIn(s).Count).ToString(CultureInfo.CurrentCulture)),
            new("This week", $"{thisWeek} of {stats.WeeklyTarget}"),
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

        ShowDayLifts(sessions);
        (DaySplitChart, DaySplit, HasDaySplit) = SplitFor(sessions);
        ShowDayRecovery();
        ShowDaySleep();
        var day = nutrition.Day(_date);
        DayActivity = ActivityItems(day);
        DayActivityCaption = day.Health?.Source is { } source
            ? $"{(day.IsToday ? "Today so far" : _date.ToString("d MMM", CultureInfo.CurrentCulture))} · {source}"
            : "No health data for this day · connect your health data in the Profile tab";
        (DayBody, DayBodyCaption) = BodyAsOf(_date);
    }

    /// <summary>
    /// Each loaded lift of the day: its best set and estimated 1RM, against the last time before it was trained (the
    /// day's take on the strength cards).
    /// </summary>
    void ShowDayLifts(List<WorkoutSession> sessions)
    {
        DayLifts = [.. sessions.SelectMany(s => s.Exercises).GroupBy(e => e.ExerciseId)
            .Select(g => (Exercise: store.GetExercise(g.Key), Sets: g.SelectMany(e => e.Sets).Where(s => s.IsCompleted && !s.IsWarmup && s.WeightKg > 0).ToList()))
            .Where(x => x.Exercise is { IsBodyweight: false } && x.Sets.Count > 0)
            .Select(x =>
            {
                var top = x.Sets.MaxBy(s => ProgressionEngine.E1Rm(s.WeightKg, s.Reps, s.Rir))!;
                var e1 = ProgressionEngine.E1Rm(top.WeightKg, top.Reps, top.Rir);
                var before = stats.E1RmHistory(x.Exercise!.Id, null, _date.AddDays(-1)).LastOrDefault();
                return new LineItem
                {
                    Title = x.Exercise.Name,
                    Detail = $"{units.Format(top.WeightKg)} {units.Label} × {top.Reps} · 1RM {units.FormatWithUnit(e1)}"
                        + (before != null ? $" · was {units.FormatWithUnit(before.Value)} on {before.Label}" : " · first time"),
                    Value = before is { Value: > 0 } b ? $"{(e1 / b.Value - 1) * 100:+0;−0;0}%" : "New",
                    OpenCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Exercise}?id={x.Exercise.Id}")),
                };
            })];
    }

    /// <summary>Recovery going into the day (right now, for today): the map, the major muscles' average, and the night before.</summary>
    void ShowDayRecovery()
    {
        var at = _date == DateTime.Today ? DateTime.Now : _date;
        var details = recovery.Details(at);
        DayRecoveryMap = MuscleMapDrawable.ForRecovery(details.ToDictionary(d => d.Muscle, d => d.Recovery), recovery.PartRecovery(at));
        var overall = details.Where(d => RecoveryService.MajorMuscles.Contains(d.Muscle)).Average(d => d.Recovery);
        DayRecoveryTitle = _date == DateTime.Today ? "Recovery right now" : "Recovery going into the day";
        DayRecoveryValue = $"{overall:P0}";
        DayRecoveryColor = RecoveryService.ColorFor(overall);
        DayRecoveryOverview = RecoveryOverview.For(details);
        DayRecoverySleep = recovery.SleepNote(at);
    }

    /// <summary>The night woken up from on the day, and what it did to recovery.</summary>
    void ShowDaySleep()
    {
        var minutes = recovery.SleepOn(_date);
        DaySleepValue = minutes is { } m ? RecoveryService.Hours(m) : "N/A";
        DaySleepCaption = minutes is { } mins
            ? $"Night to {_date.ToString("ddd d MMM", CultureInfo.CurrentCulture)} · {RecoveryService.SleepEffect(mins)}"
            : "No sleep recorded for this night. Connect Health Connect or Samsung Health in the Profile tab to bring it in.";
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

    // ---- Trends: over a range of days, picked like Nutrition's (7 days, 30, 90, a year, or one's own) ----

    public System.Collections.ObjectModel.ObservableCollection<ChipItem> RangeChips { get; } = [];
    int _rangeDays = 90;
    /// <summary>The range's last day: today for the presets, any day for a custom range.</summary>
    DateTime _rangeEnd = DateTime.Today;
    bool _customRange;
    DateTime RangeStart => _rangeEnd.AddDays(1 - _rangeDays);

    /// <summary>"in the last 90 days", or a custom range's "from 3 Sep to 20 Sep", to end a sentence with.</summary>
    string RangePhrase => _customRange
        ? $"from {RangeStart.ToString("d MMM", CultureInfo.CurrentCulture)} to {_rangeEnd.ToString("d MMM", CultureInfo.CurrentCulture)}"
        : _rangeDays == 365 ? "in the last year" : $"in the last {_rangeDays} days";

    void SelectRange(ChipItem chip)
    {
        if ((int)chip.Value! == 0)
        {
            _ = PickRange(chip);
            return;
        }
        _rangeDays = (int)chip.Value!;
        _rangeEnd = DateTime.Today;
        _customRange = false;
        foreach (var c in RangeChips)
            c.IsSelected = c == chip;
        ShowRange();
    }

    /// <summary>A range of one's own, picked on one calendar (first day, last day, Done; dots on days with a workout). Up to two years.</summary>
    async Task PickRange(ChipItem chip)
    {
        var days = store.History.Select(s => s.StartedAt.Date).ToHashSet();
        if (await dialogs.RangeCalendar("Pick a range", RangeStart, _rangeEnd, DateTime.Today, days.Contains) is not { } picked)
            return;
        var (start, end) = picked;
        if ((end - start).Days >= 731)
            start = end.AddDays(-730);
        _rangeDays = (end - start).Days + 1;
        _rangeEnd = end.Date;
        _customRange = true;
        foreach (var c in RangeChips)
            c.IsSelected = c == chip;
        ShowRange();
    }

    /// <summary>The trends worked out again for the new range, and played in.</summary>
    void ShowRange()
    {
        IsShowing = false;
        ShowTrends();
        IsShowing = true;
    }

    [ObservableProperty] string totalWorkouts = "0";
    [ObservableProperty] string perWeek = "0";
    [ObservableProperty] string totalVolume = "0";
    [ObservableProperty] string streak = "0";
    [ObservableProperty] IDrawable? volumeChart;
    [ObservableProperty] string volumeTitle = "";
    [ObservableProperty] IDrawable? workoutsChart;
    [ObservableProperty] string workoutsTitle = "";
    [ObservableProperty] IDrawable? bodyWeightChart;
    [ObservableProperty] string bodyWeightText = "";
    [ObservableProperty] string bodyWeightChange = "";
    [ObservableProperty] IDrawable muscleMap = MuscleMapDrawable.Empty;
    [ObservableProperty] List<MuscleBarItem> muscleSets = [];
    [ObservableProperty] string muscleSetsCaption = "";
    [ObservableProperty] List<LineItem> records = [];
    [ObservableProperty] bool hasRecords;
    [ObservableProperty] string recordsCaption = "";
    [ObservableProperty] string noRecordsText = "";
    [ObservableProperty] string gainsCaption = "";
    [ObservableProperty] string splitCaption = "";

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

    const int ConsistencyMinDays = 28;

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
        ActivityCaption = day.Health?.Source is { } source ? $"Today so far · {source}" : "Today · connect your health data in the Profile tab";
        Activity = ActivityItems(day);
        HasActivity = true;
    }

    /// <summary>A day's calories burned and steps from the health apps: on Trends today's, on Day the day's.</summary>
    static List<StatItem> ActivityItems(DayNutrition day)
    {
        var h = day.Health;
        static string Kcal(double? v) => v is { } k ? $"{NutritionService.Kcal(k)} kcal" : "N/A";
        return
        [
            new("Total burned", Kcal(day.BurnSource == BurnSource.Measured ? day.BurnedKcal : null)),
            new("Active", Kcal(h?.ActiveBurnedKcal)),
            // Samsung Health's own total (read directly) comes without a resting figure: what isn't activity.
            new("Resting", Kcal(h?.BasalBurnedKcal ?? (h is { TotalBurnedKcal: { } total, ActiveBurnedKcal: { } active } ? total - active : null))),
            new("Steps", h?.Steps is { } steps ? steps.ToString("#,0", CultureInfo.CurrentCulture) : "N/A"),
        ];
    }

    /// <summary>Measurements from the health apps only (not weights logged by hand, nothing estimated).</summary>
    List<BodyWeightEntry> MeasuredBody() => [.. store.Data.BodyWeights.Where(b => b.Source != null).OrderBy(b => b.Date)];

    /// <summary>
    /// The latest of each body measurement by <paramref name="upTo"/>, N/A for what the health apps haven't measured; fat
    /// mass and BMI worked out from measured values only. With a caption saying when and where from.
    /// </summary>
    (List<StatItem>, string) BodyAsOf(DateTime upTo)
    {
        var entries = MeasuredBody().Where(b => b.Date.Date <= upTo.Date).ToList();
        var latest = entries.LastOrDefault();
        double? Latest(Func<BodyWeightEntry, double?> pick) => entries.Select(pick).LastOrDefault(v => v != null);
        var weight = latest?.WeightKg;
        var fat = Latest(b => b.BodyFatPercent);
        var height = store.Profile.HeightCm;
        string Mass(double? kg) => kg is { } v ? units.FormatWithUnit(v) : "N/A";

        List<StatItem> items =
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
        var caption = latest == null
            ? upTo.Date >= DateTime.Today
                ? "No measurements from your health apps yet. Connect your health data in the Profile tab to bring in your scale's and watch's."
                : "No measurements from your health apps by this day."
            : $"Last measured {latest.Date:d MMM} · {latest.Source}";
        return (items, caption);
    }

    void ShowBody()
    {
        (Body, BodyCaption) = BodyAsOf(DateTime.Today);
        var fats = MeasuredBody().Where(b => b.BodyFatPercent != null && b.Date >= RangeStart && b.Date <= _rangeEnd).ToList();
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
        if (RangeChips.Count == 0)
        {
            foreach (var (label, days) in new[] { ("7 days", 7), ("30 days", 30), ("90 days", 90), ("1 year", 365), ("Custom", 0) })
                RangeChips.Add(new ChipItem(label, days, SelectRange) { IsSelected = days == _rangeDays });
        }
        // Worked out again only when the data changed (or the day did) since last time: it goes through all the history.
        // Either way it plays in again. A preset range moves on with the day; a custom one stays where it was put.
        if (_trendsVersion != store.Version || _trendsDay != DateTime.Today)
        {
            if (!_customRange)
                _rangeEnd = DateTime.Today;
            ShowTrends();
        }
        // Last, so everything bound plays in (charts draw, numbers count, cards rise).
        IsShowing = true;
        return Task.CompletedTask;
    }

    int _trendsVersion = -1;
    DateTime _trendsDay;

    /// <summary>Everything on Trends, over the range (but the week streak, today's activity and the latest body measurements).</summary>
    void ShowTrends()
    {
        _trendsVersion = store.Version;
        _trendsDay = DateTime.Today;
        var first = RangeStart;
        var last = _rangeEnd;
        var weeks = Math.Max(1, _rangeDays / 7.0);
        var sessions = stats.SessionsIn(first, last);
        TotalWorkouts = sessions.Count.ToString();
        PerWeek = (sessions.Count / weeks).ToString("0.#", CultureInfo.CurrentCulture);
        TotalVolume = units.FormatVolume(sessions.Sum(stats.SessionVolume));
        Streak = stats.WeekStreak().ToString();

        var period = StatsService.PeriodFor(first, last).ToString().ToLowerInvariant();
        VolumeTitle = $"Volume per {period}";
        VolumeChart = new BarChartDrawable(stats.Volume(first, last).Select(p => p with { Value = units.ToDisplay(p.Value) }).ToList(), Accent,
            v => v >= 1000 ? $"{v / 1000:0.#}k" : $"{v:0}");
        WorkoutsTitle = $"Workouts per {period}";
        WorkoutsChart = new BarChartDrawable(stats.Workouts(first, last), Violet, v => $"{v:0}");

        var weights = store.Data.BodyWeights.OrderBy(b => b.Date).ToList();
        var inRange = weights.Where(b => b.Date >= first && b.Date <= last).ToList();
        BodyWeightChart = new LineChartDrawable(
            inRange.Select(b => new ChartPoint(b.Date.ToString("d MMM", CultureInfo.CurrentCulture), units.ToDisplay(b.WeightKg))).ToList(),
            Green, v => $"{v:0.#}");
        BodyWeightText = weights.Count > 0 ? units.FormatWithUnit(weights[^1].WeightKg) : units.FormatWithUnit(store.Profile.BodyWeightKg);
        if (inRange.Count > 1)
        {
            var diff = units.ToDisplay(inRange[^1].WeightKg) - units.ToDisplay(inRange[0].WeightKg);
            BodyWeightChange = $"{(diff >= 0 ? "+" : "−")}{Math.Abs(diff):0.#} {units.Label} since {inRange[0].Date:d MMM}";
        }
        else
        {
            BodyWeightChange = "Log your weight regularly to see the trend";
        }

        // A week's worth, averaged over the range, so it reads against the 10–20 sets a week target whatever the range.
        var sets = stats.SetsPerPart(sessions).ToDictionary(p => p.Key, p => p.Value / weeks);
        MuscleSetsCaption = $"Average {RangePhrase}. 10–20 hard sets per muscle per week is a good target for growth.";
        MuscleMap = MuscleMapDrawable.ForVolume(sets);
        MuscleSets = PartTotals(sets).OrderByDescending(s => s.Value).Select(s => new MuscleBarItem
        {
            Name = s.Name,
            Value = $"{s.Value:0.#} sets",
            Progress = Math.Clamp(s.Value / 20, 0, 1),
            Color = s.Value >= 10 ? Green : s.Value > 0 ? Accent : Color.FromArgb("#272C39"),
        }).ToList();

        // The records still standing that were set in the range.
        var records = stats.PersonalRecords();
        RecordsCaption = $"Best estimated one-rep max per exercise, set {RangePhrase}.";
        NoRecordsText = records.Count == 0 ? "No records yet. Finish a workout to set your first." : $"No new records {RangePhrase}.";
        Records = records.Where(r => r.Date >= first && r.Date < last.AddDays(1)).Take(10).Select(r => new LineItem
        {
            Title = r.Exercise.Name,
            Detail = $"{units.Format(r.WeightKg)} {units.Label} × {r.Reps} · {r.Date:d MMM}",
            Value = units.FormatWithUnit(r.E1RmKg),
            OpenCommand = new AsyncRelayCommand(() => GoTo($"{Routes.Exercise}?id={r.Exercise.Id}")),
        }).ToList();
        HasRecords = Records.Count > 0;
        ShowStrength();
        ShowHabits(sessions);
        ShowRecoveryAndSleep(sessions);
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

    /// <summary>How often and how long you train over the range, and how the work is split across the body.</summary>
    void ShowHabits(List<WorkoutSession> sessions)
    {
        // At least four weeks of the grid, so a short range still has some around it; only the range's days are counted.
        var (monday, days) = stats.DailyVolume(_rangeEnd.AddDays(1 - Math.Max(_rangeDays, ConsistencyMinDays)), _rangeEnd);
        ConsistencyChart = new HeatmapDrawable(monday, days, Accent);
        var trained = sessions.Select(s => s.StartedAt.Date).Distinct().Count();
        ConsistencyText = trained == 0
            ? "Every workout lights up a day here."
            : $"{trained} training day{(trained == 1 ? "" : "s")} {RangePhrase} · {trained / Math.Max(1, _rangeDays / 7.0):0.#} a week";

        var minutes = stats.Minutes(RangeStart, _rangeEnd);
        HasDuration = sessions.Count > 0;
        DurationChart = new LineChartDrawable(minutes, Violet, v => $"{v:0} min");
        DurationText = HasDuration ? $"About {sessions.Average(s => s.Duration.TotalMinutes):0} minutes a workout {RangePhrase}" : "";

        SplitCaption = $"Working sets by movement, {RangePhrase}";
        (SplitChart, Split, HasSplit) = SplitFor(sessions);
    }

    /// <summary>The working sets of <paramref name="sessions"/> by movement (push, pull, legs, core): a donut and its legend.</summary>
    (IDrawable Chart, List<SplitItem> Items, bool Any) SplitFor(IEnumerable<WorkoutSession> sessions)
    {
        var sets = stats.SetsPerMuscle(sessions);
        var slices = SplitGroups.Select(g => new DonutSlice(g.Name, g.Muscles.Sum(m => sets.GetValueOrDefault(m)), g.Color)).ToList();
        var total = slices.Sum(s => s.Value);
        return (new DonutDrawable(slices.OrderByDescending(s => s.Value).ToList(), $"{total:0}", "sets"),
            [.. slices.Select(s => new SplitItem(s.Label, s.Color, total > 0 ? s.Value / total : 0, $"{s.Value:0} sets"))],
            total > 0);
    }

    [ObservableProperty] IDrawable? recoveryChart;
    [ObservableProperty] string recoveryCaption = "";
    [ObservableProperty] List<StatItem> recoveryStats = [];
    [ObservableProperty] IDrawable? sleepChart;
    [ObservableProperty] string sleepCaption = "";
    [ObservableProperty] List<StatItem> sleepStats = [];
    [ObservableProperty] bool hasSleep;

    /// <summary>
    /// Recovery and sleep over the range: the major muscles' recovery going into each day, and hours slept each night,
    /// a point (or bar) a day, week or month like the other charts. Today counts; days after it don't.
    /// </summary>
    void ShowRecoveryAndSleep(List<WorkoutSession> sessions)
    {
        var today = DateTime.Today;
        var days = Enumerable.Range(0, _rangeDays).Select(i => RangeStart.AddDays(i)).Where(d => d <= today).ToList();
        var periods = StatsService.Periods(RangeStart, _rangeEnd);
        var per = StatsService.PeriodFor(RangeStart, _rangeEnd).ToString().ToLowerInvariant();

        var mornings = days.Select(d => (Day: d, Recovery: recovery.Overall(d))).ToList();
        RecoveryChart = new LineChartDrawable(
            [.. periods.Select(p => (p.Label, Values: mornings.Where(m => m.Day >= p.From && m.Day < p.To).Select(m => m.Recovery).ToList()))
                .Where(x => x.Values.Count > 0).Select(x => new ChartPoint(x.Label, x.Values.Average() * 100))],
            Green, v => $"{v:0}%");
        var trained = sessions.Select(s => s.StartedAt.Date).ToHashSet();
        var onTraining = mornings.Where(m => trained.Contains(m.Day)).Select(m => m.Recovery).ToList();
        RecoveryStats = mornings.Count == 0 ? [] :
        [
            new("Average", $"{mornings.Average(m => m.Recovery):P0}"),
            new("Training days", onTraining.Count > 0 ? $"{onTraining.Average():P0}" : "–"),
            new("Started fatigued", Days(mornings.Count(m => m.Recovery < RecoveryService.ReadyThreshold))),
        ];
        RecoveryCaption = $"Your major muscles' recovery going into each day, averaged per {per}. Sleep speeds it up or slows it down.";

        var nights = days.Select(d => (Day: d, Minutes: recovery.SleepOn(d))).Where(n => n.Minutes != null).Select(n => (n.Day, Minutes: n.Minutes!.Value)).ToList();
        HasSleep = nights.Count > 0;
        SleepChart = new BarChartDrawable(
            [.. periods.Select(p =>
            {
                var inPeriod = nights.Where(n => n.Day >= p.From && n.Day < p.To).ToList();
                return new ChartPoint(p.Label, inPeriod.Count == 0 ? 0 : inPeriod.Average(n => n.Minutes) / 60.0);
            })],
            Violet, v => $"{v:0.#} h");
        var average = nights.Count > 0 ? nights.Average(n => n.Minutes) : 0;
        SleepStats = nights.Count == 0 ? [] :
        [
            new("Average a night", RecoveryService.Hours(average)),
            new("Nights recorded", $"{nights.Count} of {days.Count}"),
            new("Under 7 hours", Days(nights.Count(n => n.Minutes < 7 * 60))),
            new("Longest", RecoveryService.Hours(nights.Max(n => n.Minutes))),
        ];
        SleepCaption = nights.Count == 0
            ? $"No sleep recorded {RangePhrase}. Connect Health Connect or Samsung Health in the Profile tab to bring it in."
            : $"Hours asleep a night, averaged per {per}. At your average, {RecoveryService.SleepEffect(average)}.";

        static string Days(int n) => n == 1 ? "1 day" : $"{n} days";
    }

    void ShowStrength()
    {
        var lifts = stats.TrackedLifts(8);
        HasLifts = lifts.Count > 0;

        var trend = stats.StrengthTrend(lifts.Take(5).ToList(), RangeStart, _rangeEnd);
        HasStrengthTrend = trend.Count >= 2;
        StrengthTrendChart = new LineChartDrawable(trend, Orange, v => $"{v - 100:+0;−0;0}%");
        var change = trend.Count >= 2 ? trend[^1].Value - trend[0].Value : 0;
        StrengthTrendText = !HasStrengthTrend
            ? "Train your main lifts for a couple of weeks to see the trend."
            : $"{(change >= 0 ? "Up" : "Down")} {Math.Abs(change):0.#}% across your top {Math.Min(5, lifts.Count)} lifts {RangePhrase}";

        LiftChips.Clear();
        if (_liftId == null || lifts.All(l => l.Id != _liftId))
            _liftId = lifts.FirstOrDefault()?.Id;
        foreach (var lift in lifts)
            LiftChips.Add(new ChipItem(lift.Name, lift.Id, SelectLift) { IsSelected = lift.Id == _liftId });
        ShowLift();

        GainsCaption = $"Estimated one-rep max, {RangePhrase}.";
        Gains = [.. stats.Gains(RangeStart, _rangeEnd, 5).Select(g => new LineItem
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
        var history = stats.E1RmHistory(_liftId, RangeStart, _rangeEnd);
        LiftChart = new LineChartDrawable(history.Select(p => p with { Value = units.ToDisplay(p.Value) }).ToList(), Accent, v => $"{v:0.#}");
        if (history.Count == 0)
        {
            LiftSummary = $"Not trained {RangePhrase}.";
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
    Task OpenHistory() => GoTo(Routes.History);

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

}

/// <summary>Progress's tabs: one day's training, the trends over time, and the strength ranks.</summary>
public enum ProgressSection { Day, Trends, Ranks }
