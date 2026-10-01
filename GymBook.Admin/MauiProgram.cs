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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AdminSession>();
        builder.Services.AddSingleton(sp => new AdminApi(ApiConfig.CreateClient(), sp.GetRequiredService<AdminSession>()));

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<UsersPage>();
        builder.Services.AddTransient<UsersViewModel>();
        builder.Services.AddTransient<UserDetailPage>();
        builder.Services.AddTransient<UserDetailViewModel>();
        builder.Services.AddTransient<AiUsagePage>();
        builder.Services.AddTransient<AiUsageViewModel>();

        return builder.Build();
    }
}
