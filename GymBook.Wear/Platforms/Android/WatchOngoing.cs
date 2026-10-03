using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Wear.Ongoing;
using GymBook.Contracts;
using GymBook.Services;

namespace GymBook.Wear;

/// <summary>
/// The Ongoing Activity during a workout: Gym Book's icon on the watch face (and in the recents/launcher), with the
/// workout's name and running time, that reopens the app straight into it. For the watch's own workout and for one
/// running on the phone; gone once neither is.
/// </summary>
public static class WatchOngoing
{
    const string Channel = "workout";
    const int NotificationId = 1;

    static string? _shown;

    /// <summary>Shows, updates or removes it from the workouts in progress: the watch's own, else the phone's.</summary>
    public static void Refresh(WearWorkout phone)
    {
        try
        {
            var services = IPlatformApplication.Current?.Services;
            var own = services?.GetService<WorkoutService>()?.Active;
            // Not an out-of-date copy of the phone's (see WatchOwnership.IsWatchs).
            if (own != null && WatchOwnership.IsWatchs(own, phone, services?.GetService<PhoneLink>()?.IsConnected == true))
                Show(own.Name, new DateTimeOffset(own.StartedAt), "on your watch");
            else if (phone.IsActive)
                Show(phone.Name, phone.StartedAt, "on your phone");
            else
                Clear();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"GymBook.Wear: ongoing activity failed: {e.Message}");
        }
    }

    static void Show(string name, DateTimeOffset startedAt, string where)
    {
        var key = $"{name}|{startedAt:O}|{where}";
        if (key == _shown)
            return;
        var context = Android.App.Application.Context;
        EnsureChannel(context);

        // Reopens the app, which goes straight into the workout in progress.
        var open = new Intent(context, typeof(MainActivity)).AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        var touch = PendingIntent.GetActivity(context, 0, open, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var notification = new NotificationCompat.Builder(context, Channel)
            .SetSmallIcon(Resource.Drawable.ic_stat_workout)
            .SetContentTitle(name)
            .SetContentText($"Workout {where}")
            .SetCategory(NotificationCompat.CategoryWorkout)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetSilent(true)
            .SetContentIntent(touch);

        // The running time, counted by the system (elapsedRealtime-based), so it stays right without the app.
        var timeZero = SystemClock.ElapsedRealtime() - (long)(DateTimeOffset.Now - startedAt).TotalMilliseconds;
        var status = new Status.Builder()
            .AddTemplate("#name# · #time#")
            .AddPart("name", new Status.TextPart(name))
            .AddPart("time", new Status.StopwatchPart(timeZero))
            .Build();
        new OngoingActivity.Builder(context, NotificationId, notification)
            .SetStaticIcon(Resource.Drawable.ic_stat_workout)
            .SetTouchIntent(touch)
            .SetStatus(status)
            .SetTitle(name)
            .Build()
            .Apply(context);

        NotificationManagerCompat.From(context).Notify(NotificationId, notification.Build());
        _shown = key;
    }

    static void Clear()
    {
        if (_shown == null)
            return;
        NotificationManagerCompat.From(Android.App.Application.Context).Cancel(NotificationId);
        _shown = null;
    }

    static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O || context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            return;
        manager.CreateNotificationChannel(new NotificationChannel(Channel, "Workout in progress", NotificationImportance.Low)
        {
            Description = "Gym Book on the watch face while you're working out.",
        });
    }
}
