using System.Collections.ObjectModel;
using AppSkeleton;

namespace GymBook.Admin.Views;

/// <summary>The tabs, in nav bar (and swipe) order.</summary>
public enum AdminTab { Dashboard, Users, Reports, Ai, Account }

/// <summary>The app's root, built like the GymBook app's: tabs in a swipeable CView over a CNavBar. Users open through Shell.</summary>
public partial class MainPage : ContentPage
{
    static WeakReference<MainPage>? _instance;

    readonly ObservableCollection<CNavItem> _tabs;
    CNavItem? _current;
    bool _visible;

    public MainPage(DashboardPage dashboard, UsersPage users, ReportsPage reports, AiUsagePage ai, AccountPage account)
    {
        InitializeComponent();
        var color = (Color)Application.Current!.Resources["TextPrimary"];
        // In AdminTab order. Icons with a solid body, so the filled (selected) version looks different from the outline.
        _tabs =
        [
            new() { Glyph = "space_dashboard", PageName = "Dashboard", Page = dashboard, Color = color },
            new() { Glyph = "group", PageName = "Users", Page = users, Color = color },
            new() { Glyph = "flag", PageName = "Reports", Page = reports, Color = color },
            new() { Glyph = "auto_awesome", PageName = "AI", Page = ai, Color = color },
            new() { Glyph = "person", PageName = "Account", Page = account, Color = color },
        ];
        TabContent.CNavIconItems = _tabs;
        NavBar.LoadPageCommand = new Command<CNavItem>(Show);
        NavBar.CNavIconItems = _tabs;
        NavBar.SelectItem(_tabs[(int)AdminTab.Dashboard]);
        _instance = new(this);
    }

    /// <summary>Pops back to the tabs and switches to <paramref name="tab"/>.</summary>
    public static async Task ShowTab(AdminTab tab)
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
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

    // Tabs count as visible only while this page is on top: coming back from a user reloads the current tab.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        _current?.Page.Load();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _visible = false;
        _current?.Page.Unload();
    }
}
