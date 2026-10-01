using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ExerciseDetailPage : BasePage
{
    public ExerciseDetailPage(ExerciseDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
