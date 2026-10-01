using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

/// <summary>A page bound to a view model that reloads every time the page appears.</summary>
public class BasePage : ContentPage
{
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel vm)
            await vm.OnAppearingAsync();
    }
}
