using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanChatPage : SheetPage
{
    public PlanChatPage(PlanChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HorizontalMouseScroll.Attach(SuggestionStrip);
        // Keep the newest message in view.
        viewModel.Messages.CollectionChanged += (_, _) => Dispatcher.Dispatch(() =>
        {
            if (viewModel.Messages.Count > 0)
                MessageList.ScrollTo(viewModel.Messages.Count - 1, position: ScrollToPosition.End, animate: true);
        });
    }
}
