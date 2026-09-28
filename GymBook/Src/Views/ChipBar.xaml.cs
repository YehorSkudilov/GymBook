namespace GymBook.Views;

public partial class ChipBar : ContentView
{
    public ChipBar()
    {
        InitializeComponent();
        // Wheel and drag scrolling with a mouse on Windows, like the exercise strip in a workout.
        HorizontalMouseScroll.Attach(Scroller);
    }
}
