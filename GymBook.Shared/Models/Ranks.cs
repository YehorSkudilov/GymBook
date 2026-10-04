namespace GymBook.Models;

/// <summary>
/// What people are ranked on: strength in six muscle-group categories, each from the best of every weighted exercise for
/// it, and how consistently they train.
/// </summary>
public enum RankCategory { Chest, Back, Shoulders, Legs, Posterior, Arms, Consistency }

/// <summary>The four barbell lifts with their own, better-known strength standards (see <see cref="Ranks.ExerciseScore"/>).</summary>
public enum RankLift { Bench, Squat, Deadlift, Ohp }

/// <summary>Tiers, weakest first. Each spans 100 points of a score.</summary>
public enum RankTier { Bronze, Silver, Gold, Platinum, Diamond, Elite }

/// <summary>
/// How ranks are worked out, shared so the API (which ranks everyone) and the app (which explains it, and goes by the same
/// standards for starting weights) agree. A score is 0–600: 0–99 Bronze, 100–199 Silver, and so on up to Elite at
/// 500–600. A strength category's score is that of the user's best exercise for it: its estimated one-rep max over body
/// weight against a standard for the exercise and the lifter's gender. Consistency scores workouts a week. The overall
/// score averages every category, one not trained counting 0, so all-round strength and showing up both count.
/// </summary>
public static class Ranks
{
    public static readonly RankCategory[] Categories =
        [RankCategory.Chest, RankCategory.Back, RankCategory.Shoulders, RankCategory.Legs, RankCategory.Posterior, RankCategory.Arms, RankCategory.Consistency];

    /// <summary>The strength category an exercise counts toward, by its main muscle; null for those not ranked (abs, calves, neck).</summary>
    public static RankCategory? CategoryOf(MuscleGroup muscle) => muscle switch
    {
        MuscleGroup.Chest => RankCategory.Chest,
        MuscleGroup.Back or MuscleGroup.Traps => RankCategory.Back,
        MuscleGroup.Shoulders => RankCategory.Shoulders,
        MuscleGroup.Quads => RankCategory.Legs,
        MuscleGroup.Hamstrings or MuscleGroup.Glutes or MuscleGroup.LowerBack => RankCategory.Posterior,
        MuscleGroup.Biceps or MuscleGroup.Triceps or MuscleGroup.Forearms => RankCategory.Arms,
        _ => null,
    };

    public static string Name(RankCategory category) => category switch
    {
        RankCategory.Chest => "Chest",
        RankCategory.Back => "Back",
        RankCategory.Shoulders => "Shoulders",
        RankCategory.Legs => "Legs",
        RankCategory.Posterior => "Posterior chain",
        RankCategory.Arms => "Arms",
        _ => "Consistency",
    };

    /// <summary>The library exercises with a barbell lift's own standards: the competition movement and its close variants.</summary>
    public static RankLift? LiftOf(string exerciseId) => exerciseId switch
    {
        "bench_press" or "paused_bench_press" => RankLift.Bench,
        "back_squat" or "low_bar_squat" or "pause_squat" => RankLift.Squat,
        "deadlift" or "sumo_deadlift" => RankLift.Deadlift,
        "overhead_press" => RankLift.Ohp,
        _ => null,
    };

    /// <summary>Sets with more reps than this (reps in reserve included) say too little about a one-rep max to rank on.</summary>
    public const int MaxReps = 10;

    /// <summary>An exercise needs this many workouts with it before it ranks, so one mistyped set can't.</summary>
    public const int MinSessions = 2;

    /// <summary>
    /// A ranked lift is at most this much over the best of the exercise's other workouts: a jump past it waits for another
    /// workout to back it up.
    /// </summary>
    public const double MaxJump = 1.15;

    /// <summary>The weeks Consistency looks back over.</summary>
    public const int ConsistencyWeeks = 8;

    /// <summary>Estimated one-rep max (Epley), counting reps in reserve as reps you could have done.</summary>
    public static double E1Rm(double kg, int reps, int? rir)
    {
        var r = reps + (rir ?? 0);
        if (r <= 0 || kg <= 0)
            return 0;
        return r == 1 ? kg : kg * (1 + r / 30.0);
    }

