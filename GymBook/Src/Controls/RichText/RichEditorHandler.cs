using Microsoft.Maui.Handlers;

namespace GymBook.Controls.RichText;

// RichEditor on each platform: the platform's own rich text box, showing RichDocument's text painted with its styles,
// and saying when it's typed in or its cursor moves. All the formatting logic is RichEditor's; this only shows it.
// The colours are the app's (it's always dark).

#if ANDROID
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AColor = Android.Graphics.Color;

public class RichEditorHandler : ViewHandler<RichEditor, RichEditText>, IRichTextPlatform
{
    public static readonly IPropertyMapper<RichEditor, RichEditorHandler> Mapper = new PropertyMapper<RichEditor, RichEditorHandler>(ViewMapper)
    {
        [nameof(RichEditor.Placeholder)] = (h, v) => h.PlatformView.Hint = v.Placeholder,
        [nameof(RichEditor.FontSize)] = (h, v) => h.PlatformView.SetTextSize(ComplexUnitType.Sp, (float)v.FontSize),
    };

    public RichEditorHandler() : base(Mapper)
    {
    }

    protected override RichEditText CreatePlatformView()
    {
        var edit = new RichEditText(Context)
        {
            Background = null,
            Gravity = GravityFlags.Top | GravityFlags.Start,
            InputType = InputTypes.ClassText | InputTypes.TextFlagMultiLine | InputTypes.TextFlagCapSentences | InputTypes.TextFlagAutoCorrect,
        };
        edit.SetPadding(0, 0, 0, 0);
        edit.SetTextColor(AColor.ParseColor("#F4F6FB"));
        edit.SetHintTextColor(AColor.ParseColor("#9AA3B5"));
        edit.SetHorizontallyScrolling(false);
        return edit;
    }

    protected override void ConnectHandler(RichEditText platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Edited += VirtualView.OnPlatformTextChanged;
        platformView.SelectionMoved += VirtualView.OnPlatformSelectionChanged;
        VirtualView.Attach(this);
    }

    protected override void DisconnectHandler(RichEditText platformView)
    {
        platformView.Edited -= VirtualView.OnPlatformTextChanged;
        platformView.SelectionMoved -= VirtualView.OnPlatformSelectionChanged;
        VirtualView.Detach();
        base.DisconnectHandler(platformView);
    }

    public string Text => PlatformView.Text ?? "";
    public int SelectionStart => Math.Max(0, PlatformView.SelectionStart);
    public int SelectionLength => Math.Max(0, PlatformView.SelectionEnd - PlatformView.SelectionStart);
    public bool IsComposing => false;

    public void Replace(int start, int length, string text) => PlatformView.EditableText?.Replace(start, start + length, text);

    public void Select(int start, int length)
    {
        var count = Text.Length;
        PlatformView.SetSelection(Math.Clamp(start, 0, count), Math.Clamp(start + length, 0, count));
    }

    public void Focus() => PlatformView.RequestFocus();

    public void Paint(IReadOnlyList<StyleRun> runs, int start, int length)
    {
        if (PlatformView.EditableText is not { } text)
            return;
        // Only this editor's own spans: the keyboard's (the word being typed, underlined) stay.
        foreach (var span in text.GetSpans(0, text.Length(), Java.Lang.Class.FromType(typeof(Java.Lang.Object))) ?? [])
            if (span is IRichSpan)
                text.RemoveSpan(span);
        foreach (var run in runs)
        {
            if (run.Style.HasFlag(RunStyle.Image) && run.Url != null)
            {
                text.SetSpan(new RichImageSpan(Picture(run.Url), Context.Resources?.DisplayMetrics?.Density ?? 1), run.Start, run.Start + run.Length, SpanTypes.ExclusiveExclusive);
                continue;
            }
            foreach (var span in Spans(run.Style))
                text.SetSpan(span, run.Start, run.Start + run.Length, SpanTypes.ExclusiveExclusive);
        }
    }

    readonly Dictionary<string, Android.Graphics.Bitmap> _pictures = [];

