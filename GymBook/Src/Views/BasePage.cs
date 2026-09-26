using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>Forwards page lifecycle events to the page's view model.</summary>
public class BasePage : ContentPage
{
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel vm)
            await vm.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as BaseViewModel)?.OnDisappearing();
    }
}
