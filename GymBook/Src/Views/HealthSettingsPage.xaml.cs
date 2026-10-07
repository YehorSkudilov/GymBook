using System.ComponentModel;
using GymBook.ViewModels;

namespace GymBook.Views;

public partial class HealthSettingsPage : SheetPage
{
    readonly HealthSettingsViewModel _health;

    public HealthSettingsPage(HealthSettingsViewModel health)
    {
        InitializeComponent();
        BindingContext = _health = health;
        health.PropertyChanged += OnHealthChanged;
    }

    // Disconnected (after its confirmation, which goes over this sheet): nothing left to set here. Already closed some
    // other way: just stop listening.
    async void OnHealthChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HealthSettingsViewModel.IsConnected) || _health.IsConnected)
            return;
        _health.PropertyChanged -= OnHealthChanged;
        if (Navigation.ModalStack.Contains(this))
            await CloseAsync();
    }

    async void OnBackdropTapped(object? sender, TappedEventArgs e) => await CloseAsync();

    async void OnDoneClicked(object? sender, EventArgs e) => await CloseAsync();
}
