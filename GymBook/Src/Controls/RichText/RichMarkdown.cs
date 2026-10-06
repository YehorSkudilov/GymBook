using System.Text;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace GymBook.Controls.RichText;

/// <summary>
/// Formatted text to and from Markdown, the way GitHub reads it: a line break where one's typed, **bold**, *italics*,
/// ~~strikethrough~~, `code`, links and pictures, headings, lists, checklists, quotes and code blocks. What the editor
/// has no way to show (tables, HTML, a rule) is kept as written, as raw lines.
/// </summary>
public static class RichMarkdown
{
    static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras().UseTaskLists().UseAutoLinks().UsePipeTables().Build();

    // ---- Reading ----

    public static RichDocument Parse(string markdown)
    {
        var doc = new RichDocument();
        doc.Lines.Clear();
        markdown = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
        var lineStarts = new List<int> { 0 };
        for (var i = 0; i < markdown.Length; i++)
            if (markdown[i] == '\n')
                lineStarts.Add(i + 1);
        int LineOf(int at)
        {
            var i = lineStarts.BinarySearch(at);
            return Math.Max(0, i >= 0 ? i : ~i - 1);
        }

        var previousEnd = -1;
        foreach (var block in Markdig.Markdown.Parse(markdown, Pipeline))
        {
            // A blank line between blocks stays a blank line.
            if (previousEnd >= 0 && block.Line > previousEnd + 1)
                doc.Lines.Add(new RichLine());
            ReadBlock(doc, block, markdown, null);
            previousEnd = LineOf(Math.Max(block.Span.Start, block.Span.End));
        }
        if (doc.Lines.Count == 0)
            doc.Lines.Add(new RichLine());
        return doc;
    }

