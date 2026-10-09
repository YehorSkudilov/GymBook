using GymBook.ViewModels;

namespace GymBook.Views;

public partial class LevelPage : SheetPage
{
    public LevelPage(LevelViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
