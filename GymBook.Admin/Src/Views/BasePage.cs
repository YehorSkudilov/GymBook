using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

/// <summary>A pushed page: forwards appearing to its view model and eases its content in the first time, like the GymBook app.</summary>
public class BasePage : ContentPage
{
    bool _entered;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_entered && Content is { } content)
        {
            _entered = true;
            content.Opacity = 0;
            content.TranslationY = 14;
            _ = Task.WhenAll(content.FadeTo(1, 240, Easing.CubicOut), content.TranslateTo(0, 0, 320, Easing.CubicOut));
        }
        if (BindingContext is BaseViewModel vm)
            await vm.OnAppearingAsync();
    }
}
