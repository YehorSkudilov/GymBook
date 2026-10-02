using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ExerciseDetailPage : SheetPage
{
    public ExerciseDetailPage(ExerciseDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
