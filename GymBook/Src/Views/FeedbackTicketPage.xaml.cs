using GymBook.Services;
using GymBook.ViewModels;

namespace GymBook.Views;

public partial class FeedbackTicketPage : BasePage
{
    FeedbackTicketViewModel ViewModel => (FeedbackTicketViewModel)BindingContext;

    public FeedbackTicketPage(FeedbackTicketViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // The page itself loads as HTML; a link tapped in it opens in the browser, or is a comment's Edit or Delete.
    void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https" or "mailto" or FeedbackHtml.Scheme))
            return;
        e.Cancel = true;
        _ = ViewModel.OpenLinkAsync(uri);
    }
}
