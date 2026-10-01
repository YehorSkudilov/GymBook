namespace GymBook.Wear;

/// <summary>The watch app's settings (see <see cref="SettingsViewModel"/>).</summary>
public partial class SettingsPage : ContentPage
{
    readonly SettingsViewModel _vm;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = _vm = vm;
        vm.SignedOut += async () => await Navigation.PopAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Refresh();
    }
}
