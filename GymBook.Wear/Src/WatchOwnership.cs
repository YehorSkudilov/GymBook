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
}
