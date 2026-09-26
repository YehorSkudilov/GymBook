namespace GymBook.Models;

public enum MuscleGroup { Chest, Back, Traps, Shoulders, Biceps, Triceps, Forearms, Abs, LowerBack, Glutes, Quads, Hamstrings, Calves }

public enum Equipment { Barbell, Dumbbell, Machine, Cable, Bodyweight, Kettlebell, EzBar, Band }

public enum Mechanic { Compound, Isolation }

public enum Goal { BuildMuscle, Strength, LoseFat, GeneralFitness }

public enum Experience { Beginner, Intermediate, Advanced }

public enum EquipmentAccess { FullGym, HomeDumbbells, Bodyweight }

public enum WeightUnit { Kg, Lbs }

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
        Goal.BuildMuscle => "Build muscle",
        Goal.Strength => "Get stronger",
        Goal.LoseFat => "Lose fat",
        _ => "General fitness",
    };

    public static string Description(this Goal g) => g switch
    {
        Goal.BuildMuscle => "Hypertrophy-focused volume in moderate rep ranges",
        Goal.Strength => "Heavier compound lifts with lower reps and longer rest",
        Goal.LoseFat => "Higher reps and shorter rest to keep intensity up",
        _ => "A balanced mix of strength and muscle work",
    };

    public static string Display(this Experience e) => e.ToString();

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
