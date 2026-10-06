using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>
/// Writing a ticket or comment. The toolbar puts Markdown around the selection (or a placeholder to type over), or in
/// front of each selected line for headings, lists, checklists and quotes; attachments go in where the cursor is.
/// </summary>
public partial class FeedbackEditPage : BasePage
{
    FeedbackEditViewModel ViewModel => (FeedbackEditViewModel)BindingContext;

    public FeedbackEditPage(FeedbackEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.InsertRequested += InsertBlock;
    }

    protected override bool OnBackButtonPressed()
    {
        ViewModel.BackCommand.Execute(null);
        return true;
    }

    void OnFormat(object? sender, EventArgs e)
    {
        switch ((sender as Element)?.ClassId)
        {
            case "bold": Wrap("**", "**", "bold text"); break;
            case "italic": Wrap("_", "_", "italic text"); break;
            case "strike": Wrap("~~", "~~", "crossed out"); break;
            case "heading": Lines(_ => "### "); break;
            case "bullets": Lines(_ => "- "); break;
            case "numbers": Lines(i => $"{i + 1}. "); break;
            case "tasks": Lines(_ => "- [ ] "); break;
            case "quote": Lines(_ => "> "); break;
            case "code":
                // A block for more than one line, inline otherwise.
                var (text, start, length) = Selection();
                if (text.Substring(start, length).Contains('\n'))
                    Wrap("```\n", "\n```", "code");
                else
                    Wrap("`", "`", "code");
                break;
            case "link": Link(); break;
        }
    }

    (string Text, int Start, int Length) Selection()
    {
        var text = BodyEditor.Text ?? "";
        var start = Math.Clamp(BodyEditor.CursorPosition, 0, text.Length);
        return (text, start, Math.Clamp(BodyEditor.SelectionLength, 0, text.Length - start));
    }

    /// <summary>The selection between <paramref name="before"/> and <paramref name="after"/>, or a placeholder, selected to type over.</summary>
    void Wrap(string before, string after, string placeholder)
    {
        var (text, start, length) = Selection();
        var inner = length > 0 ? text.Substring(start, length) : placeholder;
        Set(text[..start] + before + inner + after + text[(start + length)..], start + before.Length, inner.Length);
    }

    /// <summary>Each line the selection touches (or the cursor's) starts with <paramref name="prefix"/> (given its index).</summary>
    void Lines(Func<int, string> prefix)
    {
        var (text, start, length) = Selection();
        var lineStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;
        var end = start + length;
        var lines = text[lineStart..end].Split('\n').Select((line, i) => prefix(i) + line);
        var block = string.Join("\n", lines);
        Set(text[..lineStart] + block + text[end..], lineStart + block.Length, 0);
    }

    /// <summary>[selection](https://), with the address selected to paste over.</summary>
    void Link()
    {
        var (text, start, length) = Selection();
        var label = length > 0 ? text.Substring(start, length) : "link text";
        const string url = "https://";
        var link = $"[{label}]({url})";
        Set(text[..start] + link + text[(start + length)..], start + label.Length + 3, url.Length);
    }

    /// <summary>An attachment, on a line of its own where the cursor is.</summary>
    void InsertBlock(string markdown)
    {
        var (text, start, length) = Selection();
        var snippet = (start > 0 && text[start - 1] != '\n' ? "\n" : "") + markdown + "\n";
        Set(text[..start] + snippet + text[(start + length)..], start + snippet.Length, 0);
    }

    void Set(string text, int cursor, int selection)
    {
        BodyEditor.Text = text;
        // After the text has gone in, or the cursor goes back to the start on some platforms.
        Dispatcher.Dispatch(() =>
        {
            BodyEditor.Focus();
            BodyEditor.CursorPosition = Math.Min(cursor, text.Length);
            BodyEditor.SelectionLength = Math.Min(selection, text.Length - Math.Min(cursor, text.Length));
        });
    }

    // Links in the preview open in the browser, not in the preview.
    void OnPreviewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https" or "mailto"))
            return;
        e.Cancel = true;
        _ = Launcher.Default.OpenAsync(uri);
    }
}
