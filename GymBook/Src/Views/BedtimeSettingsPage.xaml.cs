using GymBook.ViewModels;

namespace GymBook.Views;

public partial class BedtimeSettingsPage : SheetPage
{
    public BedtimeSettingsPage(BedtimeSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    async void OnBackdropTapped(object? sender, TappedEventArgs e) => await CloseAsync();

    async void OnDoneClicked(object? sender, EventArgs e) => await CloseAsync();
}
