using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanSettingsPage : SheetPage
{
    public PlanSettingsPage(PlanSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    async void OnBackdropTapped(object? sender, TappedEventArgs e) => await CloseAsync();

    async void OnDoneClicked(object? sender, EventArgs e) => await CloseAsync();
}
