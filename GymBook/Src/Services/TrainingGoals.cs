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
    public static IReadOnlyList<Goal> All { get; } = [Goal.BuildMuscle, Goal.Aesthetics, Goal.Strength, Goal.Power, Goal.LoseFat, Goal.GeneralFitness];

    /// <summary>Jumps, throws and Olympic lifts: trained for speed, in short sets.</summary>
    public static bool IsExplosive(Exercise ex) =>
        ex.Id == "kb_swing" || ExerciseLibrary.Details(ex.Id)?.Category is ExerciseCategory.Plyometric or ExerciseCategory.Olympic;

    /// <summary>Isometric holds: for the neck each rep is a hold of a few seconds (see <see cref="IsTimed"/> for the rest).</summary>
    public static bool IsIsometric(Exercise ex) =>
        ExerciseLibrary.Details(ex.Id)?.Hold == true || ex.Name.Contains("Isometric", StringComparison.OrdinalIgnoreCase);

    /// <summary>Held rather than repeated (planks, stretches, yoga poses): each "rep" is a second of holding. Not the neck's holds.</summary>
    public static bool IsTimed(Exercise ex) =>
        ExerciseLibrary.Details(ex.Id)?.Category is ExerciseCategory.Stretch or ExerciseCategory.Yoga
        || IsIsometric(ex) && ex.PrimaryMuscle != MuscleGroup.Neck;

    /// <summary>Carries and sleds: walked or pushed for time or distance, not repeated (see <see cref="DefaultUnit"/>).</summary>
    public static bool IsCarry(Exercise ex) => ExerciseLibrary.Details(ex.Id)?.Unit != null;

    /// <summary>
    /// What <paramref name="ex"/>'s sets are counted in when it's put in a plan or workout: the library's unit for
    /// carries and sleds (seconds for a farmer's walk), seconds for holds, else reps.
    /// </summary>
    public static RepUnit DefaultUnit(Exercise ex) =>
        ExerciseLibrary.Details(ex.Id)?.Unit ?? (IsTimed(ex) ? RepUnit.Seconds : RepUnit.Reps);

    /// <summary>
    /// What the sets count: <paramref name="chosen"/>, the unit kept with the plan or workout exercise. Without one (as
    /// everything was before units were kept), reps, or seconds for a hold, which always logged seconds as reps; never
    /// the library's unit, so a carry logged in reps before stays in reps.
    /// </summary>
    public static RepUnit UnitOf(Exercise? ex, RepUnit? chosen) => chosen ?? (ex != null && IsTimed(ex) ? RepUnit.Seconds : RepUnit.Reps);

    /// <summary>The unit to keep for <paramref name="unit"/>: null when that's what none means (see <see cref="UnitOf"/>).</summary>
    public static RepUnit? Stored(Exercise? ex, RepUnit unit) => unit == UnitOf(ex, null) ? null : unit;

    /// <summary>
    /// A rep range for <paramref name="ex"/> counted in <paramref name="unit"/>: the goal's when that's its usual unit,
    /// otherwise a typical one for the unit (half a minute to a minute of walking, 20–40 m, 40–80 steps).
    /// </summary>
    public static (int Min, int Max) Range(Goal goal, Experience experience, Exercise ex, RepUnit unit)
    {
        if (unit == DefaultUnit(ex))
        {
            var p = Prescription(goal, experience, ex);
            return (p.RepMin, p.RepMax);
        }
        return UnitRange(unit) ?? (8, 12);
    }

    static (int, int)? UnitRange(RepUnit unit) => unit switch
    {
        RepUnit.Seconds => (30, 60),
        RepUnit.Meters => (20, 40),
        RepUnit.Steps => (40, 80),
        _ => null,
    };

    /// <summary>Neck strength work, reps or isometric holds, trained its own careful way (its stretches and mobility drills aren't).</summary>
    static bool IsNeckStrength(Exercise ex) => ex.PrimaryMuscle == MuscleGroup.Neck && !IsTimed(ex) && !IsControl(ex);

    /// <summary>Pilates and mobility drills: a few slow, precise reps, never to fatigue.</summary>
    static bool IsControl(Exercise ex) => ExerciseLibrary.Details(ex.Id)?.Category is ExerciseCategory.Pilates or ExerciseCategory.Mobility;

    /// <summary>
    /// Sets, reps, effort and rest for <paramref name="ex"/> under <paramref name="goal"/> (the main one) and the other
    /// <paramref name="priorities"/> with it; the rest times set in <paramref name="profile"/>, if any, replace the goal's.
    /// Strength (or power) together with muscle size or aesthetics trains like powerbuilding: the big lifts heavier, the
    /// isolation work for growth.
    /// </summary>
    public static PlanExercise Prescription(Goal goal, Experience experience, Exercise ex, UserProfile? profile = null, IReadOnlyList<Goal>? priorities = null)
    {
        goal = Goals.Storable(goal);
        var goals = priorities is { Count: > 0 } ? priorities : [goal];
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
        // Heavy and big together: the main lifts heavier, the isolation work for growth.
        if (Goals.Heavy(goals) && Goals.Hypertrophy(goals))
            (min, max) = compound ? (goal is Goal.Strength or Goal.Power ? (4, 6) : (6, 10)) : (10, 15);
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

        if (IsNeckStrength(ex))
        {
            // The neck responds to steady, higher-rep work and is never taken close to failure.
            (min, max) = IsIsometric(ex) ? (5, 8) : (12, 20);
            sets = experience == Experience.Beginner ? 2 : 3;
            rir = 3;
            rest = 60;
        }
        else if (IsTimed(ex))
        {
            // Seconds, not reps: a stretch or pose for 30–60, a plank-style hold for 20–45.
            var stretch = ExerciseLibrary.Details(ex.Id)?.Category is ExerciseCategory.Stretch or ExerciseCategory.Yoga;
            (min, max) = stretch ? (30, 60) : (20, 45);
            sets = stretch ? 2 : experience == Experience.Beginner ? 2 : 3;
            rir = 3;
            rest = stretch ? 15 : 60;
        }
        else if (IsCarry(ex))
        {
            // Walked until the grip or posture gives out, or for a set time or distance; heavy, so a full rest.
            (min, max) = UnitRange(DefaultUnit(ex))!.Value;
            sets = 3;
            rir = 2;
        }
        else if (IsControl(ex))
        {
            (min, max) = (6, 10);
            sets = 2;
            rir = 3;
            rest = 30;
        }

        if (RestOverride(profile, ex) is { } own && !IsTimed(ex) && !IsControl(ex))
            rest = own;

        return new PlanExercise
        {
            ExerciseId = ex.Id, Sets = sets, RepMin = min, RepMax = max, Unit = Stored(ex, DefaultUnit(ex)), TargetRir = rir, RestSeconds = rest,
        };
    }

    /// <summary>The rest the user set for exercises like <paramref name="ex"/>, or null to rest as the goal suggests.</summary>
    public static int? RestOverride(UserProfile? profile, Exercise ex) =>
        ex.Mechanic == Mechanic.Compound ? profile?.CompoundRestSeconds : profile?.IsolationRestSeconds;

    /// <summary>Roughly how long one rep takes, including the lowering; or one second, meter or step, as <paramref name="unit"/> counts.</summary>
    public static double SecondsPerRep(Goal goal, Exercise? ex, RepUnit? unit = null)
    {
        switch (UnitOf(ex, unit))
        {
            case RepUnit.Seconds:
                return ex != null && IsNeckStrength(ex) ? 6 : 1;
            case RepUnit.Meters:
                return 1; // A loaded walk or sled covers about a meter a second.
            case RepUnit.Steps:
                return 0.6;
        }
        if (ex != null && IsNeckStrength(ex))
            return IsIsometric(ex) ? 6 : 4;
        if (ex != null && IsTimed(ex))
            return 1;
        if (ex != null && IsControl(ex))
            return 5;
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

    /// <summary>
    /// Coaching cues for doing <paramref name="ex"/> the way <paramref name="goal"/> needs, most important first;
    /// <paramref name="unit"/> is what its sets count, as kept with it (see <see cref="UnitOf"/>).
    /// </summary>
    public static List<string> Tips(Goal goal, Exercise ex, int repMin, int repMax, int restSeconds, RepUnit? unit = null)
    {
        var tips = new List<string>();
        var compound = ex.Mechanic == Mechanic.Compound;
        var rest = Units.Rest(restSeconds);
        var counted = UnitOf(ex, unit);

        if (IsNeckStrength(ex))
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

        if (IsTimed(ex) && counted == RepUnit.Seconds)
        {
            tips.Add($"Count seconds, not reps: hold for {repMin}–{repMax} seconds and log the seconds.");
            tips.Add(ExerciseLibrary.Details(ex.Id)?.Category is ExerciseCategory.Stretch or ExerciseCategory.Yoga
                ? "Ease into it and breathe slowly. A stretch should feel strong but never painful."
                : "Stop the hold as soon as your position slips, rather than sagging through the last seconds.");
            return tips;
        }

        if (counted != RepUnit.Reps)
        {
            var range = counted.Range(repMin, repMax);
            tips.Add(counted switch
            {
                RepUnit.Seconds => $"Walk tall with short, quick steps for {range}, or until your grip gives out, and log the seconds.",
                RepUnit.Meters => $"Cover {range} a set with short, quick steps, turning and coming back as needed, and log the meters.",
                _ => $"Take {range} a set, short and quick, turning and coming back as needed, and log the steps.",
            });
            tips.Add("Brace your core and keep your shoulders back the whole way. Set the weight down with a flat back, never on a rounded one.");
            tips.Add($"Short of {repMin} means the weight is too heavy, so lower it. {repMax} or more with good posture means it's time to add weight.");
            return tips;
        }

        if (IsControl(ex))
        {
            tips.Add($"{repMin}–{repMax} slow, precise reps. Quality of movement matters more than the number.");
            tips.Add("Breathe steadily and keep your core engaged throughout; stop before your form fades.");
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