    /// <summary>A picture to draw, once it's downloaded (painted again then); shrunk to about the size it's shown at.</summary>
    Android.Graphics.Bitmap? Picture(string url)
    {
        if (_pictures.TryGetValue(url, out var kept))
            return kept;
        if (RichImages.Get(url, () => VirtualView?.Repaint()) is not { } bytes)
            return null;
        var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
        Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, bounds);
        var target = RichImages.MaxWidth * 2 * (Context.Resources?.DisplayMetrics?.Density ?? 1);
        var sample = 1;
        while (bounds.OutWidth / (sample * 2) >= target)
            sample *= 2;
        if (Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, new Android.Graphics.BitmapFactory.Options { InSampleSize = sample }) is not { } bitmap)
            return null;
        _pictures[url] = bitmap;
        return bitmap;
    }

    static IEnumerable<Java.Lang.Object> Spans(RunStyle style)
    {
        var bold = style.HasFlag(RunStyle.Bold) || (style & (RunStyle.Heading1 | RunStyle.Heading2 | RunStyle.Heading3)) != 0;
        var italic = style.HasFlag(RunStyle.Italic);
        if (bold || italic)
            yield return new RichStyleSpan(bold && italic ? Android.Graphics.TypefaceStyle.BoldItalic : bold ? Android.Graphics.TypefaceStyle.Bold : Android.Graphics.TypefaceStyle.Italic);
        if (style.HasFlag(RunStyle.Strike))
            yield return new RichStrikeSpan();
        if ((style & (RunStyle.Code | RunStyle.CodeBlock)) != 0)
        {
            yield return new RichTypefaceSpan("monospace");
            yield return new RichBackgroundSpan(AColor.ParseColor("#2A3040"));
        }
        if (style.HasFlag(RunStyle.Link))
        {
            yield return new RichColorSpan(AColor.ParseColor("#6E9BFF"));
            yield return new RichUnderlineSpan();
        }
        else if ((style & (RunStyle.Marker | RunStyle.Quote | RunStyle.Checked)) != 0)
        {
            yield return new RichColorSpan(AColor.ParseColor("#9AA3B5"));
        }
        if (style.HasFlag(RunStyle.Heading1))
            yield return new RichSizeSpan(1.5f);
        else if (style.HasFlag(RunStyle.Heading2))
            yield return new RichSizeSpan(1.3f);
        else if (style.HasFlag(RunStyle.Heading3))
            yield return new RichSizeSpan(1.15f);
    }
}

/// <summary>An EditText that says when its text changes (after the change is done) and when its cursor moves.</summary>
public class RichEditText : AppCompatEditText
{
    public RichEditText(Android.Content.Context context) : base(context)
    {
    }

    protected RichEditText(IntPtr handle, Android.Runtime.JniHandleOwnership transfer) : base(handle, transfer)
    {
    }

    public event Action? Edited;
    public event Action? SelectionMoved;

    protected override void OnTextChanged(Java.Lang.ICharSequence? text, int start, int lengthBefore, int lengthAfter)
    {
        base.OnTextChanged(text, start, lengthBefore, lengthAfter);
        // Not while Android is still changing it.
        if (Edited != null)
            Post(() => Edited?.Invoke());
    }

    protected override void OnSelectionChanged(int selStart, int selEnd)
    {
        base.OnSelectionChanged(selStart, selEnd);
        SelectionMoved?.Invoke();
    }

    // Pasted text comes in plain: its look is this editor's to give.
    public override bool OnTextContextMenuItem(int id) =>
        base.OnTextContextMenuItem(id == Android.Resource.Id.Paste ? Android.Resource.Id.PasteAsPlainText : id);
}

/// <summary>Marks the spans this editor painted, so painting again takes off only those.</summary>
interface IRichSpan;

