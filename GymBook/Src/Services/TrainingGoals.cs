using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// What each training goal means for an exercise: its rep range (the minimum tells you when to lower the weight,
/// the maximum when to add some), sets, effort and rest, how fast each rep is performed, and the coaching tips
/// shown with the exercise.
/// </summary>
public static class TrainingGoals
{
    /// <summary>Goals in the order they're offered.</summary>
    public static IReadOnlyList<Goal> All { get; } = [Goal.BuildMuscle, Goal.Strength, Goal.Power, Goal.LoseFat, Goal.GeneralFitness];

    /// <summary>Jumps, throws and Olympic lifts: trained for speed, in short sets.</summary>
    public static bool IsExplosive(Exercise ex) =>
        ex.Id is "kb_swing" or "Power_Clean"
        || ExerciseLibrary.Details(ex.Id)?.Category is { } c
            && (c.Equals("Plyometrics", StringComparison.OrdinalIgnoreCase) || c.Equals("Olympic weightlifting", StringComparison.OrdinalIgnoreCase));

    /// <summary>Isometric holds count each rep as a hold of a few seconds.</summary>
    public static bool IsIsometric(Exercise ex) => ex.Name.Contains("Isometric", StringComparison.OrdinalIgnoreCase);

    /// <summary>Sets, reps, effort and rest for <paramref name="ex"/> under <paramref name="goal"/>; the rest times set in <paramref name="profile"/>, if any, replace the goal's.</summary>
    public static PlanExercise Prescription(Goal goal, Experience experience, Exercise ex, UserProfile? profile = null)
    {
        var compound = ex.Mechanic == Mechanic.Compound;
        var explosive = IsExplosive(ex);
        var (min, max) = (goal, compound) switch
        {
            // Bodybuilding: at least 10 reps on compounds, 12 on isolation work.
            (Goal.BuildMuscle, true) => (10, 14),
            (Goal.BuildMuscle, false) => (12, 16),
            // Heavy and fast on the big lifts, muscle endurance on the accessories.
            (Goal.Strength, true) => (3, 6),
            (Goal.Strength, false) => (10, 15),
            // Combat sports: 6 reps at most on heavy lifts; accessories keep the joints resilient.
            (Goal.Power, true) => (3, 6),
            (Goal.Power, false) => (8, 12),
            (Goal.LoseFat, true) => (10, 15),
            (Goal.LoseFat, false) => (12, 20),
            (_, true) => (8, 12),
            _ => (10, 15),
        };
        if (explosive)
            (min, max) = (3, 5);
        else if (ex.IsBodyweight)
            (min, max) = (Math.Max(min, 8), Math.Max(max, 15));

        var rest = (goal, compound) switch
        {
            (Goal.Strength or Goal.Power, true) => 180,
            (Goal.Strength or Goal.Power, false) => 90,
            (Goal.LoseFat, _) => 60,
            (_, true) => 120,
            _ => 75,
        };
        if (explosive)
            rest = 150;

        var sets = experience == Experience.Beginner ? 3 : compound ? 4 : 3;
        var rir = experience switch { Experience.Beginner => 3, Experience.Intermediate => 2, _ => 1 };
        // Heavy and explosive work stops while every rep is still fast and clean.
        if (goal is Goal.Strength or Goal.Power && compound || explosive)
            rir = Math.Max(rir, 2);

        if (ex.PrimaryMuscle == MuscleGroup.Neck)
        {
            // The neck responds to steady, higher-rep work and is never taken close to failure.
            (min, max) = IsIsometric(ex) ? (5, 8) : (12, 20);
            sets = experience == Experience.Beginner ? 2 : 3;
            rir = 3;
            rest = 60;
        }

        if (RestOverride(profile, ex) is { } own)
            rest = own;

        return new PlanExercise { ExerciseId = ex.Id, Sets = sets, RepMin = min, RepMax = max, TargetRir = rir, RestSeconds = rest };
    }

    /// <summary>The rest the user set for exercises like <paramref name="ex"/>, or null to rest as the goal suggests.</summary>
    public static int? RestOverride(UserProfile? profile, Exercise ex) =>
        ex.Mechanic == Mechanic.Compound ? profile?.CompoundRestSeconds : profile?.IsolationRestSeconds;

