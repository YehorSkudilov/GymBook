using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanWarmupsPage : SheetPage
{
    public PlanWarmupsPage(WarmupSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    async void OnBackdropTapped(object? sender, TappedEventArgs e) => await CloseAsync();

    async void OnDoneClicked(object? sender, EventArgs e) => await CloseAsync();
}
