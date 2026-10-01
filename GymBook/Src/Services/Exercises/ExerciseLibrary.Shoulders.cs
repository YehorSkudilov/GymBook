using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] ShoulderExercises() =>
    [
        // Barbell overhead presses
        new("overhead_press", "Overhead Press", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Traps, Abs],
            Level = Intermediate,
            Summary = "The standing barbell press: the bar goes from the front of your shoulders to locked out overhead, training the delts, triceps and the whole trunk.",
            Steps =
            [
                "Set the bar in a rack at upper-chest height and grip it just outside your shoulders.",
                "Unrack it onto the front of your shoulders, elbows slightly in front of the bar, feet hip-width.",
                "Brace your abs and glutes, then press the bar straight up, moving your head back just enough to clear it.",
                "As the bar passes your forehead, bring your head through so the bar finishes over your mid-foot with arms locked.",
                "Lower it under control back to your shoulders.",
            ],
            Tips =
            [
                "Squeeze your glutes and ribs down so you press with your shoulders, not by leaning back.",
                "Keep your forearms vertical and wrists stacked over your elbows.",
                "Don't press the bar forward around your face; move your head out of the way instead.",
            ],
            Video = new("_RlRDWO2jfg", 206.48, 272.63901),
        },
        new("seated_barbell_press", "Seated Barbell Overhead Press", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Traps],
            Level = Intermediate,
            Summary = "A barbell press done seated on an upright bench, taking the legs and most of the balance out so the shoulders and triceps do the work.",
            Steps =
            [
                "Set an upright bench in a rack or use a seated press station, with the bar just above shoulder height.",
                "Sit with your back against the pad, feet planted, and grip the bar just outside your shoulders.",
                "Unrack the bar and hold it at the front of your shoulders.",
                "Press it straight up to lockout, bringing your head through at the top.",
                "Lower it under control to your upper chest.",
            ],
            Tips =
            [
                "Keep your glutes and upper back on the bench; don't arch into an incline press.",
                "Use a back pad set to vertical or just short of it.",
            ],
            Video = new("YNK3eQVevIs"),
        },
        new("behind_neck_press", "Behind-the-Neck Press", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Traps],
            Level = Advanced,
            Summary = "An overhead press that starts and finishes behind the head, putting more work on the side delts; only for lifters with excellent shoulder mobility.",
            Steps =
            [
                "Set the bar in a rack at shoulder height and step under it so it rests on your upper traps, as for a squat.",
                "Grip it wider than for an overhead press and stand tall with your core braced.",
                "Press the bar straight up from behind your neck to lockout.",
                "Lower it slowly back to about ear level or the top of your traps.",
            ],
            Tips =
            [
                "Only do this if you can hold the bar behind your head without pain or forcing your head forward.",
                "Use light weight and a partial range to start; stop if you feel any pinching at the front of the shoulder.",
                "Don't drop the bar onto your neck or push your chin to your chest to make room.",
            ],
            Video = new("uD9kYAi7lHs", 11.759),
        },
        new("z_press", "Z Press", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Abs, Traps],
            Level = Advanced,
            Summary = "A barbell press done sitting on the floor with legs straight out, which removes any leg drive or lean-back and demands a strong, upright trunk.",
            Steps =
            [
                "Set the bar in a rack at about chin height when you are seated on the floor.",
                "Sit with your legs straight and apart, grip the bar just outside your shoulders and unrack it to your collarbones.",
                "Sit tall, brace your abs and press the bar straight overhead to lockout.",
                "Lower it under control back to your shoulders.",
            ],
            Tips =
            [
                "Keep your torso vertical; if you fall backward, the weight is too heavy or your hamstrings are too tight.",
                "Start much lighter than your standing press.",
            ],
            Video = new("0fHdnBH9Gdo"),
        },
        new("landmine_press", "Landmine Press", Shoulders, Barbell, Compound)
        {
            Secondary = [Chest, Triceps, Abs],
            Summary = "A single-arm press of a barbell anchored in a landmine, pressing up and forward on an arc that is friendly to stiff or sore shoulders.",
            Steps =
            [
                "Anchor one end of a barbell in a landmine or a corner and load the other end.",
                "Stand in a staggered stance facing the anchor and hold the end of the bar at your shoulder in one hand.",
                "Brace your abs and press the bar up and forward until your arm is straight.",
                "Let your shoulder blade move forward around your ribs at the top.",
                "Lower it under control back to your shoulder, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Try it half-kneeling (same-side knee down as the pressing arm) for more core demand.",
                "Don't twist or lean away to finish the rep.",
            ],
            Video = new("6cSTRPhpubs"),
        },
        new("bradford_press", "Bradford Press", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Traps],
            Level = Intermediate,
            Summary = "A light barbell press that moves the bar from in front of the head to behind it and back, keeping the delts under constant tension.",
            Steps =
            [
                "Hold a light barbell at the front of your shoulders with a grip just outside shoulder width.",
                "Press it up just high enough to clear the top of your head.",
                "Lower it behind your head to about ear level.",
                "Press it back over your head and lower it to the front; that front-and-back cycle is one rep.",
            ],
            Tips =
            [
                "Never lock out at the top; the bar only travels a few inches above your head.",
                "Use a much lighter weight than your overhead press and move slowly.",
                "Skip it if holding the bar behind your neck is uncomfortable.",
            ],
            Video = new("7aCSp9GtQDw"),
        },

        // Dumbbell, kettlebell and machine presses
        new("db_shoulder_press", "Seated Dumbbell Shoulder Press", Shoulders, Dumbbell, Compound)
        {
            Secondary = [Triceps],
            Summary = "Pressing two dumbbells overhead from an upright bench, a staple for building the front and side delts.",
            Steps =
            [
                "Set a bench upright, sit with your back against the pad and rest the dumbbells on your thighs.",
                "Kick them up one at a time to shoulder height, palms facing forward and elbows slightly in front of your body.",
                "Press the dumbbells up until your arms are straight and the weights are over your shoulders.",
                "Lower them under control until your hands are around ear to shoulder level.",
            ],
            Tips =
            [
                "Keep your elbows a little forward rather than flared straight out to the sides.",
                "Don't bang the dumbbells together at the top or arch off the pad.",
            ],
            Video = new("fAlu-BsnPqE"),
        },
        new("standing_db_press", "Standing Dumbbell Shoulder Press", Shoulders, Dumbbell, Compound)
        {
            Secondary = [Triceps, Abs],
            Summary = "A dumbbell press done standing, which adds core and balance work to the shoulder press.",
            Steps =
            [
                "Stand with feet hip-width and hold the dumbbells at shoulder height, palms forward or facing each other.",
                "Brace your abs and squeeze your glutes.",
                "Press the dumbbells straight up until your arms are locked out over your shoulders.",
                "Lower them under control back to your shoulders.",
            ],
            Tips =
            [
                "Keep your ribs down and don't lean back to turn it into an incline press.",
                "Don't use your legs; that's a push press.",
            ],
            Video = new("qLZU6Fl2sI4"),
        },
        new("single_arm_db_press", "Single-Arm Dumbbell Shoulder Press", Shoulders, Dumbbell, Compound)
        {
            Secondary = [Triceps, Abs],
            Summary = "A one-arm standing dumbbell press that trains each shoulder on its own and makes your trunk resist leaning to the side.",
            Steps =
            [
                "Stand tall holding one dumbbell at your shoulder, the other hand on your hip or out to the side.",
                "Brace your abs and glutes.",
                "Press the dumbbell straight up to lockout without leaning away from it.",
                "Lower it under control to your shoulder, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Stay level: your hips and shoulders shouldn't tilt as you press.",
                "Start with your weaker side and match its reps on the other.",
            ],
            Video = new("yFgmgtnWP_0"),
        },
        new("arnold_press", "Arnold Press", Shoulders, Dumbbell, Compound)
        {
            Secondary = [Triceps],
            Level = Intermediate,
            Summary = "A seated dumbbell press that starts palms-in in front of the chest and rotates to palms-forward as you press, hitting the front and side delts through a long range.",
            Steps =
            [
                "Sit on an upright bench holding the dumbbells in front of your upper chest, palms facing you and elbows in front.",
                "Press up while opening your elbows out to the sides and rotating your palms forward.",
                "Finish with arms straight overhead and palms facing forward.",
                "Reverse the rotation as you lower back to the start.",
            ],
            Tips =
            [
                "Make the rotation smooth and continuous through the whole press.",
                "Use lighter weights than a regular dumbbell press.",
            ],
            Video = new("AjB-UXErljM"),
        },
        new("kb_press", "Kettlebell Overhead Press", Shoulders, Kettlebell, Compound)
        {
            Secondary = [Triceps, Abs],
            Level = Intermediate,
            Summary = "A single-arm press from the kettlebell rack position, with the bell resting on the back of the forearm.",
            Steps =
            [
                "Clean the kettlebell to the rack position: handle in your palm, bell on the back of your forearm, elbow tucked to your ribs.",
                "Brace your abs and glutes and grip the handle firmly.",
                "Press the kettlebell straight up, letting your palm turn to face forward as it rises.",
                "Lock out with your bicep by your ear, then lower it back to the rack under control.",
                "Do all reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your wrist straight; don't let the bell bend it back.",
                "Don't lean away from the bell to finish the rep.",
            ],
            Video = new("X-uFqWtjpGI"),
        },
        new("kb_bottoms_up_press", "Kettlebell Bottoms-Up Press", Shoulders, Kettlebell, Compound)
        {
            Secondary = [Forearms, Triceps],
            Level = Intermediate,
            Summary = "A press with the kettlebell held upside down, bell up, which demands a hard grip and steady rotator cuff control.",
            Steps =
            [
                "Hold a light kettlebell by the handle with the bell pointing at the ceiling, at shoulder height.",
                "Squeeze the handle hard and keep your wrist straight and forearm vertical.",
                "Press the kettlebell slowly overhead, keeping the bell balanced upright.",
                "Lower it slowly back to shoulder height, then switch sides after your reps.",
            ],
            Tips =
            [
                "Go much lighter than a normal kettlebell press.",
                "If the bell tips, reset rather than chasing it.",
            ],
            Video = new("k5PcL_WIx94"),
        },
        new("machine_shoulder_press", "Machine Shoulder Press", Shoulders, Machine, Compound)
        {
            Secondary = [Triceps],
            Summary = "A seated press on a shoulder press machine, letting you push the delts hard without balancing a weight.",
            Steps =
            [
                "Adjust the seat so the handles start at about shoulder height.",
                "Sit with your back against the pad and grip the handles.",
                "Press up until your arms are nearly straight.",
                "Lower under control until your hands are back at shoulder level.",
            ],
            Tips =
            [
                "Keep your back and head on the pad throughout.",
                "Don't let the weight stack touch down between reps.",
            ],
            Video = new("Gkk-6q7Rq-s"),
        },
        new("smith_shoulder_press", "Smith Machine Shoulder Press", Shoulders, Machine, Compound)
        {
            Secondary = [Triceps],
            Summary = "A seated overhead press on a Smith machine, where the fixed bar path lets you focus on pushing hard with the delts.",
            Steps =
            [
                "Place an upright bench under the Smith bar so the bar comes down just in front of your face.",
                "Sit with your back against the pad and grip the bar just outside your shoulders.",
                "Unrack the bar by rotating it and press it up to nearly straight arms.",
                "Lower it under control to about chin or upper-chest level.",
            ],
            Tips =
            [
                "Position the bench so the bar clears your nose and chin without you leaning back.",
                "Always re-rack by rotating the hooks onto the safety pins.",
            ],
            Video = new("OLqZDUUD2b0"),
        },
        new("band_shoulder_press", "Band Shoulder Press", Shoulders, Band, Compound)
        {
            Secondary = [Triceps],
            Summary = "An overhead press against a resistance band anchored under your feet, a simple home or travel option that is hardest at the top.",
            Steps =
            [
                "Stand on the middle of a band with feet hip-width and hold the handles or ends at shoulder height.",
                "Brace your abs and keep your ribs down.",
                "Press your hands straight up until your arms are straight overhead.",
                "Lower them under control back to your shoulders.",
            ],
            Tips =
            [
                "Choke up on the band or widen your stance for more tension.",
                "Don't lean back as the band gets harder at the top.",
            ],
            Video = new("tQ40C0WD6OY"),
        },

        // Front raises
        new("db_front_raise", "Dumbbell Front Raise", Shoulders, Dumbbell, Isolation)
        {
            Secondary = [Chest],
            Summary = "Raising dumbbells straight in front of you to shoulder height to isolate the front delts.",
            Steps =
            [
                "Stand tall holding the dumbbells in front of your thighs, palms facing you.",
                "Keeping a soft bend in your elbows, raise the dumbbells forward to shoulder height.",
                "Pause briefly, then lower them slowly back to your thighs.",
            ],
            Tips =
            [
                "Raise both arms together or alternate; either way keep your torso still.",
                "Don't swing the weight up with your hips or lean back.",
            ],
            Video = new("zkP0MsTcIVU"),
        },
        new("cable_front_raise", "Cable Front Raise", Shoulders, Cable, Isolation)
        {
            Secondary = [Chest],
            Summary = "A front raise against a low cable, which keeps tension on the front delt through the whole range.",
            Steps =
            [
                "Attach a straight bar, rope or single handle to a low pulley and stand facing away from it, the cable between your legs.",
                "Hold the attachment in front of your thighs with straight arms and a soft elbow bend.",
                "Raise it forward to shoulder height.",
                "Lower it slowly back down.",
            ],
            Tips =
            [
                "Stand far enough forward that the weight stack doesn't touch down at the bottom.",
                "Don't rock your body to get the weight moving.",
            ],
            Video = new("vtH93qBItdk"),
        },
        new("barbell_front_raise", "Barbell Front Raise", Shoulders, Barbell, Isolation)
        {
            Secondary = [Chest],
            Summary = "A front raise with a barbell or EZ bar held in both hands, letting you load the front delts a little heavier.",
            Steps =
            [
                "Hold a light barbell in front of your thighs with an overhand, shoulder-width grip.",
                "Brace your abs and raise the bar forward with nearly straight arms to shoulder height.",
                "Lower it slowly back to your thighs.",
            ],
            Tips =
            [
                "Keep your shoulders down away from your ears.",
                "Don't lean back or bounce the bar off your thighs.",
            ],
            Video = new("_ikCPws1mbE"),
        },
        new("plate_front_raise", "Plate Front Raise", Shoulders, Other, Isolation)
        {
            Secondary = [Chest, Forearms],
            Summary = "A front raise holding a weight plate by its edges, a common finisher for the front delts.",
            Steps =
            [
                "Hold a weight plate by its sides at the 3 and 9 o'clock positions, in front of your thighs.",
                "With a slight bend in your elbows, raise the plate forward to eye level.",
                "Lower it slowly back down.",
            ],
            Tips =
            [
                "Squeeze the plate firmly; the grip work is part of it.",
                "Don't lean back or swing it up.",
            ],
            Video = new("HN8HYJTOl8c"),
        },

        // Lateral raises
        new("lateral_raise", "Dumbbell Lateral Raise", Shoulders, Dumbbell, Isolation)
        {
            Summary = "Raising dumbbells out to your sides to shoulder height, the go-to isolation exercise for the side delts.",
            Steps =
            [
                "Stand tall with the dumbbells at your sides, a slight bend in your elbows and a slight forward lean.",
                "Raise your arms out to the sides, leading with your elbows.",
                "Stop when your arms are about parallel to the floor.",
                "Lower the dumbbells slowly back to your sides.",
            ],
            Tips =
            [
                "Think of pushing the dumbbells out wide rather than up.",
                "Keep your shoulders down; don't shrug the weight up with your traps.",
                "Don't swing; light weight with control beats heavy cheating.",
            ],
            Video = new("OuG1smZTsQQ"),
        },
        new("seated_lateral_raise", "Seated Dumbbell Lateral Raise", Shoulders, Dumbbell, Isolation)
        {
            Summary = "A lateral raise done sitting on the end of a bench, which makes it much harder to swing the weight up.",
            Steps =
            [
                "Sit on the end of a bench with feet flat, holding the dumbbells at your sides.",
                "Sit tall with a slight bend in your elbows.",
                "Raise the dumbbells out to the sides until your arms are parallel to the floor.",
                "Lower them slowly until they hang just beside the bench.",
            ],
            Tips =
            [
                "Expect to use a little less weight than standing.",
                "Don't lean back or shrug at the top.",
            ],
            Video = new("xDrYB81QXmY"),
        },
        new("lean_away_lateral_raise", "Lean-Away Lateral Raise", Shoulders, Dumbbell, Isolation)
        {
            Level = Intermediate,
            Summary = "A one-arm lateral raise done while leaning away from a post, which shifts the hardest part toward the bottom of the lift.",
            Steps =
            [
                "Hold a sturdy upright with one hand and a dumbbell in the other, feet close to the post.",
                "Lean away until your working arm hangs a little out from your body.",
                "Raise the dumbbell out to the side until your arm is about parallel to the floor.",
                "Lower it slowly, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your body straight and still while leaning; only the arm moves.",
                "Don't shrug the shoulder toward your ear.",
            ],
            Video = new("SgyUoY0IZ7A", 178, 193),
        },
        new("cable_lateral_raise", "Cable Lateral Raise", Shoulders, Cable, Isolation)
        {
            Summary = "A one-arm lateral raise from a low cable, giving the side delt steady tension from the very bottom of the rep.",
            Steps =
            [
                "Set a single handle at the lowest pulley and stand side-on to it.",
                "Take the handle in the hand farther from the machine, the cable running in front of or behind your body.",
                "With a slight elbow bend, raise your arm out to the side to shoulder height.",
                "Lower it slowly, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Lead with your elbow and keep your hand no higher than your elbow.",
                "Don't lean your torso away to help the weight up.",
            ],
            Video = new("Z5FA9aq3L6A"),
        },
        new("machine_lateral_raise", "Machine Lateral Raise", Shoulders, Machine, Isolation)
        {
            Summary = "A lateral raise on a dedicated machine with pads on the arms, a stable way to take the side delts close to failure.",
            Steps =
            [
                "Adjust the seat so your shoulders line up with the machine's pivot points.",
                "Sit with your chest against the pad, arms against the pads and a light grip on the handles.",
                "Raise your arms out to the sides until they're about parallel to the floor.",
                "Lower them under control back to the start.",
            ],
            Tips =
            [
                "Push through the pads with your elbows, not your hands.",
                "Don't shrug or lift your hips off the seat.",
            ],
            Video = new("0o07iGKUarI"),
        },
        new("band_lateral_raise", "Band Lateral Raise", Shoulders, Band, Isolation)
        {
            Summary = "A lateral raise against a resistance band under your feet, which gets harder as your arms rise.",
            Steps =
            [
                "Stand on the middle of a band and hold an end or handle in each hand at your sides.",
                "With a slight bend in your elbows, raise your arms out to the sides to shoulder height.",
                "Lower them slowly back to your sides.",
            ],
            Tips =
            [
                "Cross the band in front of you for more tension, or step one foot on it for less.",
                "Don't shrug as the band tightens at the top.",
            ],
            Video = new("yfNg5sFndbw"),
        },
        new("cable_y_raise", "Cable Y-Raise", Shoulders, Cable, Isolation)
        {
            Secondary = [Traps],
            Level = Intermediate,
            Summary = "Raising two low cables crossed in front of you up and out into a Y shape, working the side delts and lower traps together.",
            Steps =
            [
                "Set both pulleys of a cable crossover at the bottom and stand in the middle.",
                "Grab the left cable with your right hand and the right cable with your left, so the cables cross in front of you.",
                "With a slight elbow bend, raise your arms up and out in a Y until your hands are just above head height.",
                "Lower them slowly until they cross in front of your hips again.",
            ],
            Tips =
            [
                "Keep your thumbs pointing up and slightly back as you raise.",
                "Don't shrug or arch your lower back at the top.",
            ],
            Video = new("SgyUoY0IZ7A", 533, 546),
        },

        // Rear delts
        new("reverse_fly", "Bent-Over Reverse Fly", Shoulders, Dumbbell, Isolation)
        {
            Secondary = [Traps, Back],
            Summary = "Raising dumbbells out to the sides while bent over, the classic free-weight move for the rear delts.",
            Steps =
            [
                "Hold the dumbbells and hinge at the hips until your torso is close to parallel with the floor, back flat and knees soft.",
                "Let the dumbbells hang under your chest, palms facing each other and elbows slightly bent.",
                "Raise your arms out to the sides until they're in line with your body.",
                "Lower them slowly back down.",
            ],
            Tips =
            [
                "Think of moving your hands out wide, not squeezing your shoulder blades together hard.",
                "Keep your back flat and your torso still; don't heave the weight up.",
            ],
            Video = new("SgyUoY0IZ7A", 696, 718),
        },
        new("chest_supported_reverse_fly", "Chest-Supported Reverse Fly", Shoulders, Dumbbell, Isolation)
        {
            Secondary = [Traps, Back],
            Summary = "A reverse fly lying face down on an incline bench, which takes your lower back out of it and stops you swinging the weight.",
            Steps =
            [
                "Set a bench to about 30–45° and lie face down on it, chest on the pad and feet on the floor.",
                "Let the dumbbells hang straight down, palms facing each other and elbows slightly bent.",
                "Raise your arms out to the sides until they're in line with your body.",
                "Lower them slowly back down.",
            ],
            Tips =
            [
                "Keep your chest on the pad throughout.",
                "Use light weights; the rear delts are small.",
            ],
            Video = new("G4KcUxqcXO8"),
        },
        new("cable_reverse_fly", "Cable Reverse Fly", Shoulders, Cable, Isolation)
        {
            Secondary = [Traps, Back],
            Summary = "Pulling crossed cables apart at shoulder height, giving the rear delts constant tension through the whole range.",
            Steps =
            [
                "Set both pulleys of a cable crossover at about shoulder height, with no handles or with single handles.",
                "Grab the left cable with your right hand and the right cable with your left, and step back so your arms are straight in front of you.",
                "Keeping a slight elbow bend, sweep your arms out and back until they're in line with your body.",
                "Return slowly until your hands cross in front of you.",
            ],
            Tips =
            [
                "Keep your arms at shoulder height and your shoulders down.",
                "Don't lean back or turn it into a row by bending your elbows.",
            ],
            Video = new("qZlDovba6ik"),
        },
        new("rear_delt_machine", "Reverse Pec Deck", Shoulders, Machine, Isolation)
        {
            Secondary = [Traps, Back],
            Summary = "Sweeping the handles of a pec deck backward while facing the pad, a stable way to isolate the rear delts.",
            Steps =
            [
                "Set the handles to the rear position and the seat so the handles are at shoulder height.",
                "Sit facing the pad, chest against it, and grab the handles with your arms straight in front of you.",
                "Sweep your arms out and back until they're in line with your body.",
                "Return slowly until the handles are back in front of you.",
            ],
            Tips =
            [
                "Keep a slight bend in your elbows and lead with the backs of your hands.",
                "Don't pull your chest off the pad or shrug.",
            ],
            Video = new("5YK4bgzXDp0"),
        },
        new("face_pull", "Face Pull", Shoulders, Cable, Compound)
        {
            Secondary = [Traps, Back],
            Summary = "Pulling a rope toward your face while rotating your hands outward, training the rear delts, rotator cuff and upper back for healthy shoulders.",
            Steps =
            [
                "Set a rope attachment at upper-chest to face height and grip it with your thumbs toward you.",
                "Step back until your arms are straight and the weight is off the stack.",
                "Pull the rope toward your forehead, spreading the ends apart and driving your elbows out and back.",
                "Finish with your hands beside your ears and your knuckles pointing back, then return slowly.",
            ],
            Tips =
            [
                "Keep your elbows at or above shoulder height.",
                "Don't lean back or turn it into a heavy row; use a weight you can pause with.",
            ],
            Video = new("SgyUoY0IZ7A", 661, 696),
        },
        new("band_face_pull", "Band Face Pull", Shoulders, Band, Compound)
        {
            Secondary = [Traps, Back],
            Summary = "A face pull with a resistance band anchored at face height, an easy home version for the rear delts and rotator cuff.",
            Steps =
            [
                "Anchor a band at about face height and hold one end in each hand, thumbs toward you.",
                "Step back until the band is taut with your arms straight.",
                "Pull your hands toward your forehead, elbows high, spreading your hands apart and rotating them back.",
                "Return slowly to straight arms.",
            ],
            Tips =
            [
                "Pause for a second at the end of each rep.",
                "Don't let your elbows drop below your shoulders.",
            ],
            Video = new("CSP7YpPv3ds"),
        },
        new("band_pull_apart", "Band Pull-Apart", Shoulders, Band, Isolation)
        {
            Secondary = [Traps, Back],
            Summary = "Stretching a band apart in front of your chest with straight arms, a simple warm-up and posture move for the rear delts and upper back.",
            Steps =
            [
                "Hold a light band in front of you at shoulder height with straight arms, hands about shoulder-width apart.",
                "Pull the band apart by moving your hands out to your sides.",
                "Stop when the band touches your chest and your arms are in line with your body.",
                "Return slowly to the start.",
            ],
            Tips =
            [
                "Keep your arms straight and your shoulders down.",
                "Don't arch your lower back or push your ribs forward.",
                "Try palms up or palms down; both work.",
            ],
            Video = new("smSSXITNpCI"),
        },

        // Upright rows
        new("upright_row", "Wide-Grip Upright Row", Shoulders, Barbell, Compound)
        {
            Secondary = [Traps, Biceps],
            Level = Intermediate,
            Summary = "Pulling a barbell up the front of your body with a wide grip, leading with the elbows, to work the side delts and upper traps.",
            Steps =
            [
                "Hold a barbell in front of your thighs with an overhand grip a little wider than your shoulders.",
                "Stand tall and pull the bar up close to your body, leading with your elbows.",
                "Stop when your elbows reach about shoulder height and the bar is around your lower chest.",
                "Lower it slowly back to your thighs.",
            ],
            Tips =
            [
                "A grip at least shoulder-width or wider is kinder to the shoulders than a narrow one.",
                "Don't pull your elbows above shoulder height if it pinches.",
                "Keep the bar close to your body and don't swing.",
            ],
            Video = new("nwkLwMRHMQo", 201, 234),
        },
        new("cable_upright_row", "Cable Upright Row", Shoulders, Cable, Compound)
        {
            Secondary = [Traps, Biceps],
            Summary = "An upright row with a bar or rope on a low pulley, giving smooth, constant tension on the side delts and traps.",
            Steps =
            [
                "Attach a straight bar or rope to a low pulley and stand close, facing it.",
                "Hold the attachment in front of your thighs with hands about shoulder-width or wider.",
                "Pull it up toward your chest, leading with your elbows, until they reach about shoulder height.",
                "Lower it slowly back down.",
            ],
            Tips =
            [
                "A rope lets your hands move apart at the top, which many find easier on the shoulders.",
                "Don't lean back to finish the pull.",
            ],
            Video = new("qr3ziolhjvQ"),
        },
        new("db_upright_row", "Dumbbell Upright Row", Shoulders, Dumbbell, Compound)
        {
            Secondary = [Traps, Biceps],
            Summary = "An upright row with a dumbbell in each hand, letting each arm follow its own path.",
            Steps =
            [
                "Stand holding the dumbbells in front of your thighs, palms facing you.",
                "Pull them up along your body, leading with your elbows, letting your hands drift apart.",
                "Stop when your elbows reach about shoulder height.",
                "Lower them slowly back down.",
            ],
            Tips =
            [
                "Keep your wrists below your elbows throughout.",
                "Don't shrug or swing the weights up.",
            ],
            Video = new("Ub6QruNKfbY"),
        },

        // Rotator cuff
        new("band_external_rotation", "Band External Rotation", Shoulders, Band, Isolation)
        {
            Summary = "Rotating your forearm outward against a band with your elbow at your side, a basic strengthener for the rotator cuff muscles that turn the arm out.",
            Steps =
            [
                "Anchor a light band at elbow height and stand side-on to it, holding the band in the hand farther from the anchor.",
                "Bend that elbow to 90° and keep it against your side; a rolled towel between elbow and ribs helps.",
                "Rotate your forearm outward, away from your body, as far as you can without moving your elbow.",
                "Return slowly, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your wrist straight and your shoulder down.",
                "Don't twist your torso to make the band move.",
            ],
            Video = new("4fM554Org3o"),
        },
        new("band_internal_rotation", "Band Internal Rotation", Shoulders, Band, Isolation)
        {
            Summary = "Rotating your forearm inward across your body against a band, strengthening the rotator cuff muscles that turn the arm in.",
            Steps =
            [
                "Anchor a band at elbow height and stand side-on to it, holding the band in the hand nearer the anchor.",
                "Bend that elbow to 90° and keep it tucked against your side.",
                "Rotate your forearm inward across your stomach without moving your elbow.",
                "Return slowly, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Move slowly in both directions.",
                "Don't let your elbow drift away from your side.",
            ],
            Video = new("-CaOjDo6pIg"),
        },
        new("cable_external_rotation", "Cable External Rotation", Shoulders, Cable, Isolation)
        {
            Summary = "An external rotation against a cable set at elbow height, with even resistance through the whole range for the rotator cuff.",
            Steps =
            [
                "Set a single handle at elbow height and stand side-on to the machine.",
                "Hold the handle in the hand farther from the machine, elbow bent to 90° and pinned to your side.",
                "Rotate your forearm outward as far as you can without moving your elbow.",
                "Return slowly, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Use the lightest plate or two; the rotator cuff is small.",
                "Don't lean or twist away to finish the rep.",
            ],
            Video = new("GxDF2AYsI1Q"),
        },
        new("cable_internal_rotation", "Cable Internal Rotation", Shoulders, Cable, Isolation)
        {
            Summary = "An internal rotation against a cable at elbow height, strengthening the muscles that turn the arm inward.",
            Steps =
            [
                "Set a single handle at elbow height and stand side-on to the machine.",
                "Hold the handle in the hand nearer the machine, elbow bent to 90° and against your side.",
                "Rotate your forearm in across your stomach without moving your elbow.",
                "Return slowly, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your upper arm still; only your forearm swings.",
                "Don't use your chest or torso to pull the handle across.",
            ],
            Video = new("jt5UXsbi0X0"),
        },
        new("side_lying_external_rotation", "Side-Lying External Rotation", Shoulders, Dumbbell, Isolation)
        {
            Summary = "Lying on your side and rotating a light dumbbell upward with your elbow at your side, a classic physio exercise for the rotator cuff.",
            Steps =
            [
                "Lie on your side with your head supported, holding a light dumbbell in your top hand.",
                "Bend your top elbow to 90° with your upper arm along your side and your forearm across your stomach; a folded towel under the elbow helps.",
                "Rotate your forearm up toward the ceiling, keeping your elbow against your side.",
                "Lower it slowly back to your stomach, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Use a very light weight; 1–3 kg is plenty for most people.",
                "Don't roll your body backward to lift the weight higher.",
            ],
            Video = new("v5bPOsQbq7g"),
        },
        new("external_rotation_90_90", "90/90 External Rotation", Shoulders, Band, Isolation)
        {
            Level = Intermediate,
            Summary = "External rotation with your upper arm raised out to shoulder height and elbow at 90°, training the rotator cuff in the overhead position used for throwing and pressing.",
            Steps =
            [
                "Anchor a band in front of you at shoulder height and hold it in one hand.",
                "Raise your upper arm out to the side to shoulder height and bend your elbow to 90°, forearm pointing forward.",
                "Rotate your forearm up and back until it points at the ceiling, keeping your upper arm still.",
                "Return slowly to forearm forward, do all reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your elbow at shoulder height and in line with your body.",
                "Don't arch your lower back to get more range.",
                "Can also be done with a cable or a light dumbbell with your elbow resting on a bench.",
            ],
            Video = new("H1d0tcL-WsI"),
        },
        new("cuban_press", "Cuban Press", Shoulders, Dumbbell, Compound)
        {
            Secondary = [Traps, Triceps],
            Level = Intermediate,
            Summary = "A light dumbbell combo of an upright row, an external rotation and an overhead press, used to strengthen the rotator cuff and upper back.",
            Steps =
            [
                "Stand holding light dumbbells in front of your thighs, palms facing you.",
                "Pull them up, leading with your elbows, until your upper arms are parallel to the floor.",
                "Keeping your elbows where they are, rotate your forearms up until they point at the ceiling.",
                "Press the dumbbells overhead, then reverse each step back to the start.",
            ],
            Tips =
            [
                "Use very light weights and move deliberately through each part.",
                "Don't let your elbows drop as you rotate.",
            ],
            Video = new("zD4Vp44roow"),
        },

        // Scapular health
        new("ytw_raise", "Incline Y-T-W Raise", Shoulders, Dumbbell, Isolation)
        {
            Secondary = [Traps, Back],
            Summary = "Lying face down on an incline bench and raising light dumbbells in Y, T and W shapes, working the rear delts, rotator cuff and mid and lower traps.",
            Steps =
            [
                "Set a bench to about 30–45° and lie face down with very light dumbbells hanging below your shoulders.",
                "Y: with thumbs up, raise your arms forward and out to make a Y, in line with your body.",
                "T: with thumbs up, raise your arms straight out to the sides to make a T.",
                "W: bend your elbows, pull them back toward your ribs and rotate your hands up and back to make a W.",
                "Lower slowly after each one; do your reps of each shape in turn.",
            ],
            Tips =
            [
                "Squeeze your shoulder blades back and down before each raise.",
                "Use 1–4 kg at most; this is about control, not load.",
                "Don't shrug toward your ears or lift your chest off the bench.",
            ],
            Video = new("3wgOMSF9LG8"),
        },
        new("prone_iyt", "Prone I-Y-T Raise", Shoulders, Bodyweight, Isolation)
        {
            Secondary = [Traps, Back],
            Summary = "Lying face down on the floor and lifting your arms in I, Y and T shapes, a no-equipment drill for the lower and mid traps and rear delts.",
            Steps =
            [
                "Lie face down on a mat with your forehead on a small towel and arms by your sides.",
                "I: with arms straight overhead and thumbs up, lift them a few centimetres off the floor and lower.",
                "Y: move your arms out to a Y, thumbs up, lift and lower.",
                "T: move your arms straight out to the sides, thumbs up, lift and lower.",
                "Hold each lift for 1–2 seconds; do your reps of each letter in turn.",
            ],
            Tips =
            [
                "Draw your shoulder blades down and back to start each lift.",
                "Keep your forehead down; don't lift your head or arch your lower back.",
            ],
            Video = new("aFtzZl4ySMo"),
        },
        new("wall_slide", "Wall Slide", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Traps, Chest],
            Category = Mobility,
            Summary = "Sliding your arms up and down a wall with your back against it, training overhead reach and the serratus and lower traps that control your shoulder blades.",
            Steps =
            [
                "Stand with your back, head and backside against a wall, feet a little in front of you.",
                "Place your arms against the wall in a goalpost shape, elbows bent to 90° at shoulder height.",
                "Slide your arms up the wall as far as you can while keeping them, your head and your lower back in contact.",
                "Slide them slowly back down to the goalpost position.",
            ],
            Tips =
            [
                "Keep your ribs down; if your lower back peels off the wall, stop lower.",
                "Facing the wall with forearms on it is an easier version that emphasises the serratus.",
                "Don't force the range; it improves with practice.",
            ],
            Video = new("Eaj_NG5_hIo"),
        },

        // Bodyweight
        new("pike_push_up", "Pike Push-Up", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Triceps, Traps],
            Summary = "A push-up with your hips high in an inverted V, putting your shoulders in an overhead-press position with no equipment.",
            Steps =
            [
                "Start in a push-up position, then walk your feet in and lift your hips until your body forms an upside-down V.",
                "Place your hands shoulder-width apart, arms straight and head between your arms.",
                "Bend your elbows and lower the top of your head toward a point just in front of your hands.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "Keep your hips high so the push stays vertical.",
                "Let your elbows point back at about 45°, not straight out.",
            ],
            Video = new("XckEEwa1BPI"),
        },
        new("elevated_pike_push_up", "Feet-Elevated Pike Push-Up", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Triceps, Traps],
            Level = Intermediate,
            Summary = "A pike push-up with your feet on a box or bench, putting more of your bodyweight on your shoulders as a step toward handstand push-ups.",
            Steps =
            [
                "Place your feet on a box or bench and your hands on the floor, shoulder-width apart.",
                "Walk your hands back and lift your hips until your torso is as close to vertical as you can manage.",
                "Lower the top of your head toward a point just in front of your hands.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "The higher the box and the more vertical your torso, the harder it gets.",
                "Keep your neck neutral and don't drop onto your head.",
            ],
            Video = new("8URA3YSur2M"),
        },
        new("handstand_push_up", "Handstand Push-Up", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Triceps, Traps],
            Level = Advanced,
            Summary = "A full vertical press of your bodyweight from a handstand against a wall, one of the hardest bodyweight shoulder exercises.",
            Steps =
            [
                "Kick or walk up into a handstand facing away from or toward a wall, hands a little wider than shoulder-width and about a hand's length from the wall.",
                "Brace your abs and squeeze your legs together.",
                "Bend your elbows and lower slowly until your head lightly touches the floor or a pad.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "Put a folded mat under your head and never drop onto it.",
                "Your head and hands should form a triangle at the bottom, not a straight line.",
                "Build up with feet-elevated pike push-ups and negatives first.",
            ],
            Video = new("lkPPVyExFpU"),
        },
        new("wall_walk", "Wall Walk", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Triceps, Abs],
            Level = Intermediate,
            Summary = "Walking your feet up a wall and your hands in toward it until you're in a handstand, then walking back down, building shoulder strength and overhead confidence.",
            Steps =
            [
                "Start face down in a push-up position with your feet against the base of a wall.",
                "Walk your feet up the wall while walking your hands back toward it.",
                "Go until your chest is close to the wall and you're nearly vertical, arms straight and shoulders pushed up toward your ears.",
                "Reverse the steps slowly to walk back down to the floor.",
            ],
            Tips =
            [
                "Keep your abs tight and your body straight; don't let your lower back sag.",
                "Only go as far up the wall as you can control.",
            ],
            Video = new("VpuoE246W1Y"),
        },
        new("wall_handstand_hold", "Wall Handstand Hold", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Triceps, Traps, Abs],
            Level = Intermediate,
            Hold = true,
            Summary = "Holding a handstand with your chest or back against a wall, building the shoulder strength and body tension for handstand work.",
            Steps =
            [
                "Walk up the wall chest-first, or kick up back-to-wall, with your hands shoulder-width apart.",
                "Lock your arms straight and push the floor away, shoulders by your ears.",
                "Squeeze your glutes and legs together and keep your ribs tucked.",
                "Hold for time, then come down the way you went up.",
            ],
            Tips =
            [
                "Chest-to-wall teaches a straighter line than back-to-wall.",
                "Spread your fingers and grip the floor for balance.",
                "Learn to bail out safely before holding for long.",
            ],
            Video = new("2v1YDTzMcO8"),
        },
        new("planche_lean", "Planche Lean", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Chest, Triceps, Abs],
            Level = Intermediate,
            Hold = true,
            Summary = "Holding a straight-arm push-up position with your shoulders leaned well forward of your hands, the first step toward a planche.",
            Steps =
            [
                "Start in a push-up position with straight arms and your fingers turned slightly out or back.",
                "Push the floor away so your upper back rounds slightly and your shoulder blades spread.",
                "Lean your shoulders forward past your hands, coming onto your toes, while keeping your arms locked.",
                "Hold the lean for time, then return to a normal push-up position.",
            ],
            Tips =
            [
                "Keep your elbows fully locked; bent arms take the work off the shoulders.",
                "Lean further over time rather than all at once; it loads the wrists and elbows heavily.",
                "Warm up your wrists first.",
            ],
            Video = new("oVpte1edulI"),
        },
        new("tuck_planche", "Tuck Planche", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Chest, Triceps, Abs],
            Level = Advanced,
            Hold = true,
            Summary = "Balancing on straight arms with your knees tucked to your chest and your feet off the floor, an advanced calisthenics hold for the front delts.",
            Steps =
            [
                "Place your hands on the floor or parallettes about shoulder-width apart, arms straight.",
                "Lean your shoulders forward past your hands and round your upper back slightly.",
                "Tuck your knees to your chest and lift your feet off the floor, balancing on your hands.",
                "Hold for time, then lower your feet back down.",
            ],
            Tips =
            [
                "Keep your arms locked and keep pushing the floor away.",
                "Get comfortable holding a deep planche lean before trying it.",
                "Parallettes are easier on the wrists than the floor.",
            ],
            Video = new("7d8fffrFeQA"),
        },
    ];
}
