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

    DialogSheet(string title, string? message, Func<DialogSheet, View> body)
    {
        Backdrop = Color.FromArgb("#B3000000");
        Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
        Shell.SetNavBarIsVisible(this, false);

        var header = new VerticalStackLayout { Spacing = 6 };
        header.Add(new Label { Text = title, Style = Resource<Style>("H2"), FontSize = 20 });
        if (!string.IsNullOrWhiteSpace(message))
            header.Add(new Label { Text = message, Style = Resource<Style>("Caption"), FontSize = 15 });

        var stack = new VerticalStackLayout { Padding = new Thickness(20, 22, 20, 18), Spacing = 16 };
        stack.Add(header);
        stack.Add(body(this));

        var sheet = new Border
        {
            BackgroundColor = Resource<Color>("Bg"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
            StrokeThickness = 0,
            Padding = 0,
            VerticalOptions = LayoutOptions.End,
            Content = new ScrollView { Content = stack },
        };
        // Swallows taps, so only the dimmed area above closes the sheet.
        sheet.GestureRecognizers.Add(new TapGestureRecognizer());

        var root = new Grid { Margin = new Thickness(0, 28, 0, 0) };
        root.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Choose(null)) });
        root.Add(sheet);
        Content = root;
    }

    /// <summary>A menu: the options as rows (the destructive one in red), then Cancel. Resolves to the chosen option.</summary>
    public static Task<string?> Menu(string title, string? destructive, string[] options) =>
        Show(new DialogSheet(title, null, s =>
        {
            var list = new VerticalStackLayout { Spacing = 10 };
            var card = new VerticalStackLayout();
            var all = destructive == null ? options : [.. options, destructive];
            for (var i = 0; i < all.Length; i++)
            {
                if (i > 0)
                    card.Add(new BoxView { HeightRequest = 1, Color = Resource<Color>("Stroke"), Margin = new Thickness(18, 0) });
                card.Add(s.Row(all[i], all[i] == destructive));
            }
            list.Add(new Border
            {
                BackgroundColor = Resource<Color>("Surface2"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
                StrokeThickness = 0,
                Padding = new Thickness(0, 4),
                Content = card,
            });
            list.Add(s.Button("Cancel", "SecondaryButton", null));
            return list;
        }));

    /// <summary>A question with a main button and Cancel. Resolves to true for the main one.</summary>
    public static async Task<bool> Confirm(string title, string message, string accept, string cancel) =>
        await Show(new DialogSheet(title, message, s =>
        {
            var buttons = new VerticalStackLayout { Spacing = 8 };
            var main = s.Button(accept, "PrimaryButton", accept);
            if (DestructiveWords.Any(w => accept.StartsWith(w, StringComparison.OrdinalIgnoreCase)))
                main.BackgroundColor = Resource<Color>("Danger");
            buttons.Add(main);
            buttons.Add(s.Button(cancel, "SecondaryButton", null));
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
