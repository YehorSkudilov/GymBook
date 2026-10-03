namespace GymBook.Views;

/// <summary>
/// The app's own dialogs, as bottom sheets in the app's style instead of the platform's alert boxes: a menu of
/// options, a confirmation, a text prompt, or a message. <see cref="Services.DialogService"/> opens them; each resolves
/// its task once it has slid away (null / false when cancelled: tapping the dimmed area, Cancel, or back).
/// </summary>
public class DialogSheet : SheetPage
{
    // Words that make a confirmation's main button red.
    static readonly string[] DestructiveWords = ["Delete", "Discard", "Remove", "Reset", "Stop", "Replace", "Sign out", "Clear"];

    readonly TaskCompletionSource<string?> _result = new();
    string? _choice;

    /// <param name="closeButton">An X at the top right that cancels, for a long sheet whose Cancel is far down.</param>
    DialogSheet(string title, string? message, Func<DialogSheet, View> body, bool closeButton = false)
    {
        Backdrop = Color.FromArgb("#B3000000");
        Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
        Shell.SetNavBarIsVisible(this, false);

        var header = new VerticalStackLayout { Spacing = 6 };
        header.Add(new Label { Text = title, Style = Resource<Style>("H2"), FontSize = 19 });
        if (!string.IsNullOrWhiteSpace(message))
            header.Add(new Label { Text = message, Style = Resource<Style>("Caption"), FontSize = 15 });

        var stack = new VerticalStackLayout { Padding = new Thickness(20, 18, 20, 16), Spacing = 12 };
        if (closeButton)
        {
            var top = new Grid { ColumnSpacing = 12, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
            top.Add(header, 0);
            var close = new ImageButton
            {
                Source = Resource<ImageSource>("IconClose"),
                WidthRequest = 36,
                HeightRequest = 36,
                Padding = 8,
                CornerRadius = 18,
                BackgroundColor = Resource<Color>("Surface2"),
                VerticalOptions = LayoutOptions.Start,
                Command = new Command(() => Choose(null)),
            };
            top.Add(close, 1);
            stack.Add(top);
        }
        else
        {
            stack.Add(header);
        }
        stack.Add(body(this));

        var sheet = new Border
        {
            BackgroundColor = Resource<Color>("Bg"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
            StrokeThickness = 0,
            Padding = 0,
            VerticalOptions = LayoutOptions.End,
            // Only as tall as what's in it (a long one scrolls).
            Content = new ScrollView { Content = stack, VerticalOptions = LayoutOptions.End },
        };
        // Swallows taps, so only the dimmed area above closes the sheet.
        sheet.GestureRecognizers.Add(new TapGestureRecognizer());

        var root = new Grid { Margin = new Thickness(0, 28, 0, 0) };
        root.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Choose(null)) });
        root.Add(sheet);
        Content = root;
    }

    /// <summary>
    /// A menu: the options as rows (the destructive one in red), then Cancel. Resolves to the chosen option. Any
    /// <paramref name="switches"/> go above them, flipped in place without closing the menu. A long menu (switches, or
    /// many options, like the plan's ···) also has an X at the top to close it, its Cancel being far down.
    /// </summary>
    public static Task<string?> Menu(string title, string? destructive, string[] options, IReadOnlyList<MenuSwitch>? switches = null) =>
        Show(new DialogSheet(title, null, s =>
        {
            var list = new VerticalStackLayout { Spacing = 10 };
            if (switches is { Count: > 0 })
            {
                var toggles = new VerticalStackLayout();
                for (var i = 0; i < switches.Count; i++)
                {
                    if (i > 0)
                        toggles.Add(Divider());
                    toggles.Add(SwitchRow(switches[i]));
                }
                list.Add(Card(toggles));
            }
            var card = new VerticalStackLayout();
            var all = destructive == null ? options : [.. options, destructive];
            for (var i = 0; i < all.Length; i++)
            {
                if (i > 0)
                    card.Add(Divider());
                card.Add(s.Row(all[i], all[i] == destructive));
            }
            // Switches alone (nothing to pick): no empty card.
            if (all.Length > 0)
                list.Add(Card(card));
            list.Add(s.Button("Cancel", "SecondaryButton", null));
            return list;
        }, closeButton: switches is { Count: > 0 } || options.Length + (destructive == null ? 0 : 1) >= 7));

    /// <summary>
    /// On/off <paramref name="switches"/> in a card, then Cancel and <paramref name="accept"/> side by side. The
    /// switches apply as they're flipped; resolves to true for <paramref name="accept"/>, false when cancelled.
    /// </summary>
    public static async Task<bool> Switches(string title, string? message, IReadOnlyList<MenuSwitch> switches, string accept) =>
        await Show(new DialogSheet(title, message, s =>
        {
            var list = new VerticalStackLayout { Spacing = 14 };
            var toggles = new VerticalStackLayout();
            for (var i = 0; i < switches.Count; i++)
            {
                if (i > 0)
                    toggles.Add(Divider());
                toggles.Add(SwitchRow(switches[i]));
            }
            list.Add(Card(toggles));
            var main = s.Button(accept, "PrimaryButton", accept);
            var other = s.Button("Cancel", "SecondaryButton", null);
            main.HeightRequest = other.HeightRequest = 48;
            var row = new Grid { ColumnSpacing = 10, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
            row.Add(other, 0);
            row.Add(main, 1);
            list.Add(row);
            return list;
        })) != null;

    static BoxView Divider() => new() { HeightRequest = 1, Color = Resource<Color>("Stroke"), Margin = new Thickness(18, 0) };

    static Border Card(View content) => new()
    {
        BackgroundColor = Resource<Color>("Surface2"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
        StrokeThickness = 0,
        Padding = new Thickness(0, 4),
        Content = content,
    };

    // A name, a line on what it does, and the switch; the whole row flips it.
    static View SwitchRow(MenuSwitch option)
    {
        var toggle = new Switch { IsToggled = option.IsOn, VerticalOptions = LayoutOptions.Center };
        toggle.Toggled += (_, e) => option.Changed(e.Value);
        var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        text.Add(new Label { Text = option.Title, FontFamily = "OpenSansSemibold", FontSize = 17, TextColor = Resource<Color>("TextPrimary") });
        if (!string.IsNullOrWhiteSpace(option.Detail))
            text.Add(new Label { Text = option.Detail, Style = Resource<Style>("Caption"), FontSize = 12 });
        var row = new Grid
        {
            Padding = new Thickness(18, 10, 12, 10),
            MinimumHeightRequest = 54,
            ColumnSpacing = 12,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
        };
        row.Add(text);
        row.Add(toggle, 1);
        row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => toggle.IsToggled = !toggle.IsToggled) });
        return row;
    }

    /// <summary>A question with a main button and Cancel. Resolves to true for the main one.</summary>
    public static async Task<bool> Confirm(string title, string message, string accept, string cancel) =>
        await Show(new DialogSheet(title, message, s =>
        {
            var main = s.Button(accept, "PrimaryButton", accept);
            if (DestructiveWords.Any(w => accept.StartsWith(w, StringComparison.OrdinalIgnoreCase)))
                main.BackgroundColor = Resource<Color>("Danger");
            var other = s.Button(cancel, "SecondaryButton", null);
            // Side by side (Cancel, then the main one) when both fit, so the sheet is only as tall as it needs; long
            // labels stack instead of being cut off.
            if (accept.Length <= 16 && cancel.Length <= 16)
            {
                main.HeightRequest = other.HeightRequest = 48;
                var row = new Grid { ColumnSpacing = 10, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
                row.Add(other, 0);
                row.Add(main, 1);
                return row;
            }
            var buttons = new VerticalStackLayout { Spacing = 8 };
            buttons.Add(main);
            buttons.Add(other);
            return buttons;
        })) != null;

    /// <summary>A message with OK.</summary>
    public static Task Alert(string title, string message) =>
        Show(new DialogSheet(title, message, s => s.Button("OK", "PrimaryButton", "OK")));

    /// <summary>A text box with Save (or <paramref name="accept"/>) and Cancel. Resolves to the text, or null when cancelled.</summary>
    public static Task<string?> Prompt(string title, string message, string initial, Keyboard keyboard, string accept)
    {
        Entry? entry = null;
        var sheet = new DialogSheet(title, message, s =>
        {
            entry = new Entry { Text = initial, Keyboard = keyboard, FontSize = 17, ReturnType = ReturnType.Done };
            entry.Completed += (_, _) => s.Choose(entry.Text ?? "");
            var box = new Border { Style = Resource<Style>("InputBox"), HeightRequest = 52, Padding = new Thickness(12, 0), Content = entry };
            var layout = new VerticalStackLayout { Spacing = 8 };
            layout.Add(box);
            var save = new Button { Text = accept, Style = Resource<Style>("PrimaryButton"), Margin = new Thickness(0, 8, 0, 0) };
            save.Clicked += (_, _) => s.Choose(entry.Text ?? "");
            layout.Add(save);
            layout.Add(s.Button("Cancel", "SecondaryButton", null));
            return layout;
        });
        // Ready to type once it has slid up.
        sheet.Appearing += async (_, _) =>
        {
            await Task.Delay(350);
            entry?.Focus();
            if (entry != null)
                entry.CursorPosition = entry.Text?.Length ?? 0;
        };
        return Show(sheet);
    }

    /// <summary>
    /// One or more whole numbers side by side, each between − and + (tap it for the number pad), with Save and Cancel. Resolves to
    /// the numbers, each kept within its field's limits, or null when cancelled.
    /// </summary>
    public static async Task<int[]?> Numbers(string title, string? message, IReadOnlyList<NumberField> fields, string accept)
    {
        var inputs = fields.Select(f => new NumberInput(f)).ToList();
        var sheet = new DialogSheet(title, message, s =>
        {
            var row = new Grid { ColumnSpacing = 14 };
            for (var i = 0; i < inputs.Count; i++)
            {
                row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                row.Add(inputs[i].View, i);
            }
            return s.SaveCancel(row, accept, () => string.Join(",", inputs.Select(x => x.Read())));
        });
        return await Show(sheet) == null ? null : [.. inputs.Select(x => x.Value)];
    }

    /// <summary>
    /// Reps per set: an exact number, or a range (lowest and highest), switched at the top. Resolves to (min, max), the
    /// same number twice for exact reps, or null when cancelled.
    /// </summary>
    public static async Task<(int Min, int Max)?> Reps(string title, int min, int max)
    {
        var exact = new NumberInput(new NumberField("Reps", max, 0, NumberField.NoLimit));
        var low = new NumberInput(new NumberField("Min", min, 0, NumberField.NoLimit));
        var high = new NumberInput(new NumberField("Max", max, 0, NumberField.NoLimit));
        var isRange = min != max;
        var sheet = new DialogSheet(title, "Reps per set", s =>
        {
            // Max under min: side by side, the two don't fit on narrow phones.
            var range = new VerticalStackLayout { Spacing = 16 };
            range.Add(low.View);
            range.Add(high.View);

            // Exact | Range: two halves of one toggle, like the calendar's.
            var toggle = new Grid { ColumnSpacing = 2, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
            var exactHalf = Half("Exact reps");
            var rangeHalf = Half("Rep range");
            toggle.Add(exactHalf, 0);
            toggle.Add(rangeHalf, 1);
            void Show(bool ranged)
            {
                // Carry the number over: exact from the top of the range, the range around the exact number.
                if (ranged && !isRange)
                {
                    var v = exact.Read();
                    low.Set(v);
                    high.Set(v);
                }
                else if (!ranged && isRange)
                    exact.Set(high.Read());
                isRange = ranged;
                exact.View.IsVisible = !ranged;
                range.IsVisible = ranged;
                Paint(exactHalf, !ranged);
                Paint(rangeHalf, ranged);
            }
            exactHalf.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Show(false)) });
            rangeHalf.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Show(true)) });
            var wrap = new Border
            {
                BackgroundColor = Resource<Color>("Surface2"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = 3,
                Content = toggle,
            };

            var body = new VerticalStackLayout { Spacing = 16 };
            body.Add(wrap);
            body.Add(exact.View);
            body.Add(range);
            // As it is now: nothing to carry over yet.
            Show(isRange);
            // Read what's typed: the box being typed in may not have lost focus yet.
            return s.SaveCancel(body, "Save", () => $"{exact.Read()},{low.Read()},{high.Read()}");
        });
        if (await DialogSheet.Show(sheet) == null)
            return null;
        if (!isRange)
            return (exact.Value, exact.Value);
        // Entered the wrong way round: swapped.
        return (Math.Min(low.Value, high.Value), Math.Max(low.Value, high.Value));
    }

    /// <summary>
    /// A rest time: minutes and seconds typed in the middle ("2:30"; "2.30" too, as number keyboards may lack ":"; or just
    /// seconds, "90"), between − and + that step
    /// 15 seconds. From no rest up to 99:59, as much as the box shows. Resolves to seconds, or null when cancelled.
    /// </summary>
    public static async Task<int?> RestTime(string title, int seconds)
    {
        const int StepSeconds = 15, Min = 0, Max = 99 * 60 + 59;
        var value = Math.Clamp(seconds, Min, Max);
        var entry = new Entry
        {
            Text = Clock(value),
            Keyboard = Keyboard.Numeric,
            MaxLength = 5,
            FontSize = 28,
            FontFamily = "OpenSansSemibold",
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        static string Clock(int s) => $"{s / 60}:{s % 60:00}";
        int Read()
        {
            var text = (entry.Text ?? "").Trim().Replace('.', ':').Replace(',', ':');
            var parts = text.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var sec))
                value = Math.Clamp(m * 60 + sec, Min, Max);
            else if (parts.Length == 1 && int.TryParse(parts[0], out var only))
                // A small number is minutes ("2"), a bigger one seconds ("90").
                value = Math.Clamp(only <= 10 ? only * 60 : only, Min, Max);
            return value;
        }
        void Set(int v)
        {
            value = Math.Clamp(v, Min, Max);
            entry.Text = Clock(value);
        }
        entry.Unfocused += (_, _) => Set(Read());

        var sheet = new DialogSheet(title, "Minutes and seconds, e.g. 2:30", s =>
        {
            Button Step(string text, int delta)
            {
                var b = new Button { Text = text, Style = Resource<Style>("StepButton"), WidthRequest = 48, HeightRequest = 48, FontSize = 22, CornerRadius = 14 };
                b.Clicked += (_, _) => Set(Read() + delta);
                return b;
            }
            var box = new Border { Style = Resource<Style>("InputBox"), HeightRequest = 60, Content = entry };
            var line = new Grid { ColumnSpacing = 8, ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)] };
            line.Add(Step("−", -StepSeconds), 0);
            line.Add(box, 1);
            line.Add(Step("+", StepSeconds), 2);
            return s.SaveCancel(line, "Save", () => "ok");
        });
        return await Show(sheet) == null ? null : Read();
    }

    /// <summary>A date and a time of day, picked with the platform's pickers; null when cancelled.</summary>
    public static async Task<DateTime?> DateAndTime(string title, string? message, DateTime initial, string accept)
    {
        var date = new DatePicker { Date = initial.Date, MaximumDate = DateTime.Today, FontSize = 17, VerticalOptions = LayoutOptions.Center };
        var time = new TimePicker { Time = initial.TimeOfDay, FontSize = 17, VerticalOptions = LayoutOptions.Center };
        var sheet = new DialogSheet(title, message, s =>
        {
            var row = new Grid { ColumnSpacing = 10, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
            row.Add(new Border { Style = Resource<Style>("InputBox"), HeightRequest = 52, Padding = new Thickness(12, 0), Content = date }, 0);
            row.Add(new Border { Style = Resource<Style>("InputBox"), HeightRequest = 52, Padding = new Thickness(12, 0), Content = time }, 1);
            return s.SaveCancel(row, accept, () => "ok");
        });
        if (await Show(sheet) == null)
            return null;
        var day = date.Date ?? initial.Date;
        var at = time.Time ?? initial.TimeOfDay;
        return day.Date + new TimeSpan(at.Hours, at.Minutes, 0);
    }

    /// <summary>
    /// A month calendar to pick a day, no later than <paramref name="max"/>: ‹ › change the month, a dot under each day
    /// that <paramref name="hasData"/>, the picked day filled with the accent and today in it. Null when cancelled.
    /// </summary>
    public static async Task<DateTime?> Calendar(string title, DateTime initial, DateTime max, Func<DateTime, bool> hasData)
    {
        var accent = Resource<Color>("Accent");
        var month = new DateTime(initial.Year, initial.Month, 1);
        var firstDay = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var monthLabel = new Label { FontFamily = "OpenSansSemibold", FontSize = 17, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var days = new Grid { RowSpacing = 2, ColumnSpacing = 2 };
        for (var c = 0; c < 7; c++)
            days.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        Button? next = null;

        var result = await Show(new DialogSheet(title, null, s =>
        {
            var layout = new VerticalStackLayout { Spacing = 10 };

            var prev = new Button { Text = "‹", Style = Resource<Style>("StepButton") };
            next = new Button { Text = "›", Style = Resource<Style>("StepButton") };
            prev.Clicked += (_, _) => { month = month.AddMonths(-1); Fill(); };
            next.Clicked += (_, _) => { if (month.AddMonths(1) <= max) { month = month.AddMonths(1); Fill(); } };
            var top = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)] };
            top.Add(prev, 0);
            top.Add(monthLabel, 1);
            top.Add(next, 2);
            layout.Add(top);

            // The weekdays' initials, starting on the culture's first day.
            var names = new Grid();
            for (var c = 0; c < 7; c++)
            {
                names.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                var name = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames[((int)firstDay + c) % 7];
                names.Add(new Label { Text = name, Style = Resource<Style>("Caption"), FontSize = 12, HorizontalOptions = LayoutOptions.Center }, c);
            }
            layout.Add(names);
            layout.Add(days);

            var buttons = new Grid { ColumnSpacing = 10, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
            var cancel = s.Button("Cancel", "SecondaryButton", null);
            var today = s.Button("Today", "PrimaryButton", max.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            cancel.HeightRequest = today.HeightRequest = 48;
            buttons.Add(cancel, 0);
            buttons.Add(today, 1);
            layout.Add(buttons);

            Fill();
            return layout;

            void Fill()
            {
                monthLabel.Text = month.ToString("MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture);
                next!.Opacity = month.AddMonths(1) <= max ? 1 : 0.3;
                days.Children.Clear();
                days.RowDefinitions.Clear();
                var offset = ((int)month.DayOfWeek - (int)firstDay + 7) % 7;
                var count = DateTime.DaysInMonth(month.Year, month.Month);
                var rows = (offset + count + 6) / 7;
                for (var r = 0; r < rows; r++)
                    days.RowDefinitions.Add(new RowDefinition(46));
                for (var d = 1; d <= count; d++)
                {
                    var date = new DateTime(month.Year, month.Month, d);
                    var cell = DayCell(date, date == initial.Date, date == DateTime.Today, date > max.Date, hasData(date), accent);
                    if (date <= max.Date)
                        cell.GestureRecognizers.Add(new TapGestureRecognizer
                        {
                            Command = new Command(() => s.Choose(date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))),
                        });
                    var index = offset + d - 1;
                    days.Add(cell, index % 7, index / 7);
                }
            }
        }));
        return result is { Length: > 0 } picked
            && DateTime.TryParseExact(picked, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var day)
            ? day
            : null;
    }

    // A day in the calendar: its number in a circle (filled when picked), a dot under it when it has data.
    static View DayCell(DateTime date, bool picked, bool today, bool future, bool hasData, Color accent)
    {
        var number = new Border
        {
            WidthRequest = 34,
            HeightRequest = 34,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 17 },
            StrokeThickness = today && !picked ? 1.5 : 0,
            Stroke = accent,
            BackgroundColor = picked ? accent : Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = date.Day.ToString(System.Globalization.CultureInfo.CurrentCulture),
                FontSize = 15,
                FontFamily = picked || today ? "OpenSansSemibold" : null,
                TextColor = picked ? Colors.White : future ? Resource<Color>("TextTertiary") : Resource<Color>("TextPrimary"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            },
        };
        var cell = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Fill, BackgroundColor = Colors.Transparent };
        cell.Add(number);
        cell.Add(new Microsoft.Maui.Controls.Shapes.Ellipse
        {
            WidthRequest = 5,
            HeightRequest = 5,
            Fill = new SolidColorBrush(hasData ? accent : Colors.Transparent),
            HorizontalOptions = LayoutOptions.Center,
        });
        return cell;
    }

    /// <summary>One half of a two-way toggle (Exact | Range, Add | Subtract): tap to pick it.</summary>
    static Border Half(string text) => new()
    {
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 9 },
        StrokeThickness = 0,
        Padding = new Thickness(0, 9),
        Content = new Label { Text = text, FontFamily = "OpenSansSemibold", FontSize = 14, HorizontalOptions = LayoutOptions.Center },
    };

    static void Paint(Border half, bool on)
    {
        half.BackgroundColor = on ? Resource<Color>("Accent") : Colors.Transparent;
        ((Label)half.Content!).TextColor = on ? Colors.White : Resource<Color>("TextSecondary");
    }

    /// <summary>The two halves in the toggle's grey track.</summary>
    static Border Toggle(Border left, Border right)
    {
        var grid = new Grid { ColumnSpacing = 2, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
        grid.Add(left, 0);
        grid.Add(right, 1);
        return new Border
        {
            BackgroundColor = Resource<Color>("Surface2"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 0,
            Padding = 3,
            Content = grid,
        };
    }

    /// <summary>
    /// An amount to add or subtract (e.g. weight across sets): Add | Subtract at the top, then the amount, typed or
    /// stepped by <paramref name="step"/>. Resolves to the signed amount (negative to subtract), or null when cancelled.
    /// </summary>
    public static async Task<double?> Change(string title, string? message, string unit, double step)
    {
        var subtract = false;
        var amount = step;
        var entry = new Entry
        {
            Text = Format(amount),
            Keyboard = Keyboard.Numeric,
            MaxLength = 6,
            FontSize = 28,
            FontFamily = "OpenSansSemibold",
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        static string Format(double v) => v.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
        double Read()
        {
            var text = entry.Text?.Replace(',', '.') ?? "";
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v >= 0)
                amount = Math.Min(v, 999);
            return amount;
        }
        void Set(double v)
        {
            amount = Math.Clamp(Math.Round(v, 2), 0, 999);
            entry.Text = Format(amount);
        }
        entry.Unfocused += (_, _) => Set(Read());

        var sheet = new DialogSheet(title, message, s =>
        {
            var add = Half("+  Add");
            var less = Half("−  Subtract");
            void Pick(bool minus)
            {
                subtract = minus;
                Paint(add, !minus);
                Paint(less, minus);
            }
            add.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Pick(false)) });
            less.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Pick(true)) });
            Pick(false);

            Button Step(string text, int direction)
            {
                var b = new Button { Text = text, Style = Resource<Style>("StepButton"), WidthRequest = 48, HeightRequest = 48, FontSize = 22, CornerRadius = 14 };
                b.Clicked += (_, _) => Set(Read() + direction * step);
                return b;
            }
            var box = new Border { Style = Resource<Style>("InputBox"), HeightRequest = 60, Content = entry };
            var line = new Grid { ColumnSpacing = 8, ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)] };
            line.Add(Step("−", -1), 0);
            line.Add(box, 1);
            line.Add(Step("+", 1), 2);
            var number = new VerticalStackLayout { Spacing = 6 };
            number.Add(new Label { Text = unit.ToUpperInvariant(), Style = Resource<Style>("Overline"), FontSize = 11, HorizontalOptions = LayoutOptions.Center });
            number.Add(line);

            var body = new VerticalStackLayout { Spacing = 16 };
            body.Add(Toggle(add, less));
            body.Add(number);
            return s.SaveCancel(body, "Apply", () => "ok");
        });
        if (await Show(sheet) == null)
            return null;
        var value = Read();
        return subtract ? -value : value;
    }

    /// <summary><paramref name="content"/>, then the main button (resolving to <paramref name="result"/>) and Cancel.</summary>
    View SaveCancel(View content, string accept, Func<string> result)
    {
        var layout = new VerticalStackLayout { Spacing = 8 };
        layout.Add(content);
        var save = new Button { Text = accept, Style = Resource<Style>("PrimaryButton"), Margin = new Thickness(0, 12, 0, 0) };
        save.Clicked += (_, _) => Choose(result());
        layout.Add(save);
        layout.Add(Button("Cancel", "SecondaryButton", null));
        return layout;
    }

    /// <summary>A labelled whole number between − and + (tap it for the number pad), kept within the field's limits.</summary>
    sealed class NumberInput
    {
        readonly NumberField _field;
        readonly Label _number;

        public NumberInput(NumberField field)
        {
            _field = field;
            Value = Math.Clamp(field.Value, field.Min, field.Max);
            // Tap the number to pick it on the number pad (swipe or type), or step it with − and +.
            _number = new Label
            {
                Text = Value.ToString(),
                FontSize = 28,
                FontFamily = "OpenSansSemibold",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            var box = new Border { Style = Resource<Style>("InputBox"), HeightRequest = 60, Content = _number };
            box.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () =>
            {
                if (await NumberPadSheet.Show(Value.ToString(), Value, 1, field.Min, Math.Min(field.Max, field.Min + 200), field.Label.ToLowerInvariant()) is { } v)
                    Set((int)Math.Round(v));
            }) });
            var line = new Grid { ColumnSpacing = 8, ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)] };
            line.Add(Step("−", -1), 0);
            line.Add(box, 1);
            line.Add(Step("+", 1), 2);
            var stack = new VerticalStackLayout { Spacing = 6 };
            stack.Add(new Label { Text = field.Label.ToUpperInvariant(), Style = Resource<Style>("Overline"), FontSize = 11, HorizontalOptions = LayoutOptions.Center });
            stack.Add(line);
            View = stack;
        }

        public View View { get; }
        public int Value { get; private set; }

        /// <summary>The number as it stands (kept within the limits).</summary>
        public int Read() => Value;

        public void Set(int value)
        {
            Value = Math.Clamp(value, _field.Min, _field.Max);
            _number.Text = Value.ToString();
        }

        Button Step(string text, int delta)
        {
            var b = new Button { Text = text, Style = Resource<Style>("StepButton"), WidthRequest = 48, HeightRequest = 48, FontSize = 22, CornerRadius = 14 };
            b.Clicked += (_, _) => Set(Read() + delta);
            return b;
        }
    }

    /// <summary>Like <see cref="Prompt"/>, for a password: hidden as it's typed, with a button to show it.</summary>
    public static Task<string?> PasswordPrompt(string title, string message, string accept)
    {
        AppSkeleton.CPasswordEntry? entry = null;
        var sheet = new DialogSheet(title, message, s =>
        {
            entry = new AppSkeleton.CPasswordEntry
            {
                ReturnType = ReturnType.Done,
                TextColor = Resource<Color>("TextPrimary"),
                PlaceholderColor = Resource<Color>("TextTertiary"),
                Placeholder = "Password",
            };
            entry.Completed += (_, _) => s.Choose(entry.Text ?? "");
            var box = new Border { Style = Resource<Style>("InputBox"), HeightRequest = 52, Padding = new Thickness(12, 0), Content = entry };
            var layout = new VerticalStackLayout { Spacing = 8 };
            layout.Add(box);
            var main = new Button { Text = accept, Style = Resource<Style>("PrimaryButton"), Margin = new Thickness(0, 8, 0, 0) };
            if (DestructiveWords.Any(w => accept.StartsWith(w, StringComparison.OrdinalIgnoreCase)))
                main.BackgroundColor = Resource<Color>("Danger");
            main.Clicked += (_, _) => s.Choose(entry.Text ?? "");
            layout.Add(main);
            layout.Add(s.Button("Cancel", "SecondaryButton", null));
            return layout;
        });
        // Ready to type once it has slid up.
        sheet.Appearing += async (_, _) =>
        {
            await Task.Delay(350);
            entry?.Focus();
        };
        return Show(sheet);
    }

    View Row(string text, bool destructive)
    {
        var label = new Label
        {
            Text = text,
            FontFamily = "OpenSansSemibold",
            FontSize = 17,
            TextColor = destructive ? Resource<Color>("Danger") : Resource<Color>("TextPrimary"),
            VerticalOptions = LayoutOptions.Center,
        };
        var row = new Grid { Padding = new Thickness(18, 0), MinimumHeightRequest = 54, Children = { label } };
        row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Choose(text)) });
        return row;
    }

    Button Button(string text, string style, string? result)
    {
        var button = new Button { Text = text, Style = Resource<Style>(style) };
        if (style == "SecondaryButton")
            button.TextColor = Resource<Color>("TextPrimary");
        button.Clicked += (_, _) => Choose(result);
        return button;
    }

    /// <summary>Slides away, then resolves with <paramref name="choice"/> (null = cancelled).</summary>
    async void Choose(string? choice)
    {
        if (_result.Task.IsCompleted || _choice != null)
            return;
        _choice = choice ?? "";
        await CloseAsync();
        _result.TrySetResult(choice);
    }

    protected override bool OnBackButtonPressed()
    {
        Choose(null);
        return true;
    }

    // Closed some other way (e.g. the page under it navigated away): treat it as cancelled.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_choice == null)
            _result.TrySetResult(null);
    }

    static async Task<string?> Show(DialogSheet sheet)
    {
        var navigation = Shell.Current?.Navigation ?? Application.Current!.Windows[0].Page!.Navigation;
        await navigation.PushModalAsync(sheet, false);
        return await sheet._result.Task;
    }

    static T Resource<T>(string key) => (T)Application.Current!.Resources[key];
}

/// <summary>An on/off setting in a <see cref="DialogSheet.Menu"/>, applied through <see cref="Changed"/> as it's flipped.</summary>
public record MenuSwitch(string Title, string? Detail, bool IsOn, Action<bool> Changed);

/// <summary>A number in <see cref="DialogSheet.Numbers"/>: its label, starting value and limits.</summary>
public record NumberField(string Label, int Value, int Min, int Max)
{
    /// <summary>A Max for numbers with no real limit (as high as the box can show).</summary>
    public const int NoLimit = 999;
}
