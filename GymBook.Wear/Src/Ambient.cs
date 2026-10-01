namespace GymBook.Wear;

/// <summary>
/// Wear OS ambient mode: when the screen would time out mid-workout, the app stays on screen dimmed (instead of the
/// watch face) and is only redrawn about once a minute. Raised by MainActivity's AmbientLifecycleObserver; the workout
/// pages hide their buttons and colour, and show times that stay right between those updates.
/// </summary>
public static class Ambient
{
    public static bool IsActive { get; private set; }

    /// <summary>Entered (true) or left (false) ambient mode. Raised on the main thread.</summary>
    public static event Action<bool>? Changed;

    /// <summary>About once a minute while in ambient mode: time to redraw. Raised on the main thread.</summary>
    public static event Action? Tick;

    internal static void Set(bool active)
    {
        IsActive = active;
        MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke(active));
    }

    internal static void Update() => MainThread.BeginInvokeOnMainThread(() => Tick?.Invoke());
}