sealed class RichStyleSpan(Android.Graphics.TypefaceStyle style) : StyleSpan(style), IRichSpan;
sealed class RichStrikeSpan : StrikethroughSpan, IRichSpan;
sealed class RichTypefaceSpan(string family) : TypefaceSpan(family), IRichSpan;
sealed class RichBackgroundSpan(AColor color) : BackgroundColorSpan(color), IRichSpan;
sealed class RichColorSpan(AColor color) : ForegroundColorSpan(color), IRichSpan;
sealed class RichUnderlineSpan : UnderlineSpan, IRichSpan;
sealed class RichSizeSpan(float proportion) : RelativeSizeSpan(proportion), IRichSpan;

/// <summary>A picture drawn in place of its character, at most <see cref="RichImages.MaxWidth"/> wide; a grey box while it downloads.</summary>
sealed class RichImageSpan : ReplacementSpan, IRichSpan
{
    readonly Android.Graphics.Bitmap? _bitmap;
    readonly int _width, _height;

    public RichImageSpan(Android.Graphics.Bitmap? bitmap, float density)
    {
        _bitmap = bitmap;
        var max = (float)RichImages.MaxWidth * density;
        if (bitmap == null || bitmap.Width == 0)
        {
            (_width, _height) = ((int)(160 * density), (int)(100 * density));
            return;
        }
        var scale = Math.Min(1f, max / bitmap.Width);
        (_width, _height) = ((int)(bitmap.Width * scale), (int)(bitmap.Height * scale));
    }

    public override int GetSize(Android.Graphics.Paint paint, Java.Lang.ICharSequence? text, int start, int end, Android.Graphics.Paint.FontMetricsInt? fm)
    {
        // The line as tall as the picture.
        if (fm != null)
        {
            fm.Ascent = fm.Top = -_height;
            fm.Descent = fm.Bottom = 0;
        }
        return _width;
    }

    public override void Draw(Android.Graphics.Canvas canvas, Java.Lang.ICharSequence? text, int start, int end, float x, int top, int y, int bottom, Android.Graphics.Paint paint)
    {
        var rect = new Android.Graphics.RectF(x, y - _height, x + _width, y);
        if (_bitmap != null)
        {
            canvas.DrawBitmap(_bitmap, null, rect, null);
            return;
        }
        using var fill = new Android.Graphics.Paint { Color = AColor.ParseColor("#2A3040") };
        canvas.DrawRoundRect(rect, 12, 12, fill);
    }
}

#elif IOS || MACCATALYST
using Foundation;
using UIKit;

public class RichEditorHandler : ViewHandler<RichEditor, UITextView>, IRichTextPlatform
{
    public static readonly IPropertyMapper<RichEditor, RichEditorHandler> Mapper = new PropertyMapper<RichEditor, RichEditorHandler>(ViewMapper)
    {
        [nameof(RichEditor.Placeholder)] = (h, v) => h._placeholder.Text = v.Placeholder,
        [nameof(RichEditor.FontSize)] = (h, v) => h.PlatformView.Font = UIFont.SystemFontOfSize((nfloat)v.FontSize),
    };

    static readonly UIColor TextColor = UIColor.FromRGB(0xF4, 0xF6, 0xFB);
    static readonly UIColor Secondary = UIColor.FromRGB(0x9A, 0xA3, 0xB5);
    static readonly UIColor LinkColor = UIColor.FromRGB(0x6E, 0x9B, 0xFF);
    static readonly UIColor CodeBackground = UIColor.FromRGB(0x2A, 0x30, 0x40);

    readonly UILabel _placeholder = new() { TextColor = Secondary, Lines = 0 };

    public RichEditorHandler() : base(Mapper)
    {
    }

    nfloat Size => (nfloat)VirtualView.FontSize;

