using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Estimates how recovered each muscle is (0 = just trained hard, 1 = fully fresh) from recent sets.
/// More sets means a longer recovery window: 24h base plus 8h per set, capped at 96h.
/// </summary>
public class RecoveryService(DataStore store)
{
    public Dictionary<MuscleGroup, double> Compute(DateTime now)
    {
        var result = Enum.GetValues<MuscleGroup>().ToDictionary(m => m, _ => 1.0);

        foreach (var session in store.History.TakeWhile(s => (now - s.EndedAt!.Value).TotalHours < 96))
        {
            var hours = (now - session.EndedAt!.Value).TotalHours;
            foreach (var (muscle, sets) in SetsPerMuscle(session))
            {
                var needed = Math.Clamp(24 + sets * 8, 24, 96);
                var recovered = Math.Clamp(hours / needed, 0, 1);
                result[muscle] = Math.Min(result[muscle], recovered);
            }
        }
        return result;
    }

    /// <summary>Primary muscles get one set each; secondary muscles get half a set.</summary>
    public Dictionary<MuscleGroup, double> SetsPerMuscle(WorkoutSession session)
    {
        var sets = new Dictionary<MuscleGroup, double>();
        foreach (var se in session.Exercises)
        {
            var ex = store.GetExercise(se.ExerciseId);
            if (ex == null)
                continue;
            var count = se.Sets.Count(s => s.IsCompleted && !s.IsWarmup);
            if (count == 0)
                continue;
            sets[ex.PrimaryMuscle] = sets.GetValueOrDefault(ex.PrimaryMuscle) + count;
            foreach (var m in ex.SecondaryMuscles)
                sets[m] = sets.GetValueOrDefault(m) + count * 0.5;
        }
        return sets;
    }

    public static Color ColorFor(double recovery) => recovery switch
    {
        >= 0.9 => Color.FromArgb("#2ED47A"),
        >= 0.6 => Color.FromArgb("#B6D83F"),
        >= 0.35 => Color.FromArgb("#FFB020"),
        _ => Color.FromArgb("#FF4D5E"),
    };
}
