namespace GymBook.Wear;

public partial class App : Application
{
    readonly IServiceProvider _services;
    readonly WatchViewModel _vm;

    public App(IServiceProvider services, WatchViewModel vm)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _services = services;
        _vm = vm;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Made here rather than taken in the constructor: the page's XAML needs App.xaml's resources, which only exist
        // once InitializeComponent above has run.
        var window = new Window(_services.GetRequiredService<MainPage>());
        // Leaving the app (or the screen turning off) doesn't make the page disappear, so follow the window too:
        // nothing runs while the app isn't on screen, and coming back picks up whatever changed on the phone.
        window.Stopped += (_, _) => _vm.Stop();
        window.Resumed += async (_, _) => await _vm.StartAsync();
        return window;
    }
}
