using GymBook.Admin.ViewModels;

namespace GymBook.Admin.Views;

public partial class UserDetailPage : BasePage
{
    public UserDetailPage(UserDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
