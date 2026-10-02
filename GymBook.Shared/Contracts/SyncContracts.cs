using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GymBook.Models;

namespace GymBook.Contracts;

/// <summary>Records created, changed or deleted (as tombstones) since the last sync.</summary>
public class SyncChanges
{
    public UserProfile? Profile { get; set; }

    [MaxItems(SyncLimits.BatchSize)]
    public List<WorkoutPlan> Plans { get; set; } = [];

    [MaxItems(SyncLimits.BatchSize)]
    public List<WorkoutSession> Sessions { get; set; } = [];

    [MaxItems(SyncLimits.BatchSize)]
    public List<Exercise> CustomExercises { get; set; } = [];

    [MaxItems(SyncLimits.BatchSize)]
    public List<BodyWeightEntry> BodyWeights { get; set; } = [];

    [MaxItems(SyncLimits.BatchSize)]
    public List<FoodEntry> FoodEntries { get; set; } = [];

    [MaxItems(SyncLimits.BatchSize)]
    public List<HealthDay> HealthDays { get; set; } = [];

    [JsonIgnore]
    public int Count => (Profile != null ? 1 : 0) + Plans.Count + Sessions.Count + CustomExercises.Count + BodyWeights.Count
        + FoodEntries.Count + HealthDays.Count;
}

/// <summary>Pushes local changes and pulls everything the server has after <see cref="Since"/>.</summary>
public class SyncRequest
{
    /// <summary>The <see cref="SyncResponse.Cursor"/> from the previous sync; 0 on a first sync.</summary>
    [Range(0, long.MaxValue)]
    public long Since { get; set; }

    public SyncChanges Changes { get; set; } = new();
}

public class SyncResponse
{
    /// <summary>Pass back as <see cref="SyncRequest.Since"/> next time.</summary>
    public long Cursor { get; set; }

    /// <summary>More server changes are waiting; sync again straight away.</summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Server-side changes the client doesn't have yet, plus the server's copy of any pushed record that
    /// lost to a newer version.
    /// </summary>
    public SyncChanges Changes { get; set; } = new();
}
