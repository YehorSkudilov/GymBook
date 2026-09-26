using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanWizardPage : BasePage
{
    readonly PlanWizardViewModel _viewModel;

    public PlanWizardPage(PlanWizardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Configures the wizard as the first-run experience, shown outside the shell.</summary>
    public PlanWizardPage ForOnboarding()
    {
        _viewModel.Start(onboarding: true);
        return this;
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }
}
