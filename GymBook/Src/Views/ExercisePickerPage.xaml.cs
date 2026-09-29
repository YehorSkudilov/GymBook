using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ExercisePickerPage : SheetPage
{
    readonly ExercisePickerViewModel _viewModel;

    public ExercisePickerPage(ExercisePickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.Cancel();
        return true;
    }
}
