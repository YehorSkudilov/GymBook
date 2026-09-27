using System.Text.Json;
using System.Text.Json.Serialization;
using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

/// <summary>Built-in exercise catalogue. Ids are stable because plans and history reference them.</summary>
public static class ExerciseLibrary
{
    public static IReadOnlyList<Exercise> All { get; } =
    [
        // Chest
        E("bench_press", "Bench Press", Chest, Barbell, Compound, "Lie on a flat bench, grip slightly wider than shoulders. Lower the bar to mid-chest with elbows at ~45°, then press back up to lockout.", Triceps, Shoulders),
        E("incline_bench_press", "Incline Bench Press", Chest, Barbell, Compound, "Set the bench to 30–45°. Lower the bar to the upper chest under control and press up and slightly back.", Shoulders, Triceps),
        E("db_bench_press", "Dumbbell Bench Press", Chest, Dumbbell, Compound, "Press two dumbbells from chest level to above the shoulders. Let them travel slightly deeper than a barbell for a full stretch.", Triceps, Shoulders),
        E("incline_db_press", "Incline Dumbbell Press", Chest, Dumbbell, Compound, "On a 30–45° bench, press the dumbbells up over the upper chest, lowering until you feel a stretch.", Shoulders, Triceps),
        E("machine_chest_press", "Machine Chest Press", Chest, Machine, Compound, "Adjust the seat so the handles line up with mid-chest. Press forward without locking out hard, then return slowly.", Triceps, Shoulders),
        E("cable_fly", "Cable Fly", Chest, Cable, Isolation, "With cables set at shoulder height, bring the handles together in a wide hugging arc while keeping a soft bend in the elbows.", Shoulders),
        E("db_fly", "Dumbbell Fly", Chest, Dumbbell, Isolation, "On a flat bench, open the arms wide with slightly bent elbows until you feel a stretch, then squeeze the dumbbells back together.", Shoulders),
        E("pec_deck", "Pec Deck", Chest, Machine, Isolation, "Sit tall with your back against the pad and bring the arms together in front of your chest. Control the return.", Shoulders),
        E("push_up", "Push-Up", Chest, Bodyweight, Compound, "Hands under shoulders, body in a straight line. Lower your chest to the floor and push back up.", Triceps, Shoulders, Abs),
        E("dips", "Chest Dip", Chest, Bodyweight, Compound, "On parallel bars, lean the torso forward and lower until the shoulders are just below the elbows, then press up.", Triceps, Shoulders),

        // Back
        E("pull_up", "Pull-Up", Back, Bodyweight, Compound, "Hang from a bar with an overhand grip. Pull your chest toward the bar, driving the elbows down, then lower fully.", Biceps, Forearms),
        E("chin_up", "Chin-Up", Back, Bodyweight, Compound, "Hang with an underhand, shoulder-width grip and pull your chin over the bar.", Biceps),
        E("lat_pulldown", "Lat Pulldown", Back, Cable, Compound, "Grip the bar slightly wider than shoulders and pull it to the upper chest, leading with the elbows.", Biceps),
        E("barbell_row", "Barbell Row", Back, Barbell, Compound, "Hinge to about 45°, keep a neutral spine and row the bar to your lower ribs.", Biceps, LowerBack, Traps),
        E("db_row", "One-Arm Dumbbell Row", Back, Dumbbell, Compound, "Support yourself on a bench and row the dumbbell toward your hip, keeping the torso still.", Biceps),
        E("seated_cable_row", "Seated Cable Row", Back, Cable, Compound, "Sit upright, pull the handle to your stomach while squeezing the shoulder blades together.", Biceps, Traps),
        E("t_bar_row", "T-Bar Row", Back, Barbell, Compound, "Straddle the bar, hinge forward and row the handle to your chest.", Biceps, Traps, LowerBack),
        E("chest_supported_row", "Chest-Supported Row", Back, Machine, Compound, "Lie chest-down on the pad and row the handles back, squeezing the upper back.", Biceps, Traps),
        E("inverted_row", "Inverted Row", Back, Bodyweight, Compound, "Hang under a bar or table with a straight body and pull your chest up to it.", Biceps),
        E("straight_arm_pulldown", "Straight-Arm Pulldown", Back, Cable, Isolation, "With arms nearly straight, sweep the bar from eye level down to your thighs using your lats."),
        E("band_row", "Band Row", Back, Band, Compound, "Anchor a band at chest height and row the handles toward your ribs.", Biceps),
        E("deadlift", "Deadlift", LowerBack, Barbell, Compound, "Bar over mid-foot, hinge down with a flat back, brace and stand up by driving the floor away.", Glutes, Hamstrings, Back, Traps, Forearms),

        // Traps
        E("barbell_shrug", "Barbell Shrug", Traps, Barbell, Isolation, "Hold the bar at arm's length and shrug the shoulders straight up toward the ears.", Forearms),
        E("db_shrug", "Dumbbell Shrug", Traps, Dumbbell, Isolation, "Hold dumbbells at your sides and shrug straight up, pausing at the top.", Forearms),

        // Neck. The first five reuse catalogue ids so they keep its photos and steps.
        E("Lying_Face_Up_Plate_Neck_Resistance", "Plate Neck Flexion", Neck, Other, Isolation, "Lie on your back on a bench with your head and neck off the end. Hold a light plate on your forehead over a folded towel. Lower your head back slowly until you feel a gentle stretch, then tuck your chin and curl your head up toward your chest. Pause, and lower for 2–3 seconds."),
        E("Lying_Face_Down_Plate_Neck_Resistance", "Plate Neck Extension", Neck, Other, Isolation, "Lie face down on a bench with your head off the end. Hold a light plate on the back of your head over a towel. Lower your chin toward your chest, then lift your head until it's in line with your spine, no further. Keep it slow and smooth."),
        E("Seated_Head_Harness_Neck_Resistance", "Head Harness Neck Extension", Neck, Other, Isolation, "Sit on a bench, lean forward with your hands on your knees and wear a head harness with a light weight hanging from it. Let your chin drop toward your chest, then raise your head until it's level. Don't throw the head back.", Traps),
        E("Isometric_Neck_Exercise_-_Front_And_Back", "Isometric Neck Hold (Front & Back)", Neck, Bodyweight, Isolation, "Sit or stand tall. Put your palm on your forehead and push your head into it while your hand resists, so nothing moves. Hold 5 seconds, relax, repeat. Then do the same with your hands clasped behind your head."),
        E("Isometric_Neck_Exercise_-_Sides", "Isometric Neck Hold (Sides)", Neck, Bodyweight, Isolation, "Sit or stand tall with one palm against the side of your head. Push your head sideways into your hand while the hand resists, so your head stays still. Hold 5 seconds, relax, repeat, then switch sides."),
        E("plate_lateral_neck_flexion", "Side-Lying Plate Neck Raise", Neck, Other, Isolation, "Lie on your side on a bench with your head off the end and a light plate on the side of your head over a towel. Lower your ear toward the floor, then raise it toward your shoulder. Do all reps, then switch sides."),
        E("band_neck_flexion", "Band Neck Flexion", Neck, Band, Isolation, "Anchor a light band behind you at head height and loop it around your forehead, padded with a towel. Step forward until there's tension, then tuck your chin and bring your head forward against the band. Return slowly to neutral."),
        E("band_neck_extension", "Band Neck Extension", Neck, Band, Isolation, "Anchor a light band in front of you at head height and loop it around the back of your head. With your chin slightly tucked, press your head back until it's in line with your spine, then return slowly."),
        E("neck_machine", "4-Way Neck Machine", Neck, Machine, Isolation, "Adjust the seat so the pad sits on your forehead, the back or the side of your head. Move your head through a comfortable range against the pad, slowly in both directions. Train front, back and both sides."),

        // Shoulders
        E("overhead_press", "Overhead Press", Shoulders, Barbell, Compound, "Standing, press the bar from the front of your shoulders to overhead lockout, moving your head through at the top.", Triceps, Traps),
        E("db_shoulder_press", "Dumbbell Shoulder Press", Shoulders, Dumbbell, Compound, "Seated or standing, press the dumbbells from shoulder height to overhead.", Triceps),
        E("machine_shoulder_press", "Machine Shoulder Press", Shoulders, Machine, Compound, "Adjust the seat so the handles start at shoulder level and press overhead.", Triceps),
        E("arnold_press", "Arnold Press", Shoulders, Dumbbell, Compound, "Start with palms facing you and rotate them outward while pressing overhead.", Triceps),
        E("lateral_raise", "Lateral Raise", Shoulders, Dumbbell, Isolation, "Raise the dumbbells out to your sides until the arms are parallel to the floor, leading with the elbows."),
        E("cable_lateral_raise", "Cable Lateral Raise", Shoulders, Cable, Isolation, "Stand side-on to a low cable and raise the handle out to shoulder height."),
        E("band_lateral_raise", "Band Lateral Raise", Shoulders, Band, Isolation, "Stand on a band and raise the handles out to your sides."),
        E("face_pull", "Face Pull", Shoulders, Cable, Isolation, "With a rope at face height, pull toward your forehead and rotate your hands outward.", Traps, Back),
        E("reverse_fly", "Reverse Dumbbell Fly", Shoulders, Dumbbell, Isolation, "Hinge forward and raise the dumbbells out to the sides to work the rear delts.", Traps, Back),
        E("rear_delt_machine", "Rear Delt Machine", Shoulders, Machine, Isolation, "Face the pec deck and sweep the handles back with straight arms.", Traps),
        E("pike_push_up", "Pike Push-Up", Shoulders, Bodyweight, Compound, "Hips high in an inverted V, lower the top of your head toward the floor and press back up.", Triceps),

        // Biceps
        E("barbell_curl", "Barbell Curl", Biceps, Barbell, Isolation, "Stand tall and curl the bar up while keeping the elbows pinned to your sides.", Forearms),
        E("db_curl", "Dumbbell Curl", Biceps, Dumbbell, Isolation, "Curl the dumbbells up, turning the palms up as they rise.", Forearms),
        E("hammer_curl", "Hammer Curl", Biceps, Dumbbell, Isolation, "Keep a neutral (thumbs-up) grip and curl the dumbbells.", Forearms),
        E("ez_bar_curl", "EZ-Bar Curl", Biceps, EzBar, Isolation, "Hold the angled grips and curl the bar with the elbows still.", Forearms),
        E("cable_curl", "Cable Curl", Biceps, Cable, Isolation, "Face a low pulley and curl the bar toward your shoulders."),
        E("preacher_curl", "Preacher Curl", Biceps, Machine, Isolation, "With your upper arms on the pad, curl the handle up and lower it to nearly straight arms."),
        E("incline_db_curl", "Incline Dumbbell Curl", Biceps, Dumbbell, Isolation, "Lie back on an incline bench, let the arms hang and curl, stretching the biceps at the bottom."),
        E("band_curl", "Band Curl", Biceps, Band, Isolation, "Stand on a band and curl the handles up."),

        // Triceps
        E("triceps_pushdown", "Triceps Pushdown", Triceps, Cable, Isolation, "Keep the elbows at your sides and push the bar or rope down until the arms are straight."),
        E("overhead_triceps_extension", "Overhead Cable Extension", Triceps, Cable, Isolation, "Face away from the cable and extend the rope overhead from behind your head."),
        E("skull_crusher", "Skull Crusher", Triceps, EzBar, Isolation, "Lying on a bench, lower the bar toward your forehead by bending the elbows, then extend."),
        E("close_grip_bench", "Close-Grip Bench Press", Triceps, Barbell, Compound, "Bench press with hands about shoulder-width apart, elbows tucked.", Chest, Shoulders),
        E("db_overhead_extension", "Dumbbell Overhead Extension", Triceps, Dumbbell, Isolation, "Hold one dumbbell overhead with both hands and lower it behind your head."),
        E("bench_dip", "Bench Dip", Triceps, Bodyweight, Compound, "Hands on a bench behind you, lower your hips by bending the elbows and press up.", Chest, Shoulders),
        E("diamond_push_up", "Diamond Push-Up", Triceps, Bodyweight, Compound, "Push-up with your hands close together forming a diamond under your chest.", Chest),
        E("band_pushdown", "Band Pushdown", Triceps, Band, Isolation, "Anchor a band high and push down until your arms are straight."),

        // Forearms
        E("wrist_curl", "Wrist Curl", Forearms, Dumbbell, Isolation, "Rest your forearms on your thighs and curl the dumbbells with the wrists only."),
        E("reverse_curl", "Reverse Curl", Forearms, EzBar, Isolation, "Curl the bar with an overhand grip.", Biceps),

        // Abs & lower back
        E("crunch", "Crunch", Abs, Bodyweight, Isolation, "Lie on your back and curl your shoulders off the floor by contracting your abs."),
        E("hanging_leg_raise", "Hanging Leg Raise", Abs, Bodyweight, Isolation, "Hang from a bar and raise your legs by curling the pelvis up.", Forearms),
        E("cable_crunch", "Cable Crunch", Abs, Cable, Isolation, "Kneel below a rope and crunch down, bringing your elbows toward your knees."),
        E("leg_raise", "Lying Leg Raise", Abs, Bodyweight, Isolation, "Lie flat and raise straight legs to vertical while keeping your lower back down."),
        E("ab_wheel", "Ab Wheel Rollout", Abs, Bodyweight, Isolation, "From your knees, roll the wheel forward as far as you can control and pull back.", LowerBack),
        E("back_extension", "Back Extension", LowerBack, Bodyweight, Isolation, "On a hyperextension bench, lower your torso and raise it until in line with your legs.", Glutes, Hamstrings),
        E("good_morning", "Good Morning", LowerBack, Barbell, Compound, "Bar on your back, hinge at the hips with soft knees until your torso is near parallel.", Hamstrings, Glutes),

        // Glutes
        E("hip_thrust", "Barbell Hip Thrust", Glutes, Barbell, Compound, "Upper back on a bench, drive the bar up with your hips until your body forms a straight line.", Hamstrings),
        E("db_hip_thrust", "Dumbbell Hip Thrust", Glutes, Dumbbell, Compound, "Hip thrust with a dumbbell resting on your hips.", Hamstrings),
        E("glute_bridge", "Glute Bridge", Glutes, Bodyweight, Isolation, "Lie on your back, feet flat, and drive your hips up by squeezing your glutes.", Hamstrings),
        E("cable_kickback", "Cable Glute Kickback", Glutes, Cable, Isolation, "Attach an ankle strap and kick the leg back while keeping the torso still."),
        E("kb_swing", "Kettlebell Swing", Glutes, Kettlebell, Compound, "Hinge and snap the hips forward to swing the kettlebell to chest height.", Hamstrings, LowerBack),

        // Quads
        E("back_squat", "Back Squat", Quads, Barbell, Compound, "Bar on your upper back, sit down between your hips until your thighs are at least parallel, then drive up.", Glutes, Hamstrings, LowerBack),
        E("front_squat", "Front Squat", Quads, Barbell, Compound, "Bar racked on the front of your shoulders, squat with an upright torso.", Glutes, Abs),
        E("leg_press", "Leg Press", Quads, Machine, Compound, "Feet shoulder-width on the platform, lower until your knees reach about 90° and press back.", Glutes),
        E("hack_squat", "Hack Squat", Quads, Machine, Compound, "With shoulders under the pads, squat down deep and drive up through your mid-foot.", Glutes),
        E("leg_extension", "Leg Extension", Quads, Machine, Isolation, "Extend your knees to straighten the legs and lower slowly."),
        E("goblet_squat", "Goblet Squat", Quads, Dumbbell, Compound, "Hold a dumbbell at your chest and squat between your knees.", Glutes),
        E("db_lunge", "Dumbbell Lunge", Quads, Dumbbell, Compound, "Step forward and lower your back knee toward the floor, then push back up.", Glutes, Hamstrings),
        E("bulgarian_split_squat", "Bulgarian Split Squat", Quads, Dumbbell, Compound, "Rear foot on a bench, lower into a deep single-leg squat.", Glutes),
        E("step_up", "Dumbbell Step-Up", Quads, Dumbbell, Compound, "Step up onto a box, driving through the front heel.", Glutes),
        E("bw_squat", "Bodyweight Squat", Quads, Bodyweight, Compound, "Squat down as deep as your mobility allows with your chest up.", Glutes),
        E("walking_lunge", "Walking Lunge", Quads, Bodyweight, Compound, "Lunge forward alternating legs with each step.", Glutes, Hamstrings),

        // Hamstrings
        E("romanian_deadlift", "Romanian Deadlift", Hamstrings, Barbell, Compound, "Soft knees, push your hips back and lower the bar along your legs until you feel a deep hamstring stretch.", Glutes, LowerBack),
        E("db_romanian_deadlift", "Dumbbell Romanian Deadlift", Hamstrings, Dumbbell, Compound, "Hinge with dumbbells in front of your thighs until you feel a strong stretch.", Glutes, LowerBack),
        E("lying_leg_curl", "Lying Leg Curl", Hamstrings, Machine, Isolation, "Lie face down and curl the pad toward your glutes."),
        E("seated_leg_curl", "Seated Leg Curl", Hamstrings, Machine, Isolation, "Seated with the pad above your ankles, curl your legs down and back."),
        E("nordic_curl", "Nordic Curl", Hamstrings, Bodyweight, Isolation, "Kneel with your ankles anchored and lower your torso forward as slowly as possible."),
        E("single_leg_rdl", "Single-Leg RDL", Hamstrings, Bodyweight, Compound, "Balance on one leg and hinge forward, extending the other leg behind you.", Glutes),

        // Calves
        E("standing_calf_raise", "Standing Calf Raise", Calves, Machine, Isolation, "Rise onto the balls of your feet as high as possible, pause and lower into a deep stretch."),
        E("seated_calf_raise", "Seated Calf Raise", Calves, Machine, Isolation, "Seated with the pad on your knees, raise your heels as high as possible."),
        E("db_calf_raise", "Dumbbell Calf Raise", Calves, Dumbbell, Isolation, "Holding dumbbells, perform calf raises off a step."),
        E("bw_calf_raise", "Single-Leg Calf Raise", Calves, Bodyweight, Isolation, "On one leg on a step, rise up as high as possible and lower slowly."),
    ];

