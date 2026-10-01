using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] BackExercises() =>
    [
        // Pull-ups and chin-ups
        new("pull_up", "Pull-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Forearms],
            Level = Intermediate,
            Summary = "The benchmark bodyweight pull for the lats: hang from a bar with an overhand grip and pull your chest up to it.",
            Steps =
            [
                "Hang from the bar with an overhand grip a little wider than your shoulders, arms straight.",
                "Pull your shoulder blades down, then drive your elbows down toward your ribs.",
                "Pull until your chin clears the bar and your chest is close to it.",
                "Lower under control all the way to straight arms.",
            ],
            Tips =
            [
                "Keep your ribs down and legs still, slightly in front of you.",
                "Think of pulling the bar down to you rather than yourself up.",
                "Don't kip or cut the bottom short; full hang to chin over the bar.",
            ],
            Video = new("ZPG8OsHKXLw"),
        },
        new("chin_up", "Chin-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Forearms],
            Level = Intermediate,
            Summary = "A pull-up with an underhand, shoulder-width grip, which brings the biceps in more and is usually a little easier.",
            Steps =
            [
                "Hang from the bar with your palms facing you, hands about shoulder-width apart.",
                "Set your shoulders down and pull your elbows down and back toward your sides.",
                "Pull until your chin is over the bar.",
                "Lower slowly to a full hang.",
            ],
            Tips =
            [
                "Keep your elbows in front of your body rather than flaring them out.",
                "Don't crane your neck to get your chin over; bring your chest up instead.",
            ],
            Video = new("mRy9m2Q9_1I"),
        },
        new("neutral_grip_pull_up", "Neutral-Grip Pull-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Forearms],
            Level = Intermediate,
            Summary = "A pull-up on parallel handles with palms facing each other, often the most shoulder- and elbow-friendly grip.",
            Steps =
            [
                "Hang from the parallel handles with palms facing each other and arms straight.",
                "Draw your shoulder blades down and pull your elbows down past your sides.",
                "Pull until your chin clears the handles.",
                "Lower under control to a full hang.",
            ],
            Tips =
            [
                "A good choice if overhand pull-ups bother your shoulders or elbows.",
                "Don't swing; keep your body still and legs quiet.",
            ],
            Video = new("D6zsOn2OUz0"),
        },
        new("wide_grip_pull_up", "Wide-Grip Pull-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Shoulders],
            Level = Intermediate,
            Summary = "A pull-up with a grip well outside the shoulders, which shortens the range and biases the lats and upper back.",
            Steps =
            [
                "Hang from the bar with an overhand grip about 1.5 times shoulder width.",
                "Pull your shoulder blades down and back.",
                "Drive your elbows down and out to bring your upper chest toward the bar.",
                "Lower slowly to straight arms.",
            ],
            Tips =
            [
                "Go only as wide as your shoulders tolerate; wider isn't better.",
                "Don't shrug at the top or let your shoulders roll forward.",
            ],
            Video = new("GRgWPT9XSQQ"),
        },
        new("weighted_pull_up", "Weighted Pull-Up", Back, Other, Compound)
        {
            Secondary = [Biceps, Forearms],
            Level = Advanced,
            Summary = "A pull-up with extra load from a dip belt, weight vest or a dumbbell between the feet, for building strength past bodyweight.",
            Steps =
            [
                "Attach the weight to a dip belt around your hips, or hold a dumbbell between your feet.",
                "Hang from the bar with an overhand grip, arms straight and the weight still.",
                "Pull your chest up until your chin clears the bar.",
                "Lower under control to a full hang.",
            ],
            Tips =
            [
                "Earn it: be able to do 8–10 strict pull-ups before adding weight.",
                "Keep the weight from swinging; brace your abs and keep your legs still.",
                "Don't trade range for load; still go to a full hang each rep.",
            ],
            Video = new("fnJ0F1Xsu7Y"),
        },
        new("assisted_pull_up_machine", "Assisted Pull-Up Machine", Back, Machine, Compound)
        {
            Secondary = [Biceps, Forearms],
            Summary = "A pull-up on a machine whose knee or foot pad pushes you up, so you can train the full movement while building up to bodyweight.",
            Steps =
            [
                "Set the counterweight; more weight means more help.",
                "Kneel or stand on the pad and grip the handles overhand, slightly wider than your shoulders.",
                "Pull your elbows down until your chin is above the handles.",
                "Lower slowly until your arms are straight.",
            ],
            Tips =
            [
                "Reduce the assistance over time until you need none.",
                "Don't let the pad bounce you up; control both directions.",
            ],
            Video = new("wFj808u2HWU"),
        },
        new("band_assisted_pull_up", "Band-Assisted Pull-Up", Back, Band, Compound)
        {
            Secondary = [Biceps, Forearms],
            Summary = "A pull-up with a long resistance band looped over the bar and under your knee or foot to take off some of your bodyweight.",
            Steps =
            [
                "Loop a long band around the bar and pull it through itself so it hangs down.",
                "Put one knee or foot in the band and take an overhand grip.",
                "Pull until your chin clears the bar.",
                "Lower slowly to straight arms.",
            ],
            Tips =
            [
                "The band helps most at the bottom, so work hard at the top.",
                "Use thinner bands as you get stronger.",
                "Don't let the band snap you up; stay in control.",
            ],
            Video = new("4yE-XGDWJPg"),
        },
        new("negative_pull_up", "Negative Pull-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Forearms],
            Summary = "A slow lowering from the top of a pull-up, used to build the strength for your first full reps.",
            Steps =
            [
                "Use a box or jump to get your chin over the bar with an overhand grip.",
                "Hold the top for a moment with your shoulders set down.",
                "Lower yourself as slowly as you can, aiming for 3–5 seconds.",
                "Step back up and repeat.",
            ],
            Tips =
            [
                "Keep the lowering even all the way down, not fast at the end.",
                "Don't drop into the bottom; finish with a controlled hang.",
            ],
            Video = new("gbPURTSxQLY"),
        },
        new("l_sit_pull_up", "L-Sit Pull-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Abs, Biceps],
            Level = Advanced,
            Summary = "A strict pull-up with the legs held straight out in front at hip height, which adds a hard hip flexor and ab hold.",
            Steps =
            [
                "Hang from the bar with an overhand, shoulder-width grip.",
                "Lift your straight legs until they're parallel to the floor.",
                "Pull your chest up to the bar while keeping the legs up.",
                "Lower to straight arms without letting the legs drop.",
            ],
            Tips =
            [
                "Point your toes and lock your knees to keep the shape.",
                "Bend the knees (tuck) if the legs keep dropping.",
            ],
            Video = new("1uzAbJYs_YI"),
        },
        new("archer_pull_up", "Archer Pull-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Forearms],
            Level = Advanced,
            Summary = "A wide-grip pull-up toward one hand while the other arm straightens along the bar, a step toward one-arm pull-ups.",
            Steps =
            [
                "Hang from the bar with a wide overhand grip.",
                "Pull up toward your right hand, letting your left arm straighten out to the side.",
                "Bring your chin over your right hand.",
                "Lower under control and repeat to the other side.",
            ],
            Tips =
            [
                "The straight arm should help as little as you can manage.",
                "Don't rotate your body; keep your chest facing the bar.",
            ],
            Video = new("ziQjz1-DmX8"),
        },
        new("typewriter_pull_up", "Typewriter Pull-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Forearms],
            Level = Advanced,
            Summary = "A wide-grip pull-up where you slide from one hand to the other at the top, keeping your chin at bar height.",
            Steps =
            [
                "Hang from the bar with a wide overhand grip.",
                "Pull up until your chin is level with the bar.",
                "Shift over toward one hand, straightening the other arm, then slide across to the other side.",
                "Return to the middle and lower slowly.",
            ],
            Tips =
            [
                "Keep your chin at bar height the whole way across.",
                "Don't drop between sides; the traverse is the exercise.",
            ],
            Video = new("mDouY-BcgXU"),
        },
        new("scap_pull_up", "Scapular Pull-Up", Back, Bodyweight, Isolation)
        {
            Secondary = [Traps],
            Summary = "Small pulls from a dead hang using only the shoulder blades, to teach the start of a pull-up and strengthen the lower traps and lats.",
            Steps =
            [
                "Hang from the bar with an overhand, shoulder-width grip and relaxed shoulders.",
                "Keeping your arms straight, pull your shoulder blades down and slightly together so your body rises a few centimetres.",
                "Pause for a second at the top.",
                "Lower slowly back to a relaxed hang.",
            ],
            Tips =
            [
                "Your elbows stay straight the whole time.",
                "Don't shrug up; the shoulders move away from the ears.",
            ],
            Video = new("R7sqe83XsRY"),
        },

        // Hangs
        new("dead_hang", "Dead Hang", Forearms, Bodyweight, Isolation)
        {
            Secondary = [Back, Shoulders],
            Hold = true,
            Summary = "Hanging from a bar with straight arms and relaxed shoulders, mainly a grip builder that also decompresses and opens the shoulders.",
            Steps =
            [
                "Grip the bar overhand, about shoulder-width apart.",
                "Lift your feet and hang with straight arms, letting your shoulders rise toward your ears.",
                "Breathe slowly and hold for the target time.",
                "Step down under control.",
            ],
            Tips =
            [
                "Start with 20–30 seconds and build up.",
                "Keep your body still; don't swing.",
                "Step down rather than drop if your grip fails suddenly.",
            ],
            Video = new("dOCQjaasbGs"),
        },
        new("active_hang", "Active Hang", Back, Bodyweight, Isolation)
        {
            Secondary = [Traps, Forearms],
            Hold = true,
            Summary = "A hang with the shoulder blades pulled down and the arms straight, training the shoulder position every pull-up starts from.",
            Steps =
            [
                "Hang from the bar with an overhand, shoulder-width grip.",
                "Pull your shoulders down away from your ears without bending your elbows.",
                "Brace your abs lightly and hold the position.",
                "Relax back to a dead hang before stepping down.",
            ],
            Tips =
            [
                "Think of putting your shoulder blades in your back pockets.",
                "Don't bend the elbows or arch your lower back to cheat the position.",
            ],
            Video = new("aGpThXQhHuA"),
        },

        // Calisthenics skills
        new("muscle_up", "Bar Muscle-Up", Back, Bodyweight, Compound)
        {
            Secondary = [Triceps, Chest, Biceps],
            Level = Advanced,
            Summary = "An explosive pull that carries your body over the bar, then a dip to lockout on top.",
            Steps =
            [
                "Hang from the bar with an overhand grip a little narrower than for pull-ups.",
                "Swing slightly, then pull explosively toward your hips, not your chin.",
                "As your chest reaches the bar, lean forward and whip your elbows over it.",
                "Press down to straight arms above the bar, then lower back under control.",
            ],
            Tips =
            [
                "You need 10+ strict pull-ups and solid straight-bar dips first.",
                "A false grip (wrist over the bar) makes the transition easier.",
                "Don't chicken-wing one elbow over; both arms go over together.",
            ],
            Video = new("ZseoNNiEtIw"),
        },
        new("ring_muscle_up", "Ring Muscle-Up", Back, Other, Compound)
        {
            Secondary = [Triceps, Chest, Biceps],
            Level = Advanced,
            Summary = "A muscle-up on gymnastic rings: a false-grip pull, a deep transition and a ring dip to support on top.",
            Steps =
            [
                "Hang from the rings with a false grip, the heel of each hand over the ring.",
                "Pull the rings to your lower chest, keeping them close to your body.",
                "Lean your chest forward over the rings and turn your elbows back to get on top.",
                "Press out of the deep dip to straight arms, then lower back through the transition.",
            ],
            Tips =
            [
                "Keep the rings close; letting them drift apart kills the transition.",
                "Master ring rows, ring dips and false-grip pull-ups first.",
            ],
            Video = new("972-Ix_0ARI"),
        },
        new("tuck_front_lever", "Tuck Front Lever", Back, Bodyweight, Compound)
        {
            Secondary = [Abs, Shoulders],
            Level = Intermediate,
            Hold = true,
            Summary = "The first front lever progression: hang under the bar with straight arms and hold your back level with the floor, knees tucked to the chest.",
            Steps =
            [
                "Hang from the bar with an overhand, shoulder-width grip.",
                "Pull your shoulders down and tuck your knees tightly to your chest.",
                "Pressing the bar toward your hips with straight arms, lift your hips until your back is horizontal.",
                "Hold, then lower your hips and return to a hang.",
            ],
            Tips =
            [
                "Keep your arms locked; bent elbows turn it into a row.",
                "Round your upper back slightly rather than arching.",
            ],
            Video = new("AGhb8V8M758", 123, 171),
        },
        new("advanced_tuck_front_lever", "Advanced Tuck Front Lever", Back, Bodyweight, Compound)
        {
            Secondary = [Abs, Shoulders],
            Level = Advanced,
            Hold = true,
            Summary = "A tuck front lever with a flat back and the hips opened to 90°, which makes the lever noticeably longer and harder.",
            Steps =
            [
                "Hang from the bar with an overhand, shoulder-width grip.",
                "Get into a tuck front lever with straight arms.",
                "Flatten your back and push your knees away from your chest until your hips are at about 90°.",
                "Hold with your back level, then lower out of it.",
            ],
            Tips =
            [
                "Your back should be flat and horizontal, not rounded.",
                "Don't let your hips sag below your shoulders.",
            ],
            Video = new("AGhb8V8M758", 171, 217),
        },
        new("straddle_front_lever", "Straddle Front Lever", Back, Bodyweight, Compound)
        {
            Secondary = [Abs, Shoulders],
            Level = Advanced,
            Hold = true,
            Summary = "A front lever with straight legs spread wide, the step before the full lever.",
            Steps =
            [
                "Hang from the bar with an overhand, shoulder-width grip.",
                "Pull up into an advanced tuck lever with straight arms.",
                "Extend both legs out to a wide straddle while keeping your body horizontal.",
                "Hold, then tuck back in and lower to a hang.",
            ],
            Tips =
            [
                "The wider the straddle, the easier it is.",
                "Squeeze your glutes and point your toes to keep a straight line.",
            ],
            Video = new("bgRVpNRMjG4"),
        },
        new("front_lever", "Front Lever", Back, Bodyweight, Compound)
        {
            Secondary = [Abs, Shoulders],
            Level = Advanced,
            Hold = true,
            Summary = "Holding your whole body straight and horizontal, face up, beneath a bar with straight arms: a top-tier lat and core strength skill.",
            Steps =
            [
                "Hang from the bar with an overhand, shoulder-width grip.",
                "Pull your shoulders down and press the bar toward your hips with straight arms.",
                "Raise your body until it's straight and level with the floor, legs together.",
                "Hold, then lower with control.",
            ],
            Tips =
            [
                "Build through the tuck, advanced tuck and straddle levers first.",
                "Keep your hips in line; a pike or arch means you're not there yet.",
                "Don't bend your elbows.",
            ],
            Video = new("AGhb8V8M758", 307, 323),
        },
        new("tuck_back_lever", "Tuck Back Lever", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Chest, Biceps, Back],
            Level = Intermediate,
            Hold = true,
            Summary = "The first back lever progression: hang face down beneath a bar or rings with straight arms and your knees tucked.",
            Steps =
            [
                "Grip the bar or rings overhand and tuck your knees to your chest.",
                "Roll backward between your arms into a skin-the-cat position.",
                "Lower your hips until your back is horizontal and face down, arms straight.",
                "Hold, then roll back up and through to come down.",
            ],
            Tips =
            [
                "Build into it slowly; it puts a lot of stretch on the biceps and front of the shoulders.",
                "Keep your elbows locked.",
                "Don't drop into the position quickly.",
            ],
            Video = new("LfyGMZh-Bz4"),
        },
        new("back_lever", "Back Lever", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Chest, Biceps, Back],
            Level = Advanced,
            Hold = true,
            Summary = "Holding your body straight and horizontal, face down, beneath a bar or rings with your arms straight behind you.",
            Steps =
            [
                "Grip the bar or rings and roll back through a tuck into an inverted hang.",
                "Lower your body backward, face down, until it's level with the floor.",
                "Straighten your legs and squeeze your glutes to hold a straight line.",
                "Hold, then tuck and roll back up to exit.",
            ],
            Tips =
            [
                "Progress through tuck and straddle back levers first.",
                "It loads the biceps tendons hard; build up gradually.",
                "Don't pike at the hips or arch the lower back.",
            ],
            Video = new("pQNSaaq1ekI"),
        },

        // Pulldowns
        new("lat_pulldown", "Lat Pulldown", Back, Cable, Compound)
        {
            Secondary = [Biceps],
            Summary = "The cable machine version of a pull-up: pull a wide bar from overhead down to your upper chest.",
            Steps =
            [
                "Set the thigh pad so your legs are locked in, and grip the bar overhand slightly wider than your shoulders.",
                "Sit tall with a slight lean back and your arms straight.",
                "Pull the bar to your upper chest, driving your elbows down and toward your sides.",
                "Let it rise slowly until your arms are straight and your lats stretch.",
            ],
            Tips =
            [
                "Lead with the elbows; your hands are just hooks.",
                "Don't lean far back and turn it into a row.",
                "Don't pull the bar behind your neck.",
            ],
            Video = new("CAwf7n6Luuc"),
        },
        new("close_grip_pulldown", "Close-Grip Lat Pulldown", Back, Cable, Compound)
        {
            Secondary = [Biceps],
            Summary = "A pulldown with a close, neutral V-handle, which gives a longer range of motion and a strong lat contraction.",
            Steps =
            [
                "Attach a V-handle, sit with your thighs under the pad and grip it with palms facing each other.",
                "Sit tall with your arms straight overhead.",
                "Pull the handle to your upper chest, keeping your elbows close to your body.",
                "Return slowly to a full stretch.",
            ],
            Tips =
            [
                "Lean back only slightly and keep your chest up.",
                "Don't round forward at the top of each rep.",
            ],
            Video = new("lYCgyaGpoDk"),
        },
        new("reverse_grip_pulldown", "Underhand Lat Pulldown", Back, Cable, Compound)
        {
            Secondary = [Biceps],
            Summary = "A pulldown with a shoulder-width, palms-up grip, the cable version of a chin-up with more biceps involvement.",
            Steps =
            [
                "Grip the bar underhand at about shoulder width and lock your thighs under the pad.",
                "Sit tall with a slight lean back.",
                "Pull the bar to your upper chest, driving the elbows down to your sides.",
                "Let it rise slowly to straight arms.",
            ],
            Tips =
            [
                "Keep your elbows in front of your body.",
                "Don't curl the bar down with your arms; pull with your back.",
            ],
            Video = new("5YC67yVpBGE"),
        },
        new("single_arm_pulldown", "Single-Arm Lat Pulldown", Back, Cable, Compound)
        {
            Secondary = [Biceps],
            Summary = "A one-arm pulldown with a D-handle on a high cable, for evening out sides and getting a deep lat stretch.",
            Steps =
            [
                "Attach a D-handle to a high cable and sit or half-kneel beneath it.",
                "Grab the handle with one hand, arm straight and slightly in front of you.",
                "Pull your elbow down to your side, turning the palm to face you as you go.",
                "Return slowly until your arm is straight and your lat is stretched.",
            ],
            Tips =
            [
                "Keep your torso square; don't twist to finish the rep.",
                "Start with your weaker side.",
            ],
            Video = new("8zA8DjHRaq0"),
        },
        new("machine_pulldown", "Machine Lat Pulldown", Back, Machine, Compound)
        {
            Secondary = [Biceps],
            Summary = "A pulldown on a plate-loaded or selectorised machine with fixed handles, often with independent arms.",
            Steps =
            [
                "Adjust the seat and thigh pad so the handles are just within reach at full stretch.",
                "Grip the handles and sit tall with your chest up.",
                "Pull the handles down until they reach shoulder level, elbows to your sides.",
                "Let them rise slowly to a full stretch.",
            ],
            Tips =
            [
                "With independent arms, try one side at a time to fix imbalances.",
                "Don't let the weight stack slam between reps.",
            ],
            Video = new("3q1Zsi3vkjo"),
        },
        new("band_pulldown", "Band Lat Pulldown", Back, Band, Compound)
        {
            Secondary = [Biceps],
            Summary = "A pulldown with a resistance band anchored overhead, for training the lats at home or as a warm-up.",
            Steps =
            [
                "Anchor a band high on a door, bar or rack and kneel facing it.",
                "Hold the ends or handles with your arms straight up and slightly forward.",
                "Pull your elbows down to your sides, bringing your hands to shoulder level.",
                "Let the band pull your arms back up slowly.",
            ],
            Tips =
            [
                "Kneel far enough back that there's tension at the top.",
                "Don't let the band yank your arms up.",
            ],
            Video = new("nmY4MTSFln8"),
        },

        // Lat isolation
        new("straight_arm_pulldown", "Straight-Arm Pulldown", Back, Cable, Isolation)
        {
            Summary = "A lat isolation exercise: with arms nearly straight, sweep a cable bar or rope from overhead down to your thighs.",
            Steps =
            [
                "Attach a straight bar or rope to a high cable and step back a little.",
                "Hinge slightly at the hips with your arms long in front of you at eye level, elbows soft.",
                "Sweep the bar down in an arc until it touches your thighs.",
                "Let it rise slowly back to eye level.",
            ],
            Tips =
            [
                "Keep the elbow angle fixed; bending it turns it into a pushdown.",
                "Don't use your body to swing the weight down.",
            ],
            Video = new("hAMcfubonDc"),
        },
        new("band_straight_arm_pulldown", "Band Straight-Arm Pulldown", Back, Band, Isolation)
        {
            Summary = "The straight-arm pulldown with a band anchored high, a handy lat activation drill before pull-ups.",
            Steps =
            [
                "Anchor a band high and hold an end in each hand, stepping back until it's taut.",
                "Hinge slightly with your arms straight in front at eye level.",
                "Sweep your straight arms down to your thighs.",
                "Return slowly to eye level.",
            ],
            Tips =
            [
                "Pause at the bottom and squeeze your lats.",
                "Don't shrug your shoulders up as the arms rise.",
            ],
            Video = new("Oz_C4Mu_ajc"),
        },
        new("cable_lat_prayer", "Cable Lat Prayer", Back, Cable, Isolation)
        {
            Summary = "A kneeling straight-arm pulldown with the hands together on a rope, hinging forward to get a deep lat stretch and contraction.",
            Steps =
            [
                "Attach a rope to a high cable and kneel a step or two back from it.",
                "Hold the rope with your hands together and arms reaching up toward the pulley.",
                "Keeping your elbows soft and fixed, pull your arms down in an arc while hinging your torso forward.",
                "Finish with your hands near your head and upper arms by your ears, then reach back up slowly.",
            ],
            Tips =
            [
                "Let your lats lengthen fully at the top of each rep.",
                "Don't bend the elbows to pull; the movement is at the shoulders.",
            ],
            Video = new("ed7oaLyf9aI"),
        },
        new("pullover_machine", "Pullover Machine", Back, Machine, Isolation)
        {
            Summary = "A machine that isolates the lats by rotating your arms from overhead down to your torso against a pad.",
            Steps =
            [
                "Adjust the seat so your shoulders line up with the machine's pivot and fasten the belt.",
                "Place your elbows or upper arms on the pads with your arms overhead.",
                "Drive the pads down in an arc until the bar reaches your stomach.",
                "Return slowly to the overhead stretch.",
            ],
            Tips =
            [
                "Push with your elbows rather than pulling with your hands.",
                "Don't arch your lower back off the seat at the top.",
            ],
            Video = new("naFwkhf1igc"),
        },

        // Barbell rows
        new("barbell_row", "Barbell Row", Back, Barbell, Compound)
        {
            Secondary = [Biceps, Traps, LowerBack],
            Level = Intermediate,
            Summary = "The classic bent-over row: hinge forward with a flat back and row a barbell to your lower ribs.",
            Steps =
            [
                "Hold the bar with an overhand grip just outside your knees.",
                "Hinge at the hips until your torso is about 45° to the floor, knees soft and back flat.",
                "Row the bar to your lower ribs, driving your elbows back.",
                "Lower it under control until your arms are straight.",
            ],
            Tips =
            [
                "Keep your torso angle fixed; don't stand up as you row.",
                "Brace your abs to protect your lower back.",
                "Don't jerk the weight up with your hips.",
            ],
            Video = new("Nqh7q3zDCoQ"),
        },
        new("pendlay_row", "Pendlay Row", Back, Barbell, Compound)
        {
            Secondary = [Traps, Biceps, LowerBack],
            Level = Intermediate,
            Summary = "A strict barbell row from a dead stop on the floor each rep, with the torso parallel to the ground.",
            Steps =
            [
                "Set up over the bar with feet hip-width, hinge until your back is flat and nearly parallel to the floor.",
                "Grip the bar overhand a little wider than shoulder width.",
                "Row it explosively to your lower chest without lifting your torso.",
                "Lower it back to the floor and let it settle before the next rep.",
            ],
            Tips =
            [
                "Reset your back position on every rep.",
                "Don't let your torso rise to finish the pull.",
                "Needs good hamstring flexibility to keep the back flat.",
            ],
            Video = new("0PSfteHhUtg"),
        },
        new("yates_row", "Yates Row", Back, Barbell, Compound)
        {
            Secondary = [Biceps, Traps],
            Level = Intermediate,
            Summary = "An underhand barbell row with a more upright torso, rowing to the waist so you can use heavy weight with less lower back strain.",
            Steps =
            [
                "Hold the bar with an underhand grip at about shoulder width.",
                "Hinge forward only slightly, torso about 20–30° from upright.",
                "Row the bar to your lower stomach, keeping your elbows close.",
                "Lower it until your arms are straight.",
            ],
            Tips =
            [
                "Squeeze your shoulder blades together at the top.",
                "Don't curl the bar with your biceps or swing with your hips.",
            ],
            Video = new("sNidAp7RWU4"),
        },
        new("seal_row", "Seal Row", Back, Barbell, Compound)
        {
            Secondary = [Traps, Biceps],
            Level = Intermediate,
            Summary = "A barbell row lying face down on a high bench, which removes all momentum and lower back load.",
            Steps =
            [
                "Lie face down on a raised flat bench high enough that your arms hang straight to the bar.",
                "Grip the bar overhand just outside shoulder width.",
                "Row it up until it touches the underside of the bench.",
                "Lower it slowly to straight arms.",
            ],
            Tips =
            [
                "Keep your chest and hips on the bench throughout.",
                "Can be done with dumbbells if you don't have a high bench and bar setup.",
            ],
            Video = new("mil_Gao30-k"),
        },
        new("t_bar_row", "T-Bar Row", Back, Barbell, Compound)
        {
            Secondary = [Biceps, Traps, LowerBack],
            Level = Intermediate,
            Summary = "A row with one end of a barbell anchored in a landmine or corner, pulling a V-handle under the chest.",
            Steps =
            [
                "Load the free end of a landmine bar and straddle it, facing the plates.",
                "Hook a V-handle under the bar by the plates and hinge forward with a flat back.",
                "Row the handle to your chest, driving your elbows back.",
                "Lower it until your arms are straight.",
            ],
            Tips =
            [
                "Use smaller plates for a longer range of motion.",
                "Keep your chest up; don't round over the bar.",
                "Don't jerk the weight up with your legs.",
            ],
            Video = new("Nm3M-4fmprk"),
        },
        new("meadows_row", "Meadows Row", Back, Barbell, Compound)
        {
            Secondary = [Biceps, Traps, Shoulders],
            Level = Intermediate,
            Summary = "A one-arm landmine row standing side-on to the bar and gripping its thick end, named after John Meadows.",
            Steps =
            [
                "Stand side-on to a landmine bar with a staggered stance, the front leg nearest the plates.",
                "Rest your forearm on your front thigh and grip the end of the bar overhand.",
                "Row it up and back, letting your elbow flare slightly.",
                "Lower it until your arm is straight and your lat is stretched.",
            ],
            Tips =
            [
                "Use 10 kg or smaller plates for more range.",
                "Don't twist your torso to lift the weight.",
            ],
            Video = new("uexTkvxwMGM"),
        },

        // Dumbbell and kettlebell rows
        new("db_row", "One-Arm Dumbbell Row", Back, Dumbbell, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A single-arm dumbbell row with one hand and knee on a bench, rowing the weight toward your hip.",
            Steps =
            [
                "Put one knee and the same hand on a flat bench, back flat and parallel to the floor.",
                "Hold a dumbbell in the other hand, arm hanging straight below your shoulder.",
                "Row the dumbbell toward your hip, keeping your elbow close.",
                "Lower it slowly until your arm is straight.",
            ],
            Tips =
            [
                "Pull toward your hip, not your armpit, to bias the lats.",
                "Don't rotate your torso to lift the weight.",
            ],
            Video = new("dFzUjzfih7k"),
        },
        new("db_bent_over_row", "Bent-Over Dumbbell Row", Back, Dumbbell, Compound)
        {
            Secondary = [Biceps, Traps, LowerBack],
            Summary = "A two-arm bent-over row with dumbbells, which lets each arm move freely and the wrists find a neutral position.",
            Steps =
            [
                "Hold a dumbbell in each hand, palms facing each other.",
                "Hinge at the hips until your torso is about 45° to the floor, back flat and knees soft.",
                "Row both dumbbells to your lower ribs.",
                "Lower them slowly until your arms are straight.",
            ],
            Tips =
            [
                "Keep your neck neutral; look at the floor a little ahead.",
                "Don't stand up as you row.",
            ],
            Video = new("5PoEksoJNaw"),
        },
        new("kroc_row", "Kroc Row", Back, Dumbbell, Compound)
        {
            Secondary = [Forearms, Biceps, Traps],
            Level = Advanced,
            Summary = "A heavy, high-rep one-arm dumbbell row with a little controlled body English, built for back size and grip strength.",
            Steps =
            [
                "Brace one hand on a bench or rack with a staggered stance and a heavy dumbbell in the other hand.",
                "Let the dumbbell hang with a slight stretch in your lat.",
                "Row it explosively toward your hip, allowing a small amount of torso movement.",
                "Lower under control and go for high reps, often 15–30.",
            ],
            Tips =
            [
                "Use straps if your grip limits the set.",
                "Some body English is fine; jerking with your lower back isn't.",
                "Save it for the last set of a back session.",
            ],
            Video = new("3rB5euZ9lu0"),
        },
        new("incline_db_row", "Incline Dumbbell Row", Back, Dumbbell, Compound)
        {
            Secondary = [Traps, Biceps, Shoulders],
            Summary = "A chest-supported dumbbell row face down on an incline bench, taking the lower back out of it.",
            Steps =
            [
                "Set a bench to about 30–45° and lie face down with your chest on the pad.",
                "Let the dumbbells hang straight down, palms facing each other.",
                "Row them up until your elbows pass your torso.",
                "Lower them slowly to straight arms.",
            ],
            Tips =
            [
                "Keep your chest on the pad the whole time.",
                "Flare the elbows for upper back, tuck them for lats.",
            ],
            Video = new("2ByilQ4NaAs"),
        },
        new("kettlebell_row", "Kettlebell Row", Back, Kettlebell, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A single-arm bent-over row with a kettlebell, supported on a bench or your knee.",
            Steps =
            [
                "Take a split stance or put one hand on a bench, hinging until your back is flat.",
                "Hold the kettlebell by the handle with your arm straight.",
                "Row it toward your hip, keeping your elbow close.",
                "Lower it slowly until your arm is straight.",
            ],
            Tips =
            [
                "Keep your hips and shoulders square to the floor.",
                "Don't let your lower back round as you lower.",
            ],
            Video = new("TO6UHf5Tsbs"),
        },
        new("gorilla_row", "Kettlebell Gorilla Row", Back, Kettlebell, Compound)
        {
            Secondary = [Biceps, LowerBack, Abs],
            Level = Intermediate,
            Summary = "Alternating rows from a deep, wide hinge with two kettlebells on the floor, pressing into one while rowing the other.",
            Steps =
            [
                "Stand wide with two kettlebells between your feet and hinge down to grip them, back flat.",
                "Press one kettlebell into the floor and row the other to your hip.",
                "Lower it back to the floor.",
                "Switch sides and keep alternating.",
            ],
            Tips =
            [
                "Keep your hips still; don't rotate as you row.",
                "Brace hard; your trunk is working the whole time.",
            ],
            Video = new("7pXpoHBCBxU"),
        },

        // Cable and machine rows
        new("seated_cable_row", "Seated Cable Row", Back, Cable, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A seated row on a low cable with a close V-handle, pulling to your stomach and squeezing the shoulder blades together.",
            Steps =
            [
                "Sit at the cable row with your feet on the platform, knees slightly bent, and grip the V-handle.",
                "Sit tall with your arms straight and your shoulders reaching forward slightly.",
                "Row the handle to your stomach, driving your elbows back past your torso.",
                "Return slowly until your arms are straight.",
            ],
            Tips =
            [
                "Keep your torso nearly upright; a small lean is fine.",
                "Don't rock back and forth to move the weight.",
            ],
            Video = new("8QuMq1GMMng"),
        },
        new("wide_grip_cable_row", "Wide-Grip Cable Row", Back, Cable, Compound)
        {
            Secondary = [Traps, Shoulders, Biceps],
            Summary = "A seated cable row with a wide overhand bar, rowing to the lower chest to bias the upper back and rear delts.",
            Steps =
            [
                "Attach a long bar to the low cable, sit with feet on the platform and grip it overhand, wider than your shoulders.",
                "Sit tall with your arms straight.",
                "Row the bar to your lower chest, elbows flaring to about 45–60° from your body.",
                "Return slowly to straight arms.",
            ],
            Tips =
            [
                "Squeeze your shoulder blades together at the end of each rep.",
                "Don't shrug the bar up toward your neck.",
            ],
            Video = new("p48Cf7htySA"),
        },
        new("single_arm_cable_row", "Single-Arm Cable Row", Back, Cable, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A one-arm cable row with a D-handle, seated or standing, for a longer range and even work on both sides.",
            Steps =
            [
                "Attach a D-handle to a cable at about chest height or low, and sit or stand facing it.",
                "Reach forward with one arm straight, letting your shoulder blade move forward.",
                "Row the handle to your side, driving the elbow back.",
                "Return slowly to full reach.",
            ],
            Tips =
            [
                "Keep your hips and torso square to the cable.",
                "Don't twist to get the last few centimetres.",
            ],
            Video = new("9TWiV80cUYs"),
        },
        new("chest_supported_row", "Chest-Supported Machine Row", Back, Machine, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A row on a machine with a chest pad, plate-loaded or selectorised, so you can push hard without any lower back load.",
            Steps =
            [
                "Adjust the seat so the handles line up with your lower chest and press your chest into the pad.",
                "Grip the handles with arms straight.",
                "Row the handles back, squeezing your shoulder blades together.",
                "Return slowly to a full stretch.",
            ],
            Tips =
            [
                "Keep your chest on the pad; don't lean back to finish.",
                "Try one arm at a time if the handles move independently.",
            ],
            Video = new("FTwvmczf7bE"),
        },
        new("high_row_machine", "Machine High Row", Back, Machine, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A plate-loaded row where the handles start high in front and come down and back, between a pulldown and a row.",
            Steps =
            [
                "Sit with your thighs under the pad and chest against the support if there is one.",
                "Grip the handles above and in front of you.",
                "Pull them down and back toward your lower chest, elbows driving past your sides.",
                "Return slowly to a full stretch.",
            ],
            Tips =
            [
                "Keep your chest up and shoulders down.",
                "Don't let the weight jerk your arms back up.",
            ],
            Video = new("zit_EFtojoE"),
        },

        // Bodyweight and band rows
        new("inverted_row", "Inverted Row", Back, Bodyweight, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A horizontal bodyweight pull: hang under a bar set at waist height with a straight body and pull your chest up to it.",
            Steps =
            [
                "Set a bar in a rack or Smith machine at about waist height and lie under it.",
                "Grip it overhand, a little wider than shoulder width, with your heels on the floor and body straight.",
                "Pull your chest up to the bar, squeezing your shoulder blades together.",
                "Lower until your arms are straight.",
            ],
            Tips =
            [
                "Lower the bar or raise your feet to make it harder.",
                "Keep your hips up; don't let them sag.",
            ],
            Video = new("k3IaBFQuMtM"),
        },
        new("ring_row", "Ring Row", Back, Other, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "An inverted row on gymnastic rings, which lets your hands rotate freely and adds a little stability work.",
            Steps =
            [
                "Set the rings at about waist height and hold them with palms facing each other.",
                "Walk your feet forward and lean back until your arms are straight and your body is in a line.",
                "Pull your chest up between the rings.",
                "Lower slowly to straight arms.",
            ],
            Tips =
            [
                "The more horizontal your body, the harder it is.",
                "Don't let your hips drop or your shoulders shrug.",
            ],
            Video = new("dk6Q-nUx03g"),
        },
        new("trx_row", "TRX Row", Back, Other, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A bodyweight row on a suspension trainer, adjusting difficulty by how far you lean back.",
            Steps =
            [
                "Shorten the suspension trainer straps and hold the handles facing the anchor.",
                "Walk your feet forward and lean back with straight arms and a straight body.",
                "Row your chest up to the handles, elbows close to your sides.",
                "Lower under control to straight arms.",
            ],
            Tips =
            [
                "Walk your feet further forward to make it harder.",
                "Keep your glutes squeezed so your body stays rigid.",
            ],
            Video = new("cOpr2l-Ehso"),
        },
        new("band_row", "Band Row", Back, Band, Compound)
        {
            Secondary = [Biceps, Traps],
            Summary = "A seated or standing row with a resistance band, anchored in front at chest height or looped around your feet.",
            Steps =
            [
                "Anchor a band at chest height, or sit and loop it around your feet.",
                "Hold the ends with arms straight and step or sit back until there's tension.",
                "Row your hands to your ribs, squeezing your shoulder blades together.",
                "Return slowly to straight arms.",
            ],
            Tips =
            [
                "Pause for a second at the end of each rep.",
                "Don't let the band snap your arms forward.",
            ],
            Video = new("LSkyinhmA8k"),
        },

        // Shrugs
        new("barbell_shrug", "Barbell Shrug", Traps, Barbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "The standard upper trap builder: hold a barbell at arm's length and shrug your shoulders straight up.",
            Steps =
            [
                "Hold the bar overhand at about shoulder width, standing tall with arms straight.",
                "Shrug your shoulders straight up toward your ears.",
                "Pause at the top for a second.",
                "Lower slowly to a full stretch.",
            ],
            Tips =
            [
                "Go straight up; don't roll your shoulders.",
                "Keep your arms straight; it's not an upright row.",
                "Use straps if grip gives out before your traps.",
            ],
            Video = new("MlqHEfydPpE"),
        },
        new("db_shrug", "Dumbbell Shrug", Traps, Dumbbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A shrug holding dumbbells at your sides, which lets your arms hang naturally and the shoulders move freely.",
            Steps =
            [
                "Stand tall with a dumbbell in each hand at your sides, palms facing in.",
                "Shrug your shoulders up toward your ears.",
                "Hold the top for a second.",
                "Lower slowly until you feel a stretch.",
            ],
            Tips =
            [
                "Keep your head still and your chin neutral.",
                "Don't bounce the reps.",
            ],
            Video = new("KbxvWGtRXnI"),
        },
        new("trap_bar_shrug", "Trap Bar Shrug", Traps, Barbell, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A shrug inside a trap (hex) bar, with the load at your sides for a natural shoulder line and heavy loading.",
            Steps =
            [
                "Stand inside the trap bar and deadlift it up to standing using the side handles.",
                "Stand tall with arms straight.",
                "Shrug your shoulders straight up and pause.",
                "Lower slowly, then set the bar down with a hip hinge when finished.",
            ],
            Tips =
            [
                "Lift the bar to standing with your legs, not with a rounded back.",
                "Don't roll your shoulders at the top.",
            ],
            Video = new("1JC7QdeUa-0"),
        },
        new("behind_back_shrug", "Behind-the-Back Barbell Shrug", Traps, Barbell, Isolation)
        {
            Secondary = [Forearms],
            Level = Intermediate,
            Summary = "A barbell shrug holding the bar behind your thighs, which pulls the shoulders back slightly and shifts work toward the mid traps.",
            Steps =
            [
                "Set a bar in a rack at mid-thigh height and grip it behind you, overhand at shoulder width.",
                "Step forward to stand tall with the bar against the backs of your thighs.",
                "Shrug your shoulders up and slightly back.",
                "Lower slowly to a full stretch.",
            ],
            Tips =
            [
                "A Smith machine makes the setup easier.",
                "Don't lean forward to make room for the bar.",
            ],
            Video = new("wu9qe0h2Qi0"),
        },
        new("smith_machine_shrug", "Smith Machine Shrug", Traps, Machine, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A barbell shrug on a Smith machine, with a fixed bar path so you can focus purely on the traps.",
            Steps =
            [
                "Set the bar at mid-thigh height and grip it overhand at shoulder width.",
                "Unrack it by twisting the bar and stand tall.",
                "Shrug your shoulders straight up and pause.",
                "Lower slowly, then rack it with a twist.",
            ],
            Tips =
            [
                "Stand so the bar travels close to your thighs.",
                "Don't bend your elbows to help.",
            ],
            Video = new("Z4fFmIB95IA"),
        },
        new("cable_shrug", "Cable Shrug", Traps, Cable, Isolation)
        {
            Secondary = [Forearms],
            Summary = "A shrug with a bar or handles on a low cable, giving steady tension through the whole range.",
            Steps =
            [
                "Attach a straight bar to a low cable, or use two handles from a double low pulley.",
                "Stand close and hold it with straight arms in front of your thighs.",
                "Shrug your shoulders straight up and pause.",
                "Lower slowly to a full stretch.",
            ],
            Tips =
            [
                "Stand tall and keep the cable close to your body.",
                "Don't lean back to move the weight.",
            ],
            Video = new("JxaV9n9l2Y8"),
        },
        new("incline_db_shrug", "Incline Dumbbell Shrug", Traps, Dumbbell, Isolation)
        {
            Summary = "A shrug lying face down on an incline bench, pulling the shoulder blades up and together to target the mid traps.",
            Steps =
            [
                "Set a bench to about 30–45° and lie face down with a dumbbell in each hand.",
                "Let your arms hang straight and your shoulder blades spread apart.",
                "Shrug your shoulder blades up and together without bending your elbows.",
                "Pause, then lower slowly.",
            ],
            Tips =
            [
                "The movement is small; don't turn it into a row.",
                "Keep your chest on the pad.",
            ],
            Video = new("J4wEVDwZsoE"),
        },
        new("overhead_shrug", "Overhead Shrug", Traps, Barbell, Isolation)
        {
            Secondary = [Shoulders],
            Level = Intermediate,
            Summary = "A shrug with a barbell locked out overhead in a wide grip, training the upper traps in the position used for pressing and snatching.",
            Steps =
            [
                "Press or push press a barbell overhead and hold it with a wide, snatch-width grip.",
                "Lock your elbows with the bar over the back of your neck.",
                "Shrug your shoulders up toward your ears, then lower them slightly.",
                "Repeat for reps, then lower the bar to your shoulders under control.",
            ],
            Tips =
            [
                "Use a light weight; the overhead lockout is the hard part.",
                "Keep your ribs down and don't arch your lower back.",
            ],
            Video = new("XWjD5aK8gTQ"),
        },
    ];
}