    /// <summary>A block's lines; inside a quote (<paramref name="within"/>), each is a quote line.</summary>
    static void ReadBlock(RichDocument doc, Block block, string source, BlockKind? within)
    {
        switch (block)
        {
            case HeadingBlock heading:
                var h = new RichLine(within ?? BlockKind.Heading) { HeadingLevel = heading.Level };
                doc.Lines.Add(h);
                ReadInlines(doc, h, heading.Inline, CharFormat.Plain);
                break;
            case ParagraphBlock paragraph:
                var p = new RichLine(within ?? BlockKind.Paragraph);
                doc.Lines.Add(p);
                ReadInlines(doc, p, paragraph.Inline, CharFormat.Plain);
                break;
            case ListBlock list:
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    var first = true;
                    foreach (var child in item)
                    {
                        if (first && child is ParagraphBlock itemText)
                        {
                            var kind = within ?? (list.IsOrdered ? BlockKind.Numbered : BlockKind.Bullet);
                            var line = new RichLine(kind);
                            doc.Lines.Add(line);
                            ReadInlines(doc, line, itemText.Inline, CharFormat.Plain);
                        }
                        else
                        {
                            // Nested lists and further paragraphs: flattened.
                            ReadBlock(doc, child, source, within);
                        }
                        first = false;
                    }
                }
                break;
            case QuoteBlock quote:
                foreach (var child in quote)
                    ReadBlock(doc, child, source, BlockKind.Quote);
                break;
            case CodeBlock code:
                for (var i = 0; i < code.Lines.Count; i++)
                    doc.Lines.Add(Plain(within ?? BlockKind.Code, code.Lines.Lines[i].Slice.ToString()));
                break;
            default:
                // Tables, HTML, rules and anything else: as written.
                var text = source.Substring(block.Span.Start, Math.Max(0, Math.Min(source.Length, block.Span.End + 1) - block.Span.Start));
                foreach (var raw in text.TrimEnd('\n').Split('\n'))
                    doc.Lines.Add(Plain(BlockKind.Raw, raw));
                break;
        }
    }

    static RichLine Plain(BlockKind kind, string text)
    {
        var line = new RichLine(kind);
        line.Append(text, CharFormat.Plain);
        return line;
    }

    static void ReadInlines(RichDocument doc, RichLine line, ContainerInline? container, CharFormat format)
    {
        if (container == null)
            return;
        foreach (var inline in container)
            line = ReadInline(doc, line, inline, format);
    }

    /// <summary>One piece of a line; returns the line to carry on with (a line break starts a new one).</summary>
    static RichLine ReadInline(RichDocument doc, RichLine line, Inline inline, CharFormat format)
    {
        switch (inline)
        {
            case TaskList task:
                if (line.Kind is BlockKind.Bullet or BlockKind.Numbered)
                    (line.Kind, line.Checked) = (BlockKind.Task, task.Checked);
                break;
            case LiteralInline literal:
                line.Append(literal.Content.ToString(), format);
                break;
            case CodeInline code:
                line.Append(code.Content, format with { Style = format.Style | InlineStyle.Code });
                break;
            case LineBreakInline:
                // A new line, continuing a quote; anything else carries on as plain text.
                var next = new RichLine(line.Kind is BlockKind.Quote or BlockKind.Code or BlockKind.Raw ? line.Kind : BlockKind.Paragraph);
                doc.Lines.Add(next);
                return next;
            case HtmlEntityInline entity:
                line.Append(entity.Transcoded.ToString(), format);
                break;
            case HtmlInline html:
                line.Append(html.Tag, format);
                break;
            case AutolinkInline auto:
                line.Append(auto.Url, format with { Link = auto.IsEmail ? "mailto:" + auto.Url : auto.Url });
                break;
            case LinkInline link:
                var linked = format with { Link = link.Url ?? "", Image = link.IsImage };
                var start = line.Text.Length;
                foreach (var child in link)
                    line = ReadInline(doc, line, child, linked);
                if (line.Text.Length == start)
                    line.Append(link.Url ?? "", linked);
                break;
            case EmphasisInline emphasis:
                var style = emphasis.DelimiterChar == '~' ? InlineStyle.Strike
                    : emphasis.DelimiterCount >= 3 ? InlineStyle.Bold | InlineStyle.Italic
                    : emphasis.DelimiterCount == 2 ? InlineStyle.Bold
                    : InlineStyle.Italic;
                foreach (var child in emphasis)
                    line = ReadInline(doc, line, child, format with { Style = format.Style | style });
                break;
            case ContainerInline container:
                foreach (var child in container)
                    line = ReadInline(doc, line, child, format);
                break;
        }
        return line;
    }

    // ---- Writing ----

    public static string Write(RichDocument doc)
    {
        var lines = doc.Lines;
        var output = new List<string>();
        var number = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var previous = i > 0 ? lines[i - 1] : null;
            // A list or quote would swallow plain text right after it, and a table needs a blank line before it.
            if (previous != null && previous.Text.Length + (previous.IsListLike ? 1 : 0) > 0 && line.Text.Length > 0
                && ((previous.IsListLike && line.Kind is BlockKind.Paragraph or BlockKind.Raw)
                    || (line.Kind == BlockKind.Raw && previous.Kind != BlockKind.Raw)
                    || (previous.Kind == BlockKind.Raw && line.Kind != BlockKind.Raw)))
                output.Add("");

            if (line.Kind == BlockKind.Code)
            {
                // Consecutive code lines are one block.
                var block = new List<string>();
                while (i < lines.Count && lines[i].Kind == BlockKind.Code)
                    block.Add(lines[i++].Text.ToString());
                i--;
                var fence = new string('`', Math.Max(3, LongestRun(string.Join("\n", block), '`') + 1));
                output.Add(fence);
                output.AddRange(block);
                output.Add(fence);
                continue;
            }

            number = line.Kind == BlockKind.Numbered ? number + 1 : 0;
            var text = Inlines(line);
            output.Add(line.Kind switch
            {
                BlockKind.Heading => new string('#', Math.Clamp(line.HeadingLevel, 1, 6)) + " " + text,
                BlockKind.Bullet => "- " + text,
                BlockKind.Numbered => $"{number}. " + text,
                BlockKind.Task => (line.Checked ? "- [x] " : "- [ ] ") + text,
                BlockKind.Quote => text.Length == 0 ? ">" : "> " + text,
                BlockKind.Raw => line.Text.ToString(),
                _ => EscapeLineStart(text),
            });
        }
        return string.Join("\n", output).TrimEnd();
    }

    /// <summary>A line's text with its styles and links as Markdown.</summary>
    static string Inlines(RichLine line)
    {
        var text = line.Text.ToString();
        var md = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            // Links first: one link may hold text of several looks.
            var link = line.Formats[i].Link;
            var image = line.Formats[i].Image;
            var end = i;
            while (end < text.Length && line.Formats[end].Link == link && line.Formats[end].Image == image)
                end++;
            var inner = Styled(text, line.Formats, i, end, image);
            md.Append(link == null ? inner : $"{(image ? "!" : "")}[{inner}]({link.Replace(" ", "%20").Replace(")", "%29")})");
            i = end;
        }
        return md.ToString();
    }

    static string Styled(string text, List<CharFormat> formats, int start, int end, bool plainOnly)
    {
        var md = new StringBuilder();
        var i = start;
        while (i < end)
        {
            var style = plainOnly ? InlineStyle.None : formats[i].Style;
            var runEnd = i;
            while (runEnd < end && (plainOnly ? InlineStyle.None : formats[runEnd].Style) == style)
                runEnd++;
            md.Append(Wrap(text[i..runEnd], style));
            i = runEnd;
        }
        return md.ToString();
    }

    /// <summary>A run of one look in its markers, with spaces at its ends left outside them (or they wouldn't count).</summary>
    static string Wrap(string run, InlineStyle style)
    {
        if (style == InlineStyle.None || run.Trim().Length == 0)
            return Escape(run);
        var lead = run[..(run.Length - run.TrimStart().Length)];
        var trail = run[run.TrimEnd().Length..];
        var core = run.Trim();
        string inner;
        if (style.HasFlag(InlineStyle.Code))
        {
            var ticks = new string('`', LongestRun(core, '`') + 1);
            var pad = core.StartsWith('`') || core.EndsWith('`') ? " " : "";
            inner = ticks + pad + core + pad + ticks;
        }
        else
        {
            inner = Escape(core);
        }
        if (style.HasFlag(InlineStyle.Strike))
            inner = "~~" + inner + "~~";
        if (style.HasFlag(InlineStyle.Italic))
            inner = "*" + inner + "*";
        if (style.HasFlag(InlineStyle.Bold))
            inner = "**" + inner + "**";
        return lead + inner + trail;
    }

    static string Escape(string text)
    {
        var md = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '~')
                md.Append('\\');
            md.Append(c);
        }
        return md.ToString();
    }

    /// <summary>Plain text that would read as a heading, list, quote or rule at the start of a line, kept as text.</summary>
    static string EscapeLineStart(string text)
    {
        if (text.Length == 0)
            return text;
        if (text[0] is '#' or '>' or '-' or '+' or '=' or '|')
            return "\\" + text;
        var digits = 0;
        while (digits < text.Length && char.IsAsciiDigit(text[digits]))
            digits++;
        if (digits > 0 && digits < text.Length && text[digits] is '.' or ')')
            return text[..digits] + "\\" + text[digits..];
        return text;
    }

    static int LongestRun(string text, char c)
    {
        int longest = 0, run = 0;
        foreach (var ch in text)
        {
            run = ch == c ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }
        return longest;
    }
}
