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

    public override Task OnAppearingAsync()
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
        return Task.CompletedTask;
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
