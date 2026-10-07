using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using GymBook.Services;

namespace GymBook;

/// <summary>The next alarm set in any clock app, which Android tells every app (no permission needed).</summary>
public class AndroidAlarmClock : IAlarmClock
{
    public bool CanRead => true;

    public DateTime? NextAlarm()
    {
        var manager = Android.App.Application.Context.GetSystemService(Context.AlarmService) as AlarmManager;
        return manager?.NextAlarmClock is { } next ? DateTimeOffset.FromUnixTimeMilliseconds(next.TriggerTime).LocalDateTime : null;
    }
}

/// <summary>
/// The bedtime reminders as alarms that post a notification (see <see cref="BedtimeReminderReceiver"/>), with the app
/// closed too. Inexact, as the system allows without asking: they may come a few minutes late while the phone dozes.
/// </summary>
public class BedtimeNotifier : IBedtimeNotifier
{
    internal const string Channel = "bedtime";
    internal const string Action = "com.yehorskudilov.gymbook.BEDTIME_REMINDER";
    internal const string KindExtra = "kind", AtExtra = "at", TitleExtra = "title", TextExtra = "text";
    // The request codes the reminders use (BedtimeService numbers them 100 and up), so the old ones can be cancelled.
    const int FirstId = 100, LastId = 140;

    public async Task<bool> RequestPermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            return true;
        var status = await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<Permissions.PostNotifications>());
        return status == PermissionStatus.Granted;
    }

    public void Schedule(IReadOnlyList<BedtimeReminder> reminders)
    {
        var context = Android.App.Application.Context;
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager manager)
            return;
        for (var id = FirstId; id <= LastId; id++)
            if (PendingIntent.GetBroadcast(context, id, new Intent(context, typeof(BedtimeReminderReceiver)).SetAction(Action),
                    PendingIntentFlags.Immutable | PendingIntentFlags.NoCreate) is { } old)
            {
                manager.Cancel(old);
                old.Cancel();
            }
        foreach (var r in reminders)
        {
            var intent = new Intent(context, typeof(BedtimeReminderReceiver)).SetAction(Action)
                .PutExtra(KindExtra, (int)r.Kind)
                .PutExtra(AtExtra, r.At.Ticks)
                .PutExtra(TitleExtra, r.Title)
                .PutExtra(TextExtra, r.Text);
            var pending = PendingIntent.GetBroadcast(context, r.Id, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
            manager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, new DateTimeOffset(r.At).ToUnixTimeMilliseconds(), pending);
        }
    }

    internal static void Post(Context context, int id, string title, string text)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O && context.GetSystemService(Context.NotificationService) is NotificationManager manager)
            manager.CreateNotificationChannel(new NotificationChannel(Channel, "Bedtime", NotificationImportance.Default)
            {
                Description = "A reminder before bed and at bedtime, for the sleep you aim for.",
            });
        var open = PendingIntent.GetActivity(context, 0, new Intent(context, typeof(MainActivity)).AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop),
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        var notification = new NotificationCompat.Builder(context, Channel)
            .SetSmallIcon(Resource.Drawable.ic_stat_workout)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(text))
            .SetCategory(NotificationCompat.CategoryReminder)
            .SetAutoCancel(true)
            .SetContentIntent(open)
            .Build();
        try
        {
            NotificationManagerCompat.From(context).Notify(id, notification);
        }
        catch (Java.Lang.SecurityException)
        {
            // Notifications not allowed: nothing to show.
        }
    }
}

/// <summary>
/// A bedtime reminder is due. Checked against bedtime as it is now first: an alarm set or moved since it was scheduled may
/// have put bedtime later, and then it's scheduled again for then instead of coming early. Then the week's reminders are
/// scheduled afresh, from the alarm as it is now.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class BedtimeReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent?.Action != BedtimeNotifier.Action
            || IPlatformApplication.Current?.Services.GetService<BedtimeService>() is not { } bedtime || !bedtime.RemindersOn)
            return;
        var now = DateTime.Now;
        var kind = (BedtimeReminderKind)intent.GetIntExtra(BedtimeNotifier.KindExtra, 0);
        var plan = bedtime.Plan(now);
        var due = kind == BedtimeReminderKind.WindDown ? plan.Bedtime.AddMinutes(-bedtime.ReminderLeadMinutes) : plan.Bedtime;
        if (due - now > TimeSpan.FromMinutes(10))
        {
            bedtime.Reschedule();
            return;
        }
        BedtimeNotifier.Post(context, 200 + (int)kind, intent.GetStringExtra(BedtimeNotifier.TitleExtra) ?? "Time for bed",
            intent.GetStringExtra(BedtimeNotifier.TextExtra) ?? "");
        bedtime.Reschedule();
    }
}

/// <summary>Alarms don't survive a restart: the bedtime reminders are scheduled again when the phone starts.</summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BedtimeBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action == Intent.ActionBootCompleted)
            IPlatformApplication.Current?.Services.GetService<BedtimeService>()?.Reschedule();
    }
}
