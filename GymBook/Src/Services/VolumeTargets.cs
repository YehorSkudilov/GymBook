using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// How many hard sets a week a whole muscle wants, by the plan's goal and the lifter's experience. Sets count as in the
/// muscle breakdown: one an exercise's main muscle, half the muscles it only helps. Ranges follow the usual evidence
/// (about 10–20 a week for growth; less for strength, where the main lifts carry it, and for beginners, who grow on less).
/// </summary>
public static class VolumeTargets
{
    public readonly record struct Range(int Min, int Max)
    {
        public override string ToString() => $"{Min}–{Max}";
    }

    /// <summary>
    /// The weekly range for <paramref name="muscle"/>, or null for the ones the big lifts already train enough
    /// (forearms through every pull and carry, the lower back through squats and hinges) or that few people train (neck).
    /// </summary>
    /// <param name="priorities">All the plan's goals: with Aesthetics among them, the muscles that shape how a body looks
    /// (shoulders, back width, upper chest, arms) want a couple of sets more.</param>
    public static Range? For(MuscleGroup muscle, Goal goal, Experience experience, IReadOnlyList<Goal>? priorities = null)
    {
        if (muscle is MuscleGroup.Forearms or MuscleGroup.LowerBack or MuscleGroup.Neck)
            return null;
        goal = Goals.Storable(goal);
        var (min, max) = goal switch
        {
            Goal.BuildMuscle => (10, 20),
            Goal.LoseFat => (8, 16),
            Goal.Strength => (6, 12),
            Goal.Power => (4, 10),
            _ => (6, 12),
        };
        (min, max) = experience switch
        {
            Experience.Beginner => (min - 2, max - 4),
            Experience.Advanced => (min + 2, max + 2),
            _ => (min, max),
        };
        if (priorities?.Contains(Goal.Aesthetics) == true
            && muscle is MuscleGroup.Shoulders or MuscleGroup.Back or MuscleGroup.Chest or MuscleGroup.Biceps or MuscleGroup.Triceps)
            (min, max) = (min + 2, max + 2);
        // Traps, abs and calves get a lot from everything else, and grow on less direct work.
        if (muscle is MuscleGroup.Traps or MuscleGroup.Abs or MuscleGroup.Calves)
            (min, max) = (min * 2 / 3, max * 2 / 3);
        return new(Math.Max(2, min), Math.Max(4, max));
    }

    public enum Status { Under, OnTarget, Over }

    public static Status Of(double sets, Range range) =>
        sets < range.Min - 0.25 ? Status.Under : sets > range.Max + 0.25 ? Status.Over : Status.OnTarget;
}
