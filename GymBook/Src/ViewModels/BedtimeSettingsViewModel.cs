using CommunityToolkit.Mvvm.ComponentModel;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// Bedtime's settings, from the Workout tab's bedtime card: the reminders and how long before bed the first comes, the
/// sleep aimed for, whether the phone's alarm counts (Android), and the wake-up time otherwise. Each applies at once.
/// </summary>
public partial class BedtimeSettingsViewModel : ObservableObject
{
    readonly BedtimeService _bedtime;
    readonly DialogService _dialogs;
    readonly bool _loading;

    static readonly int[] Leads = [0, 15, 30, 45, 60, 90];
    static readonly int[] Goals = [.. Enumerable.Range(0, 11).Select(i => 6 * 60 + i * 30)];

    public BedtimeSettingsViewModel(BedtimeService bedtime, DialogService dialogs)
    {
        (_bedtime, _dialogs) = (bedtime, dialogs);
        _loading = true;
        remindersOn = bedtime.RemindersOn;
        leadIndex = Math.Max(0, Array.IndexOf(Leads, bedtime.ReminderLeadMinutes));
        goalIndex = Math.Max(0, Array.IndexOf(Goals, Goals.MinBy(g => Math.Abs(g - bedtime.SleepGoalMinutes))));
        useAlarm = bedtime.UseAlarm;
        wakeTime = bedtime.WakeTime;
        _loading = false;
        ShowPreview();
    }

    /// <summary>Opens the sheet: from the top bar's bedtime pill, the Workout tab's bedtime card and the Profile tab.</summary>
    public static Task Open(BedtimeService bedtime, DialogService dialogs) =>
        Shell.Current.Navigation.PushModalAsync(new Views.BedtimeSettingsPage(new BedtimeSettingsViewModel(bedtime, dialogs)), false);

    public List<string> LeadNames { get; } = [.. Leads.Select(m => m == 0 ? "Only at bedtime" : $"{BedtimeService.Span(TimeSpan.FromMinutes(m))} before")];
    public List<string> GoalNames { get; } = [.. Goals.Select(m => BedtimeService.Span(TimeSpan.FromMinutes(m)))];

    /// <summary>The phone's alarm can count (Android tells apps about it; iOS and Windows don't).</summary>
    public bool CanUseAlarm => _bedtime.CanReadAlarm;

    [ObservableProperty] bool remindersOn;
    [ObservableProperty] int leadIndex;
    [ObservableProperty] int goalIndex;
    [ObservableProperty] bool useAlarm;
    [ObservableProperty] TimeSpan wakeTime;
    // Bedtime in full, above the settings, following them as they change.
    [ObservableProperty] string bedtimeTime = "";
    [ObservableProperty] string headline = "";
    [ObservableProperty] string detail = "";
    [ObservableProperty] double progress;
    [ObservableProperty] Color color = Color.FromArgb("#7C5CFF");
    [ObservableProperty] string reminderText = "";

    public string WakeCaption => CanUseAlarm ? "When there's no alarm in the next day" : "Bedtime counts back from it";

    async partial void OnRemindersOnChanged(bool value)
    {
        if (_loading)
            return;
        // Turned on: the system asks first where it does; refused, they stay off.
        if (value && !await _bedtime.RequestPermissionAsync())
        {
            RemindersOn = false;
            await _dialogs.Alert("Notifications are off", "Allow Gym Book's notifications in your phone's settings to get bedtime reminders.");
            return;
        }
        _bedtime.RemindersOn = value;
        ShowPreview();
    }

    partial void OnLeadIndexChanged(int value)
    {
        if (_loading || value < 0)
            return;
        _bedtime.ReminderLeadMinutes = Leads[value];
        ShowPreview();
    }

    partial void OnGoalIndexChanged(int value)
    {
        if (_loading || value < 0)
            return;
        _bedtime.SleepGoalMinutes = Goals[value];
        ShowPreview();
    }

    partial void OnUseAlarmChanged(bool value)
    {
        if (_loading)
            return;
        _bedtime.UseAlarm = value;
        ShowPreview();
    }

    partial void OnWakeTimeChanged(TimeSpan value)
    {
        if (_loading)
            return;
        _bedtime.WakeTime = value;
        ShowPreview();
    }

    /// <summary>
    /// Tonight's bedtime in full: "Bed in 2 h 15 min", "8 h of sleep before your 6:30 alarm", the day's progress to it (in
    /// the Workout tab's colours) and when the reminders come.
    /// </summary>
    void ShowPreview()
    {
        var now = DateTime.Now;
        var plan = _bedtime.Plan(now);
        BedtimeTime = BedtimeService.Time(plan.Bedtime);
        (Headline, Detail) = _bedtime.Describe(plan, now);
        Progress = plan.Progress(now);
        Color = BedtimeService.ColorFor(plan, now);
        ReminderText = !_bedtime.RemindersOn ? "Reminders are off"
            : _bedtime.ReminderLeadMinutes > 0
                ? $"Reminders at {BedtimeService.Time(plan.Bedtime.AddMinutes(-_bedtime.ReminderLeadMinutes))} and {BedtimeService.Time(plan.Bedtime)}"
                : $"Reminder at {BedtimeService.Time(plan.Bedtime)}";
    }
}
