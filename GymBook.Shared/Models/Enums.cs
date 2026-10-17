namespace GymBook.Models;

// New values go at the end: they are stored as numbers.
public enum MuscleGroup { Chest, Back, Traps, Shoulders, Biceps, Triceps, Forearms, Abs, LowerBack, Glutes, Quads, Hamstrings, Calves, Neck }

public enum Equipment { Barbell, Dumbbell, Machine, Cable, Bodyweight, Kettlebell, EzBar, Band, Other }

public enum Mechanic { Compound, Isolation }

/// <summary>
/// What training is for. Several can be prioritised together (see <see cref="Goals"/>): the plan's and profile's
/// <c>Goal</c> is the main one as older apps know it (never <see cref="Aesthetics"/>, which they can't read), and
/// <c>Priorities</c> all of them in order.
/// </summary>
public enum Goal { BuildMuscle, Strength, LoseFat, GeneralFitness, Power, Aesthetics }

/// <summary>Several goals prioritised together, the first the main one.</summary>
public static class Goals
{
    /// <summary>The goals a plan or profile prioritises, in order: its priorities, or just its goal when it has none.</summary>
    public static IReadOnlyList<Goal> Of(Goal goal, IReadOnlyList<Goal>? priorities) => priorities is { Count: > 0 } p ? p : [goal];

    /// <summary>
    /// The main goal as kept in <c>Goal</c>, which older apps read: Aesthetics trains like muscle size (it's muscle size
    /// spread for proportion), so it's kept as that.
    /// </summary>
    public static Goal Storable(Goal goal) => goal == Goal.Aesthetics ? Goal.BuildMuscle : goal;

    /// <summary>"Strength + Muscle size".</summary>
    public static string Display(IReadOnlyList<Goal> goals) => string.Join(" + ", goals.Select(g => g.Display()));

    /// <summary>Building muscle, for size or for looks.</summary>
    public static bool Hypertrophy(IReadOnlyList<Goal> goals) => goals.Any(g => g is Goal.BuildMuscle or Goal.Aesthetics);

    /// <summary>Lifting heavy, for strength or power.</summary>
    public static bool Heavy(IReadOnlyList<Goal> goals) => goals.Any(g => g is Goal.Strength or Goal.Power);
}

public enum Experience { Beginner, Intermediate, Advanced }

public enum EquipmentAccess { FullGym, HomeDumbbells, Bodyweight }

public enum WeightUnit { Kg, Lbs }

/// <summary>
/// What a set's count is: repetitions, or for holds and carries the seconds held, meters walked or steps taken. Kept in
/// the rep fields (SetEntry.Reps, RepMin, RepMax), so only the label and the maths that need real reps change.
/// </summary>
public enum RepUnit { Reps, Seconds, Meters, Steps }

/// <summary>
/// A meal, as Samsung Health splits the day. <see cref="MealType.Snack"/> is the afternoon snack: it was the only snack once, and
/// meals are stored by name, so it keeps its name; the morning and evening ones came later.
/// </summary>
public enum MealType { Breakfast, Lunch, Dinner, Snack, MorningSnack, EveningSnack }

public enum Sex { Male, Female }

/// <summary>Daily activity outside workouts, for estimating calories burned without health data.</summary>
public enum ActivityLevel { Sedentary, Light, Moderate, VeryActive }

/// <summary>Where health data comes from: Samsung Health itself (its Data SDK), or any app through Android's Health Connect.</summary>
public enum HealthSource { None, SamsungHealth, HealthConnect }

public static class EnumDisplay
{
    public static string Display(this MuscleGroup m) => m switch
    {
        MuscleGroup.LowerBack => "Lower back",
        _ => m.ToString(),
    };

    public static string Display(this Equipment e) => e switch
    {
        Equipment.EzBar => "EZ bar",
        _ => e.ToString(),
    };

    public static string Display(this Goal g) => g switch
    {
        Goal.BuildMuscle => "Muscle size",
        Goal.Aesthetics => "Aesthetics",
        Goal.Strength => "Strength",
        Goal.LoseFat => "Lose fat",
        Goal.Power => "Combat & power sports",
        _ => "General fitness",
    };

    public static string Description(this Goal g) => g switch
    {
        Goal.BuildMuscle => "Bodybuilding: as much muscle as you can, at least 10 reps on compounds and 12 on isolation work",
        Goal.Aesthetics => "Proportion and definition: extra work where it shows (shoulders, back width, arms, upper chest)",
        Goal.Strength => "Heavy compounds for 3–6 fast, clean reps, plus muscle endurance work",
        Goal.LoseFat => "Higher reps and shorter rest to keep intensity up",
        Goal.Power => "Boxing, jiu-jitsu, MMA, wrestling: heavy lifts for 6 reps or fewer and explosive, fast reps",
        _ => "A balanced mix of strength and muscle work",
    };

