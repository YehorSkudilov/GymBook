using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// One exercise of a signature program: the exercise (and stand-ins, in order, for when the user's equipment doesn't
/// allow it), its sets and rep range, and optionally its own effort and rest.
/// </summary>
public sealed record ProgramSlot(string[] Ids, int Sets, int RepMin, int RepMax, int? Rir = null, int? Rest = null);

public sealed record ProgramDay(string Name, ProgramSlot[] Slots);

/// <summary>
/// A built-in plan based on a well-known program from a famous lifter, coach or community. These follow the publicly
/// known structure of each program; none is affiliated with or endorsed by the person it's named after.
/// </summary>
public sealed class SignatureProgram
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    /// <summary>Who the program comes from, as shown: "Arnold Schwarzenegger".</summary>
    public required string Author { get; init; }
    /// <summary>A sentence or two about the author.</summary>
    public required string Bio { get; init; }
    /// <summary>What the program is and how it's run.</summary>
    public required string Summary { get; init; }
    /// <summary>The goal it's built for, which sets the tips and default rest; then other goals it suits.</summary>
    public required Goal Goal { get; init; }
    public Goal[] AlsoFits { get; init; } = [];
    public required Experience[] Levels { get; init; }
    public required int Days { get; init; }
    /// <summary>Roughly how long a session takes.</summary>
    public required int Minutes { get; init; }
    /// <summary>What the program is written for; stand-ins replace what other setups lack.</summary>
    public required EquipmentAccess Needs { get; init; }
    public required ProgramDay[] Workouts { get; init; }

    public string Tags => $"{Days} days a week · {LevelText} · ~{Minutes} min · {Needs.Display()}";

    string LevelText => Levels.Length == 3 ? "All levels" : string.Join(" – ", Levels.Select(l => l.Display()));
}

/// <summary>A program ranked for the user's answers, with why it fits (or doesn't quite).</summary>
public sealed record ProgramMatch(SignatureProgram Program, int Score, List<string> Reasons);

/// <summary>The signature programs, how well each fits the user's answers, and the plan each one builds.</summary>
public static class SignaturePrograms
{
    // Shorthand: S("back_squat goblet_squat bw_squat", 4, 10, 10) is 4 sets of 10, on the first exercise the user can do.
    static ProgramSlot S(string ids, int sets, int min, int max, int? rir = null, int? rest = null) =>
        new(ids.Split(' ', StringSplitOptions.RemoveEmptyEntries), sets, min, max, rir, rest);

    static ProgramDay D(string name, params ProgramSlot[] slots) => new(name, slots);

    static readonly Experience[] Beginner = [Experience.Beginner];
    static readonly Experience[] BeginnerUp = [Experience.Beginner, Experience.Intermediate];
    static readonly Experience[] Intermediate = [Experience.Intermediate];
    static readonly Experience[] IntermediateUp = [Experience.Intermediate, Experience.Advanced];
    static readonly Experience[] Advanced = [Experience.Advanced];
    static readonly Experience[] AllLevels = [Experience.Beginner, Experience.Intermediate, Experience.Advanced];

    // Common movements with stand-ins down to dumbbells, bands and bodyweight.
    const string Squat = "back_squat goblet_squat bw_squat";
    const string Bench = "bench_press db_bench_press push_up";
    const string Incline = "incline_bench_press incline_db_press decline_push_up";
    const string Ohp = "overhead_press db_shoulder_press pike_push_up";
    const string Row = "barbell_row db_row inverted_row";
    const string Deadlift = "deadlift db_romanian_deadlift single_leg_rdl";
    const string Rdl = "romanian_deadlift db_romanian_deadlift single_leg_rdl";
    const string Pulldown = "lat_pulldown pull_up band_pulldown";
    const string CableRow = "seated_cable_row db_row inverted_row";
    const string Curl = "barbell_curl db_curl band_curl";
    const string Pushdown = "triceps_pushdown db_overhead_extension band_pushdown diamond_push_up";
    const string Lateral = "lateral_raise band_lateral_raise";
    const string LegCurl = "lying_leg_curl db_leg_curl nordic_curl";
    const string LegExt = "leg_extension bulgarian_split_squat split_squat";
    const string LegPress = "leg_press bulgarian_split_squat split_squat";
    const string Calf = "standing_calf_raise db_calf_raise bw_calf_raise";
    const string FacePull = "face_pull band_face_pull reverse_fly";
    const string Dips = "dips bench_dip";

