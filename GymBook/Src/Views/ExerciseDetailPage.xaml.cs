using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ExerciseDetailPage : BasePage
{
    readonly ExerciseDetailViewModel _viewModel;
    IDispatcherTimer? _timer;

    public ExerciseDetailPage(ExerciseDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Alternates between the start and end photos, like a two-frame animation of the movement.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1.4);
        _timer.Tick += (_, _) =>
        {
            if (_viewModel.ImageEnd != null)
                _ = EndImage.FadeToAsync(EndImage.Opacity < 0.5 ? 1 : 0, 350);
        };
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _timer = null;
    }
}
