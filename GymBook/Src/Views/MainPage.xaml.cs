using System.Collections.ObjectModel;
using AppSkeleton;
using GymBook.Services;

namespace GymBook.Views;

/// <summary>The tabs, in the order they sit in the nav bar (and swipe): Workout is the big button in the middle.</summary>
public enum AppTab { Exercises, Plans, Workout, Progress, Profile }

/// <summary>The app's root: the tabs live in a swipeable CView with a CNavBar underneath. Detail pages still push through Shell.</summary>
public partial class MainPage : ContentPage
{
    const double ActiveBarInset = 70;

    static WeakReference<MainPage>? _instance;

    readonly ObservableCollection<CNavItem> _tabs;
    readonly WorkoutService _workouts;
    readonly DataStore _store;
    CNavItem? _current;
    bool _visible;
    IDispatcherTimer? _activeTimer;

    public MainPage(WorkoutService workouts, DataStore store, HomePage home, PlansPage plans, ExercisesPage exercises, StatsPage stats, ProfilePage profile)
    {
        InitializeComponent();
        _workouts = workouts;
        _store = store;

        var resources = Application.Current!.Resources;
        var color = (Color)resources["TextPrimary"];
        var accent = (Color)resources["Accent"];
        // In AppTab order. Workout sits in the middle as a larger round button (no label, it speaks for itself), lit up
        // when open; the others keep the bar's usual look (the filled icon font when selected).
        _tabs =
        [
            // Icons with a solid body, so the filled version (selected) looks different from the outline one; line-only
            // icons like format_list_bulleted and insights look the same in both.
            new() { Glyph = "list_alt", PageName = "Exercises", Page = exercises, Color = color },
            new() { Glyph = "event_note", PageName = "Plans", Page = plans, Color = color },
            new()
            {
                Glyph = "fitness_center", PageName = "Workout", Page = home,
                Color = (Color)resources["TextPrimary"], SelectedColor = Colors.White,
                IconBackground = (Color)resources["Surface3"], SelectedIconBackground = accent,
                IconSize = 26, IconBackgroundSize = 44, ItemWidth = 72, ShowLabel = false,
            },
            new() { Glyph = "analytics", PageName = "Progress", Page = stats, Color = color },
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
        _store.Changed += OnDataChanged;
    }

    // A workout can also arrive (or end) through sync, e.g. after signing in on a fresh install.
    void OnDataChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(UpdateActiveBar);

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _visible = false;
        _store.Changed -= OnDataChanged;
        _current?.Page.Unload();
        _activeTimer?.Stop();
        _activeTimer = null;
    }

    // Shown whenever a workout is in progress: checked when this page reappears (starting or finishing one goes
    // through the workout page) and when data changes (one synced from another device); the timer keeps the time ticking.
    void UpdateActiveBar()
    {
        var active = _workouts.Active;
        ActiveBar.IsVisible = active != null;
        // Room at the end of every tab so its last items can scroll clear of the pill.
        foreach (var tab in _tabs)
            ((TabView)tab.Page).BottomInset = active != null ? ActiveBarInset : 20;
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
