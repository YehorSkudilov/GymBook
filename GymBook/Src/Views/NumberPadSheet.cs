using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace GymBook.Views;

/// <summary>
/// A number (weight, reps, RIR) picked without the phone's keyboard: a wheel of values to swipe through, which settles on
/// the one in the middle (or tap one), above the app's own number pad for typing any value. The first key pressed replaces
/// the number; after that keys add to it. Typing turns the wheel to the nearest value, and turning it shows the value it
/// lands on. The check saves what's shown; Cancel, the dimmed area or back leaves it as it was.
/// Tapping a value or typing jumps the wheel there at once (no animation to wait for), and the scroll events of a turn
/// made by code are ignored until it gets there, so they can't put a value it passed on the way back in its place.
/// The wheel scrolls an empty spacer as tall as all its values; only the few on screen are views, moved along with
/// the scroll and given the value they're over (weights go up to 500 kg, a view each made it slow to open).
/// </summary>
public class NumberPadSheet : SheetPage
{
    const double ItemHeight = 58, Visible = 5;
    // Room above and below the values, so the first and last can sit in the middle.
    const double WheelPadding = ItemHeight * (Visible - 1) / 2;
    // Enough rows to cover the wheel at any scroll position, with one coming in at each edge.
    const int Rows = (int)Visible + 2;

    readonly TaskCompletionSource<double?> _result = new();
    readonly List<double> _values = [];
    // The rows on screen: row i % Rows shows value i, so a value keeps its row for as long as it's in view.
    readonly Border[] _rows = new Border[Rows];
    readonly int[] _rowValue = new int[Rows];
    readonly ScrollView _wheel;
    readonly Label _display;
    readonly string _unit;
    readonly bool _decimals;
    readonly double _max;
    readonly bool _allowEmpty;
    string _text;
    bool _fresh = true;
    bool _turningByCode;
    // A turn made by code: the value it's going to, and since when. Scroll events on the way there aren't the user's.
    int _target = -1;
    DateTime _targetSince;
    int _centered = -1;
    int _settleId;
    bool _closing;

