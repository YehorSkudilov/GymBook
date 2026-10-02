using GymBook.ViewModels;

namespace GymBook.Views;

public partial class NutritionPage : TabView
{
    public NutritionPage(NutritionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
