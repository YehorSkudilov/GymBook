using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public partial class SessionDetailViewModel(DataStore store, StatsService stats, Units units, DialogService dialogs)
    : BaseViewModel, IQueryAttributable
{
    string? _id;

    [ObservableProperty] bool isFinished;
    [ObservableProperty] string name = "";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] string duration = "";
    [ObservableProperty] string volume = "";
    [ObservableProperty] string sets = "";
    [ObservableProperty] string records = "";
    [ObservableProperty] List<LineItem> recordItems = [];
    [ObservableProperty] bool hasRecords;
    [ObservableProperty] List<WorkoutPreviewItem> exercises = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = query.TryGetValue("id", out var id) ? id?.ToString() : null;
        IsFinished = query.TryGetValue("finished", out var f) && f?.ToString() == "true";
    }

    public override Task OnAppearingAsync()
    {
        var s = store.GetSession(_id);
        if (s == null)
            return GoBack();

        Name = s.Name;
        DateText = s.StartedAt.ToString("dddd, d MMMM yyyy · HH:mm");
        Duration = Units.Duration(s.Duration);
        Volume = units.FormatVolume(stats.SessionVolume(s));
        Sets = s.WorkingSets.Count().ToString();

        var prs = stats.RecordsIn(s);
        Records = prs.Count.ToString();
        HasRecords = prs.Count > 0;
        RecordItems = prs.Select(r => new LineItem
        {
            Title = r.Exercise.Name,
            Detail = $"{units.Format(r.WeightKg)} {units.Label} × {r.Reps}",
            Value = $"e1RM {units.FormatWithUnit(r.E1RmKg)}",
        }).ToList();

        Exercises = s.Exercises.Select(e =>
        {
            var ex = store.GetExercise(e.ExerciseId);
            var lines = e.Sets.Select((set, i) =>
                $"{(set.IsWarmup ? "W" : "•")}  {(ex?.IsBodyweight == true && set.WeightKg <= 0 ? "BW" : units.FormatWithUnit(set.WeightKg))} × {set.Reps}{(set.Rir is int rir ? $"  @ {rir} RIR" : "")}");
            return new WorkoutPreviewItem
            {
                Name = ex?.Name ?? "Unknown exercise",
                Meta = ex?.Subtitle ?? "",
                Exercises = string.Join("\n", lines),
            };
        }).ToList();
        return Task.CompletedTask;
    }

    [RelayCommand]
    Task Done() => Shell.Current.GoToAsync("//home");

    [RelayCommand]
    async Task Delete()
    {
        var s = store.GetSession(_id);
        if (s == null || !await dialogs.Confirm("Delete workout?", "This removes it from your history and statistics.", "Delete"))
            return;
        store.Data.Sessions.Remove(s);
        store.Save();
        await GoBack();
    }
}

public partial class HistoryViewModel(DataStore store, StatsService stats, Units units) : BaseViewModel
{
    [ObservableProperty] List<SessionItem> items = [];
    [ObservableProperty] bool isEmpty;
    [ObservableProperty] string summary = "";

    public override Task OnAppearingAsync()
    {
        var history = store.History.ToList();
        Items = history.Select(s => SessionItem.Create(s, store, stats, units)).ToList();
        IsEmpty = Items.Count == 0;
        Summary = $"{history.Count} workouts · {units.FormatVolume(history.Sum(stats.SessionVolume))} lifted";
        return Task.CompletedTask;
    }
}
