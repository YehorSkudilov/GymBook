using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanDetailPage : BasePage
{
    public PlanDetailPage(PlanDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
