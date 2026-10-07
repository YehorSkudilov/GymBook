using System.Globalization;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>The time of the next alarm set in the phone's clock app (any app's), where the system tells apps.</summary>
public interface IAlarmClock
{
    /// <summary>Whether this platform tells apps about alarms at all (Android does; iOS and Windows don't).</summary>
    bool CanRead { get; }

    /// <summary>When the next alarm rings; null without one (or where it can't be read).</summary>
    DateTime? NextAlarm();
}

public class NoAlarmClock : IAlarmClock
{
    public bool CanRead => false;
    public DateTime? NextAlarm() => null;
}

public enum BedtimeReminderKind { WindDown, Bedtime }

/// <summary>A reminder to post at <paramref name="At"/>: a while before bedtime, or at it.</summary>
public record BedtimeReminder(int Id, BedtimeReminderKind Kind, DateTime At, string Title, string Text);

/// <summary>Posts the bedtime reminders as the system's own notifications, at their times, with the app closed too.</summary>
public interface IBedtimeNotifier
{
    /// <summary>Asks to post notifications (where the system asks); whether they're allowed.</summary>
    Task<bool> RequestPermissionAsync();

    /// <summary>Replaces every reminder scheduled before with these; none cancels them all.</summary>
    void Schedule(IReadOnlyList<BedtimeReminder> reminders);
}

public class NoBedtimeNotifier : IBedtimeNotifier
{
    public Task<bool> RequestPermissionAsync() => Task.FromResult(false);

    public void Schedule(IReadOnlyList<BedtimeReminder> reminders)
    {
    }
}

/// <summary>What a night's wake-up comes from: the phone's alarm, when the user usually wakes (from recorded sleep), or the time set.</summary>
public enum WakeSource { Alarm, Learned, Setting }

/// <summary>
/// The night ahead: when to be in bed for the sleep goal before <see cref="WakeAt"/> (the phone's alarm, or the wake-up
/// time set), and when the day began (a day before that wake-up), which the progress to bedtime counts from.
/// </summary>
public record BedtimePlan(DateTime Bedtime, DateTime WakeAt, WakeSource Source, DateTime DayStart)
{
    /// <summary>How much of the day to bedtime has gone, 0 on waking to 1 at bedtime (and after).</summary>
    public double Progress(DateTime now) => Math.Clamp((now - DayStart) / (Bedtime - DayStart), 0, 1);
}

/// <summary>
/// Bedtime: worked back from when the user wakes (the next alarm on Android when it rings within a day, else when they
/// usually wake on that kind of day by their recorded sleep, else the wake-up time set) by the sleep they aim for, shown on the Workout tab with the day's progress towards it, and reminded of with
/// notifications a while before and at it. The settings are this device's own (the alarm is this phone's), kept in its
/// preferences.
/// </summary>
public class BedtimeService(IAlarmClock alarm, IBedtimeNotifier notifier, RecoveryService recovery)
{
    const string Prefix = "bedtime.";

    /// <summary>The reminders and the wake-up time or sleep goal changed: the card and the reminders follow.</summary>
    public event EventHandler? Changed;

    public bool CanReadAlarm => alarm.CanRead;

    /// <summary>Asks to post notifications, before reminders are turned on; whether they're allowed.</summary>
    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            return await notifier.RequestPermissionAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Notifications before bed and at bedtime.</summary>
    public bool RemindersOn
    {
        get => Preferences.Default.Get(Prefix + "reminders", false);
        set => Set("reminders", value);
    }

    /// <summary>How long a night the user aims for.</summary>
    public int SleepGoalMinutes
    {
        get => Math.Clamp(Preferences.Default.Get(Prefix + "goal", 8 * 60), 5 * 60, 11 * 60);
        set => Set("goal", Math.Clamp(value, 5 * 60, 11 * 60));
    }

    /// <summary>When the user wakes without an alarm to go by.</summary>
    public TimeSpan WakeTime
    {
        get => TimeSpan.FromMinutes(Math.Clamp(Preferences.Default.Get(Prefix + "wake", 7 * 60), 0, 24 * 60 - 1));
        set => Set("wake", (int)value.TotalMinutes % (24 * 60));
    }

    /// <summary>Bedtime counts back from the phone's next alarm, when there's one within a day.</summary>
    public bool UseAlarm
    {
        get => Preferences.Default.Get(Prefix + "alarm", true);
        set => Set("alarm", value);
    }

    /// <summary>
    /// Without an alarm, bedtime counts back from when the user usually wakes on that kind of day (see
    /// <see cref="LearnedWake"/>) once there's enough sleep recorded; until then, and with this off, from <see cref="WakeTime"/>.
    /// </summary>
    public bool LearnWake
    {
        get => Preferences.Default.Get(Prefix + "learn", true);
        set => Set("learn", value);
    }

