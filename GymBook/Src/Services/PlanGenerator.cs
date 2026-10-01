using GymBook.Models;

namespace GymBook.Services;

/// <summary>Builds a training plan from the user's goal, experience, schedule and equipment.</summary>
public static class PlanGenerator
{
    // Movement slots, each with candidate exercises in order of preference.
    static readonly string[] Squat = ["back_squat", "leg_press", "hack_squat", "front_squat", "goblet_squat", "bulgarian_split_squat", "bw_squat"];
    static readonly string[] Hinge = ["romanian_deadlift", "deadlift", "db_romanian_deadlift", "single_leg_rdl", "kb_swing", "glute_bridge"];
    static readonly string[] HorizontalPush = ["bench_press", "db_bench_press", "machine_chest_press", "push_up"];
    static readonly string[] InclinePush = ["incline_db_press", "incline_bench_press", "machine_chest_press", "dips", "push_up"];
    static readonly string[] VerticalPush = ["overhead_press", "db_shoulder_press", "machine_shoulder_press", "arnold_press", "pike_push_up"];
    static readonly string[] HorizontalPull = ["barbell_row", "seated_cable_row", "db_row", "chest_supported_row", "t_bar_row", "inverted_row", "band_row"];
    static readonly string[] VerticalPull = ["lat_pulldown", "pull_up", "chin_up", "straight_arm_pulldown"];
    static readonly string[] ChestIso = ["cable_fly", "pec_deck", "db_fly", "dips", "push_up"];
    static readonly string[] SideDelt = ["lateral_raise", "cable_lateral_raise", "band_lateral_raise", "pike_push_up"];
    static readonly string[] RearDelt = ["face_pull", "reverse_fly", "rear_delt_machine", "inverted_row"];
    static readonly string[] Biceps = ["db_curl", "barbell_curl", "cable_curl", "ez_bar_curl", "band_curl", "chin_up"];
    static readonly string[] Biceps2 = ["hammer_curl", "incline_db_curl", "preacher_curl", "band_curl", "chin_up"];
    static readonly string[] Triceps = ["triceps_pushdown", "skull_crusher", "db_overhead_extension", "band_pushdown", "diamond_push_up"];
    static readonly string[] Triceps2 = ["overhead_triceps_extension", "close_grip_bench", "db_overhead_extension", "bench_dip", "diamond_push_up"];
    static readonly string[] QuadIso = ["leg_extension", "bulgarian_split_squat", "db_lunge", "step_up", "walking_lunge"];
    static readonly string[] HamCurl = ["lying_leg_curl", "seated_leg_curl", "nordic_curl", "single_leg_rdl"];
    static readonly string[] Glute = ["hip_thrust", "db_hip_thrust", "cable_kickback", "glute_bridge"];
    static readonly string[] Calves = ["standing_calf_raise", "seated_calf_raise", "db_calf_raise", "bw_calf_raise"];
    static readonly string[] Abs = ["cable_crunch", "hanging_leg_raise", "ab_wheel", "leg_raise", "crunch"];
    static readonly string[] Traps = ["barbell_shrug", "db_shrug"];
    // Neck: one direction per day, rotating through flexion, extension and side work.
    static readonly string[] NeckFlexion = ["plate_neck_flexion", "neck_machine", "band_neck_flexion", "neck_isometric_front_back"];
    static readonly string[] NeckExtension = ["plate_neck_extension", "harness_neck_extension", "band_neck_extension", "neck_isometric_front_back"];
    static readonly string[] NeckSide = ["plate_lateral_neck_flexion", "neck_machine", "neck_isometric_sides"];
    static readonly string[][] NeckSlots = [NeckFlexion, NeckExtension, NeckSide];
    // Explosive work for combat and power sports, done first while fresh.
    static readonly string[] ExplosiveLower = ["box_jump", "broad_jump", "kb_swing", "tuck_jump"];
    static readonly string[] ExplosiveUpper = ["med_ball_chest_pass", "plyo_push_up", "med_ball_slam"];
    static readonly string[] PowerLift = ["power_clean", "hang_clean", "push_press", "kb_swing"];

    static readonly string[][] FullBody = [Squat, HorizontalPush, HorizontalPull, Hinge, VerticalPush, VerticalPull, SideDelt, Biceps, Triceps, Calves, Abs];
    static readonly string[][] Upper = [HorizontalPush, HorizontalPull, VerticalPush, VerticalPull, InclinePush, SideDelt, Biceps, Triceps, RearDelt];
    static readonly string[][] Lower = [Squat, Hinge, QuadIso, HamCurl, Glute, Calves, Abs];
    static readonly string[][] Push = [HorizontalPush, VerticalPush, InclinePush, SideDelt, Triceps, ChestIso, Triceps2];
    static readonly string[][] Pull = [VerticalPull, HorizontalPull, RearDelt, Biceps, HorizontalPull, Biceps2, Traps];
    static readonly string[][] Legs = [Squat, Hinge, QuadIso, HamCurl, Calves, Glute, Abs];

