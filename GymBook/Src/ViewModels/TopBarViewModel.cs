using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The slim bar over every tab (see <see cref="Views.TopBarView"/>): tonight's bedtime (tap for it in full, and its
/// settings) and whether the app's online. Kept up to date for as long as the app runs: the connection as it changes,
/// bedtime each minute and when its settings change.
/// </summary>
public partial class TopBarViewModel : ObservableObject
{
    readonly BedtimeService _bedtime;
    readonly DialogService _dialogs;
    IDispatcherTimer? _timer;

    public TopBarViewModel(BedtimeService bedtime, DialogService dialogs)
    {
        (_bedtime, _dialogs) = (bedtime, dialogs);
        Connectivity.Current.ConnectivityChanged += (_, _) => MainThread.BeginInvokeOnMainThread(ShowConnection);
        bedtime.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(ShowBedtime);
        ShowConnection();
        ShowBedtime();
    }

    [ObservableProperty] string connectionText = "";
    [ObservableProperty] Color connectionColor = Colors.Transparent;
    [ObservableProperty] string bedtimeTime = "";
    [ObservableProperty] Color bedtimeColor = Color.FromArgb("#7C5CFF");

    /// <summary>Bedtime each minute from now on (its colour turns as it nears), once the tabs are up.</summary>
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

    void ShowConnection()
    {
        var online = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        ConnectionText = online ? "Online" : "Offline";
        ConnectionColor = online ? Color.FromArgb("#2ED47A") : Color.FromArgb("#FFB020");
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
