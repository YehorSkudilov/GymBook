namespace GymBook.Models;

/// <summary>A top-level record that syncs on its own. Last write (by <see cref="UpdatedAt"/>) wins.</summary>
public interface ISyncEntity
{
    string Id { get; set; }
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Deletes are kept as tombstones so they reach other devices.</summary>
    bool IsDeleted { get; set; }
}
