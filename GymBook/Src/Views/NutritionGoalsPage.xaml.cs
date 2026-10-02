using GymBook.ViewModels;

namespace GymBook.Views;

public partial class NutritionGoalsPage : BasePage
{
    public NutritionGoalsPage(NutritionGoalsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
