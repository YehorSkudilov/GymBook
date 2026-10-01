using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using GymBook.Services;

namespace GymBook;

/// <summary>
/// The workout in progress as a notification: its name, the exercise and set it's on, and while resting a countdown
/// (kept by the system, so it stays right with the app in the background) over a bar of the rest left. Tapping it opens
/// the workout. An alert when rest runs out while the app isn't on screen. Phone only: the watch shows its own.
/// </summary>
public class WorkoutNotifier : IWorkoutNotifier
{
    const string WorkoutChannel = "workout";
    const string RestChannel = "rest";
    const int WorkoutId = 1, RestOverId = 2;
    const string OpenWorkoutExtra = "gymbook.open_workout";

    readonly WorkoutService _workouts;
    string? _shown;

    public WorkoutNotifier(DataStore store, WorkoutService workouts)
    {
        _workouts = workouts;
        // A notification can outlive the app (killed mid-workout): gone at start if there's no workout any more.
        if (_workouts.Active == null)
        {
            var manager = NotificationManagerCompat.From(Android.App.Application.Context);
            manager.Cancel(WorkoutId);
            manager.Cancel(RestOverId);
        }
        // Finished or discarded (here, or synced from another device): it goes. Started without the workout page
        // having shown it yet (e.g. the app restarted mid-workout): a plain version until it does.
        store.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_workouts.Active is not { } active)
                Clear();
            else if (_shown == null)
                Show(new WorkoutStatus(active.Name, active.StartedAt, "Workout in progress", "", null, 0));
        });
    }

    /// <summary>Whether the app is on screen (from MainActivity), so rest alerts only come when it isn't.</summary>
    public static bool IsAppVisible { get; set; }

    /// <summary>Whether <paramref name="intent"/> came from tapping the notification.</summary>
    public static bool IsOpenWorkout(Intent? intent) => intent?.GetBooleanExtra(OpenWorkoutExtra, false) == true;

    public void Show(WorkoutStatus status)
    {
        var resting = status.RestEndsAt is { } restEnd && restEnd > DateTime.Now;
        // Redrawn when what it says changes, and while resting every few seconds for the bar.
        var restLeft = resting ? (int)(status.RestEndsAt!.Value - DateTime.Now).TotalSeconds : 0;
        var key = $"{status.Name}|{status.Detail}|{status.Progress}|{status.RestEndsAt:O}|{restLeft / 3}";
        if (key == _shown)
            return;
        var context = Android.App.Application.Context;
        EnsureChannels(context);

        var builder = new NotificationCompat.Builder(context, WorkoutChannel)
            .SetSmallIcon(Resource.Drawable.ic_stat_workout)
            .SetContentTitle(status.Name)
            .SetCategory(NotificationCompat.CategoryWorkout)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetSilent(true)
            // The watch app shows the workout itself; no copy of this notification on the watch.
            .SetLocalOnly(true)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetContentIntent(OpenIntent(context))
            .SetShowWhen(true)
            .SetUsesChronometer(true);
        if (status.Progress.Length > 0)
            builder.SetSubText(status.Progress);

        if (resting)
        {
            var end = status.RestEndsAt!.Value;
            builder.SetContentText($"Rest · next: {status.Detail}")
                .SetWhen(new DateTimeOffset(end).ToUnixTimeMilliseconds())
                .SetChronometerCountDown(true)
                .SetProgress(Math.Max(1, status.RestSeconds), Math.Clamp(restLeft, 0, Math.Max(1, status.RestSeconds)), false);
        }
        else
        {
            builder.SetContentText(status.Detail)
                .SetWhen(new DateTimeOffset(status.StartedAt).ToUnixTimeMilliseconds())
                .SetChronometerCountDown(false);
        }
        Notify(context, WorkoutId, builder.Build());
        _shown = key;
    }

    public void RestOver(string next)
    {
        if (IsAppVisible)
            return;
        var context = Android.App.Application.Context;
        EnsureChannels(context);
        var notification = new NotificationCompat.Builder(context, RestChannel)
            .SetSmallIcon(Resource.Drawable.ic_stat_workout)
            .SetContentTitle("Rest's over")
            .SetContentText($"Time for {next}")
            .SetCategory(NotificationCompat.CategoryReminder)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true)
            .SetLocalOnly(true)
            .SetTimeoutAfter(60_000)
            .SetContentIntent(OpenIntent(context))
            .Build();
        Notify(context, RestOverId, notification);
    }

    public void Clear()
    {
        if (_shown == null)
            return;
        var manager = NotificationManagerCompat.From(Android.App.Application.Context);
        manager.Cancel(WorkoutId);
        manager.Cancel(RestOverId);
        _shown = null;
    }

    static PendingIntent OpenIntent(Context context)
    {
        var intent = new Intent(context, typeof(MainActivity))
            .AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop)
            .PutExtra(OpenWorkoutExtra, true);
        return PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
    }

    static void Notify(Context context, int id, Notification notification)
    {
        try
        {
            NotificationManagerCompat.From(context).Notify(id, notification);
        }
        catch (Java.Lang.SecurityException)
        {
            // Notifications not allowed (Android 13+ asks): the workout works the same without them.
        }
    }

    static void EnsureChannels(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O || context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            return;
        manager.CreateNotificationChannel(new NotificationChannel(WorkoutChannel, "Workout in progress", NotificationImportance.Low)
        {
            Description = "The workout you're doing and its rest timer.",
        });
        manager.CreateNotificationChannel(new NotificationChannel(RestChannel, "Rest timer", NotificationImportance.High)
        {
            Description = "When rest is over and it's time for the next set.",
        });
    }
}