    public static WorkoutPlan Generate(UserProfile profile)
    {
        var (splitName, days) = Split(profile);
        var plan = new WorkoutPlan
        {
            Name = $"{splitName} · {profile.DaysPerWeek}x/week",
            Description = $"{profile.Goal.Display()} · {profile.Experience.Display()} · {profile.EquipmentAccess.Display()}",
            Goal = profile.Goal,
            DaysPerWeek = profile.DaysPerWeek,
        };

        var count = profile.SessionMinutes switch { <= 30 => 4, <= 45 => 5, <= 60 => 6, _ => 8 };
        var variants = new Dictionary<string, int>();
        var neckDay = 0;
        foreach (var (name, template) in days)
        {
            var baseName = name.Split(' ')[0];
            var variant = variants.GetValueOrDefault(baseName);
            variants[baseName] = variant + 1;
            var workout = BuildWorkout(name, WithPower(profile.Goal, template), variant, count, profile);
            if (profile.TrainNeck)
                AddNeck(workout, template, profile, ref neckDay);
            plan.Workouts.Add(workout);
        }
        plan.RestDays = [.. PlanSchedule.DefaultRestDays(plan.Workouts.Count).Order()];
        return plan;
    }

    static (string, List<(string, string[][])>) Split(UserProfile p) => p.DaysPerWeek switch
    {
        <= 2 => ("Full Body", [("Full Body A", FullBody), ("Full Body B", FullBody)]),
        3 when p.Experience == Experience.Advanced => ("Push Pull Legs", [("Push", Push), ("Pull", Pull), ("Legs", Legs)]),
        3 => ("Full Body", [("Full Body A", FullBody), ("Full Body B", FullBody), ("Full Body C", FullBody)]),
        4 => ("Upper / Lower", [("Upper A", Upper), ("Lower A", Lower), ("Upper B", Upper), ("Lower B", Lower)]),
        5 => ("Upper / Lower / PPL", [("Upper", Upper), ("Lower", Lower), ("Push", Push), ("Pull", Pull), ("Legs", Legs)]),
        _ => ("Push Pull Legs", [("Push A", Push), ("Pull A", Pull), ("Legs A", Legs), ("Push B", Push), ("Pull B", Pull), ("Legs B", Legs)]),
    };

    /// <summary>Combat and power plans open each day with explosive work: jumps on leg days, throws on upper days, a power lift on full-body days.</summary>
    static string[][] WithPower(Goal goal, string[][] template)
    {
        if (goal != Goal.Power)
            return template;
        string[] first = template == Lower || template == Legs ? ExplosiveLower : template == FullBody ? PowerLift : ExplosiveUpper;
        return [first, .. template];
    }

    /// <summary>
    /// Neck work on top of the day's exercises, so it's never squeezed out by the session length: one exercise on
    /// upper-body and full-body days, and two on every day for combat and power sports.
    /// </summary>
    static void AddNeck(PlanWorkout workout, string[][] template, UserProfile profile, ref int day)
    {
        var count = profile.Goal == Goal.Power ? 2 : template == Lower || template == Legs ? 0 : 1;
        for (var i = 0; i < count; i++)
        {
            var slot = NeckSlots[day++ % NeckSlots.Length];
            var ex = slot.Select(ExerciseLibrary.Find).OfType<Exercise>()
                .FirstOrDefault(e => profile.EquipmentAccess.Allows(e.Equipment) && workout.Exercises.All(x => x.ExerciseId != e.Id));
            if (ex != null)
                workout.Exercises.Add(Prescription(profile, ex));
        }
    }

    static PlanWorkout BuildWorkout(string name, string[][] template, int variant, int count, UserProfile profile)
    {
        var workout = new PlanWorkout { Name = name };
        var used = new HashSet<string>();
        foreach (var slot in template)
        {
            if (workout.Exercises.Count >= count)
                break;
            var available = slot.Select(ExerciseLibrary.Find).OfType<Exercise>()
                .Where(e => profile.EquipmentAccess.Allows(e.Equipment) && !used.Contains(e.Id)).ToList();
            if (available.Count == 0)
                continue;
            var ex = available[variant % available.Count];
            used.Add(ex.Id);
            workout.Exercises.Add(Prescription(profile, ex));
        }
        return workout;
    }

    /// <summary>Sets, rep range, RIR and rest for an exercise given the user's goal and experience.</summary>
    public static PlanExercise Prescription(UserProfile profile, Exercise ex) => TrainingGoals.Prescription(profile.Goal, profile.Experience, ex, profile);
}
