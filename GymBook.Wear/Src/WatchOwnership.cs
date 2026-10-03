using GymBook.Contracts;
using GymBook.Models;

namespace GymBook.Wear;

/// <summary>
/// Which workout this watch started itself. Sync brings workouts in progress from the phone too, so the watch's
/// database can hold the phone's workout as well; only the one started here is the watch's to run, the phone's is
/// shown through the phone (the companion page) while the phone is running it.
/// </summary>
public static class WatchOwnership
{
    const string Key = "wear.own_session_id";

    public static string? SessionId
    {
        get => Preferences.Default.Get<string?>(Key, null);
        set
        {
            if (value == null)
                Preferences.Default.Remove(Key);
            else
                Preferences.Default.Set(Key, value);
        }
    }

    /// <summary>
    /// Whether <paramref name="active"/>, the workout in progress in the watch's database, is the watch's to show and
    /// run: one it started itself, or one that came in by syncing while the phone can't be reached (and isn't running
    /// another). With the phone connected and not running it, it's an out-of-date copy of a workout finished or
    /// discarded there: not shown (the home, the Tile, the watch face) until a sync brings the phone's version.
    /// </summary>
    public static bool IsWatchs(WorkoutSession active, WearWorkout phone, bool phoneConnected) =>
        active.Id == SessionId || !(phone.IsActive && phone.SessionId != SessionId) && !phoneConnected;
}
