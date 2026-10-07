namespace GymBook.Views;

public partial class SleepNoteView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(SleepNoteView), "");

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public SleepNoteView() => InitializeComponent();
}
