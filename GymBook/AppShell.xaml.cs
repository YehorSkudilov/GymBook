using GymBook.Views;

namespace GymBook;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Workout, typeof(WorkoutPage));
        Routing.RegisterRoute(Routes.WorkoutMenu, typeof(WorkoutMenuPage));
        Routing.RegisterRoute(Routes.Session, typeof(SessionDetailPage));
        Routing.RegisterRoute(Routes.History, typeof(HistoryPage));
        Routing.RegisterRoute(Routes.Calendar, typeof(CalendarPage));
        Routing.RegisterRoute(Routes.Plan, typeof(PlanDetailPage));
        Routing.RegisterRoute(Routes.PlanDay, typeof(PlanDayPage));
        Routing.RegisterRoute(Routes.PlanWorkout, typeof(PlanWorkoutEditPage));
        Routing.RegisterRoute(Routes.Wizard, typeof(PlanWizardPage));
        Routing.RegisterRoute(Routes.PlanChat, typeof(PlanChatPage));
        Routing.RegisterRoute(Routes.ImportPlan, typeof(ImportPlanPage));
        Routing.RegisterRoute(Routes.RecoveryWarning, typeof(RecoveryWarningPage));
        Routing.RegisterRoute(Routes.Exercise, typeof(ExerciseDetailPage));
        Routing.RegisterRoute(Routes.SyncDetails, typeof(SyncDetailsPage));
        Routing.RegisterRoute(Routes.Recovery, typeof(RecoveryPage));
    }
}

public static class Routes
{
    public const string Workout = "workout";
    public const string WorkoutMenu = "workoutmenu";
    public const string Session = "session";
    public const string History = "history";
    public const string Calendar = "calendar";
    public const string Plan = "plan";
    public const string PlanDay = "planday";
    public const string PlanWorkout = "planworkout";
    public const string Wizard = "wizard";
    public const string PlanChat = "planchat";
    public const string ImportPlan = "importplan";
    public const string RecoveryWarning = "recoverywarning";
    public const string Exercise = "exercise";
    public const string SyncDetails = "syncdetails";
    public const string Recovery = "recovery";
}
