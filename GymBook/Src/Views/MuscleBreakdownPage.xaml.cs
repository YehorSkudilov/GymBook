using GymBook.ViewModels;

namespace GymBook.Views;

public partial class MuscleBreakdownPage : ContentPage
{
    public MuscleBreakdownPage(MuscleBreakdownViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