    protected override UITextView CreatePlatformView()
    {
        var view = new UITextView
        {
            BackgroundColor = UIColor.Clear,
            TextColor = TextColor,
            Font = UIFont.SystemFontOfSize(15),
            TextContainerInset = new UIEdgeInsets(6, 0, 6, 0),
            AllowsEditingTextAttributes = false,
        };
        view.TextContainer.LineFragmentPadding = 0;
        _placeholder.Font = view.Font ?? UIFont.SystemFontOfSize(15);
        _placeholder.TranslatesAutoresizingMaskIntoConstraints = false;
        view.AddSubview(_placeholder);
        NSLayoutConstraint.ActivateConstraints(
        [
            _placeholder.TopAnchor.ConstraintEqualTo(view.TopAnchor, 6),
            _placeholder.LeadingAnchor.ConstraintEqualTo(view.LeadingAnchor),
            _placeholder.WidthAnchor.ConstraintEqualTo(view.WidthAnchor),
        ]);
        return view;
    }

    protected override void ConnectHandler(UITextView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Changed += OnChanged;
        platformView.SelectionChanged += OnSelectionChanged;
        VirtualView.Attach(this);
        UpdatePlaceholder();
    }

    protected override void DisconnectHandler(UITextView platformView)
    {
        platformView.Changed -= OnChanged;
        platformView.SelectionChanged -= OnSelectionChanged;
        VirtualView.Detach();
        base.DisconnectHandler(platformView);
    }

    void OnChanged(object? sender, EventArgs e)
    {
        VirtualView.OnPlatformTextChanged();
        UpdatePlaceholder();
    }

    void OnSelectionChanged(object? sender, EventArgs e) => VirtualView.OnPlatformSelectionChanged();

    void UpdatePlaceholder() => _placeholder.Hidden = Text.Length > 0;

    public string Text => PlatformView.Text ?? "";
    public int SelectionStart => (int)PlatformView.SelectedRange.Location;
    public int SelectionLength => (int)PlatformView.SelectedRange.Length;
    public bool IsComposing => PlatformView.MarkedTextRange != null;

    public void Replace(int start, int length, string text)
    {
        PlatformView.TextStorage.Replace(new NSRange(start, length), text);
        UpdatePlaceholder();
    }

    public void Select(int start, int length) => PlatformView.SelectedRange = new NSRange(start, length);

    public void Focus() => PlatformView.BecomeFirstResponder();

    public void Paint(IReadOnlyList<StyleRun> runs, int start, int length)
    {
        var storage = PlatformView.TextStorage;
        var plain = new UIStringAttributes { Font = UIFont.SystemFontOfSize(Size), ForegroundColor = TextColor };
        storage.BeginEditing();
        storage.SetAttributes(plain.Dictionary, new NSRange(0, storage.Length));
        foreach (var run in runs)
        {
            if (run.Style.HasFlag(RunStyle.Image) && run.Url != null)
            {
                // The picture's character (the object character) drawn as the picture.
                storage.AddAttribute(UIStringAttributeKey.Attachment, Attachment(run.Url), new NSRange(run.Start, run.Length));
                continue;
            }
            storage.AddAttributes(Attributes(run.Style).Dictionary, new NSRange(run.Start, run.Length));
        }
        storage.EndEditing();
        // What's typed next starts plain; RichEditor paints it as it should be.
        PlatformView.TypingAttributes2 = plain.Dictionary;
        UpdatePlaceholder();
    }

    readonly Dictionary<string, UIImage> _pictures = [];

    /// <summary>A picture at most <see cref="RichImages.MaxWidth"/> wide, or a grey box until it's downloaded (painted again then).</summary>
    NSTextAttachment Attachment(string url)
    {
        if (!_pictures.TryGetValue(url, out var picture) && RichImages.Get(url, () => VirtualView?.Repaint()) is { } bytes
            && UIImage.LoadFromData(NSData.FromArray(bytes)) is { } loaded)
            _pictures[url] = picture = loaded;
        if (picture == null || picture.Size.Width <= 0)
        {
            var box = new UIGraphicsImageRenderer(new CoreGraphics.CGSize(160, 100)).CreateImage(context =>
            {
                CodeBackground.SetFill();
                UIBezierPath.FromRoundedRect(new CoreGraphics.CGRect(0, 0, 160, 100), 12).Fill();
            });
            return new NSTextAttachment { Image = box, Bounds = new CoreGraphics.CGRect(0, 0, 160, 100) };
        }
        var scale = Math.Min(1, RichImages.MaxWidth / picture.Size.Width);
        return new NSTextAttachment { Image = picture, Bounds = new CoreGraphics.CGRect(0, 0, picture.Size.Width * scale, picture.Size.Height * scale) };
    }

