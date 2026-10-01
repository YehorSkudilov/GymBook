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
}
