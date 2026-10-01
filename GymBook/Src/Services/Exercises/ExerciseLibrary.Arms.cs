using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] ArmExercises() =>
    [
        // Barbell and EZ-bar curls
        new("barbell_curl", "Barbell Curl", Biceps, Barbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "The staple biceps builder: stand tall and curl a straight bar from your thighs to your shoulders.",
            Steps =
            [
                "Stand with feet hip-width apart and hold the bar underhand, hands about shoulder-width apart.",
                "Let the bar hang at arm's length against your thighs, elbows by your sides.",
                "Curl the bar up toward your shoulders without letting your elbows drift forward.",
                "Squeeze at the top, then lower it under control until your arms are straight.",
            ],
            Tips =
            [
                "Keep your elbows pinned near your ribs; only your forearms should move.",
                "Don't swing the bar up with your hips or lean back to finish the rep.",
                "Lower slowly; the way down builds as much muscle as the way up.",
            ],
            Video = new("dIAxyJimXnA"),
        },
        new("ez_bar_curl", "EZ-Bar Curl", Biceps, EzBar, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A barbell curl on the angled EZ-bar, which puts the wrists in a more comfortable semi-supinated position.",
            Steps =
            [
                "Hold the EZ-bar on the inner or outer angled grips, palms facing up and slightly in.",
                "Stand tall with the bar at your thighs and your elbows at your sides.",
                "Curl the bar up to shoulder height, keeping your upper arms still.",
                "Lower it slowly back to straight arms.",
            ],
            Tips =
            [
                "Choose this over a straight bar if straight-bar curls bother your wrists or elbows.",
                "Don't let your shoulders roll forward at the top of the rep.",
            ],
            Video = new("OgO6cWoezNc"),
        },
        new("drag_curl", "Drag Curl", Biceps, Barbell, Isolation)
        {
            Secondary = [Forearms],
            Level = Intermediate,
            Summary = "A barbell curl where you drag the bar up your body with the elbows travelling back, which takes the front delts out and keeps tension on the biceps.",
            Steps =
            [
                "Hold a barbell underhand at shoulder width, resting against your thighs.",
                "Pull your elbows back behind your body as you curl, keeping the bar in contact with your torso.",
                "Drag the bar up to your lower chest, squeezing the biceps hard.",
                "Lower it along the same path back to your thighs.",
            ],
            Tips =
            [
                "Think of pulling the elbows back, not lifting the hands forward.",
                "Use less weight than a normal curl; the range is shorter but harder.",
                "Don't shrug or lean back to get the bar up.",
            ],
            Video = new("LMdNTHH6G8I"),
        },
        new("barbell_21s", "Barbell 21s", Biceps, Barbell, Isolation)
        {
            Secondary = [Forearms],
            Level = Intermediate,
            Summary = "A barbell curl set of 21 reps: 7 partials in the bottom half, 7 in the top half, then 7 full reps, for a big biceps pump.",
            Steps =
            [
                "Stand holding a barbell with an underhand, shoulder-width grip, arms straight.",
                "Do 7 reps curling from the bottom to halfway, forearms parallel to the floor.",
                "Without resting, do 7 reps from halfway to the top.",
                "Finish with 7 full-range curls, then lower the bar.",
            ],
            Tips =
            [
                "Use about 60–70% of your normal curl weight.",
                "Keep your elbows pinned at your sides through all 21 reps.",
                "Don't swing your hips to finish the last full reps.",
            ],
            Video = new("GNO4OtYoCYk", 541, 612),
        },
        new("cheat_curl", "Cheat Curl", Biceps, Barbell, Isolation)
        {
            Secondary = [Forearms],
            Level = Intermediate,
            Summary = "A heavy barbell curl that uses a controlled hip and torso swing to get the bar up, then fights the lowering slowly to overload the biceps.",
            Steps =
            [
                "Stand holding a barbell with an underhand, shoulder-width grip, heavier than you can curl strictly.",
                "Hinge slightly forward, then drive your hips through and lean back a little to start the bar moving.",
                "Curl the bar the rest of the way to your shoulders.",
                "Lower it slowly, taking 2–3 seconds, with your torso upright and elbows at your sides.",
            ],
            Tips =
            [
                "Use just enough body English to get past the sticking point; the biceps should still do most of the work.",
                "The slow lowering is the point; don't let the bar drop.",
                "Keep your back neutral and your abs braced; skip it if your lower back is sore.",
            ],
            Video = new("GNO4OtYoCYk", 705, 750),
        },
        new("preacher_curl", "EZ-Bar Preacher Curl", Biceps, EzBar, Isolation)
        {
            Summary = "A strict curl with your upper arms braced on a preacher bench, which stops any swinging and works the biceps hard in the stretched position.",
            Steps =
            [
                "Sit at a preacher bench with your armpits snug against the top of the pad.",
                "Hold an EZ-bar underhand with the backs of your upper arms flat on the pad.",
                "Curl the bar up until your forearms are nearly vertical.",
                "Lower it slowly until your arms are almost straight.",
            ],
            Tips =
            [
                "Control the bottom of the rep; this is where the biceps and elbow are most vulnerable.",
                "Don't lift your elbows or upper arms off the pad to finish the curl.",
                "Stop just short of locking out hard at the bottom if your elbows feel strained.",
            ],
            Video = new("sxA__DoLsgo"),
        },

        // Dumbbell curls
        new("db_curl", "Dumbbell Curl", Biceps, Dumbbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "Curl a pair of dumbbells, together or alternating, turning the palms up as they rise.",
            Steps =
            [
                "Stand tall with a dumbbell in each hand, arms hanging and palms facing your thighs.",
                "Curl the weights up, rotating your palms to face up as they pass your thighs.",
                "Squeeze at shoulder height with your elbows still at your sides.",
                "Lower slowly, turning the palms back in at the bottom.",
            ],
            Tips =
            [
                "Turning the palm up (supination) is part of the biceps' job, so make the twist deliberate.",
                "Don't swing the dumbbells or let your elbows travel forward.",
            ],
            Video = new("plU7Ca7Fke8"),
        },
        new("hammer_curl", "Hammer Curl", Biceps, Dumbbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A dumbbell curl with a neutral, thumbs-up grip that shifts the work toward the brachialis and brachioradialis.",
            Steps =
            [
                "Stand with a dumbbell in each hand, palms facing your thighs.",
                "Keeping the thumbs-up grip, curl the dumbbells toward your shoulders.",
                "Pause at the top with your elbows still at your sides.",
                "Lower them under control to straight arms.",
            ],
            Tips =
            [
                "Keep your wrists straight and the grip neutral the whole way.",
                "Don't swing your torso; go lighter if you need momentum.",
            ],
            Video = new("W0xVe5jdnOU"),
        },
        new("cross_body_hammer_curl", "Cross-Body Hammer Curl", Biceps, Dumbbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A hammer curl where each dumbbell travels across your body toward the opposite shoulder, hitting the brachialis and brachioradialis hard.",
            Steps =
            [
                "Stand with a dumbbell in each hand, palms facing your thighs.",
                "Curl one dumbbell up and across your body toward the opposite shoulder, thumb up.",
                "Pause at the top, then lower it back to your side.",
                "Repeat with the other arm and keep alternating.",
            ],
            Tips =
            [
                "Keep the upper arm close to your body; only the forearm crosses over.",
                "Don't twist your torso to help the weight across.",
            ],
            Video = new("qmQkt1Y-FX8"),
        },
        new("incline_db_curl", "Incline Dumbbell Curl", Biceps, Dumbbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "Curls lying back on an incline bench so your arms hang behind your body, loading the biceps in a deep stretch.",
            Steps =
            [
                "Set a bench to about 45–60° and sit back with a dumbbell in each hand.",
                "Let your arms hang straight down, palms facing forward.",
                "Curl the dumbbells up without letting your elbows move forward.",
                "Lower them slowly all the way back to a full stretch.",
            ],
            Tips =
            [
                "Keep your head and shoulders against the bench.",
                "Use lighter weights than standing curls; the stretched start is much harder.",
                "Don't shorten the bottom of the rep; the stretch is the point.",
            ],
            Video = new("V8H4k8lquiU"),
        },
        new("concentration_curl", "Concentration Curl", Biceps, Dumbbell, Isolation)
        {
            Summary = "A seated one-arm curl with your elbow braced against your inner thigh, making it nearly impossible to cheat.",
            Steps =
            [
                "Sit on the end of a bench with your feet wide and a dumbbell in one hand.",
                "Lean forward and rest the back of your upper arm against the inside of the same-side thigh.",
                "Curl the dumbbell up toward your shoulder, palm facing up.",
                "Squeeze hard at the top, then lower slowly to a straight arm.",
            ],
            Tips =
            [
                "Keep the elbow fixed against your thigh; don't use the leg to push the arm.",
                "Don't lift your torso to help the weight up.",
            ],
            Video = new("Bv1iH7mLfb4"),
        },
        new("spider_curl", "Spider Curl", Biceps, Dumbbell, Isolation)
        {
            Summary = "Curls lying chest-down on an incline bench with your arms hanging straight down, which keeps tension on the biceps at the top of the rep.",
            Steps =
            [
                "Set a bench to about 45° and lie face down on it, chest on the pad and chin over the top.",
                "Let your arms hang straight down with a dumbbell in each hand, palms forward.",
                "Curl the dumbbells up as high as you can without moving your upper arms.",
                "Squeeze, then lower slowly back to straight arms.",
            ],
            Tips =
            [
                "Keep your upper arms vertical throughout.",
                "Don't swing or push your chest off the bench to start the rep.",
            ],
            Video = new("kF6UsS3ZXo0"),
        },
        new("zottman_curl", "Zottman Curl", Biceps, Dumbbell, Isolation)
        {
            Secondary = [Forearms],
            Level = Intermediate,
            Summary = "Curl up with palms up, rotate to palms down at the top, then lower slowly overhand: biceps on the way up, forearms on the way down.",
            Steps =
            [
                "Stand with a dumbbell in each hand, palms facing forward.",
                "Curl the dumbbells up to shoulder height.",
                "At the top, rotate your hands so your palms face down.",
                "Lower the dumbbells slowly with the overhand grip, then turn the palms forward again at the bottom.",
            ],
            Tips =
            [
                "Take 3–4 seconds on the overhand lowering; that's where the forearm work is.",
                "Pick a weight you can control on the way down, not one you can only curl.",
            ],
            Video = new("kc-LGcbozhI"),
        },
        new("waiter_curl", "Waiter Curl", Biceps, Dumbbell, Isolation)
        {
            Summary = "Curling one dumbbell held vertically with both palms flat under the top plate, like a waiter carrying a tray, to keep the biceps under tension.",
            Steps =
            [
                "Stand holding a dumbbell upright in front of your thighs, both palms flat under the top head, fingers interlaced or overlapping.",
                "Keep your elbows at your sides and your wrists flat.",
                "Curl the dumbbell up toward your chin, keeping the handle vertical.",
                "Lower it slowly to straight arms.",
            ],
            Tips =
            [
                "Keep the dumbbell level so it doesn't tip toward you.",
                "Don't let your elbows drift forward to finish the rep.",
                "Use a moderate weight; the open-hand grip limits how much you can hold safely.",
            ],
            Video = new("8dbPdH0pIBI", 0, 65.439),
        },
        new("db_preacher_curl", "Dumbbell Preacher Curl", Biceps, Dumbbell, Isolation)
        {
            Summary = "A one-arm preacher curl with a dumbbell, letting you train each arm on its own with the upper arm braced.",
            Steps =
            [
                "Sit or stand at a preacher bench and lay the back of one upper arm flat on the pad.",
                "Hold a dumbbell underhand with that arm nearly straight.",
                "Curl the dumbbell up until your forearm is close to vertical.",
                "Lower it slowly until the arm is almost straight.",
            ],
            Tips =
            [
                "The steep back side of the pad gives a harder top position; the sloped side a harder stretch.",
                "Don't let the weight drop into the bottom of the rep.",
            ],
            Video = new("a-_aey-v-8I"),
        },

        // Cable, machine and band curls
        new("cable_curl", "Cable Curl", Biceps, Cable, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A standing curl on a low pulley with a straight or EZ handle, which keeps steady tension on the biceps through the whole rep.",
            Steps =
            [
                "Attach a straight or EZ bar to a low pulley and hold it underhand at shoulder width.",
                "Stand a step back from the stack with your elbows by your sides.",
                "Curl the handle up toward your shoulders.",
                "Lower it under control until your arms are straight.",
            ],
            Tips =
            [
                "Keep your elbows still; don't let them drift forward as you curl.",
                "Don't lean back to finish the rep.",
            ],
            Video = new("yGdzi_CciKY"),
        },
        new("rope_hammer_curl", "Rope Hammer Curl", Biceps, Cable, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A hammer curl on a low pulley with a rope, working the brachialis and brachioradialis with constant cable tension.",
            Steps =
            [
                "Attach a rope to a low pulley and hold one end in each hand, thumbs up.",
                "Stand tall with your elbows at your sides and arms straight.",
                "Curl the rope up toward your shoulders, keeping the neutral grip.",
                "Lower it slowly back to straight arms.",
            ],
            Tips =
            [
                "Keep your upper arms still and your wrists straight.",
                "Don't rock your body to start the rep.",
            ],
            Video = new("hdXgJwbaIYg"),
        },
        new("cable_preacher_curl", "Cable Preacher Curl", Biceps, Cable, Isolation)
        {
            Summary = "A preacher curl done with a low cable instead of free weights, so the biceps stay loaded at the bottom and top of the curl.",
            Steps =
            [
                "Place a preacher bench in front of a low pulley with a straight or EZ bar attached.",
                "Sit with your armpits over the top of the pad and the backs of your arms flat on it, holding the bar with an underhand grip.",
                "Curl the bar up until your forearms are close to vertical.",
                "Lower it slowly until your arms are almost straight.",
            ],
            Tips =
            [
                "Keep your upper arms on the pad the whole time.",
                "Don't lift your hips or lean back to finish the rep.",
                "Control the bottom; don't let the cable snap your elbows straight.",
            ],
            Video = new("Iwqi47sPS2M"),
        },
        new("lying_cable_curl", "Lying Cable Curl", Biceps, Cable, Isolation)
        {
            Summary = "A cable curl done lying on your back on the floor or a bench with your feet toward the pulley, which locks your body in place and stops any swinging.",
            Steps =
            [
                "Attach a straight or EZ bar to a low pulley and lie on your back with your feet toward the stack.",
                "Hold the bar with an underhand grip, arms straight and upper arms by your sides, pointing at the pulley.",
                "Curl the bar toward your chest, keeping your upper arms still.",
                "Lower it slowly until your arms are straight.",
            ],
            Tips =
            [
                "Keep your elbows on or just off the floor; don't let them drift up toward your face.",
                "Squeeze at the top for a second.",
                "Don't lift your head or shoulders to help the bar up.",
            ],
            Video = new("ywfiZIv9_L4"),
        },
        new("bayesian_curl", "Bayesian Cable Curl", Biceps, Cable, Isolation)
        {
            Summary = "A one-arm curl facing away from a low pulley so the cable pulls your arm behind you, loading the biceps in a long stretch.",
            Steps =
            [
                "Set a D-handle on a low pulley, hold it in one hand and face away from the stack.",
                "Step forward until your arm is pulled slightly behind your body, palm facing forward.",
                "Curl the handle up and forward without letting your elbow swing forward.",
                "Lower slowly until your arm is straight and stretched behind you.",
            ],
            Tips =
            [
                "A staggered stance keeps you stable.",
                "Keep your upper arm still and behind your torso; don't let it come forward to cheat the rep.",
            ],
            Video = new("4O24gFksZ04"),
        },
        new("high_cable_curl", "Overhead Cable Curl", Biceps, Cable, Isolation)
        {
            Summary = "Curls with both arms held out to the sides at shoulder height between two high pulleys, like a front double-biceps pose.",
            Steps =
            [
                "Set both pulleys at shoulder height or higher and stand in the middle holding a D-handle in each hand.",
                "Raise your arms out to the sides, palms facing up, until they're level with your shoulders.",
                "Curl the handles toward your ears, keeping your upper arms still.",
                "Extend your arms back out slowly.",
            ],
            Tips =
            [
                "Keep your elbows at shoulder height the whole set.",
                "Don't let your shoulders shrug up toward your ears.",
            ],
            Video = new("S7IBBKqWqog"),
        },
        new("machine_preacher_curl", "Machine Preacher Curl", Biceps, Machine, Isolation)
        {
            Summary = "A preacher curl on a selectorised or plate-loaded machine, which fixes the path so you can push close to failure safely.",
            Steps =
            [
                "Set the seat so your armpits rest on the top of the pad and your elbows line up with the machine's pivot.",
                "Grip the handles with your upper arms flat on the pad.",
                "Curl the handles up until your forearms are nearly vertical.",
                "Lower them slowly until your arms are almost straight.",
            ],
            Tips =
            [
                "Lining your elbows up with the pivot makes the resistance feel even through the rep.",
                "Don't lift off the seat or pad to get the last rep.",
            ],
            Video = new("Ja6ZlIDONac"),
        },
        new("band_curl", "Band Curl", Biceps, Band, Isolation)
        {
            Summary = "Curls with a resistance band anchored under your feet; the band gets harder as you near the top.",
            Steps =
            [
                "Stand on the middle of a band with feet hip-width apart and hold the ends or handles underhand.",
                "Start with your arms straight and elbows at your sides.",
                "Curl your hands up toward your shoulders.",
                "Lower slowly, resisting the band, back to straight arms.",
            ],
            Tips =
            [
                "Stand wider or choke up on the band to make it harder.",
                "Don't let the band snap your arms down; control the return.",
            ],
            Video = new("AaA7Yj3zHiU"),
        },
        new("kb_curl", "Kettlebell Curl", Biceps, Kettlebell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A biceps curl holding a kettlebell by the handle (or the horns), where the offset weight also challenges grip and the forearms.",
            Steps =
            [
                "Stand tall holding a kettlebell by the handle in one hand, arm straight, or by the horns with both hands.",
                "Keep your elbows by your sides.",
                "Curl the kettlebell up toward your shoulder.",
                "Lower it slowly to straight arms.",
            ],
            Tips =
            [
                "Don't swing; keep the elbows still.",
                "Holding it by the horns with both hands lets you go heavier.",
            ],
            Video = new("8AFAuaTXbWw"),
        },
        new("suspension_biceps_curl", "Suspension Trainer Biceps Curl", Biceps, Other, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A bodyweight curl on a suspension trainer such as a TRX: lean back and curl your body toward your hands.",
            Steps =
            [
                "Face the anchor and hold the handles palms up with your arms straight in front of you at shoulder height.",
                "Walk your feet forward and lean back, body straight from head to heels.",
                "Curl your hands toward your forehead, pulling your body up, with your elbows staying high.",
                "Lower yourself slowly until your arms are straight.",
            ],
            Tips =
            [
                "The more upright you stand, the easier it gets; walk your feet forward to make it harder.",
                "Keep your hips up and your body in one line; don't sag.",
                "Don't let your elbows drop; keep them pointing at the anchor.",
            ],
            Video = new("BV3l5Q5t9fo"),
        },
        new("bodyweight_biceps_curl", "Bodyweight Biceps Curl", Biceps, Bodyweight, Isolation)
        {
            Level = Intermediate,
            Summary = "A biceps curl using your body weight: hanging under a low bar with an underhand grip and curling your head toward your hands.",
            Steps =
            [
                "Set a bar at about hip height and hold it underhand, hands narrow.",
                "Walk your feet forward and lean back with straight arms and a straight body.",
                "Curl your forehead toward the bar by bending only at the elbows.",
                "Lower under control to straight arms.",
            ],
            Tips =
            [
                "Keep the elbows in front, not pulling back like a row.",
                "Walk your feet back (more upright) to make it easier.",
            ],
            Video = new("vByJfI8acOw"),
        },

        // Triceps: pushdowns
        new("triceps_pushdown", "Triceps Pushdown", Triceps, Cable, Isolation)
        {
            Summary = "The go-to triceps exercise: push a straight or V-bar down on a high pulley until your arms are straight.",
            Steps =
            [
                "Attach a straight bar or V-bar to a high pulley and grip it overhand at about shoulder width.",
                "Stand close to the stack with your elbows tucked at your sides, forearms roughly parallel to the floor.",
                "Push the bar down until your arms are fully straight.",
                "Let it rise back under control until your forearms are just above parallel.",
            ],
            Tips =
            [
                "Keep your elbows glued to your sides; only the forearms move.",
                "Don't lean over the bar and push with your body weight.",
                "Lock out fully and squeeze the triceps at the bottom.",
            ],
            Video = new("e0g_ZbWlWRI"),
        },
        new("rope_pushdown", "Rope Pushdown", Triceps, Cable, Isolation)
        {
            Summary = "A triceps pushdown with a rope, spreading the ends apart at the bottom for a harder lockout.",
            Steps =
            [
                "Attach a rope to a high pulley and hold one end in each hand, thumbs up.",
                "Stand tall with your elbows at your sides and forearms about parallel to the floor.",
                "Push the rope down and pull the ends apart as your arms straighten.",
                "Let the rope rise back under control.",
            ],
            Tips =
            [
                "Finish with your hands beside your thighs, not in front of them.",
                "Don't let your elbows flare or drift forward on the way up.",
            ],
            Video = new("-xa-6cQaZKY"),
        },
        new("single_arm_pushdown", "Single-Arm Cable Pushdown", Triceps, Cable, Isolation)
        {
            Summary = "A one-arm pushdown with a D-handle on a high pulley, letting you train each side on its own.",
            Steps =
            [
                "Attach a D-handle to a high pulley and grip it with one hand.",
                "Stand side-on or facing the stack with that elbow pinned to your side.",
                "Push the handle down until your arm is straight.",
                "Let it rise slowly until your forearm is just above parallel.",
            ],
            Tips =
            [
                "Use an overhand or neutral grip, whichever feels better on your elbow.",
                "Don't rotate your torso to help the weight down.",
            ],
            Video = new("oxXEsQgIUrM"),
        },
        new("cable_cross_body_triceps_extension", "Cable Cross-Body Triceps Extension", Triceps, Cable, Isolation)
        {
            Summary = "A single-arm triceps extension pulling a cable from the opposite side across the body, for a long range of motion.",
            Steps =
            [
                "Set a cable at about shoulder height and stand side-on, holding the cable (no handle or a single handle) with the far hand.",
                "Start with the hand near the opposite shoulder and the elbow pointing toward the stack.",
                "Extend the arm out across your body until it's straight.",
                "Return slowly to the bent position.",
            ],
            Tips =
            [
                "Keep the upper arm still; only the forearm moves.",
                "Use a light weight and full range rather than heavy jerky reps.",
            ],
            Video = new("xzs9RTtt5y8"),
        },
        new("reverse_grip_pushdown", "Reverse-Grip Pushdown", Triceps, Cable, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A pushdown with an underhand grip on a straight or EZ bar, which makes it harder to cheat with the shoulders.",
            Steps =
            [
                "Attach a straight or EZ bar to a high pulley and grip it underhand at shoulder width.",
                "Stand close with your elbows at your sides and forearms about parallel to the floor.",
                "Push the bar down until your arms are straight.",
                "Let it rise back under control.",
            ],
            Tips =
            [
                "Use noticeably less weight than an overhand pushdown.",
                "Keep your wrists straight; don't let them bend back under the load.",
            ],
            Video = new("Xw3MsQTwH_Q"),
        },
        new("band_pushdown", "Band Pushdown", Triceps, Band, Isolation)
        {
            Summary = "A triceps pushdown with a resistance band anchored overhead, good for home workouts and warm-ups.",
            Steps =
            [
                "Loop a band over a door anchor, pull-up bar or high hook and hold it with both hands.",
                "Stand tall with your elbows at your sides and forearms about parallel to the floor.",
                "Push your hands down until your arms are straight.",
                "Let them rise back slowly against the band.",
            ],
            Tips =
            [
                "Step back from the anchor to add tension.",
                "Don't let your elbows lift or flare as your hands come up.",
            ],
            Video = new("xW8L2POShSA"),
        },

        // Triceps: overhead extensions
        new("overhead_triceps_extension", "Overhead Cable Triceps Extension", Triceps, Cable, Isolation)
        {
            Summary = "Face away from a cable and extend a rope overhead, training the triceps' long head in a deep stretch.",
            Steps =
            [
                "Attach a rope to a pulley at about head height, grip it and turn away from the stack.",
                "Step forward into a split stance, lean slightly forward and hold the rope behind your head, elbows bent and pointing forward.",
                "Extend your arms forward and up until they're straight.",
                "Bend your elbows slowly to bring the rope back behind your head.",
            ],
            Tips =
            [
                "Keep your elbows close to your head; don't let them flare out.",
                "Brace your core so your lower back doesn't arch.",
                "Get the full stretch at the bottom; that's where this exercise pays off.",
            ],
            Video = new("U45qzGXSpHU"),
        },
        new("cable_single_arm_overhead_extension", "Single-Arm Cable Overhead Extension", Triceps, Cable, Isolation)
        {
            Summary = "A one-arm overhead extension on a cable, letting you stretch and train each triceps on its own.",
            Steps =
            [
                "Set a D-handle on a low pulley, grip it and turn away from the stack.",
                "Raise the handle behind your head with your elbow bent and pointing up.",
                "Extend your arm straight up overhead.",
                "Bend the elbow slowly to lower the handle behind your head.",
            ],
            Tips =
            [
                "Keep your upper arm still and close to your head.",
                "Don't arch your back to push the weight up.",
            ],
            Video = new("TdU3PyRqoU8"),
        },
        new("db_overhead_extension", "Dumbbell Overhead Extension", Triceps, Dumbbell, Isolation)
        {
            Summary = "Hold one dumbbell overhead with both hands and lower it behind your head, stretching the triceps' long head.",
            Steps =
            [
                "Sit on a bench with back support and hold one dumbbell by the top plate with both hands.",
                "Press it overhead until your arms are straight.",
                "Bend your elbows to lower the dumbbell behind your head.",
                "Extend your arms back to the top.",
            ],
            Tips =
            [
                "Keep your elbows pointing up and fairly close together.",
                "Brace your abs; don't arch your lower back.",
                "Lower slowly and keep a firm grip; the weight is passing behind your head.",
            ],
            Video = new("xbNm9fRoUTw"),
        },
        new("single_arm_db_overhead_extension", "Single-Arm Dumbbell Overhead Extension", Triceps, Dumbbell, Isolation)
        {
            Summary = "A one-arm overhead extension with a light dumbbell, lowering it behind your head and pressing it back up.",
            Steps =
            [
                "Sit or stand tall and hold a dumbbell overhead in one hand, arm straight.",
                "Bend your elbow to lower the dumbbell behind your head toward the opposite shoulder.",
                "Extend your arm back to straight overhead.",
                "Finish all reps, then switch arms.",
            ],
            Tips =
            [
                "Support the working elbow with your free hand if it wants to drift.",
                "Don't lean to the side to help the weight up.",
            ],
            Video = new("_w3ggqafzqU"),
        },
        new("ez_bar_overhead_extension", "EZ-Bar Overhead Extension", Triceps, EzBar, Isolation)
        {
            Level = Intermediate,
            Summary = "A seated overhead triceps extension with an EZ-bar, allowing heavier loading than a single dumbbell.",
            Steps =
            [
                "Sit on a bench with back support and hold an EZ-bar overhand on the inner angled grips.",
                "Press it overhead until your arms are straight.",
                "Bend your elbows to lower the bar behind your head.",
                "Extend your arms back to the top.",
            ],
            Tips =
            [
                "Keep your elbows pointing up; let them only flare slightly.",
                "Don't arch your lower back to press the bar.",
            ],
            Video = new("IdZ7HXnatko"),
        },
        new("band_overhead_extension", "Band Overhead Triceps Extension", Triceps, Band, Isolation)
        {
            Summary = "An overhead triceps extension with a resistance band anchored under your foot or low behind you.",
            Steps =
            [
                "Anchor a band low behind you or under your back foot in a split stance.",
                "Hold the band with both hands behind your head, elbows bent and pointing up.",
                "Extend your arms overhead until they're straight.",
                "Bend your elbows slowly to return behind your head.",
            ],
            Tips =
            [
                "Keep your elbows close to your head.",
                "Don't let your ribs flare or your back arch as you extend.",
            ],
            Video = new("Ewqe_z1HKno"),
        },

        // Triceps: lying extensions and presses
        new("skull_crusher", "Skull Crusher", Triceps, EzBar, Isolation)
        {
            Level = Intermediate,
            Summary = "A lying triceps extension: on a flat bench, lower an EZ-bar toward your forehead by bending the elbows, then extend.",
            Steps =
            [
                "Lie on a flat bench and hold an EZ-bar overhand above your chest, arms straight.",
                "Tilt your arms slightly back toward your head.",
                "Bend your elbows to lower the bar to your forehead or just behind your head.",
                "Extend your arms to bring the bar back up.",
            ],
            Tips =
            [
                "Keep your elbows pointing at the ceiling, not flaring out.",
                "Lowering behind the head gives more stretch and is easier on the elbows.",
                "Control the bar; don't let it drop toward your face.",
            ],
            Video = new("S3xRQfb4Alo"),
        },
        new("db_skull_crusher", "Dumbbell Skull Crusher", Triceps, Dumbbell, Isolation)
        {
            Summary = "A lying triceps extension with a dumbbell in each hand and palms facing in, often easier on the wrists and elbows than a bar.",
            Steps =
            [
                "Lie on a flat bench with a dumbbell in each hand, arms straight above your chest, palms facing each other.",
                "Bend your elbows to lower the dumbbells beside your head.",
                "Pause just above the bench.",
                "Extend your arms to press the dumbbells back up.",
            ],
            Tips =
            [
                "Keep your upper arms still and roughly vertical.",
                "Don't let the dumbbells drift toward your face or the elbows flare out.",
            ],
            Video = new("P4H5p4HoJeI"),
        },
        new("bodyweight_triceps_extension", "Bodyweight Triceps Extension", Triceps, Bodyweight, Isolation)
        {
            Secondary = [Abs],
            Level = Intermediate,
            Summary = "A bodyweight skull crusher: lean onto a bar or bench edge and bend at the elbows to lower your head below your hands.",
            Steps =
            [
                "Hold a low bar, bench edge or Smith bar overhand with your hands shoulder-width apart.",
                "Walk your feet back into a straight plank with your arms straight.",
                "Bend your elbows to lower your head under the bar, keeping your body rigid.",
                "Push through your palms to straighten your arms and return.",
            ],
            Tips =
            [
                "The lower the bar, the harder the exercise; start high.",
                "Keep your hips in line with your body; don't pike up.",
                "Don't let your elbows flare out to the sides.",
            ],
            Video = new("toGzo1GAJ1A"),
        },
        new("close_grip_bench", "Close-Grip Bench Press", Triceps, Barbell, Compound)
        {
            Secondary = [Chest, Shoulders],
            Level = Intermediate,
            Summary = "A bench press with hands about shoulder-width and elbows tucked, shifting the work from the chest to the triceps.",
            Steps =
            [
                "Lie on a flat bench with your feet flat and shoulder blades pulled back.",
                "Grip the bar about shoulder-width apart and unrack it over your shoulders.",
                "Lower it to your lower chest with your elbows tucked close to your sides.",
                "Press it back up until your arms are straight.",
            ],
            Tips =
            [
                "Shoulder-width is close enough; a very narrow grip strains the wrists.",
                "Don't let your elbows flare out on the way down.",
                "Use a spotter or safety arms when going heavy.",
            ],
            Video = new("FiQUzPtS90E"),
        },
        new("jm_press", "JM Press", Triceps, Barbell, Compound)
        {
            Secondary = [Chest, Shoulders],
            Level = Advanced,
            Summary = "A powerlifting hybrid of a close-grip bench press and a skull crusher, lowering the bar toward your chin with the elbows forward.",
            Steps =
            [
                "Lie on a flat bench and hold the bar slightly narrower than shoulder width, arms straight over your upper chest.",
                "Lower the bar toward your chin by bending your elbows, letting them travel forward toward your feet.",
                "Stop with the bar a few inches above your chin and your forearms pressed against your biceps.",
                "Press the bar back up and slightly toward your feet to straight arms.",
            ],
            Tips =
            [
                "Start light; the groove takes practice and the elbows take a lot of load.",
                "Don't let the bar drift down to your chest; then it's just a close-grip bench press.",
                "Use safety arms or a spotter.",
            ],
            Video = new("xvSYujmY_EY"),
        },
        new("california_press", "California Press", Triceps, Barbell, Compound)
        {
            Secondary = [Chest, Shoulders],
            Level = Advanced,
            Summary = "An old-school bench hybrid that starts as a skull crusher and finishes as a close-grip press, loading the triceps through elbow and shoulder movement.",
            Steps =
            [
                "Lie on a flat bench holding the bar with a close, shoulder-width grip, arms straight over your chest.",
                "Bend your elbows to lower the bar toward your neck, keeping your elbows pointed up like a skull crusher.",
                "About halfway down, let your elbows drop toward your sides and lower the bar to your upper chest.",
                "Press the bar back up to straight arms in one smooth movement.",
            ],
            Tips =
            [
                "Start light; it takes a few sessions to find the groove.",
                "Keep your elbows tucked, not flared, as the bar nears your chest.",
                "Use safety arms or a spotter.",
            ],
            Video = new("46irCNOOB5M"),
        },
        new("tate_press", "Tate Press", Triceps, Dumbbell, Isolation)
        {
            Level = Intermediate,
            Summary = "A lying dumbbell triceps extension where the elbows flare out and the dumbbells lower to touch your chest.",
            Steps =
            [
                "Lie on a flat bench and press two dumbbells over your chest, palms facing your feet and the ends touching.",
                "Bend your elbows out to the sides, lowering the inner ends of the dumbbells to your chest.",
                "Keep your upper arms still as you lower.",
                "Extend your elbows to press the dumbbells back up.",
            ],
            Tips =
            [
                "Use light dumbbells; this is a squeeze for the triceps, not a press.",
                "Don't let your upper arms move; only the elbows bend.",
            ],
            Video = new("cCSxFRaN2Kg"),
        },

        // Triceps: kickbacks
        new("db_kickback", "Dumbbell Triceps Kickback", Triceps, Dumbbell, Isolation)
        {
            Summary = "Hinge forward with your upper arm parallel to the floor and straighten your elbow to kick a dumbbell back.",
            Steps =
            [
                "Brace one hand and knee on a bench, or hinge forward with a flat back, dumbbell in the other hand.",
                "Lift your upper arm until it's parallel to the floor and tight to your side, elbow bent at 90°.",
                "Straighten your arm back until it's fully locked out.",
                "Bend the elbow slowly back to 90°.",
            ],
            Tips =
            [
                "Keep your upper arm still and high; if it drops, the weight is too heavy.",
                "Don't swing the dumbbell; pause briefly at lockout.",
            ],
            Video = new("792uGH9RgfU"),
        },
        new("cable_triceps_kickback", "Cable Triceps Kickback", Triceps, Cable, Isolation)
        {
            Summary = "A triceps kickback on a low pulley, which keeps tension on the muscle at full lockout where dumbbells go light.",
            Steps =
            [
                "Set a low pulley with no handle or a D-handle and hold it in one hand.",
                "Hinge forward and bring your upper arm parallel to the floor, elbow bent.",
                "Straighten your arm back until it's fully locked out.",
                "Return slowly to the bent position.",
            ],
            Tips =
            [
                "Keep your upper arm pinned to your side throughout.",
                "Don't rotate your torso to help the cable back.",
            ],
            Video = new("ZvF4Oi_6Vtg"),
        },

        // Triceps: machines, dips and push-ups
        new("machine_triceps_extension", "Machine Triceps Extension", Triceps, Machine, Isolation)
        {
            Summary = "A seated triceps extension machine with the upper arms braced on a pad, an easy way to load the triceps safely.",
            Steps =
            [
                "Adjust the seat so your elbows line up with the machine's pivot and rest on the pad.",
                "Grip the handles with your elbows bent.",
                "Push the handles down until your arms are straight.",
                "Return slowly until your forearms are about vertical.",
            ],
            Tips =
            [
                "Keep your elbows on the pad and your back against the seat.",
                "Don't let the stack slam between reps.",
            ],
            Video = new("Bx8ga1BLHLE"),
        },
        new("machine_dip", "Seated Dip Machine", Triceps, Machine, Compound)
        {
            Secondary = [Chest, Shoulders],
            Summary = "A seated machine that mimics a dip, pressing handles down beside your hips.",
            Steps =
            [
                "Sit in the machine and fasten the belt or pad if it has one.",
                "Grip the handles beside you with your elbows bent and pointing back.",
                "Press the handles down until your arms are straight.",
                "Let them rise slowly until your elbows are at about 90°.",
            ],
            Tips =
            [
                "Keep your chest up and shoulders down, away from your ears.",
                "Don't let your shoulders roll forward at the top of the rep.",
            ],
            Video = new("pMarNxAvHPc"),
        },
        new("triceps_dip", "Triceps Dip", Triceps, Bodyweight, Compound)
        {
            Secondary = [Chest, Shoulders],
            Level = Intermediate,
            Summary = "A dip on parallel bars with an upright torso and elbows tucked, keeping the emphasis on the triceps rather than the chest.",
            Steps =
            [
                "Support yourself on parallel bars with your arms straight and your body upright.",
                "Bend your elbows to lower yourself, keeping your torso vertical and elbows pointing back.",
                "Stop when your upper arms are about parallel to the floor.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "Stay upright; leaning forward turns it into a chest dip.",
                "Don't sink deeper than your shoulders tolerate; stop at about 90° at the elbow.",
                "Use an assisted dip machine or band if you can't yet do clean reps.",
            ],
            Video = new("sg3PTIA3rXI"),
        },
        new("bench_dip", "Bench Dip", Triceps, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Chest],
            Summary = "A dip with your hands on a bench behind you and your feet on the floor, an easy entry point to triceps dips.",
            Steps =
            [
                "Sit on the edge of a bench and place your hands beside your hips, fingers forward.",
                "Slide your hips off the bench with your legs out in front, knees bent or straight.",
                "Bend your elbows to lower your hips toward the floor, keeping your back close to the bench.",
                "Press through your palms to straighten your arms.",
            ],
            Tips =
            [
                "Bend your knees to make it easier, straighten them or raise your feet to make it harder.",
                "Don't go so low that your shoulders roll forward; stop with your upper arms near parallel.",
            ],
            Video = new("em94npR71nk"),
        },
        new("diamond_push_up", "Diamond Push-Up", Triceps, Bodyweight, Compound)
        {
            Secondary = [Chest, Shoulders],
            Level = Intermediate,
            Summary = "A push-up with your hands together under your chest, thumbs and index fingers forming a diamond, to load the triceps.",
            Steps =
            [
                "Get into a push-up position with your hands together under your chest, forming a diamond with your fingers.",
                "Keep your body straight from head to heels.",
                "Lower your chest toward your hands, elbows tracking back along your sides.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "Drop to your knees if you can't keep your hips in line.",
                "Don't let your elbows flare wide; keep them close to your ribs.",
            ],
            Video = new("PPTj-MW2tcs"),
        },
        new("close_grip_push_up", "Close-Grip Push-Up", Triceps, Bodyweight, Compound)
        {
            Secondary = [Chest, Shoulders],
            Summary = "A push-up with your hands about shoulder-width apart and elbows tucked to your sides, shifting more of the work to the triceps.",
            Steps =
            [
                "Get into a push-up position with your hands directly under your shoulders, fingers pointing forward.",
                "Keep your body straight from head to heels.",
                "Lower your chest toward the floor, elbows brushing your ribs.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "Drop to your knees if you can't keep your hips in line.",
                "Keep your elbows tight to your body; flaring them turns it back into a regular push-up.",
                "It's easier on the wrists than a Diamond Push-Up while still hitting the triceps hard.",
            ],
            Video = new("OpRMRhr0Ycc", 779, 796),
        },

        // Forearms: wrist curls
        new("wrist_curl", "Dumbbell Wrist Curl", Forearms, Dumbbell, Isolation)
        {
            Summary = "With your forearms resting on your thighs or a bench, curl dumbbells up using only your wrists to train the forearm flexors.",
            Steps =
            [
                "Sit and rest your forearms on your thighs or a bench, palms up, hands just past the edge.",
                "Let the dumbbells roll down toward your fingertips.",
                "Close your hands and curl your wrists up as high as you can.",
                "Lower slowly back to the stretched position.",
            ],
            Tips =
            [
                "Keep your forearms flat; only your wrists move.",
                "Don't bounce the weight; these muscles respond to slow, high reps.",
            ],
            Video = new("VGkF2NTtao0"),
        },
        new("barbell_wrist_curl", "Barbell Wrist Curl", Forearms, Barbell, Isolation)
        {
            Summary = "A wrist curl with a barbell held in both hands, forearms resting on a bench, for heavier forearm flexor work.",
            Steps =
            [
                "Kneel or sit beside a bench and rest your forearms on it, palms up, wrists just past the edge.",
                "Hold a barbell with your hands about shoulder-width apart.",
                "Let the bar roll down toward your fingers, then curl your wrists up as high as you can.",
                "Lower slowly under control.",
            ],
            Tips =
            [
                "Letting the bar roll into your fingers adds grip work; keep it controlled.",
                "Don't lift your forearms off the bench.",
            ],
            Video = new("lA3QwdAn19Y"),
        },
        new("cable_wrist_curl", "Cable Wrist Curl", Forearms, Cable, Isolation)
        {
            Summary = "A wrist curl on a low cable, which keeps constant tension on the forearm flexors through the whole range.",
            Steps =
            [
                "Attach a straight bar to a low cable and kneel or sit facing it, holding it underhand.",
                "Rest your forearms on your thighs or a bench with your wrists just past the edge.",
                "Let the bar roll down toward your fingertips.",
                "Curl your wrists up as high as you can, then lower slowly.",
            ],
            Tips =
            [
                "Keep the forearms pinned; only the wrists move.",
                "Use higher reps (12–20) and a controlled tempo.",
            ],
            Video = new("WVAaKJvToe0"),
        },
        new("reverse_wrist_curl", "Dumbbell Reverse Wrist Curl", Forearms, Dumbbell, Isolation)
        {
            Summary = "A wrist curl with palms facing down, lifting the back of the hands to train the forearm extensors.",
            Steps =
            [
                "Sit and rest your forearms on your thighs or a bench, palms down, hands just past the edge.",
                "Let your wrists bend down with the dumbbells.",
                "Lift the backs of your hands up as high as you can.",
                "Lower slowly back down.",
            ],
            Tips =
            [
                "Use much lighter weights than palms-up wrist curls.",
                "Great for balancing heavy gripping and for tennis-elbow rehab with very light loads.",
                "Don't use momentum; slow and controlled.",
            ],
            Video = new("kqjHELzVjxQ"),
        },
        new("barbell_reverse_wrist_curl", "Barbell Reverse Wrist Curl", Forearms, Barbell, Isolation)
        {
            Summary = "Standing with a barbell hanging in front of your thighs, palms down, lift the backs of your hands with your wrists to train the forearm extensors.",
            Steps =
            [
                "Stand tall holding a light barbell in front of your thighs with an overhand grip, hands about shoulder-width apart.",
                "Keep your arms straight and let your wrists bend down so your knuckles point toward the floor.",
                "Lift the backs of your hands up as far as you can, moving only at the wrists.",
                "Lower slowly back down.",
            ],
            Tips =
            [
                "Use much less weight than palms-up wrist curls; an empty or light fixed bar is often plenty.",
                "Keep your arms straight and still; don't swing the bar with your shoulders.",
            ],
            Video = new("Q9Em17et6Z8"),
        },
        new("behind_back_wrist_curl", "Barbell Behind-the-Back Wrist Curl", Forearms, Barbell, Isolation)
        {
            Summary = "Standing with a barbell held behind you, curl it up with your wrists, letting you go heavier than seated wrist curls.",
            Steps =
            [
                "Stand with a barbell behind your thighs, palms facing back, hands about shoulder-width apart.",
                "Let the bar roll down to your fingertips with your arms straight.",
                "Curl your fingers and wrists up to bring the bar back into your palms and up as far as you can.",
                "Lower slowly to the fingertips again.",
            ],
            Tips =
            [
                "Keep your arms straight; the wrists and fingers do all the work.",
                "Set the bar in a rack at hip height so you don't have to deadlift it into place.",
            ],
            Video = new("xrS1UCC24do"),
        },
        new("finger_curl", "Barbell Finger Curl", Forearms, Barbell, Isolation)
        {
            Summary = "Let a barbell roll down to your fingertips and curl it back into your palm, training the finger flexors for crushing grip.",
            Steps =
            [
                "Sit with your forearms on your thighs or a bench, palms up, holding a barbell.",
                "Open your hands and let the bar roll down to your fingertips.",
                "Curl your fingers to roll the bar back into your palms.",
                "Finish with a small wrist curl, then repeat.",
            ],
            Tips =
            [
                "Start light; it's easy to lose the bar off the fingertips.",
                "Don't rush; control the roll in both directions.",
            ],
            Video = new("gnDRXH2J5Yc", 14, 38),
        },

        // Forearms: curls and rotations
        new("reverse_curl", "Reverse Curl", Forearms, EzBar, Isolation)
        {
            Secondary = [Biceps],
            Summary = "A curl with an overhand grip, which shifts the load onto the brachioradialis and forearm extensors.",
            Steps =
            [
                "Hold an EZ-bar or straight bar overhand at shoulder width, arms straight.",
                "Keep your elbows at your sides and your wrists straight.",
                "Curl the bar up to shoulder height.",
                "Lower it slowly back to straight arms.",
            ],
            Tips =
            [
                "An EZ-bar is kinder on the wrists than a straight bar.",
                "Don't let your wrists bend back as the bar rises.",
                "Use much less weight than a normal curl.",
            ],
            Video = new("GbTcCkSKlls"),
        },
        new("db_reverse_curl", "Dumbbell Reverse Curl", Forearms, Dumbbell, Isolation)
        {
            Secondary = [Biceps],
            Summary = "An overhand curl with dumbbells, working the brachioradialis and forearm extensors one or both arms at a time.",
            Steps =
            [
                "Stand with a dumbbell in each hand, palms facing back.",
                "Keep your elbows at your sides and your wrists straight.",
                "Curl the dumbbells up to shoulder height with the overhand grip.",
                "Lower them slowly to straight arms.",
            ],
            Tips =
            [
                "Keep your knuckles in line with your forearms throughout.",
                "Don't swing; go light and slow.",
            ],
            Video = new("etJJujHLuXA"),
        },
        new("radial_deviation", "Radial Deviation", Forearms, Other, Isolation)
        {
            Summary = "Lift the head of a hammer, or a dumbbell loaded at one end, by tilting your wrist toward your thumb; good for wrist health and grip.",
            Steps =
            [
                "Stand with your arm at your side holding a hammer by the handle, the head in front of your thumb.",
                "Let the head tip down toward the floor in front of you.",
                "Tilt your wrist up toward your thumb side to lift the head, keeping your arm still.",
                "Lower slowly and repeat, then switch hands.",
            ],
            Tips =
            [
                "Grip further from the head to make it harder.",
                "Only the wrist moves; don't bend the elbow.",
            ],
            Video = new("WO5OA8nnt_g"),
        },
        new("ulnar_deviation", "Ulnar Deviation", Forearms, Other, Isolation)
        {
            Summary = "Lift the head of a hammer, or a dumbbell loaded at one end, behind you by tilting your wrist toward your little finger.",
            Steps =
            [
                "Stand with your arm at your side holding a hammer by the handle, the head behind your hand.",
                "Let the head tip down toward the floor behind you.",
                "Tilt your wrist back toward your little-finger side to lift the head, keeping your arm straight.",
                "Lower slowly and repeat, then switch hands.",
            ],
            Tips =
            [
                "Grip further from the head to make it harder.",
                "Keep your arm straight and still; don't swing.",
            ],
            Video = new("I_Yn9ePaZ_M"),
        },
        new("forearm_rotation", "Hammer Pronation and Supination", Forearms, Other, Isolation)
        {
            Secondary = [Biceps],
            Summary = "Rotate a hammer, or a dumbbell loaded at one end, palm down and palm up to strengthen the forearm rotators; a staple for elbow and wrist health.",
            Steps =
            [
                "Sit with your forearm on your thigh or a bench, wrist past the edge, holding a hammer upright by the handle.",
                "Slowly rotate your forearm to turn your palm down, lowering the head to one side.",
                "Rotate back through upright and on to palm up, lowering the head to the other side.",
                "Keep alternating slowly, then switch arms.",
            ],
            Tips =
            [
                "Choke up on the handle to make it easier, slide down to make it harder.",
                "Keep your forearm flat and still; only it rotates.",
                "Don't let the head drop fast at either end.",
            ],
            Video = new("tMJ0SUttwTI"),
        },
        new("wrist_roller", "Wrist Roller", Forearms, Other, Isolation)
        {
            Summary = "Roll a weight up on a cord by twisting a handle with your wrists, then lower it back down; brutal for the whole forearm.",
            Steps =
            [
                "Hold a wrist roller with a light plate hanging from the cord, arms straight out in front at shoulder height.",
                "Twist the handle one hand at a time to wind the cord and raise the weight.",
                "Once the weight reaches the handle, reverse and unwind it slowly to the floor.",
                "Repeat, winding in the opposite direction to work the other forearm muscles.",
            ],
            Tips =
            [
                "Rolling overhand works the extensors; rolling underhand works the flexors.",
                "Keep your arms up and still; don't rest the handle on your body.",
                "Start very light; it burns quickly.",
            ],
            Video = new("KcFoEP2HwVQ"),
        },

        // Grip
        new("plate_pinch", "Plate Pinch", Forearms, Other, Isolation)
        {
            Hold = true,
            Summary = "Pinch two smooth weight plates together between your fingers and thumb and hold them for time, building pinch grip.",
            Steps =
            [
                "Stand two plates together, smooth sides facing out.",
                "Pinch them between your fingers on one side and your thumb on the other.",
                "Lift them and hold at your side with your arm straight.",
                "Hold for the set time, then set them down under control and switch hands.",
            ],
            Tips =
            [
                "Keep your fingers straight and press hard with your thumb.",
                "Don't let the plates rest against your leg.",
                "Do this over a clear floor, away from your feet.",
            ],
            Video = new("jFTV3DQf3HE"),
        },
        new("hand_gripper", "Hand Gripper", Forearms, Other, Isolation)
        {
            Summary = "Squeeze a spring-loaded hand gripper shut to build crushing grip strength.",
            Steps =
            [
                "Hold the gripper with one handle across your palm and the other across your fingers.",
                "Squeeze until the handles touch.",
                "Hold briefly, then open slowly under control.",
                "Finish the set, then switch hands.",
            ],
            Tips =
            [
                "Pick a gripper you can close for 8–15 reps to start.",
                "Don't let it spring open; control the return.",
            ],
            Video = new("YAWTMzZnIM4"),
        },
        new("band_finger_extension", "Band Finger Extension", Forearms, Band, Isolation)
        {
            Summary = "Spread your fingers against a small rubber band to train the finger extensors, balancing grip work and helping the elbows.",
            Steps =
            [
                "Loop a small rubber band around your fingers and thumb near the tips.",
                "Bring your fingertips together.",
                "Spread your fingers and thumb apart as wide as you can.",
                "Close them slowly back together.",
            ],
            Tips =
            [
                "Use a thicker band or double it up to make it harder.",
                "Move slowly in both directions; don't let the band snap your fingers shut.",
            ],
            Video = new("x0PFZZVOGpk"),
        },
        new("fat_grip_hold", "Fat-Grip Dumbbell Hold", Forearms, Dumbbell, Isolation)
        {
            Secondary = [Traps],
            Hold = true,
            Summary = "Hold heavy dumbbells with thick grip attachments (such as Fat Gripz) for time, building open-hand grip strength.",
            Steps =
            [
                "Fit thick grip attachments on a pair of dumbbells.",
                "Pick them up and stand tall with your arms straight at your sides.",
                "Squeeze hard and hold, shoulders down and back.",
                "Hold for the set time, then set them down under control.",
            ],
            Tips =
            [
                "Choose a weight you can hold for 20–40 seconds.",
                "Don't let your shoulders round forward or shrug up.",
            ],
            Video = new("BAdRWQ3cf_E"),
        },
        new("towel_hang", "Towel Hang", Forearms, Bodyweight, Isolation)
        {
            Secondary = [Back],
            Level = Intermediate,
            Hold = true,
            Summary = "Hang from two towels draped over a pull-up bar for time, a tough test of grip that carries over to climbing and grappling.",
            Steps =
            [
                "Drape two towels over a pull-up bar, shoulder-width apart.",
                "Grip one towel in each hand, squeezing them tightly.",
                "Lift your feet and hang with your arms straight and shoulders gently engaged.",
                "Hold for the set time, then step down.",
            ],
            Tips =
            [
                "Start with one towel and one hand on the bar if two towels are too hard.",
                "Keep your shoulders active; don't hang completely limp.",
                "Use strong towels and check they're secure before you hang.",
            ],
            Video = new("tTeAcQdG73s", 22, 32),
        },
    ];
}
