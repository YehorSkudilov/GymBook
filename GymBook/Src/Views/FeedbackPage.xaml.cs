using GymBook.ViewModels;

namespace GymBook.Views;

public partial class FeedbackPage : BasePage
{
    public FeedbackPage(FeedbackViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