    static readonly Dictionary<string, Exercise> ById;
    static readonly Dictionary<string, ExerciseDetails> DetailsById;

    static ExerciseLibrary()
    {
        // ExerciseCatalog.json is generated by tools/import-exercises.mjs from free-exercise-db (public domain).
        // It adds images and step-by-step instructions to the curated list above, plus hundreds more exercises.
        using var stream = typeof(ExerciseLibrary).Assembly.GetManifestResourceStream("ExerciseCatalog.json")!;
        var catalog = JsonSerializer.Deserialize<Catalog>(stream, CatalogJson)!;

        var curated = All.ToDictionary(e => e.Id);
        var imported = new List<Exercise>();
        DetailsById = [];
        foreach (var entry in catalog.Exercises)
        {
            DetailsById[entry.Id] = new ExerciseDetails(entry.Level, entry.Force, entry.Category, entry.Steps,
                [.. entry.Images.Select(path => catalog.ImageBase + path)]);
            if (curated.ContainsKey(entry.Id))
                continue;
            imported.Add(new Exercise
            {
                Id = entry.Id,
                Name = entry.Name!,
                PrimaryMuscle = entry.Primary!.Value,
                SecondaryMuscles = entry.Secondary ?? [],
                Equipment = entry.Equipment!.Value,
                Mechanic = entry.Mechanic!.Value,
                Instructions = string.Join(" ", entry.Steps),
            });
        }

        All = [.. All, .. imported];
        ById = All.ToDictionary(e => e.Id);
    }

    public static Exercise? Find(string id) => ById.GetValueOrDefault(id);

    /// <summary>Images and extra information for a built-in exercise, when the catalogue has them.</summary>
    public static ExerciseDetails? Details(string id) => DetailsById.GetValueOrDefault(id);

    static readonly JsonSerializerOptions CatalogJson = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    record Catalog(string ImageBase, List<CatalogEntry> Exercises);

    record CatalogEntry(
        string Id, string? Name, MuscleGroup? Primary, List<MuscleGroup>? Secondary, Equipment? Equipment, Mechanic? Mechanic,
        string? Level, string? Force, string? Category, List<string> Steps, List<string> Images);

    static Exercise E(string id, string name, MuscleGroup primary, Equipment equipment, Mechanic mechanic, string instructions, params MuscleGroup[] secondary) => new()
    {
        Id = id,
        Name = name,
        PrimaryMuscle = primary,
        Equipment = equipment,
        Mechanic = mechanic,
        Instructions = instructions,
        SecondaryMuscles = [.. secondary],
    };
}