    UIStringAttributes Attributes(RunStyle style)
    {
        var size = Size * (style.HasFlag(RunStyle.Heading1) ? 1.5f : style.HasFlag(RunStyle.Heading2) ? 1.3f : style.HasFlag(RunStyle.Heading3) ? 1.15f : 1f);
        var bold = style.HasFlag(RunStyle.Bold) || (style & (RunStyle.Heading1 | RunStyle.Heading2 | RunStyle.Heading3)) != 0;
        UIFont font;
        if ((style & (RunStyle.Code | RunStyle.CodeBlock)) != 0)
        {
            font = UIFont.GetMonospacedSystemFont(size * 0.92f, bold ? UIFontWeight.Bold : UIFontWeight.Regular);
        }
        else
        {
            var traits = (bold ? UIFontDescriptorSymbolicTraits.Bold : 0) | (style.HasFlag(RunStyle.Italic) ? UIFontDescriptorSymbolicTraits.Italic : 0);
            var system = UIFont.SystemFontOfSize(size);
            var descriptor = system.FontDescriptor;
            font = traits == 0 || descriptor == null ? system : UIFont.FromDescriptor(descriptor.CreateWithTraits(traits) ?? descriptor, size);
        }
        var attributes = new UIStringAttributes { Font = font };
        if (style.HasFlag(RunStyle.Strike))
            attributes.StrikethroughStyle = NSUnderlineStyle.Single;
        if ((style & (RunStyle.Code | RunStyle.CodeBlock)) != 0)
            attributes.BackgroundColor = CodeBackground;
        if (style.HasFlag(RunStyle.Link))
        {
            attributes.ForegroundColor = LinkColor;
            attributes.UnderlineStyle = NSUnderlineStyle.Single;
        }
        else if ((style & (RunStyle.Marker | RunStyle.Quote | RunStyle.Checked)) != 0)
        {
            attributes.ForegroundColor = Secondary;
        }
        return attributes;
    }
}

#elif WINDOWS
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


public class RichEditorHandler : ViewHandler<RichEditor, RichEditBox>, IRichTextPlatform
{
    public static readonly IPropertyMapper<RichEditor, RichEditorHandler> Mapper = new PropertyMapper<RichEditor, RichEditorHandler>(ViewMapper)
    {
        [nameof(RichEditor.Placeholder)] = (h, v) => h.PlatformView.PlaceholderText = v.Placeholder,
        [nameof(RichEditor.FontSize)] = (h, v) => h.PlatformView.FontSize = v.FontSize,
    };

