using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>Opened with Navigation.PushModalAsync(page, false), so it closes itself with <see cref="SheetPage.CloseAsync"/>.</summary>
public partial class MuscleBreakdownPage : SheetPage
{
    public MuscleBreakdownPage(MuscleBreakdownViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // The hardware back button slides it down too.
    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }
}
