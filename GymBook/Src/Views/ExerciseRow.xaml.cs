namespace GymBook.Views;

/// <summary>Exercise list row. In picker mode it shows a selection check instead of a chevron.</summary>
public partial class ExerciseRow : ContentView
{
    public static readonly BindableProperty IsPickerProperty =
        BindableProperty.Create(nameof(IsPicker), typeof(bool), typeof(ExerciseRow), false, propertyChanged: (b, _, _) => ((ExerciseRow)b).UpdateAccessory());

    public ExerciseRow()
    {
        InitializeComponent();
        UpdateAccessory();
    }

    public bool IsPicker
    {
        get => (bool)GetValue(IsPickerProperty);
        set => SetValue(IsPickerProperty, value);
    }

    void UpdateAccessory()
    {
        if (IsPicker)
        {
            var check = new Border
            {
                WidthRequest = 28,
                HeightRequest = 28,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Content = new Image { Source = (ImageSource)Application.Current!.Resources["IconCheck"], WidthRequest = 16, HeightRequest = 16 },
            };
            check.SetBinding(BackgroundColorProperty, nameof(ViewModels.ExerciseItem.CheckBackground));
            Accessory.Content = check;
        }
        else
        {
            Accessory.Content = new Image { Source = (ImageSource)Application.Current!.Resources["IconChevronRight"], WidthRequest = 18, HeightRequest = 18 };
        }
    }
}