    NumberPadSheet(string? current, double value, double step, double min, double max, string unit, bool decimals, bool allowEmpty)
    {
        Backdrop = Color.FromArgb("#E6000000");
        Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
        Shell.SetNavBarIsVisible(this, false);
        _unit = unit;
        _decimals = decimals;
        _max = max;
        _allowEmpty = allowEmpty;
        _text = current ?? "";

        for (var v = min; v <= max + step / 2; v += step)
            _values.Add(Math.Round(v, 3));

        // The wheel: a spacer as tall as every value to scroll, and over it the rows that show the ones in view.
        var spacer = new ContentView { HeightRequest = WheelPadding * 2 + _values.Count * ItemHeight, BackgroundColor = Colors.Transparent };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, e) =>
        {
            if (e.GetPosition(spacer) is { } p && (p.Y - WheelPadding) / ItemHeight is var at && at >= 0 && (int)at < _values.Count)
                _ = TurnTo((int)at, animate: false);
        };
        spacer.GestureRecognizers.Add(tap);
        _wheel = new ScrollView { Content = spacer, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        _wheel.Scrolled += OnWheelScrolled;

        // Touches go through the rows to the wheel under them.
        var rows = new Grid { InputTransparent = true, IsClippedToBounds = true };
        for (var r = 0; r < Rows; r++)
        {
            _rowValue[r] = -1;
            rows.Add(_rows[r] = new Border
            {
                HeightRequest = ItemHeight - 10,
                Margin = new Thickness(0, 5, 0, 0),
                WidthRequest = 210,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 0,
                Opacity = 0,
                Content = new Label
                {
                    FontFamily = "OpenSansSemibold",
                    FontSize = 22,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
            });
        }
        var wheelArea = new Grid { HeightRequest = ItemHeight * Visible };
        wheelArea.Add(_wheel);
        wheelArea.Add(rows);
        PlaceRows(0);

        _display = new Label
        {
            FontFamily = "OpenSansSemibold",
            FontSize = 34,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        var check = new Border
        {
            BackgroundColor = Color.FromArgb("#2ED47A"),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            StrokeThickness = 0,
            HeightRequest = 58,
            Content = new Image { Source = Res<ImageSource>("IconCheck"), WidthRequest = 30, HeightRequest = 30 },
        };
        check.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(Save) });
        var displayRow = new Grid { ColumnSpacing = 10, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)] };
        displayRow.Add(_display, 1);
        displayRow.Add(check, 2);

        var pad = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 10,
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)],
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)],
        };
        for (var d = 1; d <= 9; d++)
            pad.Add(Key(d.ToString(CultureInfo.InvariantCulture)), (d - 1) % 3, (d - 1) / 3);
        if (decimals)
            pad.Add(Key("."), 0, 3);
        pad.Add(Key("0"), 1, 3);
        pad.Add(Key("⌫"), 2, 3);

        var cancel = new Button { Text = "Cancel", Style = Res<Style>("GhostButton"), FontSize = 17, HorizontalOptions = LayoutOptions.Start };
        cancel.Clicked += (_, _) => Close(null);

        var layout = new VerticalStackLayout { Padding = new Thickness(16, 10, 16, 24), Spacing = 14 };
        layout.Add(cancel);
        layout.Add(wheelArea);
        layout.Add(displayRow);
        layout.Add(pad);

        var sheet = new Border
        {
            BackgroundColor = Res<Color>("Bg"),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
            StrokeThickness = 0,
            VerticalOptions = LayoutOptions.End,
            Content = layout,
        };
        // Swallows taps, so only the dimmed area above closes the sheet.
        sheet.GestureRecognizers.Add(new TapGestureRecognizer());
        var root = new Grid();
        root.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Close(null)) });
        root.Add(sheet);
        Content = root;

        ShowText();
        // Opens on the current value (or the nearest one), once the wheel has a size to scroll in.
        _wheel.SizeChanged += async (_, _) =>
        {
            if (_centered < 0 && _wheel.Height > 0)
                await TurnTo(Nearest(value), animate: false);
        };
    }

    /// <summary>
    /// Asks for a number: <paramref name="current"/> as it's shown now (null or empty for none), the wheel from
    /// <paramref name="min"/> to <paramref name="max"/> by <paramref name="step"/>, labelled with <paramref name="unit"/>.
    /// Null when cancelled; with <paramref name="allowEmpty"/>, NaN when cleared (no value).
    /// </summary>
    public static async Task<double?> Show(string? current, double value, double step, double min, double max, string unit,
        bool decimals = false, bool allowEmpty = false)
    {
        var sheet = new NumberPadSheet(current, value, step, min, max, unit, decimals, allowEmpty);
        var navigation = Shell.Current?.Navigation ?? Application.Current!.Windows[0].Page!.Navigation;
        await navigation.PushModalAsync(sheet, false);
        return await sheet._result.Task;
    }

    string Label(double v) => $"{Format(v)} {_unit}".Trim();

    static string Format(double v) => v.ToString(v % 1 == 0 ? "0" : "0.##", CultureInfo.CurrentCulture);

    Border Key(string text)
    {
        var key = new Border
        {
            BackgroundColor = Color.FromArgb("#1D212C"),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            StrokeThickness = 0,
            HeightRequest = 62,
            Content = new Label
            {
                Text = text,
                FontFamily = "OpenSansSemibold",
                FontSize = text == "⌫" ? 24 : 30,
                TextColor = Res<Color>("TextPrimary"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            },
        };
        key.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Press(text)) });
        return key;
    }

    void Press(string key)
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Not every device has haptics.
        }
        if (key == "⌫")
        {
            _text = _fresh ? "" : _text.Length > 0 ? _text[..^1] : "";
        }
        else
        {
            var next = _fresh ? "" : _text;
            if (key == "." && (next.Contains('.') || !_decimals))
                return;
            next = key == "." && next.Length == 0 ? "0." : next + key;
            // Keep it sensible: no leading zeros, two decimals, not past the top of the wheel's range by far.
            if (next.Length > 1 && next[0] == '0' && next[1] != '.')
                next = next[1..];
            if (next.Contains('.') && next.Length - next.IndexOf('.') - 1 > 2)
                return;
            if (Parse(next) is { } v && v > _max * 2)
                return;
            _text = next;
        }
        _fresh = false;
        ShowText();
        if (Parse(_text) is { } typed)
            _ = TurnTo(Nearest(typed), animate: false, fromTyping: true);
    }

    void ShowText()
    {
        _display.Text = _text.Length == 0 ? "–" : _text;
        _display.TextColor = _fresh ? Color.FromArgb("#9AA3B5") : Res<Color>("TextPrimary");
    }

    static double? Parse(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0 ? v : null;

    int Nearest(double v)
    {
        if (_values.Count == 0)
            return 0;
        var best = 0;
        for (var i = 1; i < _values.Count; i++)
            if (Math.Abs(_values[i] - v) < Math.Abs(_values[best] - v))
                best = i;
        return best;
    }

    async Task TurnTo(int index, bool animate, bool fromTyping = false)
    {
        if (index < 0 || index >= _values.Count)
            return;
        _turningByCode = true;
        Highlight(index);
        if (!fromTyping)
        {
            _text = Format(_values[index]).Replace(',', '.');
            _fresh = true;
            ShowText();
        }
        // Already there: no scroll will come to clear it.
        if (Math.Abs(_wheel.ScrollY - index * ItemHeight) > 1)
        {
            _target = index;
            _targetSince = DateTime.Now;
        }
        await _wheel.ScrollToAsync(0, index * ItemHeight, animate);
        PlaceRows(_wheel.ScrollY);
        _turningByCode = false;
    }

    void Highlight(int index)
    {
        if (index == _centered)
            return;
        // Repainted only if in view; one out of view is painted as it comes in (see PlaceRows).
        if (_centered >= 0 && _rowValue[_centered % Rows] == _centered)
            Paint(_rows[_centered % Rows], false);
        _centered = index;
        if (_rowValue[index % Rows] == index)
            Paint(_rows[index % Rows], true);
    }

    // The rows over the values in view at this scroll position, each moved to where its value is.
    void PlaceRows(double scrollY)
    {
        var first = (int)Math.Floor((scrollY - WheelPadding) / ItemHeight);
        for (var i = first; i < first + Rows; i++)
        {
            var r = (i % Rows + Rows) % Rows;
            var row = _rows[r];
            if (i < 0 || i >= _values.Count)
            {
                row.Opacity = 0;
                continue;
            }
            if (_rowValue[r] != i)
            {
                _rowValue[r] = i;
                ((Label)row.Content!).Text = Label(_values[i]);
                Paint(row, i == _centered);
            }
            row.Opacity = 1;
            row.TranslationY = WheelPadding + i * ItemHeight - scrollY;
        }
    }

    static readonly Color RowOn = Color.FromArgb("#3A4050"), RowOff = Color.FromArgb("#1D212C");
    static readonly Color TextOn = Color.FromArgb("#F4F6FB"), TextOff = Color.FromArgb("#9AA3B5");

    static void Paint(Border item, bool on)
    {
        item.BackgroundColor = on ? RowOn : RowOff;
        ((Label)item.Content!).TextColor = on ? TextOn : TextOff;
    }

    // Swiped: the value in the middle shows as it passes, and once it stops it settles exactly on one.
    async void OnWheelScrolled(object? sender, ScrolledEventArgs e)
    {
        PlaceRows(e.ScrollY);
        // A turn made by code, on its way: not the user's, even when the event comes after the turn's task ended (it
        // can, and its position is one it passed). Over once it gets there, or after a moment in case it never does.
        if (_target >= 0)
        {
            var reached = Math.Abs(e.ScrollY - _target * ItemHeight) <= 1;
            var stale = DateTime.Now - _targetSince > TimeSpan.FromMilliseconds(700);
            if (reached || stale)
                _target = -1;
            if (!stale)
                return;
        }
        if (_turningByCode)
            return;
        var index = Math.Clamp((int)Math.Round(e.ScrollY / ItemHeight), 0, _values.Count - 1);
        if (index != _centered)
        {
            Highlight(index);
            _text = Format(_values[index]).Replace(',', '.');
            _fresh = true;
            ShowText();
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
            }
        }
        var id = ++_settleId;
        await Task.Delay(90);
        if (id == _settleId && !_turningByCode && _target < 0 && !_closing && Math.Abs(_wheel.ScrollY - index * ItemHeight) > 1)
            await TurnTo(index, animate: true);
    }

    void Save()
    {
        if (_text.Length == 0)
        {
            Close(_allowEmpty ? double.NaN : 0);
            return;
        }
        Close(Parse(_text));
    }

    async void Close(double? value)
    {
        if (_closing)
            return;
        _closing = true;
        await CloseAsync();
        _result.TrySetResult(value);
    }

    protected override bool OnBackButtonPressed()
    {
        Close(null);
        return true;
    }

    // Closed some other way (the page under it navigated away): cancelled. Closing itself sets the result afterwards.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (!_closing)
            _result.TrySetResult(null);
    }

    static T Res<T>(string key) => (T)Application.Current!.Resources[key];
}
