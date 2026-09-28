using GymBook.ViewModels;

namespace GymBook.Views;

public partial class WorkoutMenuPage : SheetPage
{
    public WorkoutMenuPage(WorkoutMenuViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
