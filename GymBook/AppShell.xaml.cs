using GymBook.Views;

namespace GymBook;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Workout, typeof(WorkoutPage));
        Routing.RegisterRoute(Routes.Session, typeof(SessionDetailPage));
        Routing.RegisterRoute(Routes.History, typeof(HistoryPage));
        Routing.RegisterRoute(Routes.Calendar, typeof(CalendarPage));
        Routing.RegisterRoute(Routes.Plan, typeof(PlanDetailPage));
        Routing.RegisterRoute(Routes.PlanDay, typeof(PlanDayPage));
        Routing.RegisterRoute(Routes.PlanWorkout, typeof(PlanWorkoutEditPage));
        Routing.RegisterRoute(Routes.Wizard, typeof(PlanWizardPage));
        Routing.RegisterRoute(Routes.Exercise, typeof(ExerciseDetailPage));
    }
}

public static class Routes
{
    public const string Workout = "workout";
    public const string Session = "session";
    public const string History = "history";
    public const string Calendar = "calendar";
    public const string Plan = "plan";
    public const string PlanDay = "planday";
    public const string PlanWorkout = "planworkout";
    public const string Wizard = "wizard";
    public const string Exercise = "exercise";
}
