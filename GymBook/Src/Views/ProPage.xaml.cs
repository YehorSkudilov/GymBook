using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ProPage : SheetPage
{
    public ProPage(ProViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
