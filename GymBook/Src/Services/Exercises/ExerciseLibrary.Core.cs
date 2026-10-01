using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] CoreExercises() =>
    [
        // Crunches
        new("crunch", "Crunch", Abs, Bodyweight, Isolation)
        {
            Summary = "The basic ab exercise: lying on your back, you curl your shoulders off the floor by shortening your abs.",
            Steps =
            [
                "Lie on your back with knees bent, feet flat and hands lightly behind your head or crossed on your chest.",
                "Breathe out and curl your head, shoulders and upper back off the floor, ribs moving toward your pelvis.",
                "Pause for a moment at the top with your lower back still on the floor.",
                "Lower back down slowly without letting your head drop.",
            ],
            Tips =
            [
                "Think of curling your spine, not sitting up; the range is small.",
                "Don't pull on your head or neck; your hands only support it.",
                "Keep a fist's space between chin and chest.",
            ],
            Video = new("lX--crZaY_c"),
        },
        new("weighted_crunch", "Weighted Crunch", Abs, Dumbbell, Isolation)
        {
            Summary = "A crunch with a dumbbell or plate held on your chest, so you can train the abs with progressive load.",
            Steps =
            [
                "Lie on your back with knees bent and feet flat, holding a dumbbell across your chest with both hands.",
                "Breathe out and curl your shoulders and upper back off the floor.",
                "Squeeze your abs at the top for a moment.",
                "Lower slowly until your shoulder blades touch the floor.",
            ],
            Tips =
            [
                "Keep the weight pinned to your chest; holding it overhead makes it far harder.",
                "Don't swing the weight or yank with your neck to get up.",
            ],
            Video = new("_nzyLUvtgvs", 17, 65),
        },
        new("decline_crunch", "Decline Crunch", Abs, Bodyweight, Isolation)
        {
            Summary = "A crunch on a decline bench with your feet hooked, which lengthens the range and makes the abs work harder.",
            Steps =
            [
                "Lie back on a decline bench with your feet hooked under the pads and hands crossed on your chest.",
                "Curl your head, shoulders and upper back up off the bench, ribs toward your hips.",
                "Stop once your shoulder blades are clear of the bench and squeeze.",
                "Uncurl slowly back to the start.",
            ],
            Tips =
            [
                "Keep it a curl; sitting all the way up turns it into a sit-up driven by the hip flexors.",
                "Don't use a steeper decline than you can control.",
            ],
            Video = new("FRzQXeN1hro", 15.339, 24.879999),
        },
        new("reverse_crunch", "Reverse Crunch", Abs, Bodyweight, Isolation)
        {
            Summary = "Curls the pelvis up toward the ribs instead of the shoulders toward the hips, with a focus on the lower abs.",
            Steps =
            [
                "Lie on your back with arms by your sides, hips and knees bent to 90° and feet off the floor.",
                "Breathe out and curl your hips off the floor, bringing your knees toward your chest.",
                "Pause when your tailbone is lifted, then lower your hips slowly back down.",
                "Keep the knee angle the same throughout.",
            ],
            Tips =
            [
                "The movement is the pelvis rolling up, not the legs swinging toward your face.",
                "Don't use momentum or push hard through your hands.",
            ],
            Video = new("yH-oSzE5_g0", 45),
        },
        new("toe_touch_crunch", "Toe Touch Crunch", Abs, Bodyweight, Isolation)
        {
            Summary = "A crunch with your legs pointed straight up, reaching your hands toward your toes.",
            Steps =
            [
                "Lie on your back and raise your legs straight up so they're vertical.",
                "Reach your arms up toward your feet.",
                "Breathe out and curl your shoulders off the floor, reaching your fingertips toward your toes.",
                "Lower your shoulders back down slowly, keeping your legs still.",
            ],
            Tips =
            [
                "Keep your legs vertical; letting them drift toward you makes it easier.",
                "Don't jerk your head forward to reach further.",
            ],
            Video = new("NR4k8hJfs-8", 0, 35.82),
        },
        new("stability_ball_crunch", "Stability Ball Crunch", Abs, Other, Isolation)
        {
            Summary = "A crunch on a stability ball, which lets your spine extend over the ball for a longer range of motion.",
            Steps =
            [
                "Sit on a stability ball and walk your feet out until your lower back is supported on it, feet hip-width.",
                "Let your upper back drape back over the ball with hands behind your head or crossed on your chest.",
                "Curl your ribs toward your hips, lifting your shoulders up and forward.",
                "Lower back over the ball slowly under control.",
            ],
            Tips =
            [
                "Widen your stance if you feel unstable.",
                "Don't let your hips drop or rise to help you up; only your spine moves.",
            ],
            Video = new("s_CXq2gEiUk"),
        },
        new("cable_crunch", "Cable Crunch", Abs, Cable, Isolation)
        {
            Summary = "A kneeling crunch against a high cable, one of the best ways to load the abs heavily.",
            Steps =
            [
                "Attach a rope to a high pulley and kneel facing it, holding the rope ends beside your head.",
                "Sit your hips back slightly and lock that hip position in place.",
                "Crunch down, curling your spine and bringing your elbows toward your knees.",
                "Pause, then let your torso rise back up slowly against the cable.",
            ],
            Tips =
            [
                "Your hips stay put; the movement is your spine rounding, not your hips bending.",
                "Keep your hands fixed by your head rather than pulling with your arms.",
                "Don't go so heavy that you're just bowing at the hips.",
            ],
            Video = new("0KEP6A1deBE", 39.360001, 86.759003),
        },
        new("machine_crunch", "Machine Crunch", Abs, Machine, Isolation)
        {
            Summary = "A seated ab crunch machine that guides the movement and makes it easy to add load.",
            Steps =
            [
                "Set the seat so the pads or handles sit at your chest or shoulders, and hook your feet if there are rollers.",
                "Grip the handles and brace your abs.",
                "Curl your torso forward and down, bringing your ribs toward your hips.",
                "Squeeze, then return slowly without letting the stack touch down.",
            ],
            Tips =
            [
                "Move by rounding your spine, not by pulling with your arms.",
                "Don't let the weight yank you back up at the top.",
            ],
            Video = new("fuPFq2EYswE"),
        },

        // Sit-ups
        new("sit_up", "Sit-Up", Abs, Bodyweight, Compound)
        {
            Summary = "A full sit-up from lying to upright, working the abs together with the hip flexors.",
            Steps =
            [
                "Lie on your back with knees bent, feet flat and arms crossed on your chest.",
                "Curl your head and shoulders up first, then keep going until your torso is upright.",
                "Pause at the top, then roll back down one segment at a time.",
                "Touch your shoulder blades down before the next rep.",
            ],
            Tips =
            [
                "Curl up rather than coming up with a flat back.",
                "Don't throw your arms or pull on your neck to get up.",
                "Anchoring your feet makes it easier but shifts more work to the hip flexors.",
            ],
            Video = new("pCX65Mtc_Kk", 27.299999),
        },
        new("decline_sit_up", "Decline Sit-Up", Abs, Bodyweight, Compound)
        {
            Summary = "A full sit-up on a decline bench with feet anchored, harder than a flat sit-up as the angle increases.",
            Level = Intermediate,
            Steps =
            [
                "Lie back on a decline bench with your feet hooked under the pads and arms crossed on your chest.",
                "Curl your head and shoulders up and keep sitting up until your torso is near upright.",
                "Lower yourself back down slowly, rolling through your spine.",
                "Stop just before your shoulders rest on the bench and go again.",
            ],
            Tips =
            [
                "Control the way down; it's half the exercise.",
                "Don't jerk yourself up with momentum or your arms.",
            ],
            Video = new("N7hf1_vcX5w"),
        },
        new("weighted_sit_up", "Weighted Sit-Up", Abs, Other, Compound)
        {
            Summary = "A sit-up holding a weight plate on your chest, a simple way to add load once bodyweight sit-ups are easy.",
            Level = Intermediate,
            Steps =
            [
                "Lie on your back with knees bent and feet flat or anchored, holding a plate against your chest.",
                "Curl your head and shoulders up and continue until your torso is upright.",
                "Lower back down slowly, rolling through your spine.",
                "Touch your shoulder blades down and repeat.",
            ],
            Tips =
            [
                "Start light; a plate held further from your body is much harder.",
                "Don't swing the plate forward to get yourself up.",
            ],
            Video = new("pTZL-Hirhwg"),
        },
        new("v_up", "V-Up", Abs, Bodyweight, Compound)
        {
            Summary = "Lifting your straight legs and torso at the same time to meet in a V, a hard full-range ab exercise.",
            Level = Intermediate,
            Steps =
            [
                "Lie flat on your back with legs straight and arms extended overhead.",
                "Brace your abs and press your lower back toward the floor.",
                "In one movement, raise your legs and torso and reach your hands toward your feet.",
                "Lower both back down slowly, stopping just before they touch the floor.",
            ],
            Tips =
            [
                "Keep your legs straight and together.",
                "Don't flop down; control the lowering to protect your lower back.",
                "Bend your knees (a tuck-up) if you can't keep good form.",
            ],
            Video = new("iP2fjvG0g3w"),
        },
        new("tuck_up", "Tuck-Up", Abs, Bodyweight, Compound)
        {
            Summary = "An easier V-up where you bring bent knees and torso together into a tuck.",
            Steps =
            [
                "Sit on the floor and lean back, legs extended slightly off the floor and hands beside your hips or reaching forward.",
                "Draw your knees in toward your chest while bringing your chest toward your knees.",
                "Squeeze into a tight tuck, balancing on your sit bones.",
                "Extend your legs and lean back again without letting your feet touch the floor.",
            ],
            Tips =
            [
                "Keep your chest lifted rather than collapsing forward.",
                "Don't rock or use momentum; move smoothly.",
            ],
            Video = new("nheTMVLN2Hw"),
        },

        // Leg raises
        new("leg_raise", "Lying Leg Raise", Abs, Bodyweight, Isolation)
        {
            Summary = "Lying on your back and raising straight legs to vertical, training the lower abs and hip flexors.",
            Steps =
            [
                "Lie on your back with legs straight and hands flat by your sides or under your hips.",
                "Press your lower back into the floor and brace your abs.",
                "Raise your legs together until they're vertical, curling your hips slightly off the floor at the top.",
                "Lower them slowly, stopping before your lower back lifts off the floor.",
            ],
            Tips =
            [
                "Only lower as far as you can keep your lower back down.",
                "Bend your knees slightly if your back arches.",
                "Don't let your legs drop quickly.",
            ],
            Video = new("sY2ZgV2Sj_s"),
        },
        new("flutter_kicks", "Flutter Kicks", Abs, Bodyweight, Isolation)
        {
            Summary = "Small, quick alternating kicks with straight legs held just off the floor, an endurance drill for the lower abs and hip flexors.",
            Steps =
            [
                "Lie on your back with legs straight and hands under your hips or flat by your sides.",
                "Lift your legs a few inches off the floor and press your lower back down.",
                "Kick your legs up and down in small, alternating movements.",
                "Keep going for the set time or count, then lower your legs.",
            ],
            Tips =
            [
                "Lift your legs higher if your lower back starts to arch.",
                "Keep the kicks small and controlled.",
            ],
            Video = new("ZEjloSZwXqI"),
        },
        new("hanging_knee_raise", "Hanging Knee Raise", Abs, Bodyweight, Isolation)
        {
            Summary = "Hanging from a bar and drawing your knees up toward your chest, the entry point to hanging ab work.",
            Secondary = [Forearms],
            Steps =
            [
                "Hang from a pull-up bar with an overhand grip, arms straight and shoulders engaged.",
                "Brace your abs and draw your knees up toward your chest.",
                "Curl your pelvis up at the top so your hips tilt toward you.",
                "Lower your legs slowly until they're straight under you again.",
            ],
            Tips =
            [
                "Stop swinging between reps before starting the next one.",
                "Don't just lift your knees to 90°; the abs work most when the pelvis curls.",
            ],
            Video = new("IHOFHNWn0_M"),
        },
        new("hanging_leg_raise", "Hanging Leg Raise", Abs, Bodyweight, Isolation)
        {
            Summary = "Hanging from a bar and raising straight legs up in front of you, a demanding lower-ab and hip-flexor exercise.",
            Secondary = [Forearms],
            Level = Intermediate,
            Steps =
            [
                "Hang from a pull-up bar with an overhand grip, arms straight and shoulders engaged.",
                "Brace your abs and raise your straight legs in front of you.",
                "Keep lifting until your legs are at least parallel to the floor, curling your pelvis up at the top.",
                "Lower them slowly without swinging.",
            ],
            Tips =
            [
                "Control the descent so you don't start swinging.",
                "Tilt your pelvis up at the top; just lifting the legs mostly trains the hip flexors.",
                "Bend your knees if you can't keep your legs straight with control.",
            ],
            Video = new("0wSUjj5j1xo"),
        },
        new("captains_chair_knee_raise", "Captain's Chair Knee Raise", Abs, Bodyweight, Isolation)
        {
            Summary = "Knee raises on a captain's chair station, supported on your forearms with your back against the pad.",
            Steps =
            [
                "Step up into the captain's chair with your forearms on the pads, gripping the handles and your back against the backrest.",
                "Let your legs hang straight down and brace your abs.",
                "Draw your knees up toward your chest, curling your hips off the backrest at the top.",
                "Lower your legs slowly back down.",
            ],
            Tips =
            [
                "Keep your shoulders pressed down away from your ears.",
                "Don't swing your legs up; lift and lower under control.",
                "Straighten your legs to make it harder.",
            ],
            Video = new("7KDDZtaUaxw"),
        },
        new("toes_to_bar", "Toes-to-Bar", Abs, Bodyweight, Compound)
        {
            Summary = "Hanging from a bar and raising your feet all the way up to touch it, combining a full leg raise with a lat pull.",
            Secondary = [Back, Forearms],
            Level = Advanced,
            Steps =
            [
                "Hang from a pull-up bar with an overhand grip, shoulders engaged.",
                "Brace your abs and pull down on the bar as you raise your legs.",
                "Curl your hips up and bring your toes to touch the bar between your hands.",
                "Lower your legs under control back to the hang.",
            ],
            Tips =
            [
                "Pressing the bar down toward your hips helps you lift your legs higher.",
                "Strict reps build strength; kipping ones are a CrossFit skill, not a substitute.",
                "Don't let your legs crash down into a swing.",
            ],
            Video = new("kjYuPnmMjfo", 158, 307),
        },
        new("dragon_flag", "Dragon Flag", Abs, Bodyweight, Compound)
        {
            Summary = "Lying on a bench holding it behind your head and lowering your rigid body from vertical, one of the hardest core exercises.",
            Secondary = [Back, Shoulders],
            Level = Advanced,
            Steps =
            [
                "Lie on a bench and grip it or a sturdy post behind your head.",
                "Swing or lift your legs and hips up so your body is vertical, resting on your upper back and shoulders.",
                "Keeping your body straight from shoulders to feet, lower it slowly toward the bench.",
                "Stop just above the bench, then raise back up or lower all the way and reset.",
            ],
            Tips =
            [
                "Keep your hips locked straight; bending at the hips makes it much easier.",
                "Master slow negatives before trying full reps.",
                "Don't put weight on your neck; you balance on your upper back.",
            ],
            Video = new("pvz7k5gO-DE"),
        },

        // Planks and anti-extension
        new("plank", "Front Plank", Abs, Bodyweight, Isolation)
        {
            Summary = "Holding a straight body on your forearms and toes, the standard exercise for core endurance and resisting arching.",
            Hold = true,
            Steps =
            [
                "Set your forearms on the floor with elbows under your shoulders.",
                "Step your feet back so your body forms a straight line from head to heels.",
                "Brace your abs and squeeze your glutes.",
                "Hold for 20–60 seconds, breathing steadily.",
            ],
            Tips =
            [
                "Keep your hips level with your shoulders: neither sagging nor piked up.",
                "Look at the floor just ahead of your hands to keep your neck neutral.",
                "Stop when your form breaks rather than holding a sagging plank.",
            ],
            Video = new("mwlp75MS6Rg"),
        },
        new("high_plank", "High Plank", Abs, Bodyweight, Isolation)
        {
            Summary = "A plank held on straight arms in the top of a push-up position.",
            Secondary = [Shoulders, Chest],
            Hold = true,
            Steps =
            [
                "Place your hands on the floor under your shoulders with arms straight.",
                "Step your feet back so your body is straight from head to heels.",
                "Brace your abs, squeeze your glutes and push the floor away.",
                "Hold for 20–60 seconds, breathing steadily.",
            ],
            Tips =
            [
                "Keep your shoulder blades spread, not sinking between your shoulders.",
                "Don't lock your elbows hard or let your hips sag.",
            ],
            Video = new("EKZfeoVuPbE"),
        },
        new("rkc_plank", "RKC Plank", Abs, Bodyweight, Isolation)
        {
            Summary = "A short, maximal-tension plank where you squeeze everything and pull your elbows toward your feet.",
            Secondary = [Glutes],
            Level = Intermediate,
            Hold = true,
            Steps =
            [
                "Set up in a forearm plank with your elbows under your eyes, slightly further forward than usual.",
                "Clasp your hands, squeeze your glutes and quads hard and tuck your pelvis under.",
                "Pull your elbows down toward your toes without moving them, as if dragging the floor.",
                "Hold for 10–20 seconds at full tension.",
            ],
            Tips =
            [
                "Short and very hard: if you can hold it for a minute you aren't squeezing enough.",
                "Breathe in short, braced breaths instead of holding your breath.",
            ],
            Video = new("4hQmAsF9iL4"),
        },
        new("long_lever_plank", "Long-Lever Plank", Abs, Bodyweight, Isolation)
        {
            Summary = "A forearm plank with your elbows placed well forward of your shoulders, which greatly increases the load on the abs.",
            Level = Intermediate,
            Hold = true,
            Steps =
            [
                "Set up in a forearm plank, then walk your elbows forward until they're a few inches in front of your head.",
                "Keep your body in a straight line from head to heels.",
                "Brace your abs hard and squeeze your glutes.",
                "Hold for 10–30 seconds.",
            ],
            Tips =
            [
                "The further forward your elbows go, the harder it gets.",
                "Stop the moment your lower back starts to sag.",
            ],
            Video = new("SwGNmrAmXP4"),
        },
        new("side_plank", "Side Plank", Abs, Bodyweight, Isolation)
        {
            Summary = "Holding your body straight on one forearm and the side of your foot, training the obliques and the deep side core.",
            Secondary = [Glutes],
            Hold = true,
            Steps =
            [
                "Lie on your side with your elbow under your shoulder and legs stacked.",
                "Lift your hips so your body forms a straight line from head to feet.",
                "Rest your top hand on your hip or reach it to the ceiling.",
                "Hold for 15–45 seconds, then switch sides.",
            ],
            Tips =
            [
                "Push your hips forward slightly so you don't pike back.",
                "Bend your knees and rest on them to make it easier.",
                "Don't let your hips sag toward the floor.",
            ],
            Video = new("44ND4bOB-T0"),
        },
        new("side_plank_hip_dip", "Side Plank Hip Dip", Abs, Bodyweight, Isolation)
        {
            Summary = "A side plank where you lower your hip toward the floor and lift it back up, adding movement to the oblique work.",
            Secondary = [Glutes],
            Level = Intermediate,
            Steps =
            [
                "Set up in a side plank on your forearm with your body in a straight line.",
                "Lower your hip toward the floor without touching it.",
                "Drive it back up past the starting line, squeezing your side.",
                "Do all your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your body facing forward; don't rotate.",
                "Move slowly and stay tall through your shoulder.",
            ],
            Video = new("ynrKPaorg-Q"),
        },
        new("plank_shoulder_tap", "Plank Shoulder Tap", Abs, Bodyweight, Compound)
        {
            Summary = "In a high plank, tapping each hand to the opposite shoulder while keeping your hips still, an anti-rotation drill.",
            Secondary = [Shoulders],
            Steps =
            [
                "Start in a high plank with hands under your shoulders and feet a little wider than hip-width.",
                "Brace your abs and squeeze your glutes.",
                "Lift one hand and tap the opposite shoulder, then place it back down.",
                "Alternate sides, keeping your hips square to the floor.",
            ],
            Tips =
            [
                "Move slowly; the challenge is keeping your hips from rocking.",
                "Widen your feet to make it easier.",
            ],
            Video = new("NV55raYCP0E"),
        },
        new("body_saw", "Body Saw", Abs, Other, Compound)
        {
            Summary = "A forearm plank with your feet on sliders or in suspension straps, rocking your body forward and back to load the abs.",
            Secondary = [Shoulders],
            Level = Intermediate,
            Steps =
            [
                "Get into a forearm plank with your feet on sliders, towels on a smooth floor, or in suspension trainer straps.",
                "Brace your abs and squeeze your glutes.",
                "Push yourself backward from your forearms so your elbows end up in front of your head.",
                "Pull yourself forward to the start using your shoulders and abs.",
            ],
            Tips =
            [
                "Only go back as far as you can keep your lower back from sagging.",
                "Keep your hips in line; don't pike up to bring yourself forward.",
            ],
            Video = new("rVxIwirRwXE"),
        },
        new("ab_wheel", "Ab Wheel Rollout", Abs, Bodyweight, Compound)
        {
            Summary = "From your knees, rolling an ab wheel forward and pulling it back, a strong anti-extension exercise for the abs.",
            Secondary = [Back, Shoulders],
            Level = Intermediate,
            Steps =
            [
                "Kneel on a mat holding the ab wheel on the floor under your shoulders.",
                "Brace your abs and tuck your pelvis slightly.",
                "Roll the wheel forward slowly, letting your hips and arms extend, as far as you can without your back arching.",
                "Pull the wheel back toward your knees using your abs and lats.",
            ],
            Tips =
            [
                "Keep your lower back from sagging; stop the rollout before it does.",
                "Lead the return with your hips, not by sitting back first.",
                "Roll toward a wall to limit the range while you build strength.",
            ],
            Video = new("Q5MT5omGNJI"),
        },
        new("standing_ab_wheel_rollout", "Standing Ab Wheel Rollout", Abs, Bodyweight, Compound)
        {
            Summary = "An ab wheel rollout from standing, rolling all the way out until your body is near the floor and back.",
            Secondary = [Back, Shoulders],
            Level = Advanced,
            Steps =
            [
                "Stand with feet hip-width, bend forward and place the ab wheel on the floor in front of your feet.",
                "Brace your abs hard and roll the wheel forward slowly.",
                "Keep extending until your body is straight and just above the floor.",
                "Pull the wheel back toward your feet and stand up.",
            ],
            Tips =
            [
                "Master full kneeling rollouts before trying this.",
                "Don't let your hips sag at full extension; it strains the lower back.",
            ],
            Video = new("2zVNyi5Uk44", 150.56, 193.599),
        },
        new("barbell_rollout", "Barbell Rollout", Abs, Barbell, Compound)
        {
            Summary = "A kneeling rollout using a loaded barbell with round plates instead of an ab wheel.",
            Secondary = [Back, Shoulders],
            Level = Intermediate,
            Steps =
            [
                "Load a barbell with round plates and kneel behind it, gripping it shoulder-width.",
                "Brace your abs with your shoulders over the bar.",
                "Roll the bar forward slowly, extending your hips and arms as far as you can keep your back flat.",
                "Pull the bar back toward your knees with your abs and lats.",
            ],
            Tips =
            [
                "Use small plates if they put the bar too high; large ones make it slightly easier.",
                "Don't let your lower back arch at the far end.",
            ],
            Video = new("3C1TRMJveXo"),
        },
        new("stability_ball_rollout", "Stability Ball Rollout", Abs, Other, Compound)
        {
            Summary = "A kneeling rollout with your forearms on a stability ball, an easier way to learn anti-extension.",
            Secondary = [Shoulders],
            Steps =
            [
                "Kneel in front of a stability ball with your forearms resting on it and your hips forward.",
                "Brace your abs and keep your body straight from knees to head.",
                "Roll the ball forward on your forearms, letting your body lean forward.",
                "Pull it back to the start using your abs.",
            ],
            Tips =
            [
                "Go only as far as you can keep your back flat.",
                "Don't bend at the hips; the movement comes from your shoulders.",
            ],
            Video = new("80-BtHgkOYo"),
        },
        new("stir_the_pot", "Stir the Pot", Abs, Other, Compound)
        {
            Summary = "A forearm plank on a stability ball, drawing small circles with your elbows to challenge the core in every direction.",
            Secondary = [Shoulders],
            Level = Intermediate,
            Steps =
            [
                "Set your forearms on a stability ball and step back into a plank with feet a little wider than hip-width.",
                "Brace your abs and squeeze your glutes.",
                "Move your forearms in small circles, as if stirring a pot.",
                "Do your circles one way, then reverse.",
            ],
            Tips =
            [
                "Keep the circles small and your body still.",
                "Don't let your hips sag or swing with the ball.",
            ],
            Video = new("vmPx6mEim8E"),
        },

        // Rotation and anti-rotation
        new("pallof_press", "Pallof Press", Abs, Cable, Compound)
        {
            Summary = "Standing side-on to a cable and pressing the handle straight out, resisting the pull to rotate.",
            Steps =
            [
                "Set a cable handle at chest height and stand side-on to it, feet shoulder-width, holding the handle at your chest with both hands.",
                "Step away until there's tension, brace your abs and squeeze your glutes.",
                "Press the handle straight out in front of your chest without letting your torso turn.",
                "Hold for a second or two, then bring it back to your chest.",
            ],
            Tips =
            [
                "Your hips and shoulders stay square the whole time.",
                "The further you press out, the harder it gets.",
                "Do both sides for equal reps.",
            ],
            Video = new("f3R99GoYvCs"),
        },
        new("band_pallof_press", "Band Pallof Press", Abs, Band, Compound)
        {
            Summary = "A Pallof press using a resistance band anchored at chest height, easy to do at home.",
            Steps =
            [
                "Anchor a band at chest height and stand side-on to it, holding the band at your chest with both hands.",
                "Step out until the band is taut and brace your abs.",
                "Press your hands straight out in front of your chest, resisting the band's pull.",
                "Pause, then bring your hands back to your chest.",
            ],
            Tips =
            [
                "Don't let your torso twist toward the anchor.",
                "Step further from the anchor to make it harder.",
            ],
            Video = new("eObpl4f-wgI"),
        },
        new("cable_woodchop_high_to_low", "High-to-Low Cable Woodchop", Abs, Cable, Compound)
        {
            Summary = "Pulling a cable diagonally from above one shoulder down across your body to the opposite hip, training rotation through the obliques.",
            Secondary = [Shoulders],
            Steps =
            [
                "Set a handle at the top of a cable and stand side-on to it, feet wider than shoulder-width.",
                "Hold the handle with both hands above your near shoulder, arms nearly straight.",
                "Pull it down and across your body to outside your far knee, rotating your torso and pivoting your back foot.",
                "Return slowly along the same path.",
            ],
            Tips =
            [
                "Rotate through your torso and hips; your arms only guide the handle.",
                "Keep your arms long and your chest tall.",
                "Don't yank the weight; move at a controlled speed.",
            ],
            Video = new("gcGNypjIQDo"),
        },
        new("cable_woodchop_low_to_high", "Low-to-High Cable Woodchop", Abs, Cable, Compound)
        {
            Summary = "Pulling a cable diagonally from beside one knee up across your body above the opposite shoulder.",
            Secondary = [Shoulders, Glutes],
            Steps =
            [
                "Set a handle at the bottom of a cable and stand side-on to it, feet wider than shoulder-width.",
                "Squat down slightly and hold the handle with both hands outside your near knee.",
                "Drive up and rotate, pulling the handle across your body to above your far shoulder.",
                "Lower it slowly along the same path.",
            ],
            Tips =
            [
                "Let your hips and back foot turn with you so your lower back doesn't twist alone.",
                "Keep your arms nearly straight.",
            ],
            Video = new("-_c9SNzxnao"),
        },
        new("landmine_rotation", "Landmine Rotation", Abs, Barbell, Compound)
        {
            Summary = "Swinging the end of a landmine barbell in an arc from one hip to the other, training rotational strength.",
            Secondary = [Shoulders],
            Level = Intermediate,
            Steps =
            [
                "Set a barbell in a landmine and stand facing it, feet shoulder-width, holding the end at arm's length overhead in front of you.",
                "Brace your abs and rotate to lower the bar in an arc toward one hip.",
                "Bring it back up through the middle and down to the other hip.",
                "Keep alternating, moving smoothly.",
            ],
            Tips =
            [
                "Keep your arms nearly straight and let your hips pivot with the bar.",
                "Start light; the lever is long and loads your spine.",
                "Don't let the bar pull you around; stop it under control at each side.",
            ],
            Video = new("MswsBPLGhE8"),
        },
        new("russian_twist", "Russian Twist", Abs, Bodyweight, Isolation)
        {
            Summary = "Sitting leaned back and rotating your torso side to side, an oblique exercise often done holding a weight.",
            Steps =
            [
                "Sit on the floor with knees bent, lean back to about 45° and lift your feet if you can.",
                "Hold your hands together or hold a weight in front of your chest.",
                "Rotate your torso to one side, bringing your hands beside your hip.",
                "Rotate to the other side and keep alternating.",
            ],
            Tips =
            [
                "Turn your ribs and shoulders, not just your arms.",
                "Keep your chest up and back long; don't slump.",
                "Keep your feet down until you can control the movement.",
            ],
            Video = new("s0kT80JLCfA"),
        },
        new("windshield_wiper", "Lying Windshield Wiper", Abs, Bodyweight, Isolation)
        {
            Summary = "Lying on your back with legs raised and lowering them side to side, a rotational exercise for the obliques.",
            Level = Intermediate,
            Steps =
            [
                "Lie on your back with arms out to the sides and legs raised straight up.",
                "Keeping your shoulders on the floor, lower your legs together toward one side.",
                "Stop before they touch the floor and bring them back up through the middle.",
                "Lower to the other side and keep alternating.",
            ],
            Tips =
            [
                "Bend your knees to 90° to make it easier.",
                "Keep both shoulders pressed into the floor.",
                "Don't let your legs drop; control the full arc.",
            ],
            Video = new("NAA9pT_PplE"),
        },
        new("hanging_windshield_wiper", "Hanging Windshield Wiper", Abs, Bodyweight, Compound)
        {
            Summary = "Hanging from a bar with your legs raised toward it and rotating them from side to side, an advanced oblique exercise.",
            Secondary = [Back, Forearms],
            Level = Advanced,
            Steps =
            [
                "Hang from a pull-up bar and raise your legs until your feet are near the bar.",
                "Keeping your legs together, rotate them down toward one side.",
                "Bring them back through the middle and rotate to the other side.",
                "Keep alternating, then lower your legs under control.",
            ],
            Tips =
            [
                "You need strict toes-to-bar strength before trying these.",
                "Keep pulling down on the bar to stay stable.",
                "Don't let momentum swing your body.",
            ],
            Video = new("OpxFecW_mQk"),
        },
        new("suitcase_hold", "Suitcase Hold", Abs, Dumbbell, Isolation)
        {
            Summary = "Standing tall holding a heavy dumbbell or kettlebell in one hand, resisting the pull to lean sideways.",
            Secondary = [Forearms, Traps],
            Hold = true,
            Steps =
            [
                "Stand tall holding a heavy dumbbell or kettlebell at your side in one hand.",
                "Brace your abs and keep your shoulders level.",
                "Hold without leaning toward or away from the weight for 20–45 seconds.",
                "Switch hands and repeat.",
            ],
            Tips =
            [
                "Imagine a string pulling the top of your head up.",
                "Don't shrug the loaded shoulder or let your hips shift sideways.",
            ],
            Video = new("lHuqn4aZcVI"),
        },

        // Stability
        new("dead_bug", "Dead Bug", Abs, Bodyweight, Compound)
        {
            Summary = "Lying on your back and slowly extending opposite arm and leg while keeping your lower back still, teaching core control.",
            Steps =
            [
                "Lie on your back with arms reaching up and hips and knees bent to 90°.",
                "Press your lower back gently into the floor and brace.",
                "Slowly reach one arm overhead and straighten the opposite leg until both hover above the floor.",
                "Bring them back to the start and switch sides.",
            ],
            Tips =
            [
                "Your lower back must stay in contact with the floor; shorten the reach if it lifts.",
                "Breathe out as you extend.",
                "Move slowly; speed defeats the purpose.",
            ],
            Video = new("bxn9FBrt4-A"),
        },
        new("bird_dog", "Bird Dog", LowerBack, Bodyweight, Compound)
        {
            Summary = "On hands and knees, reaching opposite arm and leg out long while keeping your spine still, for back and core stability.",
            Secondary = [Glutes, Abs],
            Steps =
            [
                "Kneel on all fours with hands under your shoulders and knees under your hips.",
                "Brace your abs and keep your back flat.",
                "Reach one arm forward and the opposite leg back until both are in line with your body.",
                "Hold for a second or two, return, and switch sides.",
            ],
            Tips =
            [
                "Keep your hips level; don't twist or let the lifted hip open up.",
                "Reach long rather than high so your back doesn't arch.",
            ],
            Video = new("QABW99qPiNM"),
        },
        new("hollow_body_hold", "Hollow Body Hold", Abs, Bodyweight, Isolation)
        {
            Summary = "Holding a curved, banana-shaped position on your back with arms and legs off the floor, a gymnastics core staple.",
            Level = Intermediate,
            Hold = true,
            Steps =
            [
                "Lie on your back and press your lower back firmly into the floor.",
                "Lift your shoulders, arms and legs off the floor, arms by your ears and legs straight.",
                "Lower your arms and legs as far as you can while keeping your lower back down.",
                "Hold for 15–45 seconds.",
            ],
            Tips =
            [
                "If your lower back lifts, raise your legs higher or bend your knees.",
                "Point your toes and keep your legs squeezed together.",
            ],
            Video = new("jLxtFNO0r50"),
        },
        new("hollow_rock", "Hollow Rock", Abs, Bodyweight, Isolation)
        {
            Summary = "Rocking back and forth while holding the hollow body position.",
            Level = Intermediate,
            Steps =
            [
                "Get into a hollow body hold with arms overhead and legs straight off the floor.",
                "Keeping the shape locked, rock back toward your shoulders.",
                "Rock forward toward your hips without changing the shape.",
                "Continue rocking smoothly for reps or time.",
            ],
            Tips =
            [
                "The shape doesn't change; the whole body rocks as one piece.",
                "Don't drive the rocking by swinging your arms or legs.",
            ],
            Video = new("p7j02V1fIzU"),
        },
        new("tuck_l_sit", "Tuck L-Sit", Abs, Bodyweight, Compound)
        {
            Summary = "Supporting yourself on straight arms with your knees tucked to your chest and feet off the floor, the step before a full L-sit.",
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Hold = true,
            Steps =
            [
                "Sit on the floor between parallettes or dip bars, or on the floor with hands beside your hips.",
                "Press down through straight arms and push your shoulders down away from your ears.",
                "Lift your hips and feet off the floor, knees tucked toward your chest.",
                "Hold for 10–30 seconds.",
            ],
            Tips =
            [
                "Lock your elbows and keep your shoulders depressed.",
                "Don't let your shoulders shrug up toward your ears.",
            ],
            Video = new("fEQg1Tr6LN4"),
        },
        new("l_sit", "L-Sit", Abs, Bodyweight, Compound)
        {
            Summary = "Supporting yourself on straight arms with legs held straight out in front, demanding strength in the abs, hip flexors and shoulders.",
            Secondary = [Triceps, Shoulders, Quads],
            Level = Advanced,
            Hold = true,
            Steps =
            [
                "Sit between parallettes or dip bars, or on the floor with hands beside your hips.",
                "Press down through straight arms and push your shoulders down.",
                "Lift your body and raise your straight legs until they're parallel to the floor.",
                "Hold for 10–30 seconds.",
            ],
            Tips =
            [
                "Squeeze your quads and point your toes to keep your legs straight.",
                "Build up from the tuck L-sit or one-leg-extended versions.",
                "Don't let your hips sink back behind your hands.",
            ],
            Video = new("iTX2lgpH7sw"),
        },

        // Obliques
        new("dumbbell_side_bend", "Dumbbell Side Bend", Abs, Dumbbell, Isolation)
        {
            Summary = "Standing with a dumbbell in one hand and bending sideways down and up, training the obliques and QL.",
            Secondary = [LowerBack],
            Steps =
            [
                "Stand tall with feet hip-width, holding a dumbbell in one hand at your side.",
                "Bend sideways toward the weight, sliding it down your leg.",
                "Use your opposite side to pull yourself back up to upright.",
                "Do all your reps, then switch sides.",
            ],
            Tips =
            [
                "Bend straight to the side; don't lean forward or back.",
                "Hold only one weight; two cancel each other out.",
                "Move slowly rather than swinging.",
            ],
            Video = new("BDXhn8ta3Vo"),
        },
        new("cable_side_bend", "Cable Side Bend", Abs, Cable, Isolation)
        {
            Summary = "A side bend holding a low cable handle, giving constant tension on the obliques.",
            Secondary = [LowerBack],
            Steps =
            [
                "Set a handle at the bottom of a cable and stand side-on to it, holding the handle in your near hand.",
                "Stand tall with a slight bend toward the cable.",
                "Bend away from the cable, pulling the handle up your side with your obliques.",
                "Return slowly toward the cable.",
            ],
            Tips =
            [
                "Keep your arm straight; it's a hook, not a pulling arm.",
                "Don't rotate or lean forward.",
            ],
            Video = new("hOI4insq3U0"),
        },
        new("roman_chair_side_bend", "45° Side Bend", Abs, Bodyweight, Isolation)
        {
            Summary = "Lying sideways on a 45° back extension bench and raising your torso, a hard oblique and QL exercise.",
            Secondary = [LowerBack],
            Level = Intermediate,
            Steps =
            [
                "Set yourself sideways on a 45° back extension bench with your hip on the pad and feet stacked under the rollers.",
                "Cross your arms on your chest and let your torso lower toward the floor.",
                "Raise your torso sideways until it's in line with your legs.",
                "Lower slowly and repeat, then switch sides.",
            ],
            Tips =
            [
                "Keep your body facing forward; don't twist to get up.",
                "Hold a plate on your chest to add load once it's easy.",
            ],
            Video = new("qWMlVycsl4Y"),
        },
        new("oblique_crunch", "Oblique Crunch", Abs, Bodyweight, Isolation)
        {
            Summary = "A crunch with your knees dropped to one side, so the curl comes more from the obliques.",
            Steps =
            [
                "Lie on your back with knees bent, then let both knees drop to one side.",
                "Place your hands lightly behind your head with shoulders flat.",
                "Curl your shoulders up toward the ceiling, squeezing your top side.",
                "Lower slowly, do all your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep the curl small and controlled.",
                "Don't pull on your neck.",
            ],
            Video = new("teZDcxypX54"),
        },
        new("bicycle_crunch", "Bicycle Crunch", Abs, Bodyweight, Isolation)
        {
            Summary = "A crunch where you bring each elbow toward the opposite knee in a pedalling motion, working abs and obliques.",
            Steps =
            [
                "Lie on your back with hands lightly behind your head and knees raised to 90°.",
                "Curl your shoulders off the floor.",
                "Rotate one shoulder toward the opposite knee as you straighten the other leg.",
                "Switch sides in a smooth pedalling motion.",
            ],
            Tips =
            [
                "Rotate your ribcage; just moving your elbow doesn't count.",
                "Go slowly and keep your lower back down.",
                "Don't yank on your head.",
            ],
            Video = new("eqg47ZuGZXQ"),
        },
        new("heel_taps", "Heel Taps", Abs, Bodyweight, Isolation)
        {
            Summary = "Lying with your shoulders curled up and reaching side to side to tap each heel, a simple oblique exercise.",
            Steps =
            [
                "Lie on your back with knees bent and feet flat, a little wider than hip-width.",
                "Curl your head and shoulders slightly off the floor with arms by your sides.",
                "Bend sideways to tap one heel with your hand, then the other.",
                "Keep alternating without lowering your shoulders.",
            ],
            Tips =
            [
                "Keep your shoulders lifted the whole set.",
                "Bend from your side, not by reaching with your arm.",
            ],
            Video = new("w2Um9ULrcBI"),
        },
    ];
}