    /// <summary>
    /// A typical intermediate lifter's estimated one-rep max for an exercise, as a share of body weight: roughly their
    /// 10-rep working weight (with 2 in reserve), by equipment, movement type and muscle, scaled for women (who lift less
    /// for their body weight on average, the upper body more so). Unknown gender: the men's. 0 for exercises without a
    /// standard (bodyweight and bands). The app's starting weights go by it too.
    /// </summary>
    public static double StandardRatio(Equipment equipment, Mechanic mechanic, MuscleGroup muscle, Sex? sex)
    {
        var factor = (equipment, mechanic) switch
        {
            (Equipment.Barbell, Mechanic.Compound) => 0.55,
            (Equipment.Barbell, _) => 0.3,
            (Equipment.EzBar, _) => 0.3,
            (Equipment.Dumbbell, Mechanic.Compound) => 0.16,
            (Equipment.Dumbbell, _) => 0.08,
            (Equipment.Machine, Mechanic.Compound) => 0.6,
            (Equipment.Machine, _) => 0.35,
            (Equipment.Cable, Mechanic.Compound) => 0.45,
            (Equipment.Cable, _) => 0.15,
            (Equipment.Kettlebell, _) => 0.16,
            (Equipment.Other, _) when muscle == MuscleGroup.Neck => 0.08,
            _ => 0,
        };
        var forMuscle = (muscle, mechanic) switch
        {
            (MuscleGroup.Quads or MuscleGroup.Hamstrings or MuscleGroup.Glutes or MuscleGroup.LowerBack, Mechanic.Compound) => 1.3,
            (MuscleGroup.Calves, _) => 1.4,
            (MuscleGroup.Shoulders, Mechanic.Compound) => 0.65,
            (MuscleGroup.Shoulders, _) => 0.6,
            (MuscleGroup.Biceps or MuscleGroup.Triceps or MuscleGroup.Forearms, _) => 0.85,
            (MuscleGroup.Neck, Mechanic.Isolation) when equipment == Equipment.Machine => 0.3,
            _ => 1.0,
        };
        var forSex = sex != Sex.Female ? 1
            : muscle switch
            {
                MuscleGroup.Quads or MuscleGroup.Hamstrings or MuscleGroup.Glutes or MuscleGroup.Calves or MuscleGroup.LowerBack => 0.78,
                MuscleGroup.Abs or MuscleGroup.Neck => 0.7,
                _ => 0.6,
            };
        return factor * forMuscle * (1 + 12 / 30.0) * forSex;
    }

    /// <summary>
    /// Where the tiers from Silver up start for an exercise without its own standards, as multiples of its
    /// <see cref="StandardRatio"/>: what the barbell lifts' own standards (below) work out to, so all exercises score alike.
    /// </summary>
    static readonly double[] StandardMultiples = [1.0, 1.3, 1.6, 2.0, 2.4];

    /// <summary>Past this many times an exercise's standard, a set is a typo or a cheat and is ignored.</summary>
    const double MaxMultiple = 3.2;

    /// <summary>
    /// Past world-record strength for body weight on the barbell lifts: a set beyond it is a typo or a cheat and is ignored.
    /// Men's limits, which are above women's, so nobody real is excluded.
    /// </summary>
    static double MaxRatio(RankLift lift) => lift switch
    {
        RankLift.Bench => 3.2,
        RankLift.Squat => 4.0,
        RankLift.Deadlift => 4.2,
        _ => 2.2,
    };

    /// <summary>The barbell lifts' e1RM-to-body-weight ratios that start Silver, Gold, Platinum, Diamond and Elite.</summary>
    static double[] Thresholds(RankLift lift, Sex sex) => (lift, sex) switch
    {
        (RankLift.Bench, Sex.Male) => [0.75, 1.0, 1.25, 1.5, 1.85],
        (RankLift.Squat, Sex.Male) => [1.0, 1.25, 1.6, 2.0, 2.5],
        (RankLift.Deadlift, Sex.Male) => [1.25, 1.5, 2.0, 2.4, 2.9],
        (RankLift.Ohp, Sex.Male) => [0.5, 0.65, 0.8, 1.0, 1.2],
        (RankLift.Bench, _) => [0.45, 0.6, 0.8, 1.0, 1.25],
        (RankLift.Squat, _) => [0.8, 1.0, 1.3, 1.6, 2.0],
        (RankLift.Deadlift, _) => [1.0, 1.25, 1.6, 2.0, 2.4],
        _ => [0.3, 0.4, 0.55, 0.7, 0.85],
    };

    /// <summary>
    /// The tier thresholds for an exercise (e1RM over body weight): a barbell lift's own, otherwise multiples of its
    /// standard; null when it has no standard and can't be ranked.
    /// </summary>
    static double[]? ExerciseThresholds(string exerciseId, Equipment equipment, Mechanic mechanic, MuscleGroup muscle, Sex sex)
    {
        if (LiftOf(exerciseId) is { } lift)
            return Thresholds(lift, sex);
        var standard = StandardRatio(equipment, mechanic, muscle, sex);
        return standard > 0 ? [.. StandardMultiples.Select(m => m * standard)] : null;
    }

