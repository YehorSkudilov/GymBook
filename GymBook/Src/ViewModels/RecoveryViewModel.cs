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
/// Full-screen recovery: the body map at a moment picked like on the calendar (going into a day or after its last
/// workout; or a slider from now, opened from the Workout tab), and every muscle's recovery with when it will be
/// ready and which workout tired it.
/// </summary>
public partial class RecoveryViewModel : BaseViewModel, IQueryAttributable
{
    // The big movers first, then the smaller and supporting muscles that work alongside them.
    static readonly MuscleGroup[] Major = [MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes];

    readonly RecoveryService recovery;
    readonly DataStore store;

    public RecoveryViewModel(RecoveryService recovery, DataStore store)
    {
        (this.recovery, this.store) = (recovery, store);
        Moment.Changed += (_, _) => Update();
    }

    [ObservableProperty] IDrawable map = MuscleMapDrawable.Empty;
    [ObservableProperty] string summary = "";
    /// <summary>The night slept before the moment shown and what it does to recovery; empty without sleep recorded.</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasSleep))] string sleepText = "";
    public bool HasSleep => SleepText.Length > 0;
    [ObservableProperty] List<MuscleRecoveryItem> majorMuscles = [];
    [ObservableProperty] List<MuscleRecoveryItem> supportingMuscles = [];

    /// <summary>The moment shown, the same picker as on the calendar's day card.</summary>
    public RecoveryMoment Moment { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        Moment.Apply(query, day => store.History.Where(s => s.StartedAt.Date == day && s.EndedAt != null));

    public override Task OnAppearingAsync()
    {
        Update();
        return Task.CompletedTask;
    }

    void Update()
    {
        var at = Moment.At;
        var details = recovery.Details(at);
        Map = MuscleMapDrawable.ForRecovery(details.ToDictionary(d => d.Muscle, d => d.Recovery), recovery.PartRecovery(at));

        var tired = details.Count(d => d.Recovery < 0.9);
        Summary = tired == 0 ? "Every muscle is fresh." : $"{tired} of {details.Count} muscle groups still recovering.";
        // Sleep stretches or shortens recovery: say how the last night did.
        SleepText = recovery.SleepNote(at) ?? "";
        (MajorMuscles, SupportingMuscles) = Lists(details, at);
    }

    /// <summary>The major and the supporting muscles, most tired first. Also under the calendar's recovery map.</summary>
    internal static (List<MuscleRecoveryItem> Major, List<MuscleRecoveryItem> Supporting) Lists(IReadOnlyCollection<MuscleRecovery> details, DateTime at) =>
        ([.. details.Where(d => Major.Contains(d.Muscle)).OrderBy(d => d.Recovery).Select(d => Item(d, at))],
         [.. details.Where(d => !Major.Contains(d.Muscle)).OrderBy(d => d.Recovery).Select(d => Item(d, at))]);

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
}
