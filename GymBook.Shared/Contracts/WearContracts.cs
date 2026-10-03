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

    /// <summary>Message to the watch, no payload: a workout just started on the phone, open the watch app to follow it.</summary>
    public const string OpenApp = "/gymbook/open-app";

    /// <summary>Message to the phone, no payload: sign the watch in to the phone's account ("Sign in with phone").</summary>
    public const string RequestSession = "/gymbook/request-session";

    /// <summary>Message to the watch, the phone's answer to <see cref="RequestSession"/>: a <see cref="WearSession"/>.</summary>
    public const string Session = "/gymbook/session";

    /// <summary>Message to the phone: the heart rate the watch just read, in beats per minute, as text ("128").</summary>
    public const string HeartRate = "/gymbook/heart-rate";

    /// <summary>
    /// Message to the watch, no payload: the phone's data changed and is synced to the account, so sync now (signing in
    /// through the phone first if the watch isn't signed in), also while the watch app isn't open.
    /// </summary>
    public const string SyncNow = "/gymbook/sync-now";
}

/// <summary>
/// A session of the watch's own (a separate refresh token family from the phone's, from api/account/device-session),
/// or why there isn't one, e.g. the phone app isn't signed in.
/// </summary>
public record WearSession(AuthResponse? Auth, string? Error);

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
