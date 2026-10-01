using GymBook.Admin.Services;
using GymBook.Admin.Views;

namespace GymBook.Admin;

public partial class App : Application
{
    readonly IServiceProvider _services;

    public App(IServiceProvider services, AdminApi api)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _services = services;
        // A rejected refresh token or a removed admin role: back to sign-in.
        api.SessionExpired += (_, _) => MainThread.BeginInvokeOnMainThread(ShowSignIn);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_services.GetRequiredService<LoginPage>()) { Title = "Gym Book Admin" };
#if WINDOWS
        window.Width = 1100;
        window.Height = 800;
#endif
        return window;
    }

    public static void ShowMain() => SetPage(new AppShell());

    public static void ShowSignIn()
    {
        if (Current?.Windows.FirstOrDefault()?.Page is not LoginPage)
            SetPage(Current!.Handler!.MauiContext!.Services.GetRequiredService<LoginPage>());
    }

    static void SetPage(Page page)
    {
        if (Current?.Windows.FirstOrDefault() is { } window)
            window.Page = page;
    }
}
