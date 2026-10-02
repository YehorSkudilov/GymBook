using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanDetailDayView : AppSkeleton.PageBase
{
    public PlanDetailDayView()
    {
        InitializeComponent();
    }

    // The note goes into the draft as it's typed; leaving the field shows the plan as changed.
    void OnNoteUnfocused(object? sender, FocusEventArgs e) => ((sender as BindableObject)?.BindingContext as PlanDayExercise)?.NoteDone();
}