    /// <summary>Whether an exercise can be ranked at all: weighted, with a standard, in a strength category.</summary>
    public static bool IsRankable(string exerciseId, Equipment equipment, Mechanic mechanic, MuscleGroup muscle) =>
        CategoryOf(muscle) != null && ExerciseThresholds(exerciseId, equipment, mechanic, muscle, Sex.Male) != null;

    /// <summary>The most an exercise's e1RM can be over body weight and still be believed (see <see cref="MaxRatio"/>).</summary>
    public static double MaxExerciseRatio(string exerciseId, Equipment equipment, Mechanic mechanic, MuscleGroup muscle) =>
        LiftOf(exerciseId) is { } lift ? MaxRatio(lift) : StandardRatio(equipment, mechanic, muscle, Sex.Male) * MaxMultiple;

    /// <summary>0–600 for an exercise's e1RM-to-body-weight ratio; 0 for one that can't be ranked.</summary>
    public static double ExerciseScore(string exerciseId, Equipment equipment, Mechanic mechanic, MuscleGroup muscle, Sex sex, double ratio) =>
        ExerciseThresholds(exerciseId, equipment, mechanic, muscle, sex) is { } t ? Score(t, ratio) : 0;

    /// <summary>Workouts a week that start Silver, Gold, Platinum, Diamond and Elite on Consistency.</summary>
    static readonly double[] ConsistencyThresholds = [1, 2, 3, 4, 5];

    /// <summary>0–600 for workouts a week (over the last <see cref="ConsistencyWeeks"/>).</summary>
    public static double ConsistencyScore(double workoutsPerWeek) => Score(ConsistencyThresholds, workoutsPerWeek);

    /// <summary>Days in a row with food logged that start Silver, Gold, Platinum, Diamond and Elite on the nutrition streak.</summary>
    public static readonly int[] NutritionStreakDays = [3, 7, 14, 30, 60];

    /// <summary>Days on target in the last 30 that start Silver, Gold, Platinum, Diamond and Elite.</summary>
    public static readonly int[] OnTargetDays = [5, 10, 15, 20, 25];

    /// <summary>0–600 for a food logging streak in days.</summary>
    public static double NutritionStreakScore(int days) => Score([.. NutritionStreakDays.Select(d => (double)d)], days);

    /// <summary>0–600 for days on target in the last 30 (all 30 is the top of Elite).</summary>
    public static double OnTargetScore(int daysLast30) => Score([.. OnTargetDays.Select(d => (double)d)], daysLast30);

    /// <summary>The nutrition rank: the streak and days on target, averaged, so logging and hitting the goals both count.</summary>
    public static double NutritionScore(int streak, int onTargetLast30) => (NutritionStreakScore(streak) + OnTargetScore(onTargetLast30)) / 2;

    /// <summary>
    /// The next tier up for a value against thresholds where Silver and up start (<see cref="NutritionStreakDays"/>,
    /// <see cref="OnTargetDays"/>), and where it starts; null at Elite.
    /// </summary>
    public static (RankTier Tier, int From)? NextTier(int[] thresholds, int value)
    {
        for (var i = 0; i < thresholds.Length; i++)
            if (value < thresholds[i])
                return ((RankTier)(i + 1), thresholds[i]);
        return null;
    }

    /// <summary>
    /// 0–600 for a value against tier thresholds (where Silver and up start): the tier times 100, plus how far through the
    /// tier. Elite tops out one tier-width past where it starts.
    /// </summary>
    static double Score(double[] t, double value)
    {
        if (value <= 0)
            return 0;
        if (value < t[0])
            return 100 * value / t[0];
        for (var i = 0; i < t.Length - 1; i++)
            if (value < t[i + 1])
                return 100 * (i + 1) + 100 * (value - t[i]) / (t[i + 1] - t[i]);
        var last = t.Length - 1;
        return Math.Min(600, 100 * (last + 1) + 100 * (value - t[last]) / (t[last] - t[last - 1]));
    }

    public static RankTier Tier(double score) => (RankTier)Math.Clamp((int)(score / 100), 0, (int)RankTier.Elite);

    /// <summary>The average over every category, one not ranked counting 0.</summary>
    public static double Overall(IEnumerable<double> categoryScores) => categoryScores.Sum() / Categories.Length;
}
