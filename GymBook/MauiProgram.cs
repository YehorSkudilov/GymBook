using AppSkeleton;
using GymBook.Services;
using GymBook.Services.Sync;
using GymBook.ViewModels;
using GymBook.Views;
using Microsoft.Extensions.Logging;

namespace GymBook;

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

        builder.Services.AddSingleton<DataStore>();
        builder.Services.AddSingleton<Units>();
        builder.Services.AddSingleton<ProgressionEngine>();
        builder.Services.AddSingleton<WorkoutEstimator>();
        builder.Services.AddSingleton<RecoveryService>();
        builder.Services.AddSingleton<StatsService>();
        builder.Services.AddSingleton<WorkoutService>();
        builder.Services.AddSingleton<DialogService>();
        builder.Services.AddSingleton<ExercisePickerService>();
        builder.Services.AddSingleton<AuthSession>();
        builder.Services.AddSingleton(sp => new ApiClient(ApiConfig.CreateClient(), sp.GetRequiredService<AuthSession>()));
        builder.Services.AddSingleton<SyncService>();
#if ANDROID
        builder.Services.AddSingleton<IGoogleSignIn, GoogleSignInService>();
#else
        builder.Services.AddSingleton<IGoogleSignIn, NoGoogleSignIn>();
#endif
        builder.Services.AddSingleton<AccountService>();
        builder.Services.AddSingleton<AiPlanService>();

        builder.Services.AddTransient<MainPage>();
        AddPage<HomePage, HomeViewModel>(builder.Services);
        AddPage<PlansPage, PlansViewModel>(builder.Services);
        AddPage<ExercisesPage, ExercisesViewModel>(builder.Services);
        AddPage<StatsPage, StatsViewModel>(builder.Services);
        AddPage<ProfilePage, ProfileViewModel>(builder.Services);
        AddPage<WorkoutPage, WorkoutViewModel>(builder.Services);
        AddPage<WorkoutMenuPage, WorkoutMenuViewModel>(builder.Services);
        AddPage<SessionDetailPage, SessionDetailViewModel>(builder.Services);
        AddPage<HistoryPage, HistoryViewModel>(builder.Services);
        AddPage<CalendarPage, CalendarViewModel>(builder.Services);
        AddPage<PlanDetailPage, PlanDetailViewModel>(builder.Services);
        AddPage<PlanDayPage, PlanDayViewModel>(builder.Services);
        AddPage<PlanWorkoutEditPage, PlanWorkoutEditViewModel>(builder.Services);
        AddPage<PlanWizardPage, PlanWizardViewModel>(builder.Services);
        AddPage<PlanChatPage, PlanChatViewModel>(builder.Services);
        AddPage<ImportPlanPage, ImportPlanViewModel>(builder.Services);
        AddPage<RecoveryWarningPage, RecoveryWarningViewModel>(builder.Services);
        AddPage<ExerciseDetailPage, ExerciseDetailViewModel>(builder.Services);
        AddPage<ExercisePickerPage, ExercisePickerViewModel>(builder.Services);
        AddPage<AccountPage, AccountViewModel>(builder.Services);
        AddPage<ManageAccountPage, ManageAccountViewModel>(builder.Services);
        AddPage<SyncDetailsPage, SyncDetailsViewModel>(builder.Services);
        AddPage<RecoveryPage, RecoveryViewModel>(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    static void AddPage<TPage, TViewModel>(IServiceCollection services)
        where TPage : Element
        where TViewModel : class
    {
        services.AddTransient<TPage>();
        services.AddTransient<TViewModel>();
    }
}
