namespace GymBook.Contracts;

// What the phone app and the Wear OS app (GymBook.Wear) send each other over the Wearable Data Layer. The phone owns the
// workout; the watch shows it and asks the phone to tick sets. Weights are already in the user's unit, so the watch
// doesn't need the profile.

public static class WearPaths
{
    /// <summary>
    /// Data item: the phone's <see cref="WearWorkout"/>, rewritten whenever the workout changes. The Data Layer keeps the
    /// last one, so a watch app opening later still finds it, and only passes it on when its content changed.
    /// </summary>
    public const string Workout = "/gymbook/workout";

    /// <summary>Message to the phone: a <see cref="WearCompleteSet"/>.</summary>
    public const string CompleteSet = "/gymbook/complete-set";
}

/// <summary>The workout in progress on the phone, or <see cref="IsActive"/> false when there's none.</summary>
public record WearWorkout(
    bool IsActive,
    string SessionId,
    string Name,
    DateTimeOffset StartedAt,
    string Unit,
    List<WearExercise> Exercises)
{
    public static readonly WearWorkout None = new(false, "", "", DateTimeOffset.MinValue, "kg", []);
}

public record WearExercise(string Name, int RepMin, int RepMax, int RestSeconds, List<WearSet> Sets);

/// <summary>A set; <see cref="Weight"/> is in the workout's <see cref="WearWorkout.Unit"/>.</summary>
public record WearSet(double Weight, int Reps, bool IsWarmup, bool IsCompleted, DateTimeOffset? CompletedAt);

/// <summary>Tick this set, by its place in the workout. Ignored if the phone's workout changed to another one.</summary>
public record WearCompleteSet(string SessionId, int Exercise, int Set);
