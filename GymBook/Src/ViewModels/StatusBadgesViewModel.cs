using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The badges at the far right of each tab's title (see <see cref="Views.StatusBadges"/>): tonight's bedtime (tap for it
/// in full, and its settings), kept up to date each minute and when its settings change. One for the whole app, shared by
/// every tab's badges. The connection pill beside it keeps itself up to date.
/// </summary>
public partial class StatusBadgesViewModel : ObservableObject
{
    readonly BedtimeService _bedtime;
    readonly DialogService _dialogs;
    IDispatcherTimer? _timer;

    public StatusBadgesViewModel(BedtimeService bedtime, DialogService dialogs)
    {
        (_bedtime, _dialogs) = (bedtime, dialogs);
        bedtime.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(ShowBedtime);
        ShowBedtime();
    }

    [ObservableProperty] string bedtimeTime = "";
    [ObservableProperty] Color bedtimeColor = Color.FromArgb("#7C5CFF");

    /// <summary>Bedtime now and each minute from then on (its colour turns as it nears), once a tab's badges show.</summary>
    public void Start()
    {
        ShowBedtime();
        if (_timer != null || Application.Current?.Dispatcher.CreateTimer() is not { } timer)
            return;
        _timer = timer;
        timer.Interval = TimeSpan.FromMinutes(1);
        timer.Tick += (_, _) => ShowBedtime();
        timer.Start();
    }

    void ShowBedtime()
    {
        var now = DateTime.Now;
        var plan = _bedtime.Plan(now);
        BedtimeTime = BedtimeService.Time(plan.Bedtime);
        BedtimeColor = BedtimeService.ColorFor(plan, now);
    }

    [RelayCommand]
    Task OpenBedtime() => BedtimeSettingsViewModel.Open(_bedtime, _dialogs);
}
