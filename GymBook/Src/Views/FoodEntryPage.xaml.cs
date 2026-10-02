using GymBook.ViewModels;

namespace GymBook.Views;

public partial class FoodEntryPage : BasePage
{
    public FoodEntryPage(FoodEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
