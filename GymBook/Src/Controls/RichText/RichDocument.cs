using System.Text;

namespace GymBook.Controls.RichText;

/// <summary>What a line is: plain text, a heading, an item of a list or checklist, a quote, code, or something kept as written.</summary>
public enum BlockKind { Paragraph, Heading, Bullet, Numbered, Task, Quote, Code, Raw }

[Flags]
public enum InlineStyle { None = 0, Bold = 1, Italic = 2, Strike = 4, Code = 8 }

/// <summary>
/// How one character looks: its styles, and the link it's part of. A picture is one character of its own,
/// <see cref="RichDocument.ImageChar"/>, with <see cref="Image"/> set, its address as the link and its name as <see cref="Label"/>.
/// </summary>
public readonly record struct CharFormat(InlineStyle Style, string? Link = null, bool Image = false, string? Label = null)
{
    public static readonly CharFormat Plain = new(InlineStyle.None);
}

/// <summary>What a stretch of the shown text looks like, for the platform to paint.</summary>
[Flags]
public enum RunStyle
{
    None = 0, Bold = 1, Italic = 2, Strike = 4, Code = 8, Link = 16,
    /// <summary>A list's bullet or number, a checkbox or a quote's bar: drawn, not typed.</summary>
    Marker = 32,
    Heading1 = 64, Heading2 = 128, Heading3 = 256,
    Quote = 512, CodeBlock = 1024, Checked = 2048,
    /// <summary>A picture, drawn in place of its character (<see cref="StyleRun.Url"/> is where it's from).</summary>
    Image = 4096,
}

public readonly record struct StyleRun(int Start, int Length, RunStyle Style, string? Url = null);

public sealed class RichLine
{
    public BlockKind Kind { get; set; }
    public int HeadingLevel { get; set; } = 2;
    public bool Checked { get; set; }
    public StringBuilder Text { get; } = new();
    public List<CharFormat> Formats { get; } = [];

    public RichLine(BlockKind kind = BlockKind.Paragraph) => Kind = kind;

    public void Append(string text, CharFormat format)
    {
        Text.Append(text);
        Formats.AddRange(Enumerable.Repeat(format, text.Length));
    }

    public void Insert(int at, string text, CharFormat format)
    {
        Text.Insert(at, text);
        Formats.InsertRange(at, Enumerable.Repeat(format, text.Length));
    }

    public void Remove(int at, int length)
    {
        Text.Remove(at, length);
        Formats.RemoveRange(at, length);
    }

    /// <summary>Everything from <paramref name="at"/> on, taken off into a new line of <paramref name="kind"/>.</summary>
    public RichLine SplitOff(int at, BlockKind kind)
    {
        var rest = new RichLine(kind) { HeadingLevel = HeadingLevel };
        rest.Text.Append(Text.ToString(at, Text.Length - at));
        rest.Formats.AddRange(Formats.Skip(at));
        Remove(at, Text.Length - at);
        return rest;
    }

    public bool IsListLike => Kind is BlockKind.Bullet or BlockKind.Numbered or BlockKind.Task or BlockKind.Quote;
}

/// <summary>
/// Formatted text, edited as it's shown: each line's text, after a marker drawn for its kind (a bullet, a number, a
/// checkbox, a quote's bar), with each character's look. The platform's text box shows <see cref="Render"/> (markers
/// and text, lines apart) painted with <see cref="Styles"/>; what's typed comes back through <see cref="ApplyEdit"/>,
/// worked out from what changed in the shown text. Markdown in and out is <see cref="RichMarkdown"/>.
/// </summary>
public sealed class RichDocument
{
    public List<RichLine> Lines { get; } = [new()];

    /// <summary>Styles for what's typed next, set by Bold and the like with nothing selected; null: like the text before.</summary>
    public InlineStyle? Pending { get; set; }

    // ---- What's shown ----

    /// <summary>The marker drawn before each line.</summary>
    public string[] Prefixes()
    {
        var prefixes = new string[Lines.Count];
        var number = 0;
        for (var i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            number = line.Kind == BlockKind.Numbered ? number + 1 : 0;
            prefixes[i] = line.Kind switch
            {
                BlockKind.Bullet => "•  ",
                BlockKind.Numbered => $"{number}. ",
                BlockKind.Task => line.Checked ? "☑ " : "☐ ",
                BlockKind.Quote => "┃ ",
                _ => "",
            };
        }
        return prefixes;
    }

