namespace GymBook.Models;

/// <summary>
/// Catalogue extras for a built-in exercise (from free-exercise-db). Kept off <see cref="Exercise"/> so the
/// synced entity and both databases stay unchanged; custom exercises have none.
/// </summary>
/// <param name="Images">Start and end position photos, as URLs; loaded on demand and cached by the platform.</param>
public record ExerciseDetails(string? Level, string? Force, string? Category, IReadOnlyList<string> Steps, IReadOnlyList<string> Images);
