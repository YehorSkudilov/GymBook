using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class AiUsagePage : BasePage
{
    public AiUsagePage(AiUsageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