    public string Render()
    {
        var prefixes = Prefixes();
        var text = new StringBuilder();
        for (var i = 0; i < Lines.Count; i++)
        {
            if (i > 0)
                text.Append('\n');
            text.Append(prefixes[i]).Append(Lines[i].Text);
        }
        return text.ToString();
    }

    /// <summary>Where each line starts in the shown text, and its marker.</summary>
    (int[] Starts, string[] Prefixes) Layout()
    {
        var prefixes = Prefixes();
        var starts = new int[Lines.Count];
        var at = 0;
        for (var i = 0; i < Lines.Count; i++)
        {
            starts[i] = at;
            at += prefixes[i].Length + Lines[i].Text.Length + 1;
        }
        return (starts, prefixes);
    }

    /// <summary>A place in the shown text as a line and a place in its text; inside a marker counts as its start.</summary>
    public (int Line, int Offset, bool InPrefix) Locate(int shown)
    {
        var (starts, prefixes) = Layout();
        for (var i = Lines.Count - 1; i >= 0; i--)
        {
            if (shown < starts[i])
                continue;
            var into = shown - starts[i];
            if (into < prefixes[i].Length)
                return (i, 0, true);
            return (i, Math.Min(into - prefixes[i].Length, Lines[i].Text.Length), false);
        }
        return (0, 0, prefixes[0].Length > 0);
    }

    public int ToShown(int line, int offset)
    {
        var (starts, prefixes) = Layout();
        line = Math.Clamp(line, 0, Lines.Count - 1);
        return starts[line] + prefixes[line].Length + Math.Clamp(offset, 0, Lines[line].Text.Length);
    }

    /// <summary>How the shown text looks, run by run.</summary>
    public List<StyleRun> Styles()
    {
        var (starts, prefixes) = Layout();
        var runs = new List<StyleRun>();
        for (var i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            var block = line.Kind switch
            {
                BlockKind.Heading => line.HeadingLevel <= 1 ? RunStyle.Heading1 : line.HeadingLevel == 2 ? RunStyle.Heading2 : RunStyle.Heading3,
                BlockKind.Quote => RunStyle.Quote,
                BlockKind.Code => RunStyle.CodeBlock,
                BlockKind.Task when line.Checked => RunStyle.Checked,
                _ => RunStyle.None,
            };
            if (prefixes[i].Length > 0)
                Add(runs, starts[i], prefixes[i].Length, RunStyle.Marker | (block & RunStyle.Quote));
            var textStart = starts[i] + prefixes[i].Length;
            for (var c = 0; c < line.Text.Length; c++)
            {
                var f = line.Formats[c];
                if (f.Image && line.Text[c] == ImageChar)
                {
                    // Each picture a run of its own: the platform draws it in place of its character.
                    runs.Add(new StyleRun(textStart + c, 1, RunStyle.Image, f.Link));
                    continue;
                }
                var style = block | (RunStyle)(int)f.Style;
                if (f.Link != null)
                    style |= RunStyle.Link;
                Add(runs, textStart + c, 1, style);
            }
        }
        return runs;

        // Next to one the same, the run grows.
        static void Add(List<StyleRun> runs, int start, int length, RunStyle style)
        {
            if (style == RunStyle.None)
                return;
            if (runs.Count > 0 && runs[^1] is var last && last.Style == style && last.Url == null && last.Start + last.Length == start)
                runs[^1] = last with { Length = last.Length + length };
            else
                runs.Add(new StyleRun(start, length, style));
        }
    }

    // ---- Typing ----

    /// <summary>
    /// The shown text became <paramref name="now"/>: the same change made here. Returns where the cursor goes, in the
    /// shown text as it is after (see <see cref="Render"/>, which can differ from <paramref name="now"/>: a marker
    /// backspaced into goes, a list left with Enter on an empty item).
    /// </summary>
    public int ApplyEdit(string now) => ApplyEdit(now, out _, out _);

