namespace GymBook.Models;

// New values go at the end: they are stored as numbers.
public enum MuscleGroup { Chest, Back, Traps, Shoulders, Biceps, Triceps, Forearms, Abs, LowerBack, Glutes, Quads, Hamstrings, Calves, Neck }

public enum Equipment { Barbell, Dumbbell, Machine, Cable, Bodyweight, Kettlebell, EzBar, Band, Other }

public enum Mechanic { Compound, Isolation }

public enum Goal { BuildMuscle, Strength, LoseFat, GeneralFitness, Power }

public enum Experience { Beginner, Intermediate, Advanced }

public enum EquipmentAccess { FullGym, HomeDumbbells, Bodyweight }

public enum WeightUnit { Kg, Lbs }

public enum MealType { Breakfast, Lunch, Dinner, Snack }

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
        Goal.BuildMuscle => "Bodybuilding",
        Goal.Strength => "Strength",
        Goal.LoseFat => "Lose fat",
        Goal.Power => "Combat & power sports",
        _ => "General fitness",
    };

    public static string Description(this Goal g) => g switch
    {
        Goal.BuildMuscle => "Grow muscle: at least 10 reps on compounds and 12 on isolation work",
        Goal.Strength => "Heavy compounds for 3–6 fast, clean reps, plus muscle endurance work",
        Goal.LoseFat => "Higher reps and shorter rest to keep intensity up",
        Goal.Power => "Boxing, jiu-jitsu, MMA, wrestling: heavy lifts for 6 reps or fewer and explosive, fast reps",
        _ => "A balanced mix of strength and muscle work",
    };

    public static string Display(this Experience e) => e.ToString();

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
