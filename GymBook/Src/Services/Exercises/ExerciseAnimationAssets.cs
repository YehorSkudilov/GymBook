// Generated from Resources/Raw/exercise-animations by tools/exercises/sync-thumbnail-manifest.mjs.
namespace GymBook.Services;

public static class ExerciseAnimationAssets
{
    static readonly HashSet<string> Ids = new(StringComparer.Ordinal)
    {
        "romanian_deadlift",
    };

    public static bool Exists(string id) => Ids.Contains(id);
}