    /// <inheritdoc cref="ApplyEdit(string)"/>
    /// <param name="firstLine">The first line the change touched.</param>
    /// <param name="lastLine">The last, as the lines are after it.</param>
    public int ApplyEdit(string now, out int firstLine, out int lastLine)
    {
        var before = Render();
        var a = 0;
        var max = Math.Min(before.Length, now.Length);
        while (a < max && before[a] == now[a])
            a++;
        var b = 0;
        while (b < max - a && before[before.Length - 1 - b] == now[now.Length - 1 - b])
            b++;
        var removedEnd = before.Length - b;
        var inserted = now.Substring(a, now.Length - b - a);

        var (line, offset) = Remove(a, removedEnd);
        firstLine = line;
        (line, offset) = Insert(line, offset, inserted);
        lastLine = line;
        return ToShown(line, offset);
    }

    /// <summary>Where lines <paramref name="first"/> to <paramref name="last"/> are in the shown text, markers included.</summary>
    public (int Start, int Length) LinesSpan(int first, int last)
    {
        var (starts, prefixes) = Layout();
        first = Math.Clamp(first, 0, Lines.Count - 1);
        last = Math.Clamp(last, first, Lines.Count - 1);
        var end = starts[last] + prefixes[last].Length + Lines[last].Text.Length;
        return (starts[first], end - starts[first]);
    }

    /// <summary>The lines a stretch of the shown text is on.</summary>
    public (int First, int Last) LinesOf(int start, int length) => (Locate(start).Line, Locate(start + length).Line);

    /// <summary>Takes out what was between <paramref name="start"/> and <paramref name="end"/> of the shown text; returns where that was.</summary>
    (int Line, int Offset) Remove(int start, int end)
    {
        var (l1, o1, inPrefix1) = Locate(start);
        if (end <= start)
            return (l1, o1);
        var (l2, o2, _) = Locate(end);
        if (l1 == l2)
        {
            Lines[l1].Remove(o1, o2 - o1);
            // Into a marker (backspace at the start of an item): the item becomes plain text, its text kept.
            if (inPrefix1)
                Lines[l1].Kind = BlockKind.Paragraph;
            return (l1, o1);
        }
        // Across lines: what's left of the last joins the first.
        var first = Lines[l1];
        var last = Lines[l2];
        first.Remove(o1, first.Text.Length - o1);
        first.Text.Append(last.Text.ToString(o2, last.Text.Length - o2));
        first.Formats.AddRange(last.Formats.Skip(o2));
        Lines.RemoveRange(l1 + 1, l2 - l1);
        if (inPrefix1)
            first.Kind = BlockKind.Paragraph;
        return (l1, o1);
    }

    /// <summary>Puts <paramref name="text"/> in at a place; new lines continue the line's list. Returns where it ends.</summary>
    (int Line, int Offset) Insert(int line, int offset, string text)
    {
        if (text.Length == 0)
            return (line, offset);
        var current = Lines[line];
        // Enter on an empty item leaves the list.
        if (text == "\n" && current.IsListLike && current.Text.Length == 0)
        {
            current.Kind = BlockKind.Paragraph;
            return (line, 0);
        }
        var format = TypingFormat(current, offset);
        Pending = null;
        var parts = text.Split('\n');
        for (var p = 0; p < parts.Length; p++)
        {
            if (p > 0)
            {
                var next = current.SplitOff(offset, current.Kind switch
                {
                    BlockKind.Heading => BlockKind.Paragraph,
                    var k => k,
                });
                Lines.Insert(++line, next);
                current = next;
                offset = 0;
            }
            current.Insert(offset, parts[p], format);
            offset += parts[p].Length;
        }
        return (line, offset);
    }

    /// <summary>What typing at a place looks like: the pending styles, or like the text before it (not its link).</summary>
    CharFormat TypingFormat(RichLine line, int offset)
    {
        var like = offset > 0 ? line.Formats[offset - 1] : line.Formats.Count > 0 ? line.Formats[0] : CharFormat.Plain;
        return new CharFormat(Pending ?? like.Style);
    }

    // ---- Toolbar ----

    /// <summary>The selection (in the shown text) as places in the lines.</summary>
    ((int Line, int Offset) From, (int Line, int Offset) To) Range(int start, int length)
    {
        var (l1, o1, _) = Locate(start);
        var (l2, o2, _) = Locate(start + length);
        return ((l1, o1), (l2, o2));
    }

