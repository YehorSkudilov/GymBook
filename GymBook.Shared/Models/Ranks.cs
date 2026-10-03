namespace GymBook.Models;

/// <summary>The lifts people are ranked on.</summary>
public enum RankLift { Bench, Squat, Deadlift, Ohp }

/// <summary>Strength tiers, weakest first. Each spans 100 points of <see cref="Ranks.Score"/>.</summary>
public enum RankTier { Bronze, Silver, Gold, Platinum, Diamond, Elite }

/// <summary>
/// How strength ranks are worked out, shared so the API (which ranks everyone) and the app (which explains it) agree.
/// A lift's score comes from its estimated one-rep max over body weight against standards for the lifter's sex: 0–99 is
/// Bronze, 100–199 Silver, and so on up to Elite at 500–600. The overall score averages all four lifts, an unranked
/// lift counting 0, so a rank can't be won by training only one lift.
/// </summary>
public static class Ranks
{
    public static readonly RankLift[] Lifts = [RankLift.Bench, RankLift.Squat, RankLift.Deadlift, RankLift.Ohp];

    /// <summary>The library exercises that count as each lift: the competition movement and its close variants.</summary>
    public static RankLift? LiftOf(string exerciseId) => exerciseId switch
    {
        "bench_press" or "paused_bench_press" => RankLift.Bench,
        "back_squat" or "low_bar_squat" or "pause_squat" => RankLift.Squat,
        "deadlift" or "sumo_deadlift" => RankLift.Deadlift,
        "overhead_press" => RankLift.Ohp,
        _ => null,
    };

    public static string Name(RankLift lift) => lift switch
    {
        RankLift.Bench => "Bench press",
        RankLift.Squat => "Squat",
        RankLift.Deadlift => "Deadlift",
        _ => "Overhead press",
    };

    /// <summary>Sets with more reps than this (reps in reserve included) say too little about a one-rep max to rank on.</summary>
    public const int MaxReps = 10;

    /// <summary>A lift needs this many workouts with it before it ranks, so one mistyped set can't.</summary>
    public const int MinSessions = 2;

    /// <summary>
    /// A ranked lift is at most this much over the best of the lift's other workouts: a jump past it waits for another
    /// workout to back it up.
    /// </summary>
    public const double MaxJump = 1.15;

    /// <summary>Estimated one-rep max (Epley), counting reps in reserve as reps you could have done.</summary>
    public static double E1Rm(double kg, int reps, int? rir)
    {
        var r = reps + (rir ?? 0);
        if (r <= 0 || kg <= 0)
            return 0;
        return r == 1 ? kg : kg * (1 + r / 30.0);
    }

    /// <summary>
    /// Past world-record strength for body weight: a set beyond it is a typo or a cheat and is ignored. Men's limits,
    /// which are above women's, so nobody real is excluded.
    /// </summary>
    public static double MaxRatio(RankLift lift) => lift switch
    {
        RankLift.Bench => 3.2,
        RankLift.Squat => 4.0,
        RankLift.Deadlift => 4.2,
        _ => 2.2,
    };

    /// <summary>The e1RM-to-body-weight ratios that start Silver, Gold, Platinum, Diamond and Elite.</summary>
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
    /// 0–600 for an e1RM-to-body-weight ratio: the tier times 100, plus how far through the tier. Elite tops out one
    /// tier-width past where it starts.
    /// </summary>
    public static double Score(RankLift lift, Sex sex, double ratio)
    {
        if (ratio <= 0)
            return 0;
        var t = Thresholds(lift, sex);
        if (ratio < t[0])
            return 100 * ratio / t[0];
        for (var i = 0; i < t.Length - 1; i++)
            if (ratio < t[i + 1])
                return 100 * (i + 1) + 100 * (ratio - t[i]) / (t[i + 1] - t[i]);
        var last = t.Length - 1;
        return Math.Min(600, 100 * (last + 1) + 100 * (ratio - t[last]) / (t[last] - t[last - 1]));
    }

    public static RankTier Tier(double score) => (RankTier)Math.Clamp((int)(score / 100), 0, (int)RankTier.Elite);

    /// <summary>The ratio a lift needs to reach <paramref name="tier"/>; 0 for Bronze.</summary>
    public static double RatioFor(RankLift lift, Sex sex, RankTier tier) => tier == RankTier.Bronze ? 0 : Thresholds(lift, sex)[(int)tier - 1];

    public static double Overall(IEnumerable<double> liftScores) => liftScores.Sum() / Lifts.Length;
}
