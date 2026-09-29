using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanOptionsPage : SheetPage
{
    public PlanOptionsPage(PlanOptionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    void OnDoneClicked(object? sender, EventArgs e) => _ = CloseAsync();

    void OnBackdropTapped(object? sender, TappedEventArgs e) => _ = CloseAsync();

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }
}