    IEnumerable<(RichLine Line, int Index)> Chars(int start, int length)
    {
        var (from, to) = Range(start, length);
        for (var l = from.Line; l <= to.Line; l++)
        {
            var first = l == from.Line ? from.Offset : 0;
            var end = l == to.Line ? to.Offset : Lines[l].Text.Length;
            for (var c = first; c < end; c++)
                yield return (Lines[l], c);
        }
    }

    /// <summary>
    /// Bold, italics, strikethrough or code on the selection: off if all of it has it already, on otherwise. Nothing
    /// selected: for what's typed next.
    /// </summary>
    public void ToggleStyle(InlineStyle style, int start, int length)
    {
        if (length == 0)
        {
            var (line, offset, _) = Locate(start);
            var current = Pending ?? TypingFormat(Lines[line], offset).Style;
            Pending = current ^ style;
            return;
        }
        var chars = Chars(start, length).ToList();
        var on = !chars.All(c => c.Line.Formats[c.Index].Style.HasFlag(style));
        foreach (var (line, i) in chars)
        {
            var f = line.Formats[i];
            line.Formats[i] = f with { Style = on ? f.Style | style : f.Style & ~style };
        }
    }

    /// <summary>The selected lines (or the cursor's) become <paramref name="kind"/>, or plain text if they all are already.</summary>
    public void ToggleBlock(BlockKind kind, int start, int length)
    {
        var (from, to) = Range(start, length);
        var lines = Lines.GetRange(from.Line, to.Line - from.Line + 1);
        // A checklist: unticked, ticked, then off.
        if (kind == BlockKind.Task && lines.All(l => l.Kind == BlockKind.Task))
        {
            if (lines.Any(l => !l.Checked))
            {
                lines.ForEach(l => l.Checked = true);
                return;
            }
            lines.ForEach(l => (l.Kind, l.Checked) = (BlockKind.Paragraph, false));
            return;
        }
        var off = lines.All(l => l.Kind == kind);
        foreach (var line in lines)
        {
            (line.Kind, line.Checked) = (off ? BlockKind.Paragraph : kind, false);
            if (kind == BlockKind.Heading)
                line.HeadingLevel = 2;
        }
    }

    /// <summary>The selection links to <paramref name="url"/>; nothing selected, the address itself goes in as the link.</summary>
    public int SetLink(string url, int start, int length)
    {
        if (length > 0)
        {
            foreach (var (line, i) in Chars(start, length))
                line.Formats[i] = line.Formats[i] with { Link = url, Image = false };
            return start + length;
        }
        return InsertLinked(url, url, image: false, start);
    }

    /// <summary>A link (or a picture) with <paramref name="label"/> as its text, put in at <paramref name="at"/>.</summary>
    public int InsertLinked(string label, string url, bool image, int at)
    {
        var (line, offset, _) = Locate(at);
        Lines[line].Insert(offset, label, new CharFormat(InlineStyle.None, url, image));
        return ToShown(line, offset + label.Length);
    }

    /// <summary>A picture in the shown text: the object character, drawn over as the picture by each platform.</summary>
    public const char ImageChar = '￼';

    /// <summary>A picture or file on a line of its own, after the cursor's line if that has text.</summary>
    public int InsertAttachment(string label, string url, bool image, int at)
    {
        var (line, offset, _) = Locate(at);
        var current = Lines[line];
        if (current.Text.Length > 0)
        {
            var rest = current.SplitOff(offset, current.Kind);
            if (rest.Text.Length > 0)
                Lines.Insert(line + 1, rest);
            Lines.Insert(++line, new RichLine());
        }
        Lines[line].Kind = BlockKind.Paragraph;
        // A picture shows as itself; another file by its name, as a link.
        if (image)
            Lines[line].Append(ImageChar.ToString(), new CharFormat(InlineStyle.None, url, true, label));
        else
            Lines[line].Append(label, new CharFormat(InlineStyle.None, url));
        // And a line to carry on writing on.
        Lines.Insert(line + 1, new RichLine());
        return ToShown(line + 1, 0);
    }
}
