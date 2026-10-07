using GymBook.ViewModels;

namespace GymBook.Views;

public partial class RecoveryOverviewView : ContentView
{
    public static readonly BindableProperty OverviewProperty =
        BindableProperty.Create(nameof(Overview), typeof(RecoveryOverview), typeof(RecoveryOverviewView), RecoveryOverview.Empty);

    public RecoveryOverview Overview
    {
        get => (RecoveryOverview)GetValue(OverviewProperty);
        set => SetValue(OverviewProperty, value);
    }

    public RecoveryOverviewView() => InitializeComponent();
}
