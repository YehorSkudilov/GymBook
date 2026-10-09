using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// The user's XP, level and league (see <see cref="Levels"/>), and where their lifts stand against famous ones (see
/// <see cref="FamousLifts"/>), worked out from the workout history and kept until the data changes.
/// </summary>
public class LevelService(DataStore store)
{
    int _version = -1;
    List<XpEvent> _events = [];

    public IReadOnlyList<XpEvent> Events
    {
        get
        {
            if (_version != store.Version)
            {
                _events = Levels.Events(store.Data.Sessions, store.GetPlan, store.Profile.DaysPerWeek, DateTime.Now);
                _version = store.Version;
            }
            return _events;
        }
    }

    public LevelStatus Status => Levels.StatusOf(Levels.Total(Events));

    /// <summary>What one finished workout earned: its own events, and the week's goal if it was the workout that met it.</summary>
    public List<XpEvent> EarnedBy(WorkoutSession session) =>
        [.. Events.Where(e => e.SessionId == session.Id || e.Reason == XpReason.WeekGoal && e.At == session.EndedAt)];

    /// <summary>Where XP stood just before <paramref name="session"/> earned anything.</summary>
    public LevelStatus StatusBefore(WorkoutSession session)
    {
        var earned = EarnedBy(session);
        return Levels.StatusOf(Levels.Total(Events.Where(e => e.At < session.EndedAt || e.At == session.EndedAt && !earned.Contains(e))));
    }

    /// <summary>The best estimated one-rep max ever logged for a famous lift (its competition movement and close variants); 0 for none.</summary>
    public double BestE1Rm(RankLift lift) => store.History
        .SelectMany(s => s.Exercises)
        .Where(e => Ranks.LiftOf(e.ExerciseId) == lift)
        .SelectMany(e => e.Sets)
        .Where(s => s.IsCompleted && !s.IsWarmup)
        .Select(s => SetE1Rm(s))
        .DefaultIfEmpty(0)
        .Max();

    /// <summary>A set's estimated one-rep max, as compared to famous lifts; 0 for sets with too many reps to say.</summary>
    public static double SetE1Rm(SetEntry s) => s.Reps + (s.Rir ?? 0) <= Ranks.MaxReps ? Ranks.E1Rm(s.WeightKg, s.Reps, s.Rir) : 0;

    public static string Describe(XpEvent e, Func<string, string> exerciseName) => e.Reason switch
    {
        XpReason.Workout => "Workout finished",
        XpReason.WeekGoal => "Weekly goal met",
        XpReason.Record => $"New best: {exerciseName(e.ExerciseId ?? "")}",
        XpReason.RepRecord => $"Rep record: {exerciseName(e.ExerciseId ?? "")}",
        XpReason.MissedWorkout => e.Amount == Levels.MissedXp ? "Missed a planned workout" : $"Missed {e.Amount / Levels.MissedXp} planned workouts",
        _ => $"Went up but missed the reps: {exerciseName(e.ExerciseId ?? "")}",
    };

    public static string Signed(int xp) => xp >= 0 ? $"+{xp} XP" : $"−{-xp} XP";
}