    // ---- Learning when the user wakes ----

    /// <summary>How far back the usual wake-up time looks, and how many nights of that kind of day it needs.</summary>
    const int LearnDays = 21, LearnNights = 3;

    /// <summary>
    /// When each night's sleep ended, from the health apps as they're read (HealthSyncService): kept on this device, the
    /// last couple of months, as minutes after midnight by the day woken up on.
    /// </summary>
    public void RecordWakeTimes(IEnumerable<(DateTime Day, DateTime WokeAt)> nights)
    {
        var known = WakeTimes();
        var changed = false;
        foreach (var (day, wokeAt) in nights)
        {
            var minute = (int)wokeAt.TimeOfDay.TotalMinutes;
            if (known.GetValueOrDefault(day.Date) != minute)
                (known[day.Date], changed) = (minute, true);
        }
        if (!changed)
            return;
        var cutoff = DateTime.Today.AddDays(-60);
        Preferences.Default.Set(Prefix + "woke", string.Join(";", known.Where(k => k.Key >= cutoff).OrderBy(k => k.Key)
            .Select(k => $"{k.Key:yyyy-MM-dd}={k.Value}")));
        Reschedule();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    Dictionary<DateTime, int> WakeTimes()
    {
        var known = new Dictionary<DateTime, int>();
        foreach (var part in Preferences.Default.Get(Prefix + "woke", "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            if (part.Split('=') is [var d, var m]
                && DateTime.TryParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                && int.TryParse(m, CultureInfo.InvariantCulture, out var minute))
                known[day] = minute;
        return known;
    }

    static bool IsWeekend(DateTime day) => day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>
    /// When the user usually wakes on a day like <paramref name="day"/> (weekdays and weekends apart): the middle of the
    /// last three weeks' wake-ups on those days, to the nearest 5 minutes, leaving out naps and night shifts (waking
    /// outside 3:00 to 13:00). Null with fewer than three nights of that kind to go by.
    /// </summary>
    public TimeSpan? LearnedWake(DateTime day)
    {
        var weekend = IsWeekend(day);
        var minutes = WakeTimes().Where(w => w.Key > DateTime.Today.AddDays(-LearnDays) && IsWeekend(w.Key) == weekend && w.Value is >= 3 * 60 and <= 13 * 60)
            .Select(w => w.Value).Order().ToList();
        if (minutes.Count < LearnNights)
            return null;
        var middle = minutes.Count % 2 == 1 ? minutes[minutes.Count / 2] : (minutes[minutes.Count / 2 - 1] + minutes[minutes.Count / 2]) / 2;
        return TimeSpan.FromMinutes(Math.Round(middle / 5.0) * 5);
    }

    /// <summary>When the user wakes on <paramref name="day"/> without an alarm: the usual time learned, else the one set.</summary>
    (TimeSpan Time, WakeSource Source) WakeOn(DateTime day) =>
        LearnWake && LearnedWake(day) is { } learned ? (learned, WakeSource.Learned) : (WakeTime, WakeSource.Setting);

    /// <summary>How long before bedtime the first reminder comes; 0 for only the one at bedtime.</summary>
    public int ReminderLeadMinutes
    {
        get => Math.Clamp(Preferences.Default.Get(Prefix + "lead", 30), 0, 120);
        set => Set("lead", Math.Clamp(value, 0, 120));
    }

    void Set<T>(string key, T value)
    {
        switch (value)
        {
            case bool b: Preferences.Default.Set(Prefix + key, b); break;
            case int i: Preferences.Default.Set(Prefix + key, i); break;
        }
        Reschedule();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Whether the night ending today is behind the user: the health apps recorded them waking today, before now.</summary>
    bool SleptToday(DateTime now) => WakeTimes().TryGetValue(now.Date, out var minute) && now.Date.AddMinutes(minute) <= now;

    /// <summary>
    /// The night ahead of <paramref name="now"/> (see <see cref="BedtimePlan"/>). Once today's sleep is recorded, the night
    /// ahead is tonight's: an alarm still to ring today, or today's wake-up time, is one already slept through or woken early for.
    /// </summary>
    public BedtimePlan Plan(DateTime now)
    {
        var slept = SleptToday(now);
        if (UseAlarm && alarm.NextAlarm() is { } next && next > now && next - now <= TimeSpan.FromHours(24) && !(slept && next.Date == now.Date))
            return For(next, WakeSource.Alarm);
        // The next wake-up after now: today's if it's still ahead and not slept yet, else tomorrow's (each by its own kind of day).
        var (time, source) = WakeOn(now.Date);
        if (!slept && now.Date + time > now)
            return For(now.Date + time, source);
        var tomorrow = now.Date.AddDays(1);
        (time, source) = WakeOn(tomorrow);
        return For(tomorrow + time, source);
    }

    /// <summary>The night before <paramref name="day"/>'s wake-up, without an alarm to go by.</summary>
    BedtimePlan ForDay(DateTime day)
    {
        var (time, source) = WakeOn(day);
        return For(day.Date + time, source);
    }

    BedtimePlan For(DateTime wake, WakeSource source) => new(wake.AddMinutes(-SleepGoalMinutes), wake, source, wake.AddDays(-1));

    /// <summary>"Bed in 2 h 15 min" and "8 h of sleep before your 6:30 alarm", or how far past bedtime it is.</summary>
    public (string Headline, string Detail) Describe(BedtimePlan plan, DateTime now)
    {
        var before = Before(plan);
        return now < plan.Bedtime
            ? ($"Bed in {Span(plan.Bedtime - now)}", $"{Span(TimeSpan.FromMinutes(SleepGoalMinutes))} of sleep before {before}")
            : ($"Past bedtime by {Span(now - plan.Bedtime)}", $"{Span(plan.WakeAt - now)} left before {before}");
    }

    /// <summary>Violet through the day, amber in the hour before bedtime, red once it's past.</summary>
    public static Color ColorFor(BedtimePlan plan, DateTime now) =>
        now >= plan.Bedtime ? Color.FromArgb("#FF4D5E") : plan.Bedtime - now <= TimeSpan.FromHours(1) ? Color.FromArgb("#FFB020") : Color.FromArgb("#7C5CFF");

    /// <summary>"your 6:30 alarm", "waking around 6:45, as you usually do" or "waking at 7:00".</summary>
    public static string Before(BedtimePlan plan) => plan.Source switch
    {
        WakeSource.Alarm => $"your {Time(plan.WakeAt)} alarm",
        WakeSource.Learned => $"waking around {Time(plan.WakeAt)}, as you usually do",
        _ => $"waking at {Time(plan.WakeAt)}",
    };

    public static string Time(DateTime t) => t.ToString("t", CultureInfo.CurrentCulture);

    /// <summary>"2 h 15 min", "8 h" or "40 min".</summary>
    public static string Span(TimeSpan span)
    {
        var minutes = Math.Max(0, (int)Math.Round(span.TotalMinutes));
        var (h, m) = (minutes / 60, minutes % 60);
        return h == 0 ? $"{m} min" : m == 0 ? $"{h} h" : $"{h} h {m} min";
    }

    /// <summary>
    /// The reminders for the week ahead, replacing the ones scheduled before (none while they're off): tonight's from the
    /// alarm or the wake-up time, the nights after from the wake-up time (an alarm isn't known that far ahead). Called
    /// when the app starts, when the Workout tab shows (an alarm may have been set since) and when a setting changes.
    /// </summary>
    public void Reschedule()
    {
        try
        {
            notifier.Schedule(RemindersOn ? Reminders(DateTime.Now) : []);
        }
        catch (Exception)
        {
            // Reminders are a nicety: the app works the same without them.
        }
    }

    List<BedtimeReminder> Reminders(DateTime now)
    {
        var tonight = Plan(now);
        var list = new List<BedtimeReminder>();
        var goal = Span(TimeSpan.FromMinutes(SleepGoalMinutes));
        for (var night = 0; night < 7; night++)
        {
            var plan = night == 0 ? tonight : ForDay(tonight.WakeAt.Date.AddDays(night));
            var lead = ReminderLeadMinutes;
            if (lead > 0 && plan.Bedtime.AddMinutes(-lead) > now)
                list.Add(new(100 + night * 2, BedtimeReminderKind.WindDown, plan.Bedtime.AddMinutes(-lead),
                    $"Bed in {Span(TimeSpan.FromMinutes(lead))}", $"Lights out at {Time(plan.Bedtime)} for {goal} before {Before(plan)}."));
            if (plan.Bedtime > now)
                list.Add(new(101 + night * 2, BedtimeReminderKind.Bedtime, plan.Bedtime, "Time for bed",
                    night == 0 ? RecoveryLine(plan, goal) : $"{goal} of sleep before {Before(plan)} keeps recovery on track."));
        }
        return list;
    }

    /// <summary>Tonight's bedtime reminder: the muscles still recovering by then, which a full night helps.</summary>
    string RecoveryLine(BedtimePlan plan, string goal)
    {
        var tired = recovery.Details(plan.Bedtime).Where(d => d.Recovery < RecoveryService.ReadyThreshold)
            .OrderBy(d => d.Recovery).Select(d => d.Muscle.Display()).Take(2).ToList();
        return tired.Count == 0
            ? $"{goal} of sleep before {Before(plan)} keeps recovery on track."
            : $"{string.Join(" and ", tired)} {(tired.Count == 1 ? "is" : "are")} still recovering: {goal} of sleep gets {(tired.Count == 1 ? "it" : "them")} back faster.";
    }
}
