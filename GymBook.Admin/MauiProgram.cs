using AppSkeleton;
using GymBook.Admin.Services;
using GymBook.Admin.ViewModels;
using GymBook.Admin.Views;

namespace GymBook.Admin;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseAppSkeleton()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddCIcons();
            });

        builder.Services.AddSingleton<AdminSession>();
        builder.Services.AddSingleton(sp => new AdminApi(ApiConfig.CreateClient(), sp.GetRequiredService<AdminSession>()));

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LoginViewModel>();

        // The tabs: MainPage makes one of each per sign-in.
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<UsersPage>();
        // A singleton so the dashboard's tiles can open the Users tab on a filter.
        builder.Services.AddSingleton<UsersViewModel>();
        builder.Services.AddTransient<AiUsagePage>();
        builder.Services.AddTransient<AiUsageViewModel>();
        builder.Services.AddTransient<AccountPage>();
        builder.Services.AddTransient<AccountViewModel>();

        builder.Services.AddTransient<UserDetailPage>();
        builder.Services.AddTransient<UserDetailViewModel>();

        return builder.Build();
    }
}
