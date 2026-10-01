using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class DashboardPage : BasePage
{
    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
