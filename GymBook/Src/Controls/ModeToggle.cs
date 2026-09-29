namespace GymBook.Controls;

/// <summary>
/// Two halves of one toggle: a setting's defaults, or its own ("Use defaults | Custom"), like the reps popup's
/// Exact | Range. Bind <see cref="IsOwn"/> both ways; tapping a half sets it.
/// </summary>
public class ModeToggle : ContentView
{
    public static readonly BindableProperty IsOwnProperty = BindableProperty.Create(nameof(IsOwn), typeof(bool), typeof(ModeToggle), false,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((ModeToggle)b).Paint());

    public static readonly BindableProperty DefaultsTextProperty = BindableProperty.Create(nameof(DefaultsText), typeof(string), typeof(ModeToggle),
        "Use defaults", propertyChanged: (b, _, v) => ((ModeToggle)b)._defaults.Text = (string)v);

    public static readonly BindableProperty OwnTextProperty = BindableProperty.Create(nameof(OwnText), typeof(string), typeof(ModeToggle),
        "Custom", propertyChanged: (b, _, v) => ((ModeToggle)b)._own.Text = (string)v);

    public bool IsOwn
    {
        get => (bool)GetValue(IsOwnProperty);
        set => SetValue(IsOwnProperty, value);
    }

    public string DefaultsText
    {
        get => (string)GetValue(DefaultsTextProperty);
        set => SetValue(DefaultsTextProperty, value);
    }

    public string OwnText
    {
        get => (string)GetValue(OwnTextProperty);
        set => SetValue(OwnTextProperty, value);
    }

    readonly Label _defaults, _own;
    readonly Border _defaultsHalf, _ownHalf;

    public ModeToggle()
    {
        (_defaultsHalf, _defaults) = Half("Use defaults", false);
        (_ownHalf, _own) = Half("Custom", true);
        var grid = new Grid { ColumnSpacing = 2, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
        grid.Add(_defaultsHalf, 0);
        grid.Add(_ownHalf, 1);
        Content = new Border
        {
            BackgroundColor = Resource<Color>("Surface2"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 0,
            Padding = 3,
            Content = grid,
        };
        Paint();
    }

    (Border, Label) Half(string text, bool own)
    {
        var label = new Label { Text = text, FontFamily = "OpenSansSemibold", FontSize = 14, HorizontalOptions = LayoutOptions.Center };
        var half = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 9 },
            StrokeThickness = 0,
            Padding = new Thickness(0, 9),
            Content = label,
        };
        half.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => IsOwn = own) });
        return (half, label);
    }

    void Paint()
    {
        if (_defaultsHalf == null)
            return;
        Paint(_defaultsHalf, _defaults, !IsOwn);
        Paint(_ownHalf, _own, IsOwn);
    }

    static void Paint(Border half, Label label, bool on)
    {
        half.BackgroundColor = on ? Resource<Color>("Accent") : Colors.Transparent;
        label.TextColor = on ? Colors.White : Resource<Color>("TextSecondary");
    }

    static T Resource<T>(string key) => (T)Application.Current!.Resources[key];
}
