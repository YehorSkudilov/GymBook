using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ExercisesPage : BasePage
{
    public ExercisesPage(ExercisesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
