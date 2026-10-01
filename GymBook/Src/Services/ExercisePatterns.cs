using System.Text.RegularExpressions;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>A kind of movement (a press, a row, a hinge...): what makes two exercises do the same job.</summary>
/// <param name="Title">How it reads as a filter option.</param>
/// <param name="Aliases">What people call it in a search, in lowercase words ("reverse pec deck", "rear delt").</param>
public sealed record MovementPattern(string Id, string Title, string[] Aliases);

/// <summary>
/// Sorts every exercise, custom ones too, into a movement pattern by its name (and its kind of training): the
/// exercise filters narrow by it, the search finds a whole pattern by any of its names ("reverse flys" also brings
/// the Reverse Pec Deck and the Face Pull), and replacing an exercise suggests ones of the same pattern first.
/// </summary>
public static class ExercisePatterns
{
    static readonly MovementPattern Olympic = new("olympic", "Olympic lift", ["olympic", "olympic lift", "clean", "snatch", "jerk", "oly"]);
    static readonly MovementPattern Neck = new("neck", "Neck", ["neck", "neck curl", "neck extension", "chin tuck"]);

    // In order: the first rule whose words appear in the name wins, so the more specific ones come first
    // (a "Split Squat" is a lunge, a "Triceps Dip" a dip, a "Clean and Jerk" not a press).
    static readonly (MovementPattern Pattern, Regex Name)[] Rules =
    [
        Rule("rear_delt", "Rear delt", @"reverse fly|reverse flye|reverse pec deck|rear delt|face pull|pull apart|y t w|i y t",
            "rear delt", "rear delts", "reverse fly", "reverse pec deck", "rear delt fly", "posterior delt", "face pull", "pull apart",
            "rear fly", "reverse butterfly"),
        Rule("chest_fly", "Chest fly", @"\bfly\b|\bflye\b|pec deck|crossover",
            "chest fly", "pec fly", "pec deck", "butterfly", "cable crossover", "crossover", "fly", "flye", "pec dec"),
        Rule("pullover", "Pullover", @"pullover|lat prayer|straight arm pulldown",
            "pullover", "straight arm pulldown", "lat prayer"),
        Rule("vertical_pull", "Pull-up / pulldown", @"pull up|chin up|pulldown|pull down|muscle up|front lever|active hang|scapular pull",
            "pull up", "pullup", "chin up", "chinup", "lat pulldown", "pulldown", "pull down", "vertical pull", "lat pull"),
        Rule("upright_row", "Upright row", @"upright row|high pull", "upright row", "high pull"),
        Rule("row", "Row", @"\brow\b|\brows\b|rowing", "row", "rows", "horizontal pull", "seated row", "cable row", "bent over row"),
        Rule("shrug", "Shrug", @"shrug", "shrug", "shrugs", "trap raise"),
        Rule("rotator_cuff", "Rotator cuff", @"external rotation|internal rotation|cuban press",
            "rotator cuff", "external rotation", "internal rotation", "cuban press"),
        Rule("hinge", "Hinge / deadlift", @"deadlift|\brdl\b|good morning|rack pull|pull through|jefferson curl|swing|back extension|hyperextension|superman",
            "hinge", "deadlift", "deadlifts", "rdl", "romanian", "stiff leg", "good morning", "hip hinge", "back extension", "hyperextension", "hyper", "swing"),
        // Kettlebell and sandbag cleans and snatches are Strength in the library; they're Olympic-style lifts all the same.
        (Olympic, new Regex(@"\bclean\b|snatch", RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        Rule("overhead_press", "Overhead press", @"overhead press|shoulder press|military|push press|jerk|z press|landmine press|bradford|arnold|log press|pike push up|handstand push up|behind the neck press|bottoms up press",
            "overhead press", "ohp", "shoulder press", "military press", "vertical press", "arnold press", "press overhead"),
        Rule("lateral_raise", "Lateral raise", @"lateral raise|y raise", "lateral raise", "side raise", "side lateral", "lat raise", "laterals", "side delt"),
        Rule("front_raise", "Front raise", @"front raise", "front raise", "front delt raise"),
        Rule("dip", "Dip", @"(?<!hip )\bdip\b|\bdips\b", "dip", "dips", "parallel bar dip"),
        Rule("triceps_extension", "Triceps extension", @"pushdown|push down|triceps|tricep|overhead extension|skull crusher|jm press|tate press|dip machine",
            "triceps extension", "tricep extension", "skull crusher", "skullcrusher", "french press", "lying triceps extension",
            "pushdown", "push down", "kickback", "tricep pushdown", "triceps pushdown", "overhead extension"),
        Rule("horizontal_press", "Chest press", @"bench press|chest press|floor press|dumbbell press|machine press|smith machine press|push up|squeeze press|svend press|serratus punch",
            "bench press", "chest press", "bench", "press up", "push up", "pushup", "horizontal press", "incline press", "decline press", "flat press"),
        Rule("leg_curl", "Leg curl", @"leg curl|nordic curl|glute ham|razor curl", "leg curl", "hamstring curl", "ham curl", "nordic", "nordics", "lying leg curl", "seated leg curl"),
        Rule("grip", "Wrist and grip", @"wrist|finger|gripper|pinch|deviation|pronation|reverse curl|fat grip|towel hang|dead hang",
            "grip", "wrist curl", "forearm curl", "reverse curl", "wrist", "forearms"),
        Rule("biceps_curl", "Biceps curl", @"\bcurl\b|\bcurls\b", "biceps curl", "bicep curl", "curl", "curls", "arm curl", "hammer curl", "preacher curl"),
        Rule("leg_extension", "Leg extension", @"leg extension|reverse nordic|spanish squat", "leg extension", "quad extension", "knee extension"),
        Rule("calf_raise", "Calf raise", @"calf raise|tibialis", "calf raise", "calf raises", "calves", "calf", "tibialis raise"),
        Rule("lunge", "Lunge / split squat", @"lunge|split squat|step up|step down|pistol|shrimp|cossack|curtsy",
            "lunge", "lunges", "split squat", "step up", "single leg squat", "bulgarian", "bss"),
        Rule("squat", "Squat / leg press", @"squat|leg press|hack|wall sit|belt squat", "squat", "squats", "leg press", "hack squat", "knee dominant"),
        Rule("hip_thrust", "Hip thrust / bridge", @"hip thrust|glute bridge|frog pump|bridging|shoulder bridge|pelvic curl", "hip thrust", "glute bridge", "bridge", "hip thrusts"),
        Rule("hip_abduction", "Hip abduction", @"abduction|clamshell|\bclam\b|lateral walk|monster walk|fire hydrant", "abduction", "hip abductor", "abductor", "outer thigh", "clam", "glute med"),
        Rule("hip_adduction", "Hip adduction", @"adduction|copenhagen|inner thigh", "adductor", "adduction", "inner thigh", "groin"),
        Rule("glute_kickback", "Glute kickback", @"kickback|donkey kick|hip extension", "glute kickback", "kickback", "donkey kick", "hip extension"),
        Rule("carry", "Carry", @"carry|farmer|yoke walk|sled|stone|tire flip|keg", "carry", "farmers walk", "farmer carry", "loaded carry", "sled", "strongman"),
        Rule("rotation", "Twist / side bend", @"woodchop|pallof|russian twist|windshield|rotation|side bend|windmill|oblique|rotational|bicycle crunch",
            "twist", "rotation", "oblique", "obliques", "woodchop", "wood chop", "anti rotation", "side bend"),
        Rule("ab_flexion", "Crunch / leg raise", @"crunch|sit up|v up|tuck up|leg raise|knee raise|toes to bar|dragon flag|flutter|heel taps",
            "crunch", "crunches", "sit up", "situp", "leg raise", "ab crunch", "knee raise", "hanging leg raise"),
        Rule("ab_brace", "Plank / bracing", @"plank|rollout|roll out|body saw|stir the pot|dead bug|hollow|l sit|suitcase hold|bird dog",
            "plank", "planks", "core stability", "rollout", "ab wheel", "hollow", "dead bug", "brace"),
    ];

    // Everything that isn't strength training is grouped by its kind.
    static readonly Dictionary<ExerciseCategory, MovementPattern> ByCategory = new()
    {
        [ExerciseCategory.Olympic] = Olympic,
        [ExerciseCategory.Plyometric] = new("plyometric", "Jump / throw", ["plyometric", "plyo", "jump", "jumps", "throw", "explosive"]),
        [ExerciseCategory.Cardio] = new("cardio", "Cardio", ["cardio", "conditioning", "hiit", "endurance"]),
        [ExerciseCategory.Stretch] = new("stretch", "Stretch", ["stretch", "stretches", "stretching", "flexibility"]),
        [ExerciseCategory.Mobility] = new("mobility", "Mobility", ["mobility", "warm up", "warmup", "foam roll", "rehab"]),
        [ExerciseCategory.Yoga] = new("yoga", "Yoga pose", ["yoga", "pose", "asana"]),
        [ExerciseCategory.Pilates] = new("pilates", "Pilates", ["pilates", "reformer", "mat pilates"]),
    };

    static readonly MovementPattern Other = new("other", "Other", []);

    /// <summary>Every pattern, in the order filters list them.</summary>
    public static readonly IReadOnlyList<MovementPattern> All =
        [.. Rules.Select(r => r.Pattern).Concat(ByCategory.Values).Append(Neck).Append(Other).Distinct()];

    static readonly Dictionary<string, MovementPattern> Cache = [];

    /// <summary>The exercise's movement pattern, from its kind of training and its name.</summary>
    public static MovementPattern Of(Exercise e)
    {
        if (!e.IsCustom && Cache.TryGetValue(e.Id, out var cached))
            return cached;
        var category = ExerciseLibrary.Details(e.Id)?.Category ?? ExerciseCategory.Strength;
        var name = string.Join(' ', ExerciseSearch.Words(e.Name));
        // Olympic lifts go by their name too: a push press or a jerk is a press to anyone looking for one.
        var pattern = ByCategory.TryGetValue(category, out var kind) && category != ExerciseCategory.Olympic ? kind
            : e.PrimaryMuscle == MuscleGroup.Neck ? Neck
            : Rules.FirstOrDefault(r => r.Name.IsMatch(name)).Pattern ?? kind ?? Other;
        if (!e.IsCustom)
            Cache[e.Id] = pattern;
        return pattern;
    }

    static (MovementPattern, Regex) Rule(string id, string title, string name, params string[] aliases) =>
        (new MovementPattern(id, title, aliases), new Regex(name, RegexOptions.CultureInvariant | RegexOptions.Compiled));
}
