using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using GymBook.Services;

namespace GymBook;

/// <summary>
/// The workout in progress as the app's one notification: its name, what's being done or comes next, its timer (kept by
/// the system, so it stays right with the app in the background) and a bar: the rest left while resting, otherwise the
/// workout's sets done. When rest runs out while the app isn't on screen, this same notification says so with a sound and
/// a buzz. It can't be put away while the workout's going: swiped off, it comes straight back. Tapping it opens the
/// workout. Phone only: the watch shows its own.
/// </summary>
public class WorkoutNotifier : IWorkoutNotifier
{
    const string WorkoutChannel = "workout";
    const string RestChannel = "rest";
    // RestOverId was a separate "rest's over" notification in earlier versions; only cancelled now, in case one is left.
    const int WorkoutId = 1, RestOverId = 2;
    const string OpenWorkoutExtra = "gymbook.open_workout";
    internal const string DismissedAction = "com.yehorskudilov.gymbook.WORKOUT_NOTIFICATION_DISMISSED";

    readonly WorkoutService _workouts;
    string? _shown;
    WorkoutStatus? _last;

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
                Show(new WorkoutStatus(active.Name, "", WorkoutPhase.InSet, "Workout in progress", active.StartedAt, null, 0));
        });
    }

    /// <summary>Whether the app is on screen (from MainActivity), so rest alerts only come when it isn't.</summary>
    public static bool IsAppVisible { get; set; }

    /// <summary>Whether <paramref name="intent"/> came from tapping the notification.</summary>
    public static bool IsOpenWorkout(Intent? intent) => intent?.GetBooleanExtra(OpenWorkoutExtra, false) == true;

    public void Show(WorkoutStatus status) => Post(status, alert: false);

    public void RestOver(string next)
    {
        if (IsAppVisible || _last is not { } last)
            return;
        // The same notification, now saying the rest is over, posted once with a sound and a buzz.
        Post(last with { Phase = WorkoutPhase.RestOver, Next = next, RestDueAt = last.RestDueAt ?? DateTime.Now }, alert: true);
    }

    /// <summary>Swiped away while the workout's still going: put it straight back.</summary>
    internal void Restore()
    {
        if (_workouts.Active == null || _last is not { } last)
            return;
        _shown = null;
        Post(last, alert: false);
    }

    void Post(WorkoutStatus status, bool alert)
    {
        _last = status;
        var now = DateTime.Now;
        var resting = status.Phase == WorkoutPhase.Resting && status.RestDueAt > now;
        // Redrawn when what it says changes, and while resting every few seconds for the bar.
        var restLeft = resting ? (int)(status.RestDueAt!.Value - now).TotalSeconds : 0;
        var key = $"{status.Name}|{status.Phase}|{status.Next}|{status.Progress}|{status.Since:O}|{status.RestDueAt:O}|{restLeft / 3}|{status.SetsDone}/{status.SetsTotal}";
        if (key == _shown && !alert)
            return;
        var context = Android.App.Application.Context;
        EnsureChannels(context);

        // The rest channel makes a sound; a post on it replaces this notification like any other, and the next quiet
        // update goes back to the workout channel.
        var builder = new NotificationCompat.Builder(context, alert ? RestChannel : WorkoutChannel)
            .SetSmallIcon(Resource.Drawable.ic_stat_workout)
            .SetContentTitle(status.Name)
            .SetCategory(alert ? NotificationCompat.CategoryAlarm : NotificationCompat.CategoryWorkout)
            .SetOngoing(true)
            .SetAutoCancel(false)
            .SetOnlyAlertOnce(!alert)
            .SetSilent(!alert)
            .SetPriority(alert ? NotificationCompat.PriorityHigh : NotificationCompat.PriorityLow)
            // The watch app shows the workout itself; no copy of this notification on the watch.
            .SetLocalOnly(true)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetContentIntent(OpenIntent(context))
            .SetDeleteIntent(DismissedIntent(context))
            .SetShowWhen(true)
            .SetUsesChronometer(true);
        if (alert)
            builder.SetDefaults(NotificationCompat.DefaultSound | NotificationCompat.DefaultVibrate);
        if (status.Progress.Length > 0)
            builder.SetSubText(status.Progress);

        // The line under the title, the timer beside it, and the bar.
        switch (status.Phase)
        {
            case WorkoutPhase.Resting when resting:
                builder.SetContentText($"Rest · next: {status.Next}")
                    .SetWhen(new DateTimeOffset(status.RestDueAt!.Value).ToUnixTimeMilliseconds())
                    .SetChronometerCountDown(true)
                    .SetProgress(Math.Max(1, status.RestSeconds), Math.Clamp(restLeft, 0, Math.Max(1, status.RestSeconds)), false);
                break;
            case WorkoutPhase.Resting or WorkoutPhase.RestOver:
                // Counting up how far over the rest is; the bar is the workout's.
                builder.SetContentText($"Rest over · next: {status.Next}")
                    .SetWhen(new DateTimeOffset(status.RestDueAt ?? status.Since).ToUnixTimeMilliseconds())
                    .SetChronometerCountDown(false);
                SetsBar(builder, status);
                break;
            case WorkoutPhase.Done:
                builder.SetContentText("Every set done · tap to finish")
                    .SetWhen(new DateTimeOffset(status.Since).ToUnixTimeMilliseconds())
                    .SetChronometerCountDown(false);
                SetsBar(builder, status);
                break;
            default:
                // The set being done, timed since it started.
                builder.SetContentText($"Now: {status.Next}")
                    .SetWhen(new DateTimeOffset(status.Since).ToUnixTimeMilliseconds())
                    .SetChronometerCountDown(false);
                SetsBar(builder, status);
                break;
        }
        Notify(context, WorkoutId, builder.Build());
        // After an alert, the next update is drawn again (quietly, on the workout channel).
        _shown = alert ? null : key;
    }

    /// <summary>The workout's working sets done, as the bar (none before there are sets).</summary>
    static void SetsBar(NotificationCompat.Builder builder, WorkoutStatus status)
    {
        if (status.SetsTotal > 0)
            builder.SetProgress(status.SetsTotal, Math.Clamp(status.SetsDone, 0, status.SetsTotal), false);
    }

    public void Clear()
    {
        _last = null;
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

    static PendingIntent DismissedIntent(Context context)
    {
        var intent = new Intent(context, typeof(WorkoutNotificationDismissedReceiver)).SetAction(DismissedAction);
        return PendingIntent.GetBroadcast(context, 1, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
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
            Description = "The workout you're doing, its timer and progress.",
        });
        manager.CreateNotificationChannel(new NotificationChannel(RestChannel, "Rest timer", NotificationImportance.High)
        {
            Description = "When rest is over and it's time for the next set.",
        });
    }
}

/// <summary>
/// The workout notification was swiped away (Android 14+ lets that happen even to ongoing ones): it comes back while the
/// workout is still going.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class WorkoutNotificationDismissedReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != WorkoutNotifier.DismissedAction)
            return;
        if (IPlatformApplication.Current?.Services.GetService<IWorkoutNotifier>() is WorkoutNotifier notifier)
            MainThread.BeginInvokeOnMainThread(notifier.Restore);
    }
}
