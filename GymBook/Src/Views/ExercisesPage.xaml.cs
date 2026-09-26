using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ExercisesPage : TabView
{
    public ExercisesPage(ExercisesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
