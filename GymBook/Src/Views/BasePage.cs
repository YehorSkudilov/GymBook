using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>Forwards page lifecycle events to the page's view model, and eases the page's content in the first time it shows.</summary>
public class BasePage : ContentPage
{
    bool _entered;

    /// <summary>
    /// Whether the content fades and rises in when the page first appears. Off for pages that bring themselves in
    /// (sheets slide up; the workout-complete screen has its own show).
    /// </summary>
    protected virtual bool AnimatesIn => true;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_entered && AnimatesIn && Content is { } content)
        {
            _entered = true;
            content.Opacity = 0;
            content.TranslationY = 14;
            _ = Task.WhenAll(content.FadeTo(1, 240, Easing.CubicOut), content.TranslateTo(0, 0, 320, Easing.CubicOut));
        }
        if (BindingContext is BaseViewModel vm)
            await vm.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as BaseViewModel)?.OnDisappearing();
    }
}
