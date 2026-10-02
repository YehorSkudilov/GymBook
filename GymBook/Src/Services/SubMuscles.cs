using GymBook.Models;

namespace GymBook.Services;

/// <summary>A finer part of a <see cref="MuscleGroup"/> for the muscle breakdown: the delts' three heads, upper and lower chest, and so on.</summary>
public enum SubMuscle
{
    FrontDelts, SideDelts, RearDelts,
    UpperChest, MidLowerChest,
    Lats, UpperBack,
    Abs, Obliques,
    Biceps, Brachialis,
    ForearmFlexors, ForearmExtensors,
    TricepsLongHead, TricepsLateralHeads,
    // The groups that aren't split.
    Traps, LowerBack, Glutes, Quads, Hamstrings, Calves, Neck,
}

/// <summary>
/// Which part of a muscle group an exercise works. Exercises only record whole groups, so this goes by the kind of
/// movement: its name (lateral raises are side delts, face pulls rear delts, inclines upper chest) and, for a group it
/// only helps with, what the exercise mainly trains (presses help the front delts, rows and pulls the rear delts).
/// It works the same for custom exercises.
/// </summary>
public static class SubMuscles
{
    public static SubMuscle For(Exercise exercise, MuscleGroup group)
    {
        var name = exercise.Name.ToLowerInvariant();
        bool Has(params string[] words) => words.Any(name.Contains);
        var primary = group == exercise.PrimaryMuscle;

        return group switch
        {
            MuscleGroup.Shoulders => Has("rear", "reverse fly", "reverse pec", "face pull", "pull-apart", "external rotation", "y-t-w", "i-y-t", "back lever")
                    ? SubMuscle.RearDelts
                : Has("lateral", "lu raise", "y-raise", "upright row", "behind-the-neck", "cuban", "scaption", "bus driver")
                    ? SubMuscle.SideDelts
                : primary || exercise.PrimaryMuscle is MuscleGroup.Chest or MuscleGroup.Triceps
                    ? SubMuscle.FrontDelts
                : exercise.PrimaryMuscle is MuscleGroup.Back or MuscleGroup.Traps or MuscleGroup.Biceps or MuscleGroup.LowerBack
                  || Has("row", "pull", "chin", "fly")
                    ? SubMuscle.RearDelts
                : SubMuscle.FrontDelts,
            MuscleGroup.Chest => Has("incline", "low-to-high", "reverse-grip", "landmine", "guillotine", "pike", "overhead")
                ? SubMuscle.UpperChest : SubMuscle.MidLowerChest,
            MuscleGroup.Back => Has("pull-up", "pull up", "chin-up", "pulldown", "pullover", "lat ", "lats", "muscle-up", "front lever",
                    "straight-arm", "skin the cat", "rope climb", "hang", "push-through", "pulling straps")
                ? SubMuscle.Lats : SubMuscle.UpperBack,
            MuscleGroup.Abs => Has("oblique", "side", "twist", "russian", "woodchop", "pallof", "windshield", "rotation", "rotational",
                    "bicycle", "criss-cross", "suitcase", "windmill", "heel taps", "mermaid", "human flag", "corkscrew", "saw", "landmine")
                ? SubMuscle.Obliques : SubMuscle.Abs,
            MuscleGroup.Biceps => Has("hammer", "reverse curl", "reverse-grip curl", "zottman")
                ? SubMuscle.Brachialis : SubMuscle.Biceps,
            MuscleGroup.Forearms => Has("reverse", "extension", "extensor", "hammer", "zottman", "radial deviation", "wrist roller")
                ? SubMuscle.ForearmExtensors : SubMuscle.ForearmFlexors,
            MuscleGroup.Triceps => primary && Has("overhead", "skull", "lying", "jm press", "california", "tate", "bodyweight triceps", "pullover")
                ? SubMuscle.TricepsLongHead : SubMuscle.TricepsLateralHeads,
            MuscleGroup.Traps => SubMuscle.Traps,
            MuscleGroup.LowerBack => SubMuscle.LowerBack,
            MuscleGroup.Glutes => SubMuscle.Glutes,
            MuscleGroup.Quads => SubMuscle.Quads,
            MuscleGroup.Hamstrings => SubMuscle.Hamstrings,
            MuscleGroup.Calves => SubMuscle.Calves,
            _ => SubMuscle.Neck,
        };
    }

    public static MuscleGroup Group(this SubMuscle m) => m switch
    {
        SubMuscle.FrontDelts or SubMuscle.SideDelts or SubMuscle.RearDelts => MuscleGroup.Shoulders,
        SubMuscle.UpperChest or SubMuscle.MidLowerChest => MuscleGroup.Chest,
        SubMuscle.Lats or SubMuscle.UpperBack => MuscleGroup.Back,
        SubMuscle.Abs or SubMuscle.Obliques => MuscleGroup.Abs,
        SubMuscle.Biceps or SubMuscle.Brachialis => MuscleGroup.Biceps,
        SubMuscle.ForearmFlexors or SubMuscle.ForearmExtensors => MuscleGroup.Forearms,
        SubMuscle.TricepsLongHead or SubMuscle.TricepsLateralHeads => MuscleGroup.Triceps,
        SubMuscle.Traps => MuscleGroup.Traps,
        SubMuscle.LowerBack => MuscleGroup.LowerBack,
        SubMuscle.Glutes => MuscleGroup.Glutes,
        SubMuscle.Quads => MuscleGroup.Quads,
        SubMuscle.Hamstrings => MuscleGroup.Hamstrings,
        SubMuscle.Calves => MuscleGroup.Calves,
        _ => MuscleGroup.Neck,
    };

    public static string Display(this SubMuscle m) => m switch
    {
        SubMuscle.FrontDelts => "Front delts",
        SubMuscle.SideDelts => "Side delts",
        SubMuscle.RearDelts => "Rear delts",
        SubMuscle.UpperChest => "Upper chest",
        SubMuscle.MidLowerChest => "Mid & lower chest",
        SubMuscle.UpperBack => "Upper back",
        SubMuscle.Brachialis => "Brachialis",
        SubMuscle.ForearmFlexors => "Forearm flexors",
        SubMuscle.ForearmExtensors => "Forearm extensors",
        SubMuscle.TricepsLongHead => "Triceps long head",
        SubMuscle.TricepsLateralHeads => "Triceps lateral & medial heads",
        SubMuscle.LowerBack => "Lower back",
        _ => m.ToString(),
    };
}
