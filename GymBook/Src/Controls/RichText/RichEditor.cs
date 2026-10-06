namespace GymBook.Controls.RichText;

/// <summary>What <see cref="RichEditor"/> needs of each platform's text box (see RichEditorHandler).</summary>
public interface IRichTextPlatform
{
    /// <summary>The text shown, lines apart with '\n'.</summary>
    string Text { get; }
    int SelectionStart { get; }
    int SelectionLength { get; }
    /// <summary>Mid-word in an input method (iOS's marked text): edits wait until it's done.</summary>
    bool IsComposing { get; }
    void Replace(int start, int length, string text);
    void Select(int start, int length);
    /// <summary>
    /// The text painted with <paramref name="runs"/> (anything not in a run plain): at least the part from
    /// <paramref name="start"/> for <paramref name="length"/> (whole lines), where it changed; the rest kept its look as
    /// it moved. A platform where painting is cheap may paint it all.
    /// </summary>
    void Paint(IReadOnlyList<StyleRun> runs, int start, int length);
    void Focus();
}

/// <summary>
/// A text box that shows formatting as it's typed, like a word processor: bold is bold, headings big, lists with their
/// bullets and numbers, checklists with their boxes. Its text is <see cref="Markdown"/>, as GitHub reads it (see
/// <see cref="RichMarkdown"/>), so whatever binds to it deals only in Markdown. The formatting itself is kept in a
/// <see cref="RichDocument"/>; each platform only shows it and says what was typed (see RichEditorHandler).
/// </summary>
public class RichEditor : View
{
    public static readonly BindableProperty MarkdownProperty = BindableProperty.Create(nameof(Markdown), typeof(string), typeof(RichEditor), "",
        BindingMode.TwoWay, propertyChanged: (b, _, v) => ((RichEditor)b).OnMarkdownChanged((string?)v ?? ""));

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(RichEditor), "");
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(RichEditor), 15d);

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    RichDocument _doc = new();
    // The Markdown last read or written here, so its own change coming back isn't read again.
    string _markdown = "";
    IRichTextPlatform? _platform;
    // While this changes the platform's text: its change events are this, not typing.
    bool _busy;
    // Where the cursor was when styles were picked for what's typed next: moving away drops them.
    int _pendingAt = -1;

    /// <summary>Whether the text box shows nothing (for a placeholder).</summary>
    public bool IsEmpty => _doc.Lines.Count == 1 && _doc.Lines[0].Text.Length == 0 && _doc.Lines[0].Kind == BlockKind.Paragraph;

    void OnMarkdownChanged(string markdown)
    {
        if (markdown == _markdown)
            return;
        _markdown = markdown;
        _doc = RichMarkdown.Parse(markdown);
        if (_platform != null)
            Show(_doc.ToShown(_doc.Lines.Count - 1, int.MaxValue), 0);
    }

    // ---- The platform ----

    internal void Attach(IRichTextPlatform platform)
    {
        _platform = platform;
        Show(_doc.ToShown(_doc.Lines.Count - 1, int.MaxValue), 0);
    }

    internal void Detach() => _platform = null;

    /// <summary>Something was typed, deleted or pasted.</summary>
    internal void OnPlatformTextChanged()
    {
        if (_busy || _platform is not { IsComposing: false } platform)
            return;
        var now = platform.Text;
        // Only its look changed (Windows says so too).
        if (now == _doc.Render())
            return;
        var caret = _doc.ApplyEdit(now, out var first, out var last);
        _busy = true;
        try
        {
            var shown = _doc.Render();
            if (shown != now)
            {
                // A marker came or went (a new item's bullet, one backspaced away): the text box catches up.
                var (start, length) = Patch(platform, shown);
                var (patchFirst, patchLast) = _doc.LinesOf(start, length);
                (first, last) = (Math.Min(first, patchFirst), Math.Max(last, patchLast));
                platform.Select(caret, 0);
            }
            // Only the lines that changed are painted again.
            var (dirtyStart, dirtyLength) = _doc.LinesSpan(first, last);
            platform.Paint(_doc.Styles(), dirtyStart, dirtyLength);
        }
        finally
        {
            _busy = false;
        }
        Publish();
    }

    internal void OnPlatformSelectionChanged()
    {
        if (_busy || _platform == null || _doc.Pending == null)
            return;
        // The cursor moving as a letter goes in (its edit still to come, on Android) keeps them for that letter.
        if (_platform.Text != _doc.Render())
            return;
        if (_platform.SelectionStart != _pendingAt || _platform.SelectionLength != 0)
            _doc.Pending = null;
    }

    /// <summary>The document, shown: the text box's text made the same with the least change, then painted.</summary>
    void Show(int selectionStart, int selectionLength)
    {
        if (_platform is not { } platform)
            return;
        _busy = true;
        try
        {
            var shown = _doc.Render();
            if (shown != platform.Text)
                Patch(platform, shown);
            platform.Select(Math.Clamp(selectionStart, 0, shown.Length), Math.Clamp(selectionLength, 0, shown.Length - Math.Clamp(selectionStart, 0, shown.Length)));
            platform.Paint(_doc.Styles(), 0, shown.Length);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// Only the part that differs is replaced, so the rest (and the keyboard's place in it) stays put. Returns where the
    /// new part is.
    /// </summary>
    static (int Start, int Length) Patch(IRichTextPlatform platform, string shown)
    {
        var now = platform.Text;
        var a = 0;
        var max = Math.Min(now.Length, shown.Length);
        while (a < max && now[a] == shown[a])
            a++;
        var b = 0;
        while (b < max - a && now[now.Length - 1 - b] == shown[shown.Length - 1 - b])
            b++;
        platform.Replace(a, now.Length - b - a, shown.Substring(a, shown.Length - b - a));
        return (a, shown.Length - b - a);
    }

    void Publish()
    {
        _markdown = RichMarkdown.Write(_doc);
        Markdown = _markdown;
        OnPropertyChanged(nameof(IsEmpty));
    }

    // ---- Formatting, from the toolbar ----

    (int Start, int Length) Selection => _platform == null ? (0, 0) : (_platform.SelectionStart, _platform.SelectionLength);

    /// <summary>Bold, italics, strikethrough or code: on the selection, or for what's typed next.</summary>
    public void ToggleStyle(InlineStyle style)
    {
        var (start, length) = Selection;
        _doc.ToggleStyle(style, start, length);
        if (length == 0)
            _pendingAt = start;
        Refresh(start, length);
    }

    /// <summary>The selected lines (or the cursor's) as a heading, list, checklist or quote, or back to plain text.</summary>
    public void ToggleBlock(BlockKind kind)
    {
        var (start, length) = Selection;
        var (l1, o1, _) = _doc.Locate(start);
        var (l2, o2, _) = _doc.Locate(start + length);
        _doc.ToggleBlock(kind, start, length);
        // The same text selected, wherever the markers moved it.
        var from = _doc.ToShown(l1, o1);
        Refresh(from, _doc.ToShown(l2, o2) - from);
    }

    /// <summary>The selection links to <paramref name="url"/>; nothing selected, the address goes in as a link.</summary>
    public void SetLink(string url)
    {
        var (start, length) = Selection;
        Refresh(_doc.SetLink(url, start, length), 0);
    }

    /// <summary>A picture or file, as a link on a line of its own, where the cursor is.</summary>
    public void InsertAttachment(string label, string url, bool image)
    {
        var (start, _) = Selection;
        Refresh(_doc.InsertAttachment(label, url, image, start), 0);
    }

    void Refresh(int selectionStart, int selectionLength)
    {
        Show(selectionStart, selectionLength);
        _platform?.Focus();
        Publish();
    }
}
