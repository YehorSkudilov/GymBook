namespace GymBook.Wear;

public partial class MainPage : ContentPage
{
    readonly WatchViewModel _vm;

    public MainPage(WatchViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // Only follow the phone and read the heart rate while the app is on screen, to spare the watch's battery.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.StartAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Stop();
    }
}
