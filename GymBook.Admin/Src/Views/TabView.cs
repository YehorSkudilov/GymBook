using AppSkeleton;
using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

/// <summary>
/// A tab hosted in <see cref="MainPage"/>'s CView, as in the GymBook app: wraps its content in its own ScrollView with
/// the page padding (so each tab keeps its scroll position) and forwards tab lifecycle to the view model. Tabs whose
/// content scrolls by itself (a CollectionView) set <see cref="ScrollsItself"/> and only get the padding.
/// </summary>
public class TabView : PageBase
{
    // Sides line up with the nav bar (inset 10 in MainPage); the bottom clears the nav bar floating over the tabs.
    const double SideGutter = 10, Gutter = 20, NavBarClearance = 70;

    public static readonly BindableProperty ScrollsItselfProperty =
        BindableProperty.Create(nameof(ScrollsItself), typeof(bool), typeof(TabView), false,
            propertyChanged: (b, _, _) => ((TabView)b).UpdateTemplate());

    public TabView() => UpdateTemplate();

    public bool ScrollsItself
    {
        get => (bool)GetValue(ScrollsItselfProperty);
        set => SetValue(ScrollsItselfProperty, value);
    }

    /// <summary>Space a self-scrolling tab leaves after its last item, so it scrolls clear of the nav bar.</summary>
    public static double BottomInset => NavBarClearance;

    // The ScrollView goes in through a ControlTemplate instead of by swapping Content (see the GymBook app's TabView).
    void UpdateTemplate()
    {
        if (ScrollsItself)
        {
            ControlTemplate = null;
            Padding = new Thickness(SideGutter, Gutter, SideGutter, 0);
            return;
        }
        Padding = 0;
        ControlTemplate = new ControlTemplate(() => new ScrollView
        {
            Content = new ContentPresenter(),
            Padding = new Thickness(SideGutter, Gutter, SideGutter, NavBarClearance),
        });
    }

    public override async void Load()
    {
        if (BindingContext is BaseViewModel vm)
            await vm.OnAppearingAsync();
    }
}
