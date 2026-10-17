using System.Globalization;

namespace GymBook.Models;

/// <summary>The kind of training an exercise is: decides how it's prescribed (explosive, held, easy-paced) and how it's grouped.</summary>
public enum ExerciseCategory { Strength, Plyometric, Olympic, Pilates, Yoga, Mobility, Stretch, Cardio }

public enum ExerciseLevel { Beginner, Intermediate, Advanced }

/// <summary>A demonstration on YouTube. <see cref="Start"/> and <see cref="End"/> (seconds) trim a longer video to the demonstration itself.</summary>
public record ExerciseVideo(string YouTubeId, double? Start = null, double? End = null)
{
    static string UrlSecond(double seconds) => Math.Round(seconds, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);

    public string WatchUrl => $"https://www.youtube.com/watch?v={YouTubeId}" + (Start is { } s ? $"&t={UrlSecond(s)}s" : "");

    /// <summary>The embedded player, playing just the demonstration.</summary>
    public string EmbedUrl => $"https://www.youtube.com/embed/{YouTubeId}?rel=0&playsinline=1&modestbranding=1"
        + (Start is { } s ? $"&start={UrlSecond(s)}" : "") + (End is { } e ? $"&end={UrlSecond(e)}" : "");
}

/// <summary>
/// The library's extras for a built-in exercise. Kept off <see cref="Exercise"/> so the synced entity and both
/// databases stay unchanged; custom exercises have none.
/// </summary>
/// <param name="Hold">Held still rather than moved: each rep is a second of holding (a hold of a few seconds for the neck).</param>
/// <param name="Unit">
/// What its sets are counted in when it's put in a plan or workout, if not reps: a carry walked for seconds or meters, a
/// sled pushed for meters (see TrainingGoals.DefaultUnit). The user can count it another way in each plan.
/// </param>
public record ExerciseDetails(ExerciseCategory Category, ExerciseLevel Level, bool Hold, IReadOnlyList<string> Steps, IReadOnlyList<string> Tips,
    ExerciseVideo? Video, RepUnit? Unit = null);