    public static IReadOnlyList<SignatureProgram> All { get; } =
    [
        new()
        {
            Id = "golden_six",
            Name = "The Golden Six",
            Author = "Arnold Schwarzenegger",
            Bio = "Seven-time Mr. Olympia and the most famous bodybuilder of all time. This simple full-body routine is widely credited to him as the place for beginners to start.",
            Summary = "Six classic lifts, the same workout three times a week. Add weight once you hit every rep on every set. The original used a behind-the-neck press; this uses a front press, which is easier on the shoulders.",
            Goal = Goal.BuildMuscle,
            AlsoFits = [Goal.GeneralFitness, Goal.LoseFat],
            Levels = Beginner,
            Days = 3,
            Minutes = 45,
            Needs = EquipmentAccess.FullGym,
            Workouts = [.. Enumerable.Range(1, 3).Select(i => D($"Golden Six · Day {i}",
                S(Squat, 4, 10, 10),
                S("bench_press db_bench_press wide_push_up", 3, 10, 10),
                S("chin_up band_assisted_pull_up", 3, 6, 12),
                S(Ohp, 4, 10, 10),
                S(Curl, 3, 10, 10),
                S("sit_up", 3, 15, 25)))],
        },
        new()
        {
            Id = "reg_park_5x5",
            Name = "Reg Park 5×5",
            Author = "Reg Park",
            Bio = "Three-time Mr. Universe, star of the 1960s Hercules films and Arnold Schwarzenegger's boyhood idol and mentor. His 1960 booklet laid out one of the first 5×5 programs.",
            Summary = "Squat, bench and deadlift three times a week, five sets of five. Park's first two sets were warm-ups, so here that's warm-ups plus three working sets of five. Add weight when all three sets go.",
            Goal = Goal.Strength,
            AlsoFits = [Goal.BuildMuscle, Goal.GeneralFitness],
            Levels = BeginnerUp,
            Days = 3,
            Minutes = 60,
            Needs = EquipmentAccess.FullGym,
            Workouts = [.. Enumerable.Range(1, 3).Select(i => D($"Park 5×5 · Day {i}",
                S(Squat, 3, 5, 5),
                S(Bench, 3, 5, 5),
                S(Deadlift, 3, 5, 5),
                S("back_extension quadruped_hip_extension", 3, 10, 10),
                S(Calf, 3, 20, 25)))],
        },
        new()
        {
            Id = "stronglifts_5x5",
            Name = "StrongLifts-Style 5×5",
            Author = "Mehdi Hadim",
            Bio = "Lifting coach who brought the 5×5 to millions of beginners through the free StrongLifts guide and app.",
            Summary = "Two short workouts, A and B, three times a week. Squat every session, five sets of five on everything except one heavy set of deadlifts. Add a little weight every session while you get all the reps. In the original, weeks alternate A-B-A and B-A-B.",
            Goal = Goal.Strength,
            AlsoFits = [Goal.GeneralFitness],
            Levels = Beginner,
            Days = 3,
            Minutes = 45,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Workout A", S(Squat, 5, 5, 5), S(Bench, 5, 5, 5), S(Row, 5, 5, 5)),
                D("Workout B", S(Squat, 5, 5, 5), S(Ohp, 5, 5, 5), S(Deadlift, 1, 5, 5)),
                D("Workout A", S(Squat, 5, 5, 5), S(Bench, 5, 5, 5), S(Row, 5, 5, 5)),
            ],
        },
        new()
        {
            Id = "starting_strength",
            Name = "Novice Linear Progression",
            Author = "Mark Rippetoe",
            Bio = "Strength coach and author of Starting Strength, the textbook on the barbell lifts that taught a generation to squat, press and pull.",
            Summary = "Based on the Starting Strength novice program: low-bar squats every session, alternating bench and press, deadlifts and power cleans. Three sets of five, heavier every workout for as long as it lasts.",
            Goal = Goal.Strength,
            AlsoFits = [Goal.Power],
            Levels = Beginner,
            Days = 3,
            Minutes = 60,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Workout A", S("low_bar_squat " + Squat, 3, 5, 5), S(Bench, 3, 5, 5), S(Deadlift, 1, 5, 5)),
                D("Workout B", S("low_bar_squat " + Squat, 3, 5, 5), S(Ohp, 3, 5, 5), S("power_clean kb_swing broad_jump", 5, 3, 3)),
                D("Workout A", S("low_bar_squat " + Squat, 3, 5, 5), S(Bench, 3, 5, 5), S(Deadlift, 1, 5, 5)),
            ],
        },
        new()
        {
            Id = "wendler_bbb",
            Name = "5/3/1 Boring But Big",
            Author = "Jim Wendler",
            Bio = "Former college football player and elite powerlifter whose 5/3/1 method is one of the most widely run strength programs in the world.",
            Summary = "Four days, one main lift each, worked up to a top set of 3–5 reps on a monthly wave. Then five sets of ten on the opposite lift (a common Boring But Big variation) and an assistance exercise. Inspired by 5/3/1; follow Wendler's percentages for the main sets if you know them.",
            Goal = Goal.Strength,
            AlsoFits = [Goal.BuildMuscle],
            Levels = IntermediateUp,
            Days = 4,
            Minutes = 60,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Press Day", S(Ohp, 3, 3, 5), S(Bench, 5, 10, 10, rir: 3), S("chin_up pull_up band_assisted_pull_up", 5, 6, 10)),
                D("Deadlift Day", S(Deadlift, 3, 3, 5), S(Squat, 5, 10, 10, rir: 3), S("hanging_leg_raise leg_raise", 5, 10, 15)),
                D("Bench Day", S(Bench, 3, 3, 5), S(Ohp, 5, 10, 10, rir: 3), S("db_row inverted_row band_row", 5, 10, 10)),
                D("Squat Day", S(Squat, 3, 3, 5), S(Deadlift, 5, 10, 10, rir: 3), S(LegCurl, 5, 10, 10)),
            ],
        },
        new()
        {
            Id = "phul",
            Name = "PHUL",
            Author = "Brandon Campbell",
            Bio = "Strength coach and writer whose Power Hypertrophy Upper Lower program, first published on Muscle & Strength, became a staple for lifters who want size and strength together.",
            Summary = "Power Hypertrophy Upper Lower: two heavy days for strength (3–5 reps on the big lifts) and two lighter days of 8–12 reps for size, as an upper/lower split four days a week.",
            Goal = Goal.BuildMuscle,
            AlsoFits = [Goal.Strength, Goal.GeneralFitness],
            Levels = Intermediate,
            Days = 4,
            Minutes = 75,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Upper Power",
                    S(Bench, 4, 3, 5), S("incline_db_press decline_push_up", 4, 6, 10), S(Row, 4, 3, 5), S(Pulldown, 4, 6, 10),
                    S(Ohp, 3, 5, 8), S(Curl, 3, 6, 10), S("skull_crusher db_skull_crusher diamond_push_up", 3, 6, 10)),
                D("Lower Power",
                    S(Squat, 4, 3, 5), S(Deadlift, 4, 3, 5), S(LegPress, 5, 10, 15), S(LegCurl, 4, 6, 10), S(Calf, 4, 6, 10)),
                D("Upper Hypertrophy",
                    S(Incline, 4, 8, 12), S("cable_fly db_fly band_fly", 4, 8, 12), S(CableRow, 4, 8, 12), S("db_row band_row", 4, 8, 12),
                    S(Lateral, 4, 8, 12), S("incline_db_curl band_curl", 4, 8, 12), S(Pushdown, 4, 8, 12)),
                D("Lower Hypertrophy",
                    S("front_squat goblet_squat bw_squat", 4, 8, 12), S("barbell_lunge db_lunge bw_lunge", 4, 8, 12), S(LegExt, 4, 10, 15),
                    S("seated_leg_curl db_leg_curl nordic_curl", 4, 10, 15), S("seated_calf_raise db_calf_raise bw_calf_raise", 4, 8, 12)),
            ],
        },
        new()
        {
            Id = "phat",
            Name = "PHAT",
            Author = "Layne Norton",
            Bio = "Pro natural bodybuilder and international-level powerlifter with a PhD in nutritional sciences. PHAT was his blend of powerlifting and bodybuilding training.",
            Summary = "Power Hypertrophy Adaptive Training: two heavy power days, then three bodybuilding days that open with fast, light speed sets (6 × 3 at about 65%) before higher-rep work. Five days, high volume.",
            Goal = Goal.BuildMuscle,
            AlsoFits = [Goal.Strength],
            Levels = IntermediateUp,
            Days = 5,
            Minutes = 90,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Upper Power",
                    S("pendlay_row " + Row, 3, 3, 5), S("weighted_pull_up pull_up", 2, 6, 10), S(Bench, 3, 3, 5), S("weighted_chest_dip dips bench_dip", 2, 6, 10),
                    S("db_shoulder_press pike_push_up", 3, 6, 10), S(Curl, 3, 6, 10), S("skull_crusher db_skull_crusher diamond_push_up", 3, 6, 10)),
                D("Lower Power",
                    S(Squat, 3, 3, 5), S("hack_squat bulgarian_split_squat split_squat", 2, 6, 10), S(LegExt, 2, 6, 10),
                    S("stiff_leg_deadlift db_romanian_deadlift single_leg_rdl", 3, 5, 8), S(LegCurl, 2, 6, 10), S(Calf, 3, 6, 10),
                    S("seated_calf_raise db_calf_raise bw_calf_raise", 2, 6, 10)),
                D("Back & Shoulders",
                    S("pendlay_row " + Row, 6, 3, 3, rir: 4), S("t_bar_row db_row inverted_row", 3, 8, 12), S(Pulldown, 3, 8, 12),
                    S(CableRow, 2, 12, 15), S("db_shoulder_press pike_push_up", 3, 8, 12), S(Lateral, 3, 12, 20), S("barbell_shrug db_shrug", 2, 12, 15)),
                D("Legs",
                    S(Squat, 6, 3, 3, rir: 4), S("hack_squat bulgarian_split_squat split_squat", 3, 8, 12), S(LegPress, 2, 12, 15), S(LegExt, 3, 15, 20),
                    S(Rdl, 3, 8, 12), S(LegCurl, 2, 12, 15), S("seated_calf_raise db_calf_raise bw_calf_raise", 4, 10, 15)),
                D("Chest & Arms",
                    S("db_bench_press push_up", 6, 3, 3, rir: 4), S("incline_db_press decline_push_up", 3, 8, 12), S("machine_chest_press db_bench_press push_up", 3, 12, 15),
                    S("cable_fly db_fly band_fly", 2, 15, 20), S("preacher_curl db_preacher_curl band_curl", 3, 8, 12), S("spider_curl concentration_curl band_curl", 2, 15, 20),
                    S("overhead_triceps_extension db_overhead_extension band_overhead_extension", 3, 8, 12), S(Pushdown, 2, 12, 15)),
            ],
        },
        new()
        {
            Id = "reddit_ppl",
            Name = "Reddit Push Pull Legs",
            Author = "Metallicadpa",
            Bio = "The Reddit user whose free push/pull/legs routine, posted to r/Fitness in 2017, became one of the most recommended beginner programs online.",
            Summary = "Push, pull, legs, twice a week. Each day opens with a 5×5 strength lift (or one heavy set of deadlifts), then 8–12-rep work for size. Add weight to the main lift every session while you can.",
            Goal = Goal.BuildMuscle,
            AlsoFits = [Goal.GeneralFitness, Goal.LoseFat],
            Levels = BeginnerUp,
            Days = 6,
            Minutes = 75,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Pull A", S(Deadlift, 1, 5, 5), S(Pulldown, 3, 8, 12), S(CableRow, 3, 8, 12), S(FacePull, 5, 15, 20),
                    S("hammer_curl band_curl", 4, 8, 12), S("db_curl band_curl", 4, 8, 12)),
                D("Push A", S(Bench, 5, 5, 5), S(Ohp, 3, 8, 12), S("incline_db_press decline_push_up", 3, 8, 12), S(Pushdown, 3, 8, 12),
                    S(Lateral, 3, 15, 20), S("overhead_triceps_extension db_overhead_extension band_overhead_extension", 3, 8, 12)),
                D("Legs A", S(Squat, 3, 5, 5), S(Rdl, 3, 8, 12), S(LegPress, 3, 8, 12), S(LegCurl, 3, 8, 12), S(Calf, 5, 8, 12)),
                D("Pull B", S(Row, 5, 5, 5), S("pull_up band_assisted_pull_up", 3, 8, 12), S("seated_cable_row band_row", 3, 8, 12), S(FacePull, 5, 15, 20),
                    S("hammer_curl band_curl", 4, 8, 12), S("db_curl band_curl", 4, 8, 12)),
                D("Push B", S(Ohp, 5, 5, 5), S(Bench, 3, 8, 12), S("incline_db_press decline_push_up", 3, 8, 12), S(Pushdown, 3, 8, 12),
                    S(Lateral, 3, 15, 20), S("overhead_triceps_extension db_overhead_extension band_overhead_extension", 3, 8, 12)),
                D("Legs B", S(Squat, 3, 5, 5), S(Rdl, 3, 8, 12), S(LegPress, 3, 8, 12), S(LegCurl, 3, 8, 12), S(Calf, 5, 8, 12)),
            ],
        },
        new()
        {
            Id = "gzclp",
            Name = "GZCLP",
            Author = "Cody Lefever",
            Bio = "Powerlifting coach known online as GZCL, whose tiered method grew into GZCLP, a popular free linear progression.",
            Summary = "Every day has three tiers: a heavy T1 lift for 5 × 3, a T2 lift for 3 × 10, and a T3 accessory for 3 × 15+ with the last set taken close to failure. Four rotating workouts, three or four times a week.",
            Goal = Goal.Strength,
            AlsoFits = [Goal.BuildMuscle],
            Levels = BeginnerUp,
            Days = 4,
            Minutes = 50,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("A1", S(Squat, 5, 3, 3), S(Bench, 3, 10, 10), S(Pulldown, 3, 15, 25, rir: 1)),
                D("B1", S(Ohp, 5, 3, 3), S(Deadlift, 3, 10, 10), S("db_row band_row inverted_row", 3, 15, 25, rir: 1)),
                D("A2", S(Bench, 5, 3, 3), S(Squat, 3, 10, 10), S(Pulldown, 3, 15, 25, rir: 1)),
                D("B2", S(Deadlift, 5, 3, 3), S(Ohp, 3, 10, 10), S("db_row band_row inverted_row", 3, 15, 25, rir: 1)),
            ],
        },
        new()
        {
            Id = "heavy_duty",
            Name = "Heavy Duty",
            Author = "Mike Mentzer",
            Bio = "1978 Mr. Universe, the first to win with a perfect score. His Heavy Duty system argued that brief, infrequent, all-out workouts build the most muscle.",
            Summary = "One working set per exercise, after warm-ups, taken to failure. Where an isolation exercise comes first, go straight from it to the next lift (pre-exhaust). Mentzer had his lifters rest four or more days between workouts; add rest days freely.",
            Goal = Goal.BuildMuscle,
            AlsoFits = [Goal.GeneralFitness],
            Levels = IntermediateUp,
            Days = 3,
            Minutes = 30,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Chest & Back",
                    S("pec_deck db_fly band_fly", 1, 6, 10, rir: 0, rest: 15), S(Incline, 1, 6, 10, rir: 0),
                    S("pullover_machine db_pullover band_straight_arm_pulldown", 1, 6, 10, rir: 0, rest: 15), S("reverse_grip_pulldown chin_up", 1, 6, 10, rir: 0),
                    S(Deadlift, 1, 5, 8, rir: 1)),
                D("Legs",
                    S(LegExt, 1, 8, 12, rir: 0, rest: 15), S(LegPress, 1, 8, 12, rir: 0), S(LegCurl, 1, 6, 10, rir: 0), S(Calf, 1, 12, 20, rir: 0)),
                D("Shoulders & Arms",
                    S(Lateral, 1, 6, 10, rir: 0), S("rear_delt_machine reverse_fly band_face_pull", 1, 6, 10, rir: 0), S(Curl, 1, 6, 10, rir: 0),
                    S(Pushdown, 1, 6, 10, rir: 0, rest: 15), S(Dips, 1, 6, 10, rir: 0)),
            ],
        },
        new()
        {
            Id = "blood_and_guts",
            Name = "Blood & Guts",
            Author = "Dorian Yates",
            Bio = "Six-time Mr. Olympia (1992–1997) who built the most massive physique of his era on short, brutal sessions of one all-out set per exercise.",
            Summary = "A four-day split. Each exercise gets its warm-ups and then one working set taken to failure, so sessions stay under an hour. Yates' trademark underhand row is in here.",
            Goal = Goal.BuildMuscle,
            Levels = IntermediateUp,
            Days = 4,
            Minutes = 50,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Chest & Biceps",
                    S(Incline, 1, 6, 8, rir: 0), S(Bench, 1, 6, 8, rir: 0), S("incline_db_fly db_fly band_fly", 1, 8, 10, rir: 0),
                    S("cable_fly db_fly band_fly", 1, 8, 10, rir: 0), S("db_curl band_curl", 1, 8, 10, rir: 0), S("machine_preacher_curl db_preacher_curl band_curl", 1, 8, 10, rir: 0)),
                D("Legs",
                    S(LegExt, 1, 10, 12, rir: 0), S(LegPress, 1, 10, 12, rir: 0), S("hack_squat goblet_squat bw_squat", 1, 10, 12, rir: 0),
                    S(LegCurl, 1, 8, 10, rir: 0), S("stiff_leg_deadlift db_romanian_deadlift single_leg_rdl", 1, 8, 10, rir: 1), S(Calf, 1, 10, 12, rir: 0)),
                D("Shoulders & Triceps",
                    S("machine_shoulder_press db_shoulder_press pike_push_up", 1, 6, 8, rir: 0), S(Lateral, 1, 8, 10, rir: 0),
                    S("rear_delt_machine reverse_fly band_face_pull", 1, 8, 10, rir: 0), S(Pushdown, 1, 8, 10, rir: 0),
                    S("overhead_triceps_extension db_overhead_extension band_overhead_extension", 1, 8, 10, rir: 0)),
                D("Back",
                    S("pullover_machine db_pullover band_straight_arm_pulldown", 1, 8, 10, rir: 0), S("close_grip_pulldown chin_up", 1, 8, 10, rir: 0),
                    S("yates_row db_row inverted_row", 1, 8, 10, rir: 0), S("single_arm_cable_row db_row band_row", 1, 8, 10, rir: 0),
                    S(Deadlift, 1, 6, 8, rir: 1)),
            ],
        },
        new()
        {
            Id = "arnold_split",
            Name = "Arnold Split",
            Author = "Arnold Schwarzenegger",
            Bio = "Seven-time Mr. Olympia and the most famous bodybuilder of all time, known for training twice as hard and twice as often as anyone else.",
            Summary = "The classic golden-era double split: chest and back together, shoulders and arms, then legs, each twice a week with different exercises. Lots of sets of 8–12; for experienced lifters who can recover from it.",
            Goal = Goal.BuildMuscle,
            Levels = Advanced,
            Days = 6,
            Minutes = 90,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Chest & Back A",
                    S(Bench, 4, 8, 12), S(Incline, 4, 8, 12), S("db_fly band_fly", 3, 10, 12), S("wide_grip_pull_up pull_up", 4, 8, 12),
                    S(Row, 4, 8, 12), S("t_bar_row db_row inverted_row", 3, 8, 12), S("db_pullover band_straight_arm_pulldown", 3, 10, 12)),
                D("Shoulders & Arms A",
                    S(Ohp, 4, 8, 12), S(Lateral, 4, 10, 12), S("reverse_fly band_face_pull", 3, 10, 12), S(Curl, 4, 8, 12),
                    S("incline_db_curl band_curl", 3, 10, 12), S("close_grip_bench diamond_push_up", 4, 8, 12), S("skull_crusher db_skull_crusher bench_dip", 3, 10, 12)),
                D("Legs A",
                    S(Squat, 5, 8, 12), S("barbell_lunge db_lunge bw_lunge", 3, 10, 12), S(LegExt, 3, 12, 15), S(LegCurl, 4, 10, 12),
                    S("stiff_leg_deadlift db_romanian_deadlift single_leg_rdl", 3, 10, 12), S(Calf, 5, 10, 15), S("crunch", 3, 15, 25)),
                D("Chest & Back B",
                    S("incline_db_press decline_push_up", 4, 8, 12), S("db_bench_press push_up", 4, 8, 12), S("cable_fly db_fly band_fly", 3, 10, 12),
                    S("chin_up band_assisted_pull_up", 4, 8, 12), S(CableRow, 4, 8, 12), S("db_row band_row", 3, 8, 12), S(Dips, 3, 8, 12)),
                D("Shoulders & Arms B",
                    S("arnold_press db_shoulder_press pike_push_up", 4, 8, 12), S("cable_lateral_raise lateral_raise band_lateral_raise", 4, 10, 12),
                    S(FacePull, 3, 12, 15), S("ez_bar_curl db_curl band_curl", 4, 8, 12), S("hammer_curl band_curl", 3, 10, 12),
                    S("overhead_triceps_extension db_overhead_extension band_overhead_extension", 4, 10, 12), S("reverse_curl db_reverse_curl", 3, 12, 15)),
                D("Legs B",
                    S("front_squat goblet_squat bw_squat", 5, 8, 12), S("hack_squat bulgarian_split_squat split_squat", 3, 10, 12), S("seated_leg_curl db_leg_curl nordic_curl", 4, 10, 12),
                    S(Rdl, 3, 10, 12), S("seated_calf_raise db_calf_raise bw_calf_raise", 5, 12, 15), S("hanging_leg_raise leg_raise", 3, 10, 15)),
            ],
        },
        new()
        {
            Id = "texas_method",
            Name = "Texas Method",
            Author = "Glenn Pendlay",
            Bio = "Olympic weightlifting coach who produced many US national champions. The Texas Method grew out of his training with lifters in Texas.",
            Summary = "For lifters past beginner gains: a hard volume day (5 × 5) on Monday, a light recovery day, then an intensity day on Friday where you try for a new five-rep best. Progress comes week to week instead of every session.",
            Goal = Goal.Strength,
            AlsoFits = [Goal.Power],
            Levels = Intermediate,
            Days = 3,
            Minutes = 75,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Volume Day", S(Squat, 5, 5, 5), S(Bench, 5, 5, 5), S(Deadlift, 1, 5, 5)),
                D("Recovery Day", S(Squat, 2, 5, 5, rir: 4), S(Ohp, 3, 5, 5), S("chin_up band_assisted_pull_up", 3, 6, 10), S("back_extension quadruped_hip_extension", 3, 10, 10)),
                D("Intensity Day", S(Squat, 1, 5, 5, rir: 1), S(Bench, 1, 5, 5, rir: 1), S("power_clean kb_swing broad_jump", 5, 3, 3)),
            ],
        },
        new()
        {
            Id = "bill_starr_5x5",
            Name = "Big Three 5×5",
            Author = "Bill Starr",
            Bio = "Weightlifting champion and strength coach of the Baltimore Colts in the 1970s. His book The Strongest Shall Survive gave football its classic heavy, light and medium 5×5.",
            Summary = "Starr's Big Three (power clean, bench press and squat) on a heavy, light and medium day each week, with a little back and ab work. Built for athletes who need strength and power.",
            Goal = Goal.Strength,
            AlsoFits = [Goal.Power, Goal.GeneralFitness],
            Levels = BeginnerUp,
            Days = 3,
            Minutes = 60,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Heavy Day", S("power_clean kb_swing broad_jump", 5, 3, 5), S(Bench, 5, 5, 5), S(Squat, 5, 5, 5),
                    S("back_extension quadruped_hip_extension", 2, 10, 12), S("sit_up", 4, 15, 25)),
                D("Light Day", S("power_clean kb_swing broad_jump", 5, 3, 5, rir: 4), S(Incline, 4, 5, 5), S(Squat, 4, 5, 5, rir: 4), S("sit_up", 3, 15, 25)),
                D("Medium Day", S("power_clean kb_swing broad_jump", 5, 3, 5), S(Bench, 4, 5, 5), S(Squat, 4, 5, 5, rir: 3), S(Dips, 3, 5, 8)),
            ],
        },
        new()
        {
            Id = "ws4sb",
            Name = "Westside for Skinny Bastards",
            Author = "Joe DeFranco",
            Bio = "Strength coach to NFL draft prospects and pro fighters from his New Jersey gym. This program adapted Westside powerlifting for athletes.",
            Summary = "Four days: a max-effort upper day (work up to a heavy 3–5), an explosive lower day that starts with jumps, a repetition upper day for muscle, and a max-effort lower day. Strength, speed and size for athletes.",
            Goal = Goal.Power,
            AlsoFits = [Goal.Strength, Goal.BuildMuscle],
            Levels = IntermediateUp,
            Days = 4,
            Minutes = 60,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Max-Effort Upper", S("floor_press db_floor_press push_up", 3, 3, 5, rir: 1), S("db_bench_press push_up", 2, 10, 15),
                    S(Row, 4, 6, 10), S(FacePull, 3, 12, 15), S("hammer_curl band_curl", 3, 8, 12)),
                D("Explosive Lower", S("box_jump broad_jump", 5, 3, 3), S("box_squat " + Squat, 3, 3, 5, rir: 1), S("bulgarian_split_squat split_squat", 3, 6, 10),
                    S("glute_ham_raise back_extension nordic_curl", 3, 8, 12), S("hanging_leg_raise leg_raise", 3, 10, 15)),
                D("Repetition Upper", S("db_shoulder_press pike_push_up", 3, 8, 12), S("pull_up band_assisted_pull_up", 3, 6, 12),
                    S("db_bench_press push_up", 3, 10, 15), S(Lateral, 3, 12, 15), S(Pushdown, 3, 10, 15), S(FacePull, 3, 15, 20)),
                D("Max-Effort Lower", S("broad_jump box_jump", 4, 3, 3), S("trap_bar_deadlift " + Deadlift, 3, 3, 5, rir: 1),
                    S("walking_lunge db_walking_lunge bw_lunge", 3, 8, 12), S(Rdl, 3, 8, 12), S("plank", 3, 30, 45)),
            ],
        },
        new()
        {
            Id = "tyson_bodyweight",
            Name = "Catskill Bodyweight Circuit",
            Author = "Mike Tyson",
            Bio = "Undisputed heavyweight boxing champion and the youngest heavyweight champion ever, at 20. Under Cus D'Amato in the Catskills he did huge daily volumes of push-ups, sit-ups, dips and neck bridges.",
            Summary = "Inspired by Tyson's famous calisthenics, scaled to a volume a normal person can recover from: high-rep push-ups, sit-ups, dips and squats, plus neck work, four days a week. Needs nothing but the floor and somewhere to dip.",
            Goal = Goal.Power,
            AlsoFits = [Goal.GeneralFitness, Goal.LoseFat],
            Levels = BeginnerUp,
            Days = 4,
            Minutes = 45,
            Needs = EquipmentAccess.Bodyweight,
            Workouts =
            [
                D("Circuit A", S("push_up knee_push_up", 5, 15, 30), S("sit_up", 5, 20, 30), S(Dips, 4, 10, 20), S("bw_squat", 4, 25, 40),
                    S("wrestlers_bridge neck_isometric_front_back", 3, 5, 8)),
                D("Circuit B", S("diamond_push_up push_up", 4, 10, 20), S("bicycle_crunch crunch", 4, 20, 30), S("pull_up band_assisted_pull_up", 4, 5, 12),
                    S("jump_squat bw_squat", 4, 10, 15), S("neck_isometric_sides", 3, 5, 8)),
                D("Circuit A", S("push_up knee_push_up", 5, 15, 30), S("sit_up", 5, 20, 30), S(Dips, 4, 10, 20), S("bw_squat", 4, 25, 40),
                    S("wrestlers_bridge neck_isometric_front_back", 3, 5, 8)),
                D("Circuit B", S("diamond_push_up push_up", 4, 10, 20), S("bicycle_crunch crunch", 4, 20, 30), S("pull_up band_assisted_pull_up", 4, 5, 12),
                    S("jump_squat bw_squat", 4, 10, 15), S("neck_isometric_sides", 3, 5, 8)),
            ],
        },
        new()
        {
            Id = "bwf_recommended_routine",
            Name = "Bodyweight Recommended Routine",
            Author = "the r/bodyweightfitness community",
            Bio = "One of Reddit's biggest training communities. Its free Recommended Routine is the usual starting point for building strength with just a bar and the floor.",
            Summary = "A full-body session three times a week: paired pulling, pushing and leg exercises, then core work. Move to a harder variation of an exercise once you get 3 sets at the top of its rep range.",
            Goal = Goal.GeneralFitness,
            AlsoFits = [Goal.BuildMuscle, Goal.LoseFat],
            Levels = BeginnerUp,
            Days = 3,
            Minutes = 60,
            Needs = EquipmentAccess.Bodyweight,
            Workouts = [.. Enumerable.Range(1, 3).Select(i => D($"Full Body · Day {i}",
                S("pull_up negative_pull_up band_assisted_pull_up", 3, 5, 8),
                S("split_squat bw_lunge", 3, 5, 8),
                S("dips bench_dip", 3, 5, 8),
                S("single_leg_rdl glute_bridge", 3, 5, 8),
                S("inverted_row band_row", 3, 5, 8),
                S("push_up knee_push_up", 3, 5, 8),
                S("hanging_knee_raise leg_raise", 3, 8, 12),
                S("hollow_body_hold plank", 3, 20, 40)))],
        },
        new()
        {
            Id = "simple_sinister",
            Name = "Simple & Sinister",
            Author = "Pavel Tsatsouline",
            Bio = "Former Soviet special-forces physical-training instructor credited with bringing the kettlebell to the West.",
            Summary = "A short daily practice with one kettlebell: 100 one-arm swings (10 sets of 10) and 10 Turkish get-ups (five a side). Move up a bell once the session feels easy. Strength and conditioning in about half an hour.",
            Goal = Goal.GeneralFitness,
            AlsoFits = [Goal.Strength, Goal.LoseFat, Goal.Power],
            Levels = AllLevels,
            Days = 5,
            Minutes = 30,
            Needs = EquipmentAccess.HomeDumbbells,
            Workouts = [.. Enumerable.Range(1, 5).Select(i => D($"Practice · Day {i}",
                S("kb_single_arm_swing kb_swing", 10, 10, 10, rir: 3, rest: 45),
                S("kb_turkish_get_up", 5, 2, 2, rir: 3, rest: 60)))],
        },
        new()
        {
            Id = "ten_thousand_swings",
            Name = "10,000 Swing Challenge",
            Author = "Dan John",
            Bio = "Discus thrower, Highland Games competitor and long-time strength coach whose books made simple, hard training popular again.",
            Summary = "500 kettlebell swings a session, five days a week, for four weeks: 10,000 in total. Between rounds of swings come a few reps of a strength lift. A month-long fat-loss and conditioning challenge.",
            Goal = Goal.LoseFat,
            AlsoFits = [Goal.GeneralFitness, Goal.Power],
            Levels = IntermediateUp,
            Days = 5,
            Minutes = 45,
            Needs = EquipmentAccess.HomeDumbbells,
            Workouts = [.. Enumerable.Range(0, 5).Select(i => D($"500 Swings · Day {i + 1}",
                S("kb_swing kb_single_arm_swing", 20, 25, 25, rir: 3, rest: 30),
                (i % 3) switch
                {
                    0 => S("kb_press db_shoulder_press pike_push_up", 5, 1, 3),
                    1 => S("kb_goblet_squat goblet_squat bw_squat", 5, 2, 5),
                    _ => S("chin_up band_assisted_pull_up", 5, 1, 3),
                }))],
        },
        new()
        {
            Id = "glute_focused",
            Name = "Glute-Focused Full Body",
            Author = "Bret Contreras",
            Bio = "Sports scientist with a PhD, known as \"the Glute Guy\", who popularised the barbell hip thrust.",
            Summary = "Inspired by Contreras' approach: a hip-thrust or bridge every session, a squat and a hinge pattern, abduction work for the upper glutes, plus enough upper-body training to stay balanced. Three days a week.",
            Goal = Goal.BuildMuscle,
            AlsoFits = [Goal.LoseFat, Goal.GeneralFitness],
            Levels = BeginnerUp,
            Days = 3,
            Minutes = 60,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Day 1 · Hip Thrust",
                    S("hip_thrust db_hip_thrust single_leg_hip_thrust", 4, 6, 10), S(Squat, 3, 8, 12), S(Pulldown, 3, 10, 12),
                    S("db_bench_press push_up", 3, 8, 12), S("cable_hip_abduction banded_seated_abduction side_lying_hip_abduction", 3, 15, 20)),
                D("Day 2 · Hinge",
                    S(Rdl, 3, 8, 10), S("bulgarian_split_squat split_squat", 3, 10, 12), S("back_extension_45 back_extension quadruped_hip_extension", 3, 15, 20),
                    S("db_row band_row", 3, 10, 12), S("db_shoulder_press pike_push_up", 3, 10, 12), S("hip_abduction_machine banded_lateral_walk side_lying_hip_abduction", 3, 20, 30)),
                D("Day 3 · Bridge",
                    S("barbell_glute_bridge glute_bridge", 3, 12, 20), S(LegPress, 3, 10, 15), S("cable_kickback band_kickback quadruped_hip_extension", 3, 12, 15),
                    S("chin_up band_assisted_pull_up", 3, 5, 10), S("incline_db_press decline_push_up", 3, 8, 12), S("banded_lateral_walk side_lying_hip_abduction", 2, 15, 20)),
            ],
        },
        new()
        {
            Id = "steve_reeves",
            Name = "Classic Physique",
            Author = "Steve Reeves",
            Bio = "1950 Mr. Universe and star of the Hercules films, famous for a balanced, classic physique built on three full-body workouts a week.",
            Summary = "Reeves trained his whole body three times a week, about three sets of 8–12 per exercise, aiming for proportion rather than size alone. Two alternating sessions.",
            Goal = Goal.GeneralFitness,
            AlsoFits = [Goal.BuildMuscle, Goal.LoseFat],
            Levels = BeginnerUp,
            Days = 3,
            Minutes = 75,
            Needs = EquipmentAccess.FullGym,
            Workouts =
            [
                D("Workout A", S(Ohp, 3, 8, 12), S("incline_db_press decline_push_up", 3, 8, 12), S("chin_up band_assisted_pull_up", 3, 8, 12),
                    S(Row, 3, 8, 12), S(Squat, 3, 8, 12), S("stiff_leg_deadlift db_romanian_deadlift single_leg_rdl", 3, 8, 12), S(Curl, 3, 8, 12),
                    S(Calf, 3, 12, 15)),
                D("Workout B", S("db_shoulder_press pike_push_up", 3, 8, 12), S(Bench, 3, 8, 12), S(Pulldown, 3, 8, 12),
                    S(CableRow, 3, 8, 12), S("front_squat goblet_squat bw_squat", 3, 8, 12), S(LegCurl, 3, 8, 12),
                    S("skull_crusher db_skull_crusher diamond_push_up", 3, 8, 12), S("seated_calf_raise db_calf_raise bw_calf_raise", 3, 12, 15)),
                D("Workout A", S(Ohp, 3, 8, 12), S("incline_db_press decline_push_up", 3, 8, 12), S("chin_up band_assisted_pull_up", 3, 8, 12),
                    S(Row, 3, 8, 12), S(Squat, 3, 8, 12), S("stiff_leg_deadlift db_romanian_deadlift single_leg_rdl", 3, 8, 12), S(Curl, 3, 8, 12),
                    S(Calf, 3, 12, 15)),
            ],
        },
        new()
        {
            Id = "big_five",
            Name = "The Big Five",
            Author = "Doug McGuff",
            Bio = "Emergency physician and co-author of Body by Science, which made the case for one short, very intense workout a week.",
            Summary = "Five big machine movements, one very slow set each (about 10 seconds up and 10 down) until you can't move the weight. Moving straight from one exercise to the next, it takes under 20 minutes. McGuff suggests once a week; twice suits most people at first.",
            Goal = Goal.GeneralFitness,
            AlsoFits = [Goal.BuildMuscle, Goal.Strength],
            Levels = AllLevels,
            Days = 2,
            Minutes = 20,
            Needs = EquipmentAccess.FullGym,
            Workouts = [.. Enumerable.Range(1, 2).Select(i => D($"Big Five · Day {i}",
                S("seated_cable_row db_row inverted_row", 1, 4, 8, rir: 0, rest: 30),
                S("machine_chest_press db_bench_press push_up", 1, 4, 8, rir: 0, rest: 30),
                S("lat_pulldown pull_up band_pulldown", 1, 4, 8, rir: 0, rest: 30),
                S("machine_shoulder_press db_shoulder_press pike_push_up", 1, 4, 8, rir: 0, rest: 30),
                S("leg_press goblet_squat bw_squat", 1, 4, 8, rir: 0, rest: 30)))],
        },
    ];

    public static SignatureProgram? Find(string? id) => All.FirstOrDefault(p => p.Id == id);

    /// <summary>Every program scored against the answers, best fit first.</summary>
    public static List<ProgramMatch> Rank(UserProfile answers) =>
        [.. All.Select(p => Match(p, answers)).OrderByDescending(m => m.Score)];

    /// <summary>How well <paramref name="program"/> fits the answers: goal first, then experience, days, equipment and session length.</summary>
    public static ProgramMatch Match(SignatureProgram program, UserProfile answers)
    {
        var score = 0;
        var reasons = new List<string>();

        if (program.Goal == answers.Goal)
        {
            score += 40;
            reasons.Add($"Built for {GoalText(answers.Goal)}");
        }
        else if (program.AlsoFits.Contains(answers.Goal))
        {
            score += 25;
            reasons.Add($"Good for {GoalText(answers.Goal)}");
        }
        else
            reasons.Add($"Made for {GoalText(program.Goal)}");

        if (program.Levels.Contains(answers.Experience))
        {
            score += 25;
            reasons.Add(program.Levels.Length == 3 ? "Suits every level" : $"Made for {answers.Experience.Display().ToLowerInvariant()}s");
        }
        else if (program.Levels.Min() > answers.Experience)
        {
            score -= 10 + 15 * (program.Levels.Min() - answers.Experience);
            reasons.Add($"For {program.Levels.Min().Display().ToLowerInvariant()} lifters");
        }
        else
        {
            score += 5;
            reasons.Add("Simpler than you need");
        }

        var dayDiff = Math.Abs(program.Days - answers.DaysPerWeek);
        score += dayDiff switch { 0 => 25, 1 => 10, 2 => -5, _ => -20 };
        reasons.Add(dayDiff == 0 ? $"{program.Days} days, as you asked" : $"{program.Days} days a week (you said {answers.DaysPerWeek})");

        // Equipment: what share of the exercises the user can do as written, or with a stand-in.
        var slots = program.Workouts.SelectMany(w => w.Slots).ToList();
        var asWritten = slots.Count(s => Allowed(s.Ids[0], answers.EquipmentAccess));
        var doable = slots.Count(s => s.Ids.Any(id => Allowed(id, answers.EquipmentAccess)));
        if (asWritten == slots.Count)
            score += 15;
        else if (doable == slots.Count)
        {
            score += 2;
            reasons.Add("Swaps in exercises for your equipment");
        }
        else
        {
            score -= 40;
            reasons.Add($"Needs a {program.Needs.Display().ToLowerInvariant()}");
        }

        var over = program.Minutes - answers.SessionMinutes;
        score += over <= 0 ? 10 : 10 - over / 4;
        if (over > 10)
            reasons.Add($"Sessions take about {program.Minutes} min");

        return new ProgramMatch(program, score, reasons);
    }

    static string GoalText(Goal goal) => goal switch
    {
        Goal.BuildMuscle => "building muscle",
        Goal.Strength => "strength",
        Goal.LoseFat => "losing fat",
        Goal.Power => "power and combat sports",
        _ => "general fitness",
    };

    static bool Allowed(string id, EquipmentAccess access) => ExerciseLibrary.Find(id) is { } ex && access.Allows(ex.Equipment);

    /// <summary>
    /// The plan for <paramref name="program"/>: each exercise is the first one the user's equipment allows (or the
    /// program's own when none is), with the program's sets and reps. Neck work is added when the user wants it and
    /// left out when they don't.
    /// </summary>
    public static WorkoutPlan Build(SignatureProgram program, UserProfile profile)
    {
        var plan = new WorkoutPlan
        {
            Name = program.Name,
            Description = $"{program.Summary}\n\nInspired by {program.Author}'s program. Not affiliated with or endorsed by {program.Author}.",
            Goal = program.Goal,
            DaysPerWeek = program.Workouts.Length,
        };
        var neckDay = 0;
        foreach (var day in program.Workouts)
        {
            var workout = new PlanWorkout { Name = day.Name };
            foreach (var slot in day.Slots)
            {
                var ex = slot.Ids.Select(ExerciseLibrary.Find).OfType<Exercise>().FirstOrDefault(e => profile.EquipmentAccess.Allows(e.Equipment))
                    ?? ExerciseLibrary.Find(slot.Ids[0]);
                if (ex == null || workout.Exercises.Any(e => e.ExerciseId == ex.Id) || ex.PrimaryMuscle == MuscleGroup.Neck && !profile.TrainNeck)
                    continue;
                var pe = TrainingGoals.Prescription(program.Goal, profile.Experience, ex, profile);
                // The neck keeps its own careful prescription, whatever the program did.
                if (ex.PrimaryMuscle == MuscleGroup.Neck)
                {
                    workout.Exercises.Add(pe);
                    continue;
                }
                pe.Sets = slot.Sets;
                pe.RepMin = slot.RepMin;
                pe.RepMax = slot.RepMax;
                if (slot.Rir is { } rir)
                    pe.TargetRir = rir;
                if (slot.Rest is { } rest)
                    pe.RestSeconds = rest;
                workout.Exercises.Add(pe);
            }
            if (profile.TrainNeck && workout.Exercises.All(e => ExerciseLibrary.Find(e.ExerciseId)?.PrimaryMuscle != MuscleGroup.Neck))
                PlanGenerator.AddNeck(workout, IsLowerDay(workout) ? 0 : 1, profile, ref neckDay);
            plan.Workouts.Add(workout);
        }
        plan.RestDays = [.. PlanSchedule.DefaultRestDays(plan.Workouts.Count).Order()];
        return plan;
    }

    /// <summary>Mostly legs: no neck work that day, as with the built-in generator.</summary>
    static bool IsLowerDay(PlanWorkout workout)
    {
        var muscles = workout.Exercises.Select(e => ExerciseLibrary.Find(e.ExerciseId)?.PrimaryMuscle).ToList();
        var legs = muscles.Count(m => m is MuscleGroup.Quads or MuscleGroup.Hamstrings or MuscleGroup.Glutes or MuscleGroup.Calves);
        return legs * 2 > muscles.Count;
    }
}
