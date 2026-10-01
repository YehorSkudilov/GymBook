namespace GymBook.Wear;

/// <summary>The watch's home (see <see cref="HomeViewModel"/>).</summary>
public partial class HomePage : ContentPage
{
    readonly HomeViewModel _vm;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }
}
