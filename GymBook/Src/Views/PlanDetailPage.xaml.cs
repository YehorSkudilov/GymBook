using System.Collections.ObjectModel;
using AppSkeleton;
using GymBook.ViewModels;

namespace GymBook.Views;

public partial class PlanDetailPage : BasePage
{
    // A page per day: what the day pager (a CView, like the app's tabs) swipes between.
    readonly ObservableCollection<CNavItem> _days = [];
    int _shown = -1;

    public PlanDetailPage(PlanDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HorizontalMouseScroll.Attach(DayStrip);
        DayPager.CNavIconItems = _days;
        // Swiped to a day: the pager is already showing it, so it just becomes the selected one.
        DayPager.SwipeNavigationCommand = new Command<CNavItem>(item => viewModel.ShowDay(_days.IndexOf(item)));
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlanDetailViewModel.DayPages))
                ShowDays(viewModel);
        };
    }

    // After every refresh of the page. The same days as before (an edit, another day picked): each page takes its
    // day's new state, and the pager slides over to the selected one if it isn't there yet. Days added, removed or
    // moved: new pages, with the selected one straight in.
    void ShowDays(PlanDetailViewModel vm)
    {
        var pages = vm.DayPages;
        DayPager.IsVisible = pages.Count > 0;
        if (pages.Count == 0)
        {
            _days.Clear();
            _shown = -1;
            return;
        }
        var sameDays = pages.Count == _days.Count
            && pages.Select(p => p.Workout).SequenceEqual(_days.Select(d => ((PlanDetailDay)d.Page.BindingContext).Workout));
        if (sameDays)
        {
            for (var i = 0; i < pages.Count; i++)
                _days[i].Page.BindingContext = pages[i];
        }
        else
        {
            // The pager no longer has the page it's showing, so the next one comes in without a slide.
            _days.Clear();
            foreach (var page in pages)
                _days.Add(new CNavItem { PageName = page.DayName, Page = new PlanDetailDayView { BindingContext = page } });
        }
        var selected = Math.Clamp(vm.SelectedDay, 0, pages.Count - 1);
        DayPager.SetContent(_days[selected]);

        if (selected != _shown)
        {
            _shown = selected;
            // Its chip into view, once the rebuilt chips are laid out.
            Dispatcher.Dispatch(() =>
            {
                if (DayChips.Children.ElementAtOrDefault(selected) is Element chip)
                    _ = DayStrip.ScrollToAsync(chip, ScrollToPosition.MakeVisible, true);
            });
        }
    }

    // The hardware back button, like the back arrow: unsaved changes are saved or discarded first.
    protected override bool OnBackButtonPressed()
    {
        var vm = (PlanDetailViewModel)BindingContext;
        if (!vm.HasChanges)
            return base.OnBackButtonPressed();
        vm.BackCommand.Execute(null);
        return true;
    }
}
