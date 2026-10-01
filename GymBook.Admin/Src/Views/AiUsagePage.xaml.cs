using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class AiUsagePage : TabView
{
    public AiUsagePage(AiUsageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
