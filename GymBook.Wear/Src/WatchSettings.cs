namespace GymBook.Wear;

/// <summary>
/// Settings that only concern this watch, kept on it (the account's settings, like the weight unit, live in the
/// profile and sync).
/// </summary>
public static class WatchSettings
{
    const string HeartRateKey = "wear.heart_rate", RestBuzzKey = "wear.rest_buzz", TapFeedbackKey = "wear.tap_feedback";

    /// <summary>Read the heart rate during workouts. Off saves some battery.</summary>
    public static bool HeartRate
    {
        get => Preferences.Default.Get(HeartRateKey, true);
        set => Preferences.Default.Set(HeartRateKey, value);
    }

    /// <summary>Buzz when the rest is over.</summary>
    public static bool RestBuzz
    {
        get => Preferences.Default.Get(RestBuzzKey, true);
        set => Preferences.Default.Set(RestBuzzKey, value);
    }

    /// <summary>A short tap felt when a set is ticked.</summary>
    public static bool TapFeedback
    {
        get => Preferences.Default.Get(TapFeedbackKey, true);
        set => Preferences.Default.Set(TapFeedbackKey, value);
    }
}