    /// <summary>Roughly how long one rep takes, including the lowering.</summary>
    public static double SecondsPerRep(Goal goal, Exercise? ex)
    {
        if (ex != null && ex.PrimaryMuscle == MuscleGroup.Neck)
            return IsIsometric(ex) ? 6 : 4;
        if (ex != null && IsExplosive(ex))
            return 2.5;
        return goal switch
        {
            Goal.BuildMuscle => 4,
            Goal.Power => 2.5,
            Goal.LoseFat => 3,
            _ => 3.5,
        };
    }

    /// <summary>Coaching cues for doing <paramref name="ex"/> the way <paramref name="goal"/> needs, most important first.</summary>
    public static List<string> Tips(Goal goal, Exercise ex, int repMin, int repMax, int restSeconds)
    {
        var tips = new List<string>();
        var compound = ex.Mechanic == Mechanic.Compound;
        var rest = Units.Rest(restSeconds);

        if (ex.PrimaryMuscle == MuscleGroup.Neck)
        {
            tips.Add(IsIsometric(ex)
                ? $"Each rep is a 5-second hold: push steadily into your hand without letting your head move. {repMin}–{repMax} holds."
                : $"{repMin}–{repMax} slow reps: 2 seconds up, a short pause, 2–3 seconds down.");
            tips.Add("Start much lighter than you think. The neck gets strong quickly but strains easily.");
            tips.Add("Move only through a pain-free range and never jerk, bounce or swing the weight.");
            tips.Add("Stop straight away if you feel sharp pain, dizziness, numbness or tingling.");
            tips.Add(goal == Goal.Power
                ? "Train it 2–3 times a week: a strong neck helps you absorb punches and resist chokes and takedowns."
                : "Train it 2–3 times a week, like any other muscle. It supports your posture and protects the cervical spine.");
            return tips;
        }

        if (IsExplosive(ex))
        {
            tips.Add($"Maximum speed on every rep, {repMin}–{repMax} reps. Reset fully between reps.");
            tips.Add("End the set as soon as a rep is slower or lower than the first; tired reps don't build power.");
            tips.Add($"Rest the full {rest} so every set stays explosive.");
            return tips;
        }

        switch (goal, compound)
        {
            case (Goal.Strength, true):
                tips.Add($"Heavy: {repMin}–{repMax} reps. Drive every rep up fast with good form, in quick succession.");
                tips.Add("Stop the set when your form starts to break down, even if you could grind out another rep.");
                tips.Add($"Rest the full {rest}. Strength needs recovery between sets.");
                break;
            case (Goal.Strength, false):
                tips.Add($"Muscle endurance: {repMin}–{repMax} steady reps, keeping tension on the muscle throughout.");
                break;
            case (Goal.Power, true):
                tips.Add($"Explosive: lower under control, then lift as fast as you can. {repMax} reps at most.");
                tips.Add("Stop when the bar noticeably slows down. Speed, not grinding, carries over to your sport.");
                tips.Add($"Rest the full {rest} so each set is as fast as the first.");
                break;
            case (Goal.Power, false):
                tips.Add($"Resilience work: {repMin}–{repMax} controlled reps to toughen the joints you use in training and sparring.");
                break;
            case (Goal.BuildMuscle, true):
                tips.Add($"At least {repMin} reps. Lower for 2–3 seconds and use a full range of motion.");
                tips.Add("Finish 1–2 reps short of failure with your form intact.");
                break;
            case (Goal.BuildMuscle, false):
                tips.Add($"At least {repMin} reps. Squeeze at the top and lower slowly for 2–3 seconds.");
                tips.Add("Feel the target muscle work; momentum only moves the weight, not the muscle.");
                break;
            case (Goal.LoseFat, _):
                tips.Add($"{repMin}–{repMax} reps at a steady pace, with short rests ({rest}) to keep your heart rate up.");
                break;
            default:
                tips.Add($"{repMin}–{repMax} smooth, controlled reps with good form.");
                break;
        }
        tips.Add($"Fewer than {repMin} reps means the weight is too heavy, so lower it. {repMax} or more with reps to spare means it's time to add weight.");
        return tips;
    }
}
