using GymBook.Admin.Views;

namespace GymBook.Admin;

public partial class AppShell : Shell
{
    public const string UserRoute = "user";

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(UserRoute, typeof(UserDetailPage));
    }
}
