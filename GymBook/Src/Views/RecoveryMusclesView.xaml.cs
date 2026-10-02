using GymBook.ViewModels;

namespace GymBook.Views;

public partial class RecoveryMusclesView : ContentView
{
    public static readonly BindableProperty MajorProperty =
        BindableProperty.Create(nameof(Major), typeof(IList<MuscleRecoveryItem>), typeof(RecoveryMusclesView));

    public static readonly BindableProperty SupportingProperty =
        BindableProperty.Create(nameof(Supporting), typeof(IList<MuscleRecoveryItem>), typeof(RecoveryMusclesView));

    public IList<MuscleRecoveryItem>? Major
    {
        get => (IList<MuscleRecoveryItem>?)GetValue(MajorProperty);
        set => SetValue(MajorProperty, value);
    }

    public IList<MuscleRecoveryItem>? Supporting
    {
        get => (IList<MuscleRecoveryItem>?)GetValue(SupportingProperty);
        set => SetValue(SupportingProperty, value);
    }

    public RecoveryMusclesView() => InitializeComponent();
}
