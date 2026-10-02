using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.Wear;

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

        // The phone app's data, workout and sync services (linked from GymBook/, see the csproj), set up as the phone does.
        builder.Services.AddSingleton<DataStore>();
        builder.Services.AddSingleton<Units>();
        builder.Services.AddSingleton<ProgressionEngine>();
        builder.Services.AddSingleton<WorkoutService>();
        builder.Services.AddSingleton<RecoveryService>();
        builder.Services.AddSingleton<WorkoutEstimator>();
        builder.Services.AddSingleton<StatsService>();
        builder.Services.AddSingleton<AuthSession>();
        builder.Services.AddSingleton(sp => new ApiClient(ApiConfig.CreateClient(), sp.GetRequiredService<AuthSession>()));
        builder.Services.AddSingleton<SyncService>();
        builder.Services.AddSingleton<LiveSync>();

        // The watch's own.
        builder.Services.AddSingleton<PhoneLink>();
        builder.Services.AddSingleton<HeartRateMonitor>();
        builder.Services.AddSingleton<WatchAccount>();

        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddTransient<CompanionViewModel>();
        builder.Services.AddTransient<CompanionPage>();
        builder.Services.AddTransient<WorkoutViewModel>();
        builder.Services.AddTransient<WorkoutPage>();
        builder.Services.AddTransient<SignInViewModel>();
        builder.Services.AddTransient<SignInPage>();
        builder.Services.AddTransient<ExercisePickerViewModel>();
        builder.Services.AddTransient<ExercisePickerPage>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();

        // The crown or rotating bezel scrolls every page.
        RotaryScroll.Register();

        var app = builder.Build();
        // Every heart rate read (while a workout is on screen) goes to the phone too, to show there.
        var phone = app.Services.GetRequiredService<PhoneLink>();
        app.Services.GetRequiredService<HeartRateMonitor>().Changed += phone.SendHeartRate;
        return app;
    }
}
