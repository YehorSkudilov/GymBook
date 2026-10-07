using System.Security;
using GymBook.Services;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace GymBook.WinUI;

/// <summary>The bedtime reminders as toasts Windows shows at their times, with the app closed too.</summary>
public class WindowsBedtimeNotifier : IBedtimeNotifier
{
    const string Group = "bedtime";

    public Task<bool> RequestPermissionAsync() => Task.FromResult(true);

    public void Schedule(IReadOnlyList<BedtimeReminder> reminders)
    {
        var notifier = ToastNotificationManager.CreateToastNotifier();
        foreach (var old in notifier.GetScheduledToastNotifications().Where(t => t.Group == Group))
            notifier.RemoveFromSchedule(old);
        foreach (var r in reminders)
        {
            var xml = new XmlDocument();
            xml.LoadXml($"<toast><visual><binding template=\"ToastGeneric\"><text>{SecurityElement.Escape(r.Title)}</text><text>{SecurityElement.Escape(r.Text)}</text></binding></visual></toast>");
            notifier.AddToSchedule(new ScheduledToastNotification(xml, new DateTimeOffset(r.At)) { Group = Group, Tag = $"{r.Id}" });
        }
    }
}
