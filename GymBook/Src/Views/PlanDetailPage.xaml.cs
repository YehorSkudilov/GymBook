using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanDetailPage : BasePage
{
    public PlanDetailPage(PlanDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HorizontalMouseScroll.Attach(DayStrip);
    }

    // The hardware back button, like the back arrow: unsaved changes are saved or discarded first.
    protected override bool OnBackButtonPressed()
    {
        var vm = (PlanDetailViewModel)BindingContext;
        if (!vm.HasChanges)
            return base.OnBackButtonPressed();
        vm.BackCommand.Execute(null);
        return true;
    }
}
