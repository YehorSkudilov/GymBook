using GymBook.Views;

namespace GymBook;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Workout, typeof(WorkoutPage));
        Routing.RegisterRoute(Routes.WorkoutMenu, typeof(WorkoutMenuPage));
        Routing.RegisterRoute(Routes.History, typeof(HistoryPage));
        Routing.RegisterRoute(Routes.Calendar, typeof(CalendarPage));
        Routing.RegisterRoute(Routes.Plan, typeof(PlanDetailPage));
        Routing.RegisterRoute(Routes.PlanDay, typeof(PlanDayPage));
        Routing.RegisterRoute(Routes.Wizard, typeof(PlanWizardPage));
        Routing.RegisterRoute(Routes.PlanChat, typeof(PlanChatPage));
        Routing.RegisterRoute(Routes.PlanReview, typeof(PlanReviewPage));
        Routing.RegisterRoute(Routes.ImportPlan, typeof(ImportPlanPage));
        Routing.RegisterRoute(Routes.RecoveryWarning, typeof(RecoveryWarningPage));
        Routing.RegisterRoute(Routes.Exercise, typeof(ExerciseDetailPage));
        Routing.RegisterRoute(Routes.SyncDetails, typeof(SyncDetailsPage));
        Routing.RegisterRoute(Routes.Recovery, typeof(RecoveryPage));
        Routing.RegisterRoute(Routes.WorkoutDone, typeof(WorkoutDonePage));
        Routing.RegisterRoute(Routes.Food, typeof(FoodEntryPage));
        Routing.RegisterRoute(Routes.NutritionGoals, typeof(NutritionGoalsPage));
        Routing.RegisterRoute(Routes.ExerciseLibrary, typeof(ExerciseLibraryPage));
        Routing.RegisterRoute(Routes.PlanWorkouts, typeof(PlanWorkoutsPage));
        Routing.RegisterRoute(Routes.PublicProfile, typeof(PublicProfilePage));
        Routing.RegisterRoute(Routes.Ranks, typeof(RanksPage));
        Routing.RegisterRoute(Routes.PlanShare, typeof(PlanSharePage));
        Routing.RegisterRoute(Routes.SharedPlan, typeof(SharedPlanPage));
    }
}

public static class Routes
{
    public const string Workout = "workout";
    public const string WorkoutMenu = "workoutmenu";
    public const string History = "history";
    public const string Calendar = "calendar";
    public const string Plan = "plan";
    public const string PlanDay = "planday";
    public const string Wizard = "wizard";
    public const string PlanChat = "planchat";
    public const string PlanReview = "planreview";
    public const string ImportPlan = "importplan";
    public const string RecoveryWarning = "recoverywarning";
    public const string Exercise = "exercise";
    public const string SyncDetails = "syncdetails";
    public const string Recovery = "recovery";
    public const string WorkoutDone = "workoutdone";
    public const string Food = "food";
    public const string NutritionGoals = "nutritiongoals";
    public const string ExerciseLibrary = "exerciselibrary";
    public const string PlanWorkouts = "planworkouts";
    public const string PublicProfile = "publicprofile";
    public const string Ranks = "ranks";
    public const string PlanShare = "planshare";
    public const string SharedPlan = "sharedplan";
}
