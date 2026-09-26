using System.Collections.ObjectModel;
using AppSkeleton;
using GymBook.Services;

namespace GymBook.Views;

public enum AppTab { Workout, Plans, Exercises, Progress, Profile }

/// <summary>The app's root: the tabs live in a swipeable CView with a CNavBar underneath. Detail pages still push through Shell.</summary>
public partial class MainPage : ContentPage
{
    const double ActiveBarInset = 70;

    static WeakReference<MainPage>? _instance;

    readonly ObservableCollection<CNavItem> _tabs;
    readonly WorkoutService _workouts;
    CNavItem? _current;
    bool _visible;
    IDispatcherTimer? _activeTimer;

    public MainPage(WorkoutService workouts, HomePage home, PlansPage plans, ExercisesPage exercises, StatsPage stats, ProfilePage profile)
    {
        InitializeComponent();
        _workouts = workouts;

        var color = (Color)Application.Current!.Resources["TextPrimary"];
        _tabs =
        [
            new() { Glyph = "fitness_center", PageName = "Workout", Page = home, Color = color },
            new() { Glyph = "event_note", PageName = "Plans", Page = plans, Color = color },
            new() { Glyph = "format_list_bulleted", PageName = "Exercises", Page = exercises, Color = color },
            new() { Glyph = "insights", PageName = "Progress", Page = stats, Color = color },
            new() { Glyph = "person", PageName = "Profile", Page = profile, Color = color },
        ];

        TabContent.CNavIconItems = _tabs;
        NavBar.LoadPageCommand = new Command<CNavItem>(Show);
        NavBar.CNavIconItems = _tabs;
        NavBar.SelectItem(_tabs[(int)AppTab.Workout]);

        _instance = new(this);
    }

    /// <summary>Pops back to the root and switches to <paramref name="tab"/>.</summary>
    public static async Task ShowTab(AppTab tab)
    {
        await Shell.Current.GoToAsync("//main");
        if (_instance?.TryGetTarget(out var page) == true)
            page.NavBar.SelectItem(page._tabs[(int)tab]);
    }

    void Show(CNavItem item)
    {
        if (item == _current)
            return;
        if (_visible)
            _current?.Page.Unload();
        _current = item;
        TabContent.SetContent(item);
        if (_visible)
            item.Page.Load();
    }

    // Tabs only count as visible while this page is on top, as they did when each tab was its own Shell page:
    // returning from a detail page reloads the current tab.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        _current?.Page.Load();
        UpdateActiveBar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _visible = false;
        _current?.Page.Unload();
        _activeTimer?.Stop();
        _activeTimer = null;
    }

    // Shown whenever a workout is in progress. Starting or finishing one always goes through the workout page,
    // so checking when this page reappears is enough; the timer just keeps the time ticking.
    void UpdateActiveBar()
    {
        var active = _workouts.Active;
        ActiveBar.IsVisible = active != null;
        // Room at the end of every tab so its last items can scroll clear of the pill.
        foreach (var tab in _tabs)
            ((TabView)tab.Page).BottomInset = active != null ? ActiveBarInset : 0;
        if (active == null)
        {
            _activeTimer?.Stop();
            _activeTimer = null;
            return;
        }

        ActiveName.Text = active.Name;
        ActiveTime.Text = Elapsed(active.Duration);
        if (_activeTimer == null)
        {
            _activeTimer = Dispatcher.CreateTimer();
            _activeTimer.Interval = TimeSpan.FromSeconds(1);
            _activeTimer.Tick += (_, _) => UpdateActiveBar();
            _activeTimer.Start();
        }
    }

    static string Elapsed(TimeSpan t) => t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");

    async void OnActiveBarTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync(Routes.Workout);
}
