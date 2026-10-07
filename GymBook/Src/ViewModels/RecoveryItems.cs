using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>A muscle's row under a recovery map.</summary>
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
/// How many muscle groups are still recovering at a moment, the same under every recovery map (see
/// <see cref="Views.RecoveryOverviewView"/>): a headline, a strip with a segment per muscle group, and the ones not
/// ready to train yet by name.
/// </summary>
public record RecoveryOverview(string Headline, IDrawable Strip, string Detail)
{
    public static readonly RecoveryOverview Empty = new("", new RecoveryOverviewDrawable([]), "");

    public bool HasDetail => Detail.Length > 0;

    public static RecoveryOverview For(IReadOnlyCollection<MuscleRecovery> details)
    {
        var recovering = details.Count(d => d.Recovery < 0.9);
        var tired = details.Where(d => d.Recovery < RecoveryService.ReadyThreshold).OrderBy(d => d.Recovery).Select(d => d.Muscle.Display()).ToList();
        var headline = recovering == 0 ? $"All {details.Count} muscle groups fresh"
            : $"{recovering} of {details.Count} muscle {(details.Count == 1 ? "group" : "groups")} still recovering";
        var detail = tired.Count > 0 ? $"Not ready to train: {string.Join(", ", tired)}"
            : recovering > 0 ? "All nearly there: fine to train." : "";
        return new(headline, new RecoveryOverviewDrawable([.. details.Select(d => d.Recovery)]), detail);
    }
}

/// <summary>The muscle rows under the recovery maps (the Workout tab, a calendar day, Progress's day).</summary>
public static class RecoveryItems
{
    /// <summary>The major and the supporting muscles, most tired first, with when each will be fresh and what tired it.</summary>
    public static (List<MuscleRecoveryItem> Major, List<MuscleRecoveryItem> Supporting) Lists(IReadOnlyCollection<MuscleRecovery> details, DateTime at) =>
        ([.. details.Where(d => RecoveryService.MajorMuscles.Contains(d.Muscle)).OrderBy(d => d.Recovery).Select(d => Item(d, at))],
         [.. details.Where(d => !RecoveryService.MajorMuscles.Contains(d.Muscle)).OrderBy(d => d.Recovery).Select(d => Item(d, at))]);


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
