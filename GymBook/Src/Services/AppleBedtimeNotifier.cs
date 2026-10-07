#if IOS || MACCATALYST
using Foundation;
using UserNotifications;

namespace GymBook.Services;

/// <summary>The bedtime reminders as local notifications at their times (iPhone, iPad and Mac), with the app closed too.</summary>
public class AppleBedtimeNotifier : IBedtimeNotifier
{
    const string Prefix = "bedtime-";

    public async Task<bool> RequestPermissionAsync()
    {
        var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        return granted;
    }

    public void Schedule(IReadOnlyList<BedtimeReminder> reminders)
    {
        var center = UNUserNotificationCenter.Current;
        center.GetPendingNotificationRequests(pending =>
        {
            center.RemovePendingNotificationRequests([.. pending.Select(p => p.Identifier).Where(id => id.StartsWith(Prefix))]);
            foreach (var r in reminders)
            {
                var content = new UNMutableNotificationContent { Title = r.Title, Body = r.Text, Sound = UNNotificationSound.Default };
                var when = new NSDateComponents { Year = r.At.Year, Month = r.At.Month, Day = r.At.Day, Hour = r.At.Hour, Minute = r.At.Minute };
                var request = UNNotificationRequest.FromIdentifier($"{Prefix}{r.Id}", content, UNCalendarNotificationTrigger.CreateTrigger(when, false));
                center.AddNotificationRequest(request, _ => { });
            }
        });
    }
}
#endif