    public static string Display(this Experience e) => e.ToString();

    /// <summary>The unit after a count: "12 reps", "45 sec", "30 m", "40 steps".</summary>
    public static string Display(this RepUnit u) => u switch
    {
        RepUnit.Seconds => "sec",
        RepUnit.Meters => "m",
        RepUnit.Steps => "steps",
        _ => "reps",
    };

    /// <summary>The unit's name in a menu: "Seconds".</summary>
    public static string Name(this RepUnit u) => u switch
    {
        RepUnit.Seconds => "Seconds",
        RepUnit.Meters => "Meters",
        RepUnit.Steps => "Steps",
        _ => "Reps",
    };

    /// <summary>A count with its unit: "1 rep", "12 reps", "45 sec".</summary>
    public static string Count(this RepUnit u, int n) => u == RepUnit.Reps && n == 1 ? "1 rep" : $"{n} {u.Display()}";

    /// <summary>A range with its unit: "8–12 reps", "30–60 sec", or one count when both ends are the same.</summary>
    public static string Range(this RepUnit u, int min, int max) => min == max ? u.Count(min) : $"{min}–{max} {u.Display()}";

    public static string Display(this ActivityLevel a) => a switch
    {
        ActivityLevel.Sedentary => "Sedentary",
        ActivityLevel.Light => "Lightly active",
        ActivityLevel.Moderate => "Moderately active",
        _ => "Very active",
    };

    public static string Description(this ActivityLevel a) => a switch
    {
        ActivityLevel.Sedentary => "Desk job, little walking",
        ActivityLevel.Light => "Some walking, on your feet now and then",
        ActivityLevel.Moderate => "On your feet most of the day",
        _ => "Physical job or lots of daily activity",
    };

    /// <summary>The meals in the order they're listed: the three meals, then the three snacks (as Samsung Health lists them).</summary>
    public static readonly MealType[] Meals =
        [MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.MorningSnack, MealType.Snack, MealType.EveningSnack];

    public static string Display(this MealType m) => m switch
    {
        MealType.MorningSnack => "Morning snack",
        MealType.Snack => "Afternoon snack",
        MealType.EveningSnack => "Evening snack",
        _ => m.ToString(),
    };

    /// <summary>A snack by when it was eaten: before noon the morning one, before 5 p.m. the afternoon one, else the evening one.</summary>
    public static MealType SnackAt(DateTime time) => time.Hour switch
    {
        >= 4 and < 12 => MealType.MorningSnack,
        >= 12 and < 17 => MealType.Snack,
        _ => MealType.EveningSnack,
    };

    public static string Display(this HealthSource s) => s switch
    {
        HealthSource.SamsungHealth => "Samsung Health",
        HealthSource.HealthConnect => "Health Connect",
        _ => "Off",
    };

    public static string Description(this Experience e) => e switch
    {
        Experience.Beginner => "Less than 1 year of consistent training",
        Experience.Intermediate => "1–3 years of consistent training",
        _ => "More than 3 years of structured training",
    };

    public static string Display(this EquipmentAccess e) => e switch
    {
        EquipmentAccess.FullGym => "Commercial gym",
        EquipmentAccess.HomeDumbbells => "Home gym with dumbbells",
        _ => "Bodyweight only",
    };

    public static string Description(this EquipmentAccess e) => e switch
    {
        EquipmentAccess.FullGym => "Barbells, dumbbells, cables and machines",
        EquipmentAccess.HomeDumbbells => "Dumbbells, bands, a bench and a pull-up bar",
        _ => "No equipment except a pull-up bar",
    };

    public static bool Allows(this EquipmentAccess access, Equipment e) => access switch
    {
        EquipmentAccess.FullGym => true,
        EquipmentAccess.HomeDumbbells => e is Equipment.Dumbbell or Equipment.Bodyweight or Equipment.Band or Equipment.Kettlebell,
        _ => e is Equipment.Bodyweight or Equipment.Band,
    };
}

/// <summary>
/// Someone's part in a shared plan: the owner made it; an editor's changes reach everyone in it; a viewer follows it but
/// can't change its workouts.
/// </summary>
public enum PlanShareRole { Owner, Editor, Viewer }