    static readonly Windows.UI.Color TextColor = Microsoft.UI.ColorHelper.FromArgb(255, 0xF4, 0xF6, 0xFB);
    static readonly Windows.UI.Color Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 0x9A, 0xA3, 0xB5);
    static readonly Windows.UI.Color LinkColor = Microsoft.UI.ColorHelper.FromArgb(255, 0x6E, 0x9B, 0xFF);
    static readonly Windows.UI.Color CodeBackground = Microsoft.UI.ColorHelper.FromArgb(255, 0x2A, 0x30, 0x40);
    static readonly Windows.UI.Color Clear = Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0);
    // The card the editor sits on (InnerCard, Surface2).
    static readonly Windows.UI.Color CardBackground = Microsoft.UI.ColorHelper.FromArgb(255, 0x1D, 0x21, 0x2C);

    public RichEditorHandler() : base(Mapper)
    {
    }

    protected override RichEditBox CreatePlatformView()
    {
        var box = new RichEditBox
        {
            AcceptsReturn = true,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            IsSpellCheckEnabled = true,
            // Ctrl+B and the like would format outside the document; the toolbar does it.
            DisabledFormattingAccelerators = DisabledFormattingAccelerators.All,
            ClipboardCopyFormat = RichEditClipboardFormat.PlainText,
            Padding = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 4),
        };
        // No box of its own (border, background, focus line): the card around it is the box.
        var clear = new Microsoft.UI.Xaml.Media.SolidColorBrush(Clear);
        foreach (var key in new[]
                 {
                     "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
                     "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused",
                 })
            box.Resources[key] = clear;
        box.Resources["TextControlForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(TextColor);
        box.Resources["TextControlForegroundFocused"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(TextColor);
        box.Resources["TextControlForegroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(TextColor);
        box.Resources["TextControlPlaceholderForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Secondary);
        box.Resources["TextControlPlaceholderForegroundFocused"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Secondary);
        box.Resources["TextControlPlaceholderForegroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Secondary);
        box.Resources["TextControlBorderThemeThickness"] = new Microsoft.UI.Xaml.Thickness(0);
        box.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(0);
        box.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        box.Background = clear;
        return box;
    }

    protected override void ConnectHandler(RichEditBox platformView)
    {
        base.ConnectHandler(platformView);
        platformView.TextChanged += OnTextChanged;
        platformView.SelectionChanged += OnSelectionChanged;
        platformView.Paste += OnPaste;
        VirtualView.Attach(this);
    }

    protected override void DisconnectHandler(RichEditBox platformView)
    {
        platformView.TextChanged -= OnTextChanged;
        platformView.SelectionChanged -= OnSelectionChanged;
        platformView.Paste -= OnPaste;
        VirtualView.Detach();
        base.DisconnectHandler(platformView);
    }

    void OnTextChanged(object sender, RoutedEventArgs e) => VirtualView.OnPlatformTextChanged();

    void OnSelectionChanged(object sender, RoutedEventArgs e) => VirtualView.OnPlatformSelectionChanged();

    // Pasted text comes in plain: its look is this editor's to give.
    async void OnPaste(object sender, TextControlPasteEventArgs e)
    {
        e.Handled = true;
        var content = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        if (!content.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            return;
        var text = (await content.GetTextAsync()).Replace("\r\n", "\r").Replace('\n', '\r');
        var selection = PlatformView.Document.Selection;
        selection.SetText(TextSetOptions.None, text);
        selection.Collapse(false);
    }

    /// <summary>The document's text with '\n' between lines, without the paragraph mark RichEditBox always ends with.</summary>
    public string Text
    {
        get
        {
            PlatformView.Document.GetText(TextGetOptions.None, out var text);
            text = text.Replace('\r', '\n');
            return text.EndsWith('\n') ? text[..^1] : text;
        }
    }

    public int SelectionStart => PlatformView.Document.Selection.StartPosition;
    public int SelectionLength => PlatformView.Document.Selection.EndPosition - PlatformView.Document.Selection.StartPosition;
    public bool IsComposing => false;

    public void Replace(int start, int length, string text) =>
        PlatformView.Document.GetRange(start, start + length).SetText(TextSetOptions.None, text.Replace('\n', '\r'));

    public void Select(int start, int length) => PlatformView.Document.Selection.SetRange(start, start + length);

    public void Focus() => PlatformView.Focus(FocusState.Programmatic);

    /// <summary>
    /// Only the lines that changed: each property set on a range lays the text out again, so the format for each run is
    /// made up first and given in one go, all of it shown at the end.
    /// </summary>
    public void Paint(IReadOnlyList<StyleRun> runs, int start, int length)
    {
        var document = PlatformView.Document;
        var end = start + length;
        document.BatchDisplayUpdates();
        try
        {
            var plain = document.GetDefaultCharacterFormat().GetClone();
            var size = plain.Size;
            plain.Bold = FormatEffect.Off;
            plain.Italic = FormatEffect.Off;
            plain.Strikethrough = FormatEffect.Off;
            plain.Underline = UnderlineType.None;
            plain.ForegroundColor = TextColor;
            // RichEditBox has no see-through background: a transparent one shows black. The card's colour instead.
            plain.BackgroundColor = CardBackground;
            plain.Name = "Segoe UI";
            // To the line's end mark too, so what's typed at the end of a line starts plain.
            document.GetRange(start, end + 1).CharacterFormat = plain;
            foreach (var run in runs)
            {
                if (run.Start + run.Length <= start || run.Start >= end)
                    continue;
                if (run.Style.HasFlag(RunStyle.Image) && run.Url != null)
                {
                    ShowPicture(document, run.Start, run.Url);
                    continue;
                }
                document.GetRange(Math.Max(run.Start, start), Math.Min(run.Start + run.Length, end)).CharacterFormat = Format(plain, size, run.Style);
            }
        }
        finally
        {
            document.ApplyDisplayUpdates();
        }
    }

    // Each picture, ready to embed (copied each time), and the size it's shown at, once downloaded and measured.
    readonly Dictionary<string, (Windows.Storage.Streams.InMemoryRandomAccessStream Stream, int Width, int Height)> _pictures = [];
    readonly HashSet<string> _measuring = [];

    /// <summary>
    /// The picture's character, made the picture: RichEditBox embeds it as an object, which reads back as the same
    /// object character, so the text stays the same. Already one (its own text is its name, not the character): left
    /// as it is. Not downloaded yet: it is, then everything is painted again.
    /// </summary>
    void ShowPicture(RichEditTextDocument document, int at, string url)
    {
        var range = document.GetRange(at, at + 1);
        range.GetText(TextGetOptions.UseObjectText, out var shown);
        if (shown != RichDocument.ImageChar.ToString())
            return;
        if (!_pictures.TryGetValue(url, out var picture))
        {
            _ = MeasureAsync(url);
            return;
        }
        using var copy = picture.Stream.CloneStream();
        range.SetText(TextSetOptions.None, "");
        range.InsertImage(picture.Width, picture.Height, picture.Height, VerticalCharacterAlignment.Baseline, "image", copy);
    }

    async Task MeasureAsync(string url)
    {
        if (!_measuring.Add(url))
            return;
        try
        {
            if (await RichImages.GetAsync(url) is not { } bytes)
                return;
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            using (var writer = new Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                writer.DetachStream();
            }
            stream.Seek(0);
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream.CloneStream());
            var scale = Math.Min(1, RichImages.MaxWidth / Math.Max(1, decoder.PixelWidth));
            _pictures[url] = (stream, (int)(decoder.PixelWidth * scale), (int)(decoder.PixelHeight * scale));
            VirtualView?.Repaint();
        }
        catch (Exception)
        {
            // Not a picture Windows can read: it stays a placeholder.
        }
        finally
        {
            _measuring.Remove(url);
        }
    }

    static ITextCharacterFormat Format(ITextCharacterFormat plain, float size, RunStyle style)
    {
        var format = plain.GetClone();
        if (style.HasFlag(RunStyle.Bold) || (style & (RunStyle.Heading1 | RunStyle.Heading2 | RunStyle.Heading3)) != 0)
            format.Bold = FormatEffect.On;
        if (style.HasFlag(RunStyle.Italic))
            format.Italic = FormatEffect.On;
        if (style.HasFlag(RunStyle.Strike))
            format.Strikethrough = FormatEffect.On;
        if ((style & (RunStyle.Code | RunStyle.CodeBlock)) != 0)
        {
            format.Name = "Consolas";
            format.BackgroundColor = CodeBackground;
        }
        if (style.HasFlag(RunStyle.Link))
        {
            format.ForegroundColor = LinkColor;
            format.Underline = UnderlineType.Single;
        }
        else if ((style & (RunStyle.Marker | RunStyle.Quote | RunStyle.Checked)) != 0)
        {
            format.ForegroundColor = Secondary;
        }
        if (style.HasFlag(RunStyle.Heading1))
            format.Size = size * 1.5f;
        else if (style.HasFlag(RunStyle.Heading2))
            format.Size = size * 1.3f;
        else if (style.HasFlag(RunStyle.Heading3))
            format.Size = size * 1.15f;
        return format;
    }
}
#endif
