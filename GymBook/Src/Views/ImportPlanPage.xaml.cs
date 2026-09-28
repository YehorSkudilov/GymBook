using GymBook.ViewModels;

namespace GymBook.Views;

public partial class ImportPlanPage : SheetPage
{
    public ImportPlanPage(ImportPlanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
