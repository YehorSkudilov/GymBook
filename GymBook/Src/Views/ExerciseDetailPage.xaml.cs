using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ExerciseDetailPage : BasePage
{
    public ExerciseDetailPage(ExerciseDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    void OnVideoLoadFailed(object? sender, EventArgs e) => ((ExerciseDetailViewModel)BindingContext).VideoLoadFailed();

    /// <summary>The animation player holds a native player until its handler is disconnected: done when the page closes.</summary>
    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        if (!Navigation.NavigationStack.Contains(this))
            AnimationPlayer.Handler?.DisconnectHandler();
    }
}
