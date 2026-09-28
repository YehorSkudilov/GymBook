using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>A muscle's row on the recovery page.</summary>
public class MuscleRecoveryItem
{
    public required string Name { get; init; }
    public required double Progress { get; init; }
    public required string Percent { get; init; }
    public required string Status { get; init; }
    public required string Detail { get; init; }
    public required Color Color { get; init; }
}

/// <summary>
/// Full-screen recovery: the body map, a preview slider to look back up to a week or ahead until everything is fresh,
/// and every muscle's recovery with when it will be ready and which workout tired it.
/// </summary>
public partial class RecoveryViewModel(RecoveryService recovery) : BaseViewModel, IQueryAttributable
{
    // The big movers first, then the smaller and supporting muscles that work alongside them.
    static readonly MuscleGroup[] Major = [MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes];

    [ObservableProperty] IDrawable map = MuscleMapDrawable.Empty;
    [ObservableProperty] double hours;
    [ObservableProperty] string when = "Now";
    [ObservableProperty] bool isPreview;
    [ObservableProperty] string summary = "";
    [ObservableProperty] List<MuscleRecoveryItem> majorMuscles = [];
    [ObservableProperty] List<MuscleRecoveryItem> supportingMuscles = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("hours", out var h) && double.TryParse(h?.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var value))
            Hours = Math.Clamp(value, -RecoveryService.PreviewPastHours, RecoveryService.PreviewFutureHours);
    }

    public override Task OnAppearingAsync()
    {
        Update();
        return Task.CompletedTask;
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

    void Update()
    {
        var at = DateTime.Now.AddHours(Hours);
        var details = recovery.Details(at);
        Map = MuscleMapDrawable.ForRecovery(details.ToDictionary(d => d.Muscle, d => d.Recovery));
        When = RecoveryService.PreviewLabel(Hours);
        IsPreview = Hours != 0;

        var tired = details.Count(d => d.Recovery < 0.9);
        Summary = tired == 0 ? "Every muscle is fresh." : $"{tired} of {details.Count} muscle groups still recovering.";
        MajorMuscles = [.. details.Where(d => Major.Contains(d.Muscle)).OrderBy(d => d.Recovery).Select(d => Item(d, at))];
        SupportingMuscles = [.. details.Where(d => !Major.Contains(d.Muscle)).OrderBy(d => d.Recovery).Select(d => Item(d, at))];
    }

    static MuscleRecoveryItem Item(MuscleRecovery r, DateTime at) => new()
    {
        Name = r.Muscle.Display(),
        Progress = r.Recovery,
        Percent = $"{r.Recovery:P0}",
        Status = RecoveryService.StatusFor(r.Recovery),
        Color = RecoveryService.ColorFor(r.Recovery),
        Detail = r is { LimitingSession: { } s, ReadyAt: { } ready } && r.Recovery < 1
            ? $"Fresh {Relative(ready - at)} · {s.Name}, {s.EndedAt:ddd HH:mm} ({r.Sets:0.#} sets)"
            : "Fully recovered",
    };

    static string Relative(TimeSpan span)
    {
        var h = (int)Math.Ceiling(span.TotalHours);
        return h >= 24 ? $"in {h / 24} d {h % 24} h" : $"in {h} h";
    }

    [RelayCommand]
    void Now() => Hours = 0;
}
