using System.Text.RegularExpressions;
using GymBook.Controls.RichText;
using GymBook.ViewModels;

namespace GymBook.Views;

/// <summary>
/// Writing a ticket or comment in a text box that shows formatting as it's typed (see <see cref="RichEditor"/>). The
/// toolbar formats the selection, or what's typed next; headings, lists, checklists and quotes go on the selected lines;
/// attachments go in where the cursor is.
/// </summary>
public partial class FeedbackEditPage : BasePage
{
    FeedbackEditViewModel ViewModel => (FeedbackEditViewModel)BindingContext;

    public FeedbackEditPage(FeedbackEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.InsertRequested += InsertAttachment;
    }

    protected override bool OnBackButtonPressed()
    {
        ViewModel.BackCommand.Execute(null);
        return true;
    }

    async void OnFormat(object? sender, EventArgs e)
    {
        switch ((sender as Element)?.ClassId)
        {
            case "bold": BodyEditor.ToggleStyle(InlineStyle.Bold); break;
            case "italic": BodyEditor.ToggleStyle(InlineStyle.Italic); break;
            case "strike": BodyEditor.ToggleStyle(InlineStyle.Strike); break;
            case "code": BodyEditor.ToggleStyle(InlineStyle.Code); break;
            case "heading": BodyEditor.ToggleBlock(BlockKind.Heading); break;
            case "bullets": BodyEditor.ToggleBlock(BlockKind.Bullet); break;
            case "numbers": BodyEditor.ToggleBlock(BlockKind.Numbered); break;
            case "tasks": BodyEditor.ToggleBlock(BlockKind.Task); break;
            case "quote": BodyEditor.ToggleBlock(BlockKind.Quote); break;
            case "link":
                if (await ViewModel.AskLinkAsync() is { } url)
                    BodyEditor.SetLink(url);
                break;
        }
    }

    /// <summary>An uploaded attachment's Markdown (![name](url) for a picture, [name](url) otherwise), as a link in the text.</summary>
    void InsertAttachment(string markdown)
    {
        if (AttachmentLink().Match(markdown) is { Success: true } m)
            BodyEditor.InsertAttachment(m.Groups["label"].Value, m.Groups["url"].Value, image: m.Groups["image"].Length > 0);
    }

    [GeneratedRegex(@"^(?<image>!?)\[(?<label>[^\]]*)\]\((?<url>[^)\s]+)\)$")]
    private static partial Regex AttachmentLink();
}

