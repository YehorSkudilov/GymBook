using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanReviewPage : SheetPage
{
    public PlanReviewPage(PlanReviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
