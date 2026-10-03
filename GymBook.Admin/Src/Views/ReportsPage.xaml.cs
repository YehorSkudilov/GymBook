using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class ReportsPage : TabView
{
    public ReportsPage(ReportsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
