using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] MobilityExercises() =>
    [
        // Controlled articular rotations
        new("mob_shoulder_cars", "Shoulder CARs", Shoulders, Bodyweight, Isolation)
        {
            Secondary = [Back],
            Category = Mobility,
            Summary = "Controlled articular rotations for the shoulder: the arm traces the biggest circle it can, slowly, while the rest of the body stays still.",
            Steps =
            [
                "Stand tall, brace your core and make a tight fist with your free hand to lock your trunk in place.",
                "Raise the working arm forward and up overhead as high as it will go, thumb leading.",
                "Turn the palm out and continue the circle behind you, reaching back and down as far as you can.",
                "Bring the arm down to your side to finish the circle, then reverse the direction.",
                "Do 3–5 slow circles each way per arm.",
            ],
            Tips =
            [
                "Move as slowly as you can while still moving: tension, not speed.",
                "Don't let your ribs flare or your torso twist to fake extra range.",
                "Stay inside a pain-free range; the circle grows over weeks.",
            ],
            Video = new("ghXn2-ZYfU4"),
        },
        new("mob_hip_cars", "Hip CARs", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Abs],
            Category = Mobility,
            Summary = "Controlled articular rotations for the hip: the knee draws a slow, full circle through flexion, abduction, rotation and extension, on all fours or standing.",
            Steps =
            [
                "Set up on hands and knees, or stand holding a support, with your core braced.",
                "Pull one knee toward your chest as far as it will go.",
                "Open the knee out to the side, then rotate the hip so the foot swings up and the knee drops back behind you.",
                "Extend the leg back and bring the knee home to the start, then reverse the circle.",
                "Do 3–5 slow circles each way per leg.",
            ],
            Tips =
            [
                "Only the hip should move; keep your pelvis and spine still.",
                "Don't swing the leg or use momentum through the hard part of the circle.",
            ],
            Video = new("PO1of6rKX3Q"),
        },
        new("mob_ankle_cars", "Ankle CARs", Calves, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "Slow, controlled circles of the ankle through its full range to keep the joint and lower-leg muscles mobile.",
            Steps =
            [
                "Sit or stand on one leg and hold your shin still with your hand or by bracing the thigh.",
                "Pull your toes up toward your shin as far as they go.",
                "Turn the sole of the foot in, point the toes down, then turn the sole out to complete the circle.",
                "Do 3–5 slow circles each way per ankle.",
            ],
            Tips =
            [
                "Keep the shin still so the movement comes only from the ankle.",
                "Don't rush through the stiff corners of the circle.",
            ],
            Video = new("vIDJiMShg4o"),
        },

        // Hips
        new("mob_90_90", "90/90 Hip Switch", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Abs],
            Category = Mobility,
            Summary = "Seated hip rotation drill: from the 90/90 position you rotate both legs side to side, taking one hip into internal rotation and the other into external rotation.",
            Steps =
            [
                "Sit with both knees bent to 90°, one leg in front of you and one out to the side, feet flat on the floor.",
                "Sit tall, with your hands behind you for support if you need them.",
                "Lift both knees and rotate them over to the other side until you land in the mirrored 90/90 position.",
                "Pause, sit tall, then switch back. Do 6–10 switches.",
            ],
            Tips =
            [
                "Keep your chest up and turn from the hips, not the lower back.",
                "Progress by taking your hands off the floor.",
                "Don't force the knees down; let the range build over time.",
            ],
            Video = new("qq_Z7sAmVrA"),
        },
        new("mob_hip_airplane", "Hip Airplane", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Abs],
            Category = Mobility,
            Level = Intermediate,
            Summary = "Single-leg balance drill that rotates the pelvis around the standing hip, training hip rotation control and glute strength.",
            Steps =
            [
                "Stand on one leg, hinge forward until your torso and free leg are roughly parallel to the floor, arms out to the sides.",
                "Keeping your standing knee soft, rotate your pelvis and torso open toward the ceiling as far as you can.",
                "Rotate back the other way so your pelvis turns down toward the standing leg.",
                "Do 5–8 slow rotations, then switch legs.",
            ],
            Tips =
            [
                "The rotation comes from the standing hip; keep your torso and free leg moving as one piece.",
                "Hold a wall or rack with one hand until your balance allows a free version.",
                "Don't let the standing knee cave inward.",
            ],
            Video = new("9svtEV4vkp0"),
        },
        new("mob_quadruped_rock_back", "Quadruped Rock-Back", Glutes, Bodyweight, Compound)
        {
            Secondary = [LowerBack],
            Category = Mobility,
            Summary = "On hands and knees you rock your hips back toward your heels, working deep hip flexion while keeping a neutral spine.",
            Steps =
            [
                "Start on all fours with hands under shoulders and knees under hips, knees a little wider than hip-width.",
                "Set a flat back and brace lightly.",
                "Rock your hips back toward your heels as far as you can without your lower back rounding.",
                "Rock forward to the start. Do 10–15 slow reps.",
            ],
            Tips =
            [
                "Stop where your lower back starts to round; that is your current hip flexion range.",
                "Try different knee widths to find where your hips move best.",
            ],
            Video = new("GImwCsuBLyo"),
        },
        new("mob_frog_rock_back", "Frog Rock-Back", Quads, Bodyweight, Isolation)
        {
            Secondary = [Glutes],
            Category = Mobility,
            Summary = "Wide-kneed rock-back that opens the adductors (inner thighs) dynamically.",
            Steps =
            [
                "Kneel on a mat with your knees wide, shins parallel and ankles in line with the knees, resting on your forearms.",
                "Keep a flat back with your hips in line with your knees.",
                "Rock your hips back toward your heels until you feel a stretch in the inner thighs.",
                "Rock forward to the start. Do 10–12 slow reps.",
            ],
            Tips =
            [
                "Pad your knees; only widen them as far as is comfortable.",
                "Don't let your back sag or round as you rock.",
            ],
            Video = new("eSHUKW7eK2M"),
        },
        new("mob_half_kneeling_hip_flexor_rock", "Half-Kneeling Hip Flexor Rock", Abs, Bodyweight, Isolation)
        {
            Secondary = [Quads],
            Category = Mobility,
            Summary = "A dynamic hip flexor opener: from a half-kneeling lunge you rock in and out of the stretch at the front of the rear hip.",
            Steps =
            [
                "Kneel on one knee with the other foot flat in front, both knees at about 90°.",
                "Tuck your pelvis under and squeeze the glute of the kneeling leg.",
                "Shift your hips forward until you feel a stretch at the front of the rear hip, pause for a second, then ease back.",
                "Do 8–12 rocks, then switch sides.",
            ],
            Tips =
            [
                "Keep the glute squeezed so the stretch stays in the hip, not the lower back.",
                "Don't arch your lower back to get further forward.",
            ],
            Video = new("5nDGCpip0Yk"),
        },
        new("mob_leg_swing_front", "Front-to-Back Leg Swing", Hamstrings, Bodyweight, Isolation)
        {
            Secondary = [Abs, Glutes],
            Category = Mobility,
            Summary = "A warm-up drill that swings a straight leg forward and back, dynamically loosening the hamstrings and hip flexors.",
            Steps =
            [
                "Stand side-on to a wall or rack and hold it for balance.",
                "Swing the outside leg forward and up, then back behind you, in a smooth arc.",
                "Let the range grow a little with each swing.",
                "Do 10–15 swings, then switch legs.",
            ],
            Tips =
            [
                "Stand tall; don't round your back to kick higher.",
                "Keep the swing controlled rather than flinging the leg.",
            ],
            Video = new("E68-pMl1Im8"),
        },
        new("mob_leg_swing_side", "Side-to-Side Leg Swing", Quads, Bodyweight, Isolation)
        {
            Secondary = [Glutes],
            Category = Mobility,
            Summary = "A warm-up drill that swings the leg across the body and out to the side, loosening the adductors and outer hip.",
            Steps =
            [
                "Face a wall or rack and hold it with both hands.",
                "Swing one leg across in front of the standing leg, then out to the side.",
                "Let the range grow a little with each swing, keeping your hips facing forward.",
                "Do 10–15 swings, then switch legs.",
            ],
            Tips =
            [
                "Keep your torso upright and your hips square to the wall.",
                "Don't twist your pelvis to make the swing bigger.",
            ],
            Video = new("6aw8CAH_65Y"),
        },
        new("mob_deep_squat_pry", "Deep Squat Prying", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves],
            Category = Mobility,
            Summary = "Sit in the bottom of a deep squat and use your elbows and small weight shifts to pry open the hips and ankles. Holding a light kettlebell at the chest makes it easier to stay upright.",
            Steps =
            [
                "Squat as deep as you can with feet a little wider than hip-width and toes slightly out, holding a light weight at your chest if you like.",
                "Wedge your elbows inside your knees and gently press the knees out.",
                "Shift your weight slowly side to side and forward over each foot, pausing where it feels tight.",
                "Stay in the bottom for 30–60 seconds, standing up to rest when needed.",
            ],
            Tips =
            [
                "Keep your heels down and your chest up.",
                "Let your knees travel forward over your toes; that's where ankle mobility is built.",
                "Don't force depth with a rounded lower back.",
            ],
            Video = new("h6icB2irtCc"),
        },
        new("mob_worlds_greatest_stretch", "World's Greatest Stretch", Abs, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Back, Glutes],
            Category = Mobility,
            Summary = "A flowing lunge sequence that opens the rear hip flexor, the front hip and hamstring, and the upper back in one drill.",
            Steps =
            [
                "Step into a long lunge with your left foot forward and place both hands on the floor inside the left foot.",
                "Drop your left elbow toward the inside of your left ankle.",
                "Rotate your chest open and reach your left arm to the ceiling, eyes following the hand.",
                "Return the hand to the floor, push your hips back and straighten the front leg to stretch the hamstring.",
                "Step through to the other side. Do 3–5 reps per side.",
            ],
            Tips =
            [
                "Keep the back leg straight and the back knee off the floor.",
                "Rotate from the upper back, not by collapsing the hips.",
            ],
            Video = new("-CiWQ2IvY34"),
        },
        new("mob_spiderman_lunge_rotation", "Spiderman Lunge with Rotation", Abs, Bodyweight, Compound)
        {
            Secondary = [Quads, Glutes, Back],
            Category = Mobility,
            Summary = "From a plank you step one foot outside the hand into a deep lunge, then rotate the chest open, stretching the hip flexors, groin and upper back.",
            Steps =
            [
                "Start in a high plank.",
                "Step your right foot to the outside of your right hand, sinking your hips into a deep lunge.",
                "Lift your right hand and rotate your chest open, reaching to the ceiling.",
                "Bring the hand down, step back to plank and repeat on the other side. Do 4–6 reps per side.",
            ],
            Tips =
            [
                "Keep the back leg long and active.",
                "Don't let your hips pike up as you step through.",
            ],
            Video = new("qCDFp8cPrqw"),
        },
        new("mob_walking_lunge_reach", "Walking Lunge with Overhead Reach", Abs, Bodyweight, Compound)
        {
            Secondary = [Quads, Glutes, Back],
            Category = Mobility,
            Summary = "A warm-up walking lunge with both arms reaching overhead and slightly toward the front leg, stretching the rear hip flexor and the side of the trunk.",
            Steps =
            [
                "Step forward into a lunge, lowering the back knee toward the floor.",
                "At the bottom, squeeze the back glute and reach both arms overhead, leaning slightly away from the back leg.",
                "Pause for a breath, then step through into the next lunge.",
                "Do 6–10 lunges per leg.",
            ],
            Tips =
            [
                "Tuck the pelvis under so you feel the stretch at the front of the back hip.",
                "Don't arch your lower back as you reach up.",
            ],
            Video = new("ZREHb_-VRhA"),
        },
        new("mob_inchworm", "Inchworm", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Abs, Shoulders],
            Category = Mobility,
            Summary = "Walk your hands out from a forward fold to a plank and back, a warm-up that stretches the hamstrings and wakes up the shoulders and core.",
            Steps =
            [
                "Stand tall, hinge forward and place your hands on the floor, bending your knees only as much as you need.",
                "Walk your hands forward until you reach a high plank.",
                "Walk your feet in toward your hands with legs as straight as you can, or walk your hands back.",
                "Stand up and repeat. Do 5–8 reps.",
            ],
            Tips =
            [
                "Keep your hips level and core braced in the plank.",
                "Take small steps with your feet to feel the hamstring stretch.",
            ],
            Video = new("ZY2ji_Ho0dA"),
        },

        // Spine
        new("mob_cat_cow", "Cat-Cow", LowerBack, Bodyweight, Isolation)
        {
            Secondary = [Abs, Back],
            Category = Mobility,
            Summary = "On hands and knees you alternate between rounding and arching the spine, moving it gently through flexion and extension.",
            Steps =
            [
                "Start on all fours with hands under shoulders and knees under hips.",
                "Breathe in as you drop your belly, lift your chest and tailbone and look slightly up (cow).",
                "Breathe out as you round your back toward the ceiling, tuck your tailbone and let your head drop (cat).",
                "Move slowly between the two for 8–12 breaths.",
            ],
            Tips =
            [
                "Try to move one vertebra at a time, from the tailbone up.",
                "Don't push into painful end range; the aim is smooth movement.",
            ],
            Video = new("vuyUwtHl694"),
        },
        new("mob_open_book", "Open Book Rotation", Back, Bodyweight, Isolation)
        {
            Secondary = [Chest],
            Category = Mobility,
            Summary = "Side-lying thoracic rotation: the top arm opens like the cover of a book to rotate the upper back and stretch the chest.",
            Steps =
            [
                "Lie on your side with hips and knees bent to 90° and arms straight out in front of you, palms together.",
                "Keep your knees stacked and pressed together.",
                "Sweep the top arm up and over to the other side, rotating your chest toward the ceiling and following the hand with your eyes.",
                "Pause for a breath at the end, then return. Do 8–10 reps per side.",
            ],
            Tips =
            [
                "Keep the knees glued together so the twist comes from your upper back, not the lower back.",
                "Don't force the hand to the floor; open only as far as your back allows.",
            ],
            Video = new("Tqv_2oe64vk"),
        },
        new("mob_quadruped_tspine_rotation", "Quadruped Thoracic Rotation", Back, Bodyweight, Isolation)
        {
            Secondary = [Shoulders],
            Category = Mobility,
            Summary = "On all fours with one hand behind your head, you rotate the upper back down and up, building thoracic rotation without twisting the lower back.",
            Steps =
            [
                "Start on all fours and sit your hips back slightly to lock the lower back.",
                "Place one hand behind your head, elbow pointing to the side.",
                "Rotate that elbow down toward the opposite wrist.",
                "Rotate up and open, pointing the elbow at the ceiling. Do 8–10 reps per side.",
            ],
            Tips =
            [
                "Follow the elbow with your eyes and breathe out as you open.",
                "Keep your hips still; only the upper back should turn.",
            ],
            Video = new("QWwiOHexU8I"),
        },
        new("mob_thread_the_needle", "Thread the Needle", Back, Bodyweight, Isolation)
        {
            Secondary = [Shoulders, Traps],
            Category = Mobility,
            Summary = "On all fours you slide one arm under the other and rest on that shoulder, rotating and stretching the upper back and rear shoulder.",
            Steps =
            [
                "Start on all fours with hands under shoulders and knees under hips.",
                "Reach one arm up toward the ceiling to open the chest.",
                "Sweep that arm down and thread it under the other arm, palm up, lowering the shoulder and side of the head to the floor.",
                "Rest for a breath or two, then return. Do 6–8 reps per side.",
            ],
            Tips =
            [
                "Keep your hips over your knees.",
                "Don't put weight on your neck; the shoulder takes the load.",
            ],
            Video = new("SkQhKf74nZk"),
        },
        new("mob_prone_scorpion", "Prone Scorpion", Abs, Bodyweight, Compound)
        {
            Secondary = [LowerBack, Chest],
            Category = Mobility,
            Level = Intermediate,
            Summary = "Lying face down, you swing one foot up and across toward the opposite hand, rotating the spine and opening the hip flexors and chest.",
            Steps =
            [
                "Lie face down with your arms out to the sides in a T.",
                "Bend one knee and lift that leg, reaching the foot across your body toward the opposite hand.",
                "Let your hips rotate but keep both shoulders as close to the floor as you can.",
                "Return under control and alternate sides. Do 6–8 reps per side.",
            ],
            Tips =
            [
                "Move slowly and stay in a comfortable range.",
                "Skip this one if back extension with rotation aggravates your lower back.",
            ],
            Video = new("Ta1wtgZCsVE"),
        },
        new("mob_foam_roller_thoracic_extension", "Foam Roller Thoracic Extension", Back, Other, Isolation)
        {
            Category = Mobility,
            Summary = "Lying over a foam roller, you extend the upper back over it segment by segment to undo a hunched posture.",
            Steps =
            [
                "Sit on the floor with a foam roller across your upper back, just below the shoulder blades.",
                "Support your head with your hands, elbows in, and lift your hips or keep them down.",
                "Extend your upper back over the roller, letting your head drop back slightly, then return.",
                "Do 5 reps, move the roller up an inch, and repeat 2–3 times.",
            ],
            Tips =
            [
                "Keep your ribs down so the bend happens in the upper back, not the lower back.",
                "Don't roll onto the lower back or the neck.",
            ],
            Video = new("81kPLsMt6wY"),
        },

        // Ankles
        new("mob_ankle_dorsiflexion_rock", "Knee-to-Wall Ankle Rock", Calves, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "In a half-kneeling or split stance by a wall, you drive the front knee forward over the toes to improve ankle dorsiflexion for squats and lunges.",
            Steps =
            [
                "Kneel in front of a wall with your front foot a few inches from it.",
                "Keeping the heel down, drive the knee forward toward the wall over your middle toes.",
                "Pause for a second at the end, then ease back.",
                "Do 10–15 rocks per side, moving the foot back as you get more range.",
            ],
            Tips =
            [
                "The heel must stay planted; that's the whole point.",
                "Don't let the arch collapse or the knee fall inward.",
            ],
            Video = new("ElrpduJn92Y"),
        },
        new("mob_banded_ankle_distraction", "Banded Ankle Distraction", Calves, Band, Isolation)
        {
            Category = Mobility,
            Summary = "A heavy band anchored behind you pulls the ankle joint back while you drive the knee forward, freeing up stiff dorsiflexion.",
            Steps =
            [
                "Loop a heavy band around a low anchor and around the front of your ankle, just below the ankle bones.",
                "Step forward until the band is tight, into a half-kneeling or split stance.",
                "Drive the knee forward over the toes, heel down, pause, then ease back.",
                "Do 10–15 reps per side.",
            ],
            Tips =
            [
                "Set the band low on the front of the ankle, not on the shin.",
                "Keep the heel planted throughout.",
            ],
            Video = new("ILSbK8RnGdI", 22),
        },

        // Shoulders and wrists
        new("mob_band_shoulder_dislocate", "Band Shoulder Dislocate", Shoulders, Band, Isolation)
        {
            Secondary = [Chest, Back],
            Category = Mobility,
            Summary = "Holding a light band or dowel with a wide grip, you pass it from in front of your hips over your head to behind your back and back again.",
            Steps =
            [
                "Hold a light band or dowel in front of your thighs with a wide overhand grip.",
                "Keep your arms straight and raise it up over your head.",
                "Continue the arc behind you until it touches your lower back or glutes.",
                "Reverse the path to the front. Do 10–15 slow reps.",
            ],
            Tips =
            [
                "Grip wide enough that you never have to bend your elbows.",
                "Narrow the grip gradually as your shoulders loosen up.",
                "Don't arch your lower back to get the bar over.",
            ],
            Video = new("qG_pdxLm4t8"),
        },
        new("mob_arm_circles", "Arm Circles", Shoulders, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "Straight-arm circles forward and backward, growing from small to large, a simple shoulder warm-up.",
            Steps =
            [
                "Stand tall with your arms out to the sides at shoulder height.",
                "Make small circles forward, gradually making them bigger.",
                "After 10–15 circles, reverse the direction.",
            ],
            Tips =
            [
                "Keep your shoulders down away from your ears.",
                "Use controlled circles rather than flinging the arms.",
            ],
            Video = new("3STTSi_jdHk"),
        },
        new("mob_wrist_circles", "Wrist Circles", Forearms, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "Slow circles of the wrists in both directions to warm up the wrists and forearms before pressing, handstands or yoga.",
            Steps =
            [
                "Interlace your fingers or make loose fists, elbows bent.",
                "Roll your wrists in big, slow circles.",
                "Do 10 circles, then reverse the direction.",
            ],
            Tips =
            [
                "Try to reach the end of the range in every direction.",
                "Keep your forearms still so the wrists do the moving.",
            ],
            Video = new("wRSk1_C6yOM"),
        },
        new("mob_quadruped_wrist_rocks", "Quadruped Wrist Rocks", Forearms, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "On all fours you rock your weight over your hands in different positions to prepare the wrists for loaded extension.",
            Steps =
            [
                "Start on all fours with hands under shoulders, fingers forward.",
                "Shift your weight forward over your hands, then back. Do 10 rocks.",
                "Turn the fingers to point back toward your knees and gently rock back. Do 10 rocks.",
                "Turn the hands over onto their backs and rock gently. Do 10 rocks.",
            ],
            Tips =
            [
                "Start with light pressure and increase it gradually.",
                "Spread your fingers and press through the whole hand.",
            ],
            Video = new("r1HERKUN5Vw"),
        },

        // Soft-tissue work
        new("mob_foam_roll_quads", "Foam Roll Quads", Quads, Other, Isolation)
        {
            Category = Mobility,
            Summary = "Face-down foam rolling of the front of the thighs to ease stiffness before or after training. Needs a foam roller.",
            Steps =
            [
                "Lie face down with a foam roller under your thighs, resting on your forearms.",
                "Roll slowly from just above the knees up to the hips.",
                "Pause on tender spots for a few breaths, bending and straightening the knee.",
                "Roll for 30–90 seconds, one leg at a time for more pressure.",
            ],
            Tips =
            [
                "Keep your core braced so your lower back doesn't sag.",
                "Don't roll directly over the kneecap.",
            ],
            Video = new("1XzS9y-vJD8"),
        },
        new("mob_foam_roll_it_band", "Foam Roll IT Band", Quads, Other, Isolation)
        {
            Secondary = [Glutes],
            Category = Mobility,
            Summary = "Side-lying foam rolling of the outer thigh, from the hip to just above the knee. Needs a foam roller.",
            Steps =
            [
                "Lie on your side with the roller under the outer thigh, top leg crossed in front with the foot on the floor.",
                "Support yourself on your forearm and top foot.",
                "Roll slowly from just below the hip to just above the knee.",
                "Roll for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Take weight through the top foot to control the pressure; this area is often tender.",
                "Tilt slightly forward to reach the front of the outer thigh.",
                "Don't roll over the side of the knee.",
            ],
            Video = new("Sr9RWVMzyi8"),
        },
        new("mob_foam_roll_upper_back", "Foam Roll Upper Back", Back, Other, Isolation)
        {
            Secondary = [Traps],
            Category = Mobility,
            Summary = "Lying back over a foam roller, you roll the muscles between and around the shoulder blades. Needs a foam roller.",
            Steps =
            [
                "Lie on your back with the roller under your upper back, knees bent and feet flat.",
                "Cross your arms over your chest to spread your shoulder blades.",
                "Lift your hips and roll slowly from the bottom of your shoulder blades to the top.",
                "Roll for 30–90 seconds.",
            ],
            Tips =
            [
                "Support your head with your hands if your neck gets tired.",
                "Don't roll down onto the lower back.",
            ],
            Video = new("X8P9KSaYOkE", 95, 142),
        },
        new("mob_foam_roll_lats", "Foam Roll Lats", Back, Other, Isolation)
        {
            Category = Mobility,
            Summary = "Side-lying foam rolling of the lat, along the side of the back under the armpit. Needs a foam roller.",
            Steps =
            [
                "Lie on your side with the roller under your armpit and that arm stretched overhead.",
                "Turn slightly toward your back.",
                "Roll slowly from the armpit down to the bottom of the rib cage.",
                "Roll for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Small rotations toward the front and back find different parts of the muscle.",
                "Keep the movement slow.",
            ],
            Video = new("1GaR-a9TWYM"),
        },
        new("mob_foam_roll_calves", "Foam Roll Calves", Calves, Other, Isolation)
        {
            Category = Mobility,
            Summary = "Seated foam rolling of the calves, from the ankle to below the knee. Needs a foam roller.",
            Steps =
            [
                "Sit with the roller under your calves and your hands on the floor behind you.",
                "Lift your hips and roll from just above the ankles to just below the knees.",
                "Turn the legs in and out to cover the inner and outer calf.",
                "Roll for 30–60 seconds, stacking one leg on the other for more pressure.",
            ],
            Tips =
            [
                "Pause and point and flex the foot on tight spots.",
                "Don't roll on the back of the knee.",
            ],
            Video = new("nZZe9ai7Vvw"),
        },
        new("mob_foam_roll_glutes", "Foam Roll Glutes", Glutes, Other, Isolation)
        {
            Category = Mobility,
            Summary = "Seated foam rolling of the glutes with one ankle crossed over the opposite knee. Needs a foam roller.",
            Steps =
            [
                "Sit on the roller and cross one ankle over the opposite knee.",
                "Lean toward the side of the crossed leg, hands on the floor behind you.",
                "Roll slowly over the glute in small movements.",
                "Roll for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Lean further to reach the outer hip.",
                "Control the pressure with your hands and supporting foot.",
            ],
            Video = new("G8zhISjVxfU"),
        },
        new("mob_foam_roll_hamstrings", "Foam Roll Hamstrings", Hamstrings, Other, Isolation)
        {
            Category = Mobility,
            Summary = "Seated foam rolling of the back of the thighs. Needs a foam roller.",
            Steps =
            [
                "Sit with the roller under your thighs and your hands on the floor behind you.",
                "Lift your hips and roll from just above the knees to just below the glutes.",
                "Turn the legs in and out to cover the inner and outer hamstrings.",
                "Roll for 30–60 seconds, one leg at a time for more pressure.",
            ],
            Tips =
            [
                "Keep your arms straight and shoulders down.",
                "Don't roll on the back of the knee.",
            ],
            Video = new("lLswx0vSvtk"),
        },
        new("mob_ball_pec_release", "Lacrosse Ball Pec Release", Chest, Other, Isolation)
        {
            Secondary = [Shoulders],
            Category = Mobility,
            Summary = "Pinning a lacrosse or massage ball against a wall with your chest to release tight pecs and the front of the shoulder.",
            Steps =
            [
                "Place a lacrosse ball on your chest just below the collarbone and inside the shoulder, then lean into a wall or doorframe.",
                "Find a tender spot and hold gentle pressure for a few breaths.",
                "Slowly move the arm out to the side and back down while keeping the pressure.",
                "Work for 30–90 seconds per side.",
            ],
            Tips =
            [
                "Stay on muscle; avoid the collarbone, armpit and any spot that tingles or goes numb.",
                "Moderate pressure is enough.",
            ],
            Video = new("b2P0ZB9ASME"),
        },
        new("mob_ball_glute_release", "Lacrosse Ball Glute Release", Glutes, Other, Isolation)
        {
            Category = Mobility,
            Summary = "Sitting or leaning on a lacrosse or massage ball to work into tight spots in the glutes and deep hip rotators.",
            Steps =
            [
                "Sit on the floor with a lacrosse ball under one glute, or pin it between your glute and a wall.",
                "Roll slowly until you find a tender spot.",
                "Hold gentle pressure and breathe, letting the leg fall in and out to move the muscle under the ball.",
                "Work for 30–90 seconds per side.",
            ],
            Tips =
            [
                "Stay off the tailbone and the bony points of the hip.",
                "Stop if you feel shooting or tingling pain down the leg.",
            ],
            Video = new("tH1qhsuO4AE", 15, 128),
        },

        // Upper body stretches
        new("stretch_doorway_chest", "Doorway Chest Stretch", Chest, Bodyweight, Isolation)
        {
            Secondary = [Shoulders],
            Category = ExerciseCategory.Stretch,
            Summary = "With your forearms on a doorframe you step through to stretch the pecs and front delts.",
            Steps =
            [
                "Stand in a doorway and place your forearms on the frame, elbows at about shoulder height.",
                "Step one foot forward through the doorway.",
                "Lean your chest forward until you feel a stretch across the front of the chest and shoulders.",
                "Hold for 30–60 seconds, breathing slowly.",
            ],
            Tips =
            [
                "Raise or lower the elbows to shift the stretch to different parts of the chest.",
                "Keep your ribs down; don't arch your lower back to lean further.",
                "Back off if you feel pinching in the front of the shoulder.",
            ],
            Video = new("B9uY01NoqBg"),
        },
        new("stretch_behind_back_chest", "Behind-the-Back Chest Stretch", Chest, Bodyweight, Isolation)
        {
            Secondary = [Shoulders, Biceps],
            Category = ExerciseCategory.Stretch,
            Summary = "Clasping your hands behind your back and lifting them away from you to open the chest and front shoulders, no equipment needed.",
            Steps =
            [
                "Stand tall and interlace your fingers behind your back.",
                "Straighten your arms and squeeze your shoulder blades together.",
                "Lift your hands away from your back until you feel a stretch across the chest and shoulders.",
                "Hold for 30–45 seconds.",
            ],
            Tips =
            [
                "Keep your chin level and your chest lifted.",
                "Hold a towel between your hands if you can't clasp them.",
            ],
            Video = new("bbhQYh-Dfm4"),
        },
        new("stretch_cross_body_shoulder", "Cross-Body Shoulder Stretch", Shoulders, Bodyweight, Isolation)
        {
            Secondary = [Back],
            Category = ExerciseCategory.Stretch,
            Summary = "Pulling one arm across your chest with the other to stretch the back of the shoulder.",
            Steps =
            [
                "Stand or sit tall and bring one straight arm across your chest.",
                "Hold it above the elbow with the other hand.",
                "Pull the arm gently toward your chest until you feel a stretch at the back of the shoulder.",
                "Hold for 30 seconds per side.",
            ],
            Tips =
            [
                "Keep the shoulder of the stretched arm down, not hunched up.",
                "Pull at the upper arm, not the elbow joint.",
            ],
            Video = new("swvXpKN832E"),
        },
        new("stretch_sleeper", "Sleeper Stretch", Shoulders, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Lying on your side, you press the forearm toward the floor to stretch the shoulder's internal rotation and the back of the rotator cuff.",
            Steps =
            [
                "Lie on one side with the bottom arm out in front at shoulder height and the elbow bent to 90°, fingers to the ceiling.",
                "Roll back slightly so you're not lying directly on the shoulder.",
                "Use the top hand to gently press the bottom forearm down toward the floor.",
                "Hold for 30 seconds when you feel a stretch at the back of the shoulder, then switch sides.",
            ],
            Tips =
            [
                "Use gentle pressure; this is a mild stretch, not a forced one.",
                "Stop if you feel pinching at the front of the shoulder.",
            ],
            Video = new("9BN8bRVq3Xo"),
        },
        new("stretch_overhead_triceps", "Overhead Triceps Stretch", Triceps, Bodyweight, Compound)
        {
            Secondary = [Back],
            Category = ExerciseCategory.Stretch,
            Summary = "Reaching one hand down your back with the elbow pointing up to stretch the triceps and lats.",
            Steps =
            [
                "Raise one arm overhead and bend the elbow so your hand reaches down between your shoulder blades.",
                "Hold the elbow with your other hand.",
                "Gently pull the elbow back and toward your head until you feel a stretch in the back of the upper arm.",
                "Hold for 30 seconds per side.",
            ],
            Tips =
            [
                "Keep your head up and your ribs down.",
                "Don't push your head forward to make room for the arm.",
            ],
            Video = new("qsyrvNY8uBA"),
        },
        new("stretch_wall_biceps", "Wall Biceps Stretch", Biceps, Bodyweight, Compound)
        {
            Secondary = [Chest, Shoulders],
            Category = ExerciseCategory.Stretch,
            Summary = "With a straight arm against a wall behind you, you turn away to stretch the biceps and front of the shoulder.",
            Steps =
            [
                "Stand side-on to a wall and place your palm on it slightly behind you at shoulder height, thumb up, arm straight.",
                "Slowly turn your chest away from the wall.",
                "Stop when you feel a stretch along the front of the arm and shoulder.",
                "Hold for 30 seconds per side.",
            ],
            Tips =
            [
                "Keep the elbow straight but not locked hard.",
                "Moving the hand lower stretches the biceps more and the chest less.",
            ],
            Video = new("QY4gCIYbGQk"),
        },
        new("stretch_bench_lat", "Kneeling Bench Lat Stretch", Back, Bodyweight, Compound)
        {
            Secondary = [Triceps, Chest],
            Category = ExerciseCategory.Stretch,
            Summary = "Kneeling in front of a bench with your elbows on it, you sink your chest toward the floor to stretch the lats and triceps.",
            Steps =
            [
                "Kneel facing a bench and rest your elbows on it, shoulder-width apart.",
                "Bend your elbows so your hands meet behind your head, or hold a dowel.",
                "Sit your hips back and let your chest sink toward the floor.",
                "Hold for 30–60 seconds.",
            ],
            Tips =
            [
                "Keep a neutral back; brace your ribs down so the stretch goes to the lats, not the lower back.",
                "Breathe out to sink a little deeper.",
            ],
            Video = new("6Fc0u9xPkL8"),
        },
        new("stretch_childs_pose_side_reach", "Child's Pose Side Reach", Back, Bodyweight, Compound)
        {
            Secondary = [LowerBack],
            Category = ExerciseCategory.Stretch,
            Summary = "A child's pose with both hands walked over to one side, stretching the lat and side of the trunk on the opposite side.",
            Steps =
            [
                "Kneel and sit your hips back toward your heels, arms stretched out in front on the floor.",
                "Walk both hands over to the right.",
                "Press the left hand into the floor and sink your left hip back until you feel a stretch along the left side.",
                "Hold for 30–45 seconds, then switch sides.",
            ],
            Tips =
            [
                "Keep your hips sitting back toward your heels.",
                "Breathe into the side that's stretching.",
            ],
            Video = new("2FMOcD_jY5A"),
        },
        new("stretch_upper_back_hug", "Upper Back Hug Stretch", Back, Bodyweight, Isolation)
        {
            Secondary = [Traps, Shoulders],
            Category = ExerciseCategory.Stretch,
            Summary = "Hugging yourself and rounding forward to spread the shoulder blades and stretch the upper back.",
            Steps =
            [
                "Stand or sit tall and wrap your arms around yourself, hands reaching for the opposite shoulder blades.",
                "Tuck your chin and round your upper back.",
                "Pull your shoulder blades apart until you feel a stretch between them.",
                "Hold for 30 seconds, then swap which arm is on top and repeat.",
            ],
            Tips =
            [
                "Breathe into your upper back to deepen the stretch.",
                "Keep your lower back still; round only the upper back.",
            ],
            Video = new("eogvpyw4R3E"),
        },
        new("stretch_wrist_flexor", "Wrist Flexor Stretch", Forearms, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Pulling the fingers back with the arm straight to stretch the muscles on the palm side of the forearm.",
            Steps =
            [
                "Hold one arm straight out in front, palm facing forward and fingers pointing up.",
                "With the other hand, gently pull the fingers back toward you.",
                "Hold for 20–30 seconds when you feel a stretch along the inside of the forearm, then switch.",
            ],
            Tips =
            [
                "Keep the elbow straight for the strongest stretch.",
                "Apply gentle pressure; don't crank the fingers.",
            ],
            Video = new("i-JV2PsFzWA"),
        },
        new("stretch_wrist_extensor", "Wrist Extensor Stretch", Forearms, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Bending the hand down with the arm straight to stretch the muscles on the back of the forearm.",
            Steps =
            [
                "Hold one arm straight out in front with the palm facing down.",
                "Let the hand drop so the fingers point to the floor.",
                "With the other hand, gently press the back of the hand toward you.",
                "Hold for 20–30 seconds, then switch.",
            ],
            Tips =
            [
                "Keep the elbow straight.",
                "Making a loose fist increases the stretch.",
            ],
            Video = new("_uINTR_7X-g"),
        },

        // Hamstring stretches
        new("stretch_standing_hamstring", "Standing Hamstring Stretch", Hamstrings, Bodyweight, Isolation)
        {
            Secondary = [Calves],
            Category = ExerciseCategory.Stretch,
            Summary = "With one heel on a step or low bench, you hinge forward from the hips to stretch the back of the thigh.",
            Steps =
            [
                "Stand facing a low step or bench and place one heel on it, leg straight and toes up.",
                "Keep your back flat and your hips square.",
                "Hinge forward from the hips until you feel a stretch behind the thigh.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Lead with your chest, not your head; rounding the back takes the stretch away from the hamstring.",
                "A slight bend in the standing knee is fine.",
            ],
            Video = new("qQ26F282VRo"),
        },
        new("stretch_seated_hamstring", "Seated Single-Leg Hamstring Stretch", Hamstrings, Bodyweight, Isolation)
        {
            Secondary = [LowerBack, Calves],
            Category = ExerciseCategory.Stretch,
            Summary = "Seated with one leg straight and the other foot tucked in, you fold forward over the straight leg.",
            Steps =
            [
                "Sit on the floor with one leg straight and the other bent, its foot against the inside of the straight thigh.",
                "Sit tall and turn your chest to face the straight leg.",
                "Hinge forward from the hips and reach toward your foot.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Sit on a folded towel if your lower back rounds before you can lean forward.",
                "Think about bringing your belly to your thigh, not your nose to your knee.",
            ],
            Video = new("aJvfeuu71gw"),
        },
        new("stretch_supine_hamstring_strap", "Supine Hamstring Strap Stretch", Hamstrings, Band, Isolation)
        {
            Secondary = [Calves],
            Category = ExerciseCategory.Stretch,
            Summary = "Lying on your back with a strap or band around one foot, you draw the straight leg up to stretch the hamstring without loading the lower back.",
            Steps =
            [
                "Lie on your back with a strap, band or towel looped around one foot.",
                "Straighten that leg toward the ceiling, keeping the other leg flat on the floor or bent with the foot down.",
                "Gently pull the leg toward you until you feel a stretch behind the thigh.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Keep your hips and lower back flat on the floor.",
                "Relax your shoulders and let the strap do the work.",
            ],
            Video = new("Il1L75v6gq0"),
        },

        // Quad and hip flexor stretches
        new("stretch_standing_quad", "Standing Quad Stretch", Quads, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Standing on one leg, you pull the other heel to your glute to stretch the front of the thigh.",
            Steps =
            [
                "Stand tall, holding a wall for balance if needed.",
                "Bend one knee and hold that ankle or foot behind you.",
                "Pull the heel toward your glute with your knees together and the knee pointing down.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Tuck your pelvis slightly to feel more stretch at the top of the thigh.",
                "Don't arch your lower back or let the knee drift forward or out.",
            ],
            Video = new("j-KreYRNOCc"),
        },
        new("stretch_side_lying_quad", "Side-Lying Quad Stretch", Quads, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Lying on your side, you pull the top heel to your glute: a quad stretch without the balance demand.",
            Steps =
            [
                "Lie on your side with your head resting on your bottom arm and the bottom knee bent for stability.",
                "Bend the top knee and hold that ankle behind you.",
                "Pull the heel toward your glute and ease the knee slightly back.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Keep your knees in line; don't let the top knee float up.",
                "Use a strap around the foot if you can't reach it.",
            ],
            Video = new("Z9NPI_3HXUY"),
        },
        new("stretch_couch", "Couch Stretch", Quads, Bodyweight, Compound)
        {
            Secondary = [Abs],
            Category = ExerciseCategory.Stretch,
            Level = Intermediate,
            Summary = "A deep stretch for the quads and hip flexors with the back knee on the floor and the back shin up against a wall or couch.",
            Steps =
            [
                "Kneel with your back to a wall or couch and slide one knee into the corner, shin and top of the foot up against it.",
                "Step the other foot forward into a lunge, foot flat.",
                "Squeeze the glute of the back leg and slowly raise your torso toward upright.",
                "Hold for 60–120 seconds per side.",
            ],
            Tips =
            [
                "Pad the back knee.",
                "Keep your ribs down and pelvis tucked; arching the lower back defeats the stretch.",
                "Start with your hands on the front knee or a box until you can sit upright.",
            ],
            Video = new("Fg-lwNBzVV8"),
        },
        new("stretch_kneeling_hip_flexor", "Kneeling Hip Flexor Stretch", Abs, Bodyweight, Isolation)
        {
            Secondary = [Quads],
            Category = ExerciseCategory.Stretch,
            Summary = "A half-kneeling stretch for the front of the rear hip, made effective by tucking the pelvis and squeezing the glute.",
            Steps =
            [
                "Kneel on one knee with the other foot flat in front, both knees at about 90°.",
                "Tuck your pelvis under and squeeze the glute of the kneeling leg.",
                "Shift your hips slightly forward until you feel a stretch at the front of the rear hip.",
                "Reach the arm on the kneeling side overhead to deepen it. Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Small forward shift, big glute squeeze; that's where the stretch comes from.",
                "Don't arch your lower back or lean the torso forward.",
            ],
            Video = new("34SlL-PPCWQ"),
        },

        // Hip and glute stretches
        new("stretch_sleeping_pigeon", "Sleeping Pigeon Stretch", Glutes, Bodyweight, Compound)
        {
            Secondary = [Abs],
            Category = ExerciseCategory.Stretch,
            Level = Intermediate,
            Summary = "A deep glute and outer hip stretch with the front shin across the mat and the torso folded forward over it.",
            Steps =
            [
                "From all fours, bring one knee forward behind that wrist and angle the shin across the mat.",
                "Slide the back leg straight behind you, top of the foot down.",
                "Square your hips and fold forward over the front leg, resting on your forearms or forehead.",
                "Hold for 60–90 seconds per side.",
            ],
            Tips =
            [
                "Bring the front foot closer to your body if you feel any strain in the knee.",
                "Put a cushion under the front hip if it doesn't reach the floor.",
            ],
            Video = new("AI5A1PRYX7E"),
        },
        new("stretch_figure_four", "Supine Figure-Four Stretch", Glutes, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Lying on your back with one ankle crossed over the other knee, you pull the legs in to stretch the glute and piriformis. A knee-friendly alternative to pigeon.",
            Steps =
            [
                "Lie on your back with both knees bent and feet flat.",
                "Cross one ankle over the opposite knee.",
                "Thread your hands behind the bottom thigh and pull it toward your chest.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Keep your head and shoulders relaxed on the floor.",
                "Flex the crossed foot to protect the knee.",
            ],
            Video = new("xVq2-g_leTI"),
        },
        new("stretch_supine_glute", "Supine Glute Stretch", Glutes, Bodyweight, Isolation)
        {
            Secondary = [LowerBack],
            Category = ExerciseCategory.Stretch,
            Summary = "Lying on your back, you pull one knee toward the opposite shoulder to stretch the glute.",
            Steps =
            [
                "Lie on your back with both legs straight.",
                "Bend one knee and hold it with both hands.",
                "Pull it up and across toward the opposite shoulder until you feel a stretch in the glute.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Keep the other leg long and relaxed on the floor.",
                "Keep your lower back on the floor.",
            ],
            Video = new("SvGC2zyQKyA"),
        },
        new("stretch_seated_piriformis", "Seated Piriformis Stretch", Glutes, Bodyweight, Isolation)
        {
            Secondary = [LowerBack],
            Category = ExerciseCategory.Stretch,
            Summary = "Sitting on a chair or bench with one ankle on the opposite knee, you lean forward to stretch the piriformis and deep hip rotators.",
            Steps =
            [
                "Sit tall on a chair or bench with your feet flat.",
                "Cross one ankle over the opposite knee.",
                "Keeping your back flat, hinge forward from the hips until you feel a stretch deep in the glute.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Press the crossed knee gently down to deepen the stretch.",
                "Lead with your chest; don't round your back.",
            ],
            Video = new("d2nExbVh7M0"),
        },
        new("stretch_standing_it_band", "Standing IT Band Stretch", Glutes, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Crossing one leg behind the other and leaning away to stretch the outer hip (TFL and glute med) and the top of the IT band.",
            Steps =
            [
                "Stand tall and cross your right leg behind your left.",
                "Push your right hip out to the side.",
                "Reach your right arm overhead and lean your upper body to the left.",
                "Hold for 30 seconds when you feel a stretch along the outer right hip, then switch.",
            ],
            Tips =
            [
                "Hold a wall with your free hand for balance.",
                "Don't twist; keep your hips and chest facing forward.",
            ],
            Video = new("wzDoSQ8-GWY"),
        },

        // Adductor stretches
        new("stretch_butterfly", "Butterfly Stretch", Quads, Bodyweight, Isolation)
        {
            Secondary = [Glutes],
            Category = ExerciseCategory.Stretch,
            Summary = "Seated with the soles of your feet together and knees out, you lean forward to stretch the inner thighs.",
            Steps =
            [
                "Sit tall with the soles of your feet together and your knees falling out to the sides.",
                "Hold your feet or ankles and draw the heels comfortably close to your body.",
                "Hinge forward from the hips, keeping your back long.",
                "Hold for 30–60 seconds.",
            ],
            Tips =
            [
                "Let gravity lower the knees; don't push them down with your elbows hard.",
                "Sit on a folded towel if your back rounds.",
            ],
            Video = new("4J7kbCmPScQ"),
        },
        new("stretch_frog", "Frog Stretch", Quads, Bodyweight, Isolation)
        {
            Secondary = [Glutes],
            Category = ExerciseCategory.Stretch,
            Level = Intermediate,
            Summary = "A deep static adductor stretch on hands or forearms with the knees wide and shins parallel.",
            Steps =
            [
                "Kneel on a mat, rest on your forearms and slide your knees out wide.",
                "Turn your feet out so your ankles line up behind your knees and your shins are parallel.",
                "Ease your hips back until you feel a strong stretch in the inner thighs.",
                "Hold for 30–60 seconds, breathing slowly.",
            ],
            Tips =
            [
                "Pad your knees well.",
                "Keep your back flat; don't let your belly sag toward the floor.",
                "Go wide gradually; this one is intense.",
            ],
            Video = new("6-JQqiI_Plo"),
        },
        new("stretch_kneeling_adductor", "Kneeling Adductor Stretch", Quads, Bodyweight, Isolation)
        {
            Secondary = [Hamstrings],
            Category = ExerciseCategory.Stretch,
            Summary = "On one knee with the other leg straight out to the side, you sit your hips back to stretch the inner thigh of the straight leg.",
            Steps =
            [
                "Start on all fours and straighten one leg out to the side, foot flat and toes forward.",
                "Keep the kneeling knee under your hip.",
                "Sit your hips back toward your kneeling heel until you feel a stretch along the inner thigh.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Keep a flat back as you sit back.",
                "Turn the toes up to the ceiling to bias the hamstring side of the groin.",
            ],
            Video = new("m6S7_qSm6k8"),
        },
        new("stretch_seated_straddle", "Seated Straddle Stretch", Quads, Bodyweight, Isolation)
        {
            Secondary = [Hamstrings, LowerBack],
            Category = ExerciseCategory.Stretch,
            Summary = "Seated with the legs spread wide, you fold forward to stretch the adductors and hamstrings.",
            Steps =
            [
                "Sit with your legs straight and spread as wide as is comfortable, kneecaps and toes pointing up.",
                "Sit tall and place your hands on the floor in front of you.",
                "Walk your hands forward, hinging from the hips, until you feel a stretch in the inner thighs.",
                "Hold for 30–60 seconds.",
            ],
            Tips =
            [
                "Keep the knees pointing to the ceiling; don't let the legs roll in.",
                "Sit on a folded towel if your lower back rounds.",
            ],
            Video = new("uKg1rqo9YrE"),
        },

        // Calf and shin stretches
        new("stretch_wall_calf", "Wall Calf Stretch", Calves, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Leaning into a wall with the back leg straight and heel down to stretch the upper calf (gastrocnemius).",
            Steps =
            [
                "Stand facing a wall with your hands on it.",
                "Step one foot back, leg straight, heel on the floor and toes pointing forward.",
                "Bend the front knee and lean your hips toward the wall until you feel a stretch in the back calf.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Keep the back heel firmly down.",
                "Point the back toes straight ahead, not out.",
            ],
            Video = new("tUA4MO1kXV8"),
        },
        new("stretch_soleus", "Bent-Knee Soleus Stretch", Calves, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "The wall calf stretch with the back knee bent, which shifts the stretch to the lower calf (soleus) and Achilles.",
            Steps =
            [
                "Stand facing a wall with one foot back, both heels down.",
                "Bend the back knee while keeping its heel on the floor.",
                "Sink your hips down until you feel a stretch low in the calf, near the Achilles.",
                "Hold for 30–60 seconds per side.",
            ],
            Tips =
            [
                "Shorten your stance if the heel lifts.",
                "Keep the knee tracking over the toes.",
            ],
            Video = new("si9ZbZ9XVlk"),
        },
        new("stretch_heel_drop_calf", "Heel-Drop Calf Stretch", Calves, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Standing on the edge of a step, you let one heel sink below the step to stretch the calf and Achilles tendon.",
            Steps =
            [
                "Stand on the edge of a step with the balls of your feet on it, holding a rail.",
                "Shift your weight onto one foot.",
                "Slowly lower that heel below the step until you feel a stretch in the calf and Achilles.",
                "Hold for 30 seconds, then switch. Bend the knee slightly to target the Achilles more.",
            ],
            Tips =
            [
                "Lower slowly; don't bounce at the bottom.",
                "Hold on to something solid.",
            ],
            Video = new("6ou4w4ICXvo"),
        },
        new("stretch_kneeling_shin", "Kneeling Shin Stretch", Calves, Bodyweight, Isolation)
        {
            Secondary = [Quads],
            Category = ExerciseCategory.Stretch,
            Summary = "Sitting back on your heels with the tops of your feet flat on the floor to stretch the shins (tibialis anterior) and front of the ankles.",
            Steps =
            [
                "Kneel on a mat with the tops of your feet flat on the floor.",
                "Sit your hips back onto your heels.",
                "To deepen it, lean back on your hands and lift your knees slightly off the floor.",
                "Hold for 30–60 seconds.",
            ],
            Tips =
            [
                "Place a folded towel under your ankles if this is too intense.",
                "Skip the knee lift if your knees complain.",
            ],
            Video = new("wLnYZRHLhnY"),
        },

        // Spine and trunk stretches
        new("stretch_knees_to_chest", "Knees-to-Chest Stretch", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Category = ExerciseCategory.Stretch,
            Summary = "Lying on your back, you hug both knees to your chest to gently stretch the lower back and glutes.",
            Steps =
            [
                "Lie on your back with your knees bent.",
                "Bring both knees toward your chest and hold behind the thighs or over the shins.",
                "Pull gently until your tailbone lifts slightly and you feel the lower back lengthen.",
                "Hold for 30–60 seconds, or rock gently side to side.",
            ],
            Tips =
            [
                "Keep your head and shoulders on the floor.",
                "Breathe slowly into your belly.",
            ],
            Video = new("LugNxxfIdvo"),
        },
        new("stretch_supine_spinal_twist", "Supine Spinal Twist", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Glutes, Chest],
            Category = ExerciseCategory.Stretch,
            Summary = "Lying on your back, you drop both bent knees to one side while the shoulders stay down, stretching the lower back, hips and chest.",
            Steps =
            [
                "Lie on your back with your knees bent and arms out in a T, palms down.",
                "Let both knees fall slowly to one side.",
                "Turn your head the opposite way if it's comfortable.",
                "Hold for 30–60 seconds, then switch sides.",
            ],
            Tips =
            [
                "Keep both shoulders on the floor; stop the knees where the shoulder starts to lift.",
                "Put a cushion under the knees if they don't reach the floor.",
            ],
            Video = new("mNdJti7ZwKI"),
        },
        new("stretch_prone_press_up", "Prone Press-Up", Abs, Bodyweight, Isolation)
        {
            Secondary = [LowerBack],
            Category = ExerciseCategory.Stretch,
            Summary = "A passive back-extension stretch: lying face down, you press your chest up with your arms while the hips and lower back stay relaxed, stretching the abs and hip flexors.",
            Steps =
            [
                "Lie face down with your hands under your shoulders.",
                "Press your chest up, straightening the arms as far as is comfortable while your hips stay on the floor.",
                "Let your lower back and glutes relax completely.",
                "Hold for 20–30 seconds, or do 8–10 slow press-ups.",
            ],
            Tips =
            [
                "Your arms do the work; your back muscles stay relaxed.",
                "Stay on your forearms if straight arms pinch your lower back.",
                "Stop if you feel pain spreading down a leg.",
            ],
            Video = new("UqSP7ZrHxRE"),
        },
        new("stretch_standing_side_bend", "Standing Side Bend Stretch", Abs, Bodyweight, Isolation)
        {
            Secondary = [Back],
            Category = ExerciseCategory.Stretch,
            Summary = "Standing tall and reaching one arm over your head to the side to stretch the obliques and lats.",
            Steps =
            [
                "Stand with your feet hip-width apart.",
                "Reach one arm overhead.",
                "Bend sideways away from that arm, reaching up and over rather than down.",
                "Hold for 20–30 seconds, then switch sides.",
            ],
            Tips =
            [
                "Bend straight to the side; don't lean forward or back.",
                "Keep both feet planted and hips level.",
            ],
            Video = new("Vko-SJok-fk"),
        },

        // Yoga: standing poses
        new("yoga_mountain", "Mountain Pose", Abs, Bodyweight, Compound)
        {
            Secondary = [Glutes, Quads],
            Category = Yoga,
            Summary = "Mountain Pose (Tadasana), the foundation of every standing pose: standing tall and aligned with every muscle quietly engaged.",
            Steps =
            [
                "Stand with your feet together or hip-width apart, weight spread evenly through both feet.",
                "Lift your kneecaps, firm your thighs and lengthen your tailbone down.",
                "Draw your belly in gently, broaden your collarbones and let your arms hang at your sides, palms forward.",
                "Lengthen through the crown of your head and breathe for 5–10 breaths.",
            ],
            Tips =
            [
                "Stack ankles, knees, hips, shoulders and ears.",
                "Don't lock the knees back or push the ribs forward.",
            ],
            Video = new("5NxDs-ovJU8"),
        },
        new("yoga_chair", "Chair Pose", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Shoulders],
            Category = Yoga,
            Hold = true,
            Summary = "Chair Pose (Utkatasana): a held half-squat with the arms reaching overhead that builds strength in the legs and endurance in the shoulders.",
            Steps =
            [
                "Stand with your feet together or hip-width apart.",
                "Bend your knees and sit your hips back as if onto a chair.",
                "Raise your arms overhead alongside your ears.",
                "Hold for 5–8 breaths, then straighten up.",
            ],
            Tips =
            [
                "Keep your weight in your heels; you should be able to see your toes.",
                "Draw the ribs in so your lower back doesn't arch.",
            ],
            Video = new("YBGtKQgExZo"),
        },
        new("yoga_warrior_1", "Warrior I", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Abs, Shoulders],
            Category = Yoga,
            Hold = true,
            Summary = "Warrior I (Virabhadrasana I): a deep lunge with the hips squared to the front and arms overhead, strengthening the front leg and stretching the back hip flexor.",
            Steps =
            [
                "From standing, step one foot back about a leg's length and turn the back foot out 30–45°, heel down.",
                "Square your hips toward the front of the mat.",
                "Bend the front knee toward 90°, stacking it over the ankle.",
                "Raise your arms overhead and hold for 5–8 breaths per side.",
            ],
            Tips =
            [
                "Widen your stance side to side if squaring the hips is hard.",
                "Keep the back leg strong and the outer edge of the back foot pressing down.",
                "Don't let the front knee cave inward.",
            ],
            Video = new("vP3QCUAMi9M"),
        },
        new("yoga_warrior_2", "Warrior II", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Shoulders],
            Category = Yoga,
            Hold = true,
            Summary = "Warrior II (Virabhadrasana II): a wide-legged lunge with the hips open to the side and arms reaching front and back.",
            Steps =
            [
                "Stand with your feet wide apart, front foot pointing forward and back foot parallel to the back edge of the mat.",
                "Bend the front knee over the ankle, toward 90°.",
                "Extend your arms out at shoulder height, front and back, and look past your front hand.",
                "Hold for 5–8 breaths per side.",
            ],
            Tips =
            [
                "Line up the front knee with the second toe.",
                "Keep your torso upright between your legs; don't lean toward the front leg.",
            ],
            Video = new("7Xl-TNyOJec"),
        },
        new("yoga_warrior_3", "Warrior III", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, LowerBack, Abs],
            Category = Yoga,
            Level = Intermediate,
            Hold = true,
            Summary = "Warrior III (Virabhadrasana III): balancing on one leg with the torso and back leg in a line parallel to the floor.",
            Steps =
            [
                "Stand on one leg with a soft knee and hinge forward from the hip.",
                "Lift the other leg straight behind you as your torso lowers.",
                "Reach your arms forward or keep your hands at your chest, making one long line from fingers to heel.",
                "Hold for 5 breaths, then switch legs.",
            ],
            Tips =
            [
                "Keep your hips level; the lifted hip tends to roll open.",
                "Put your hands on blocks or a chair until your balance improves.",
            ],
            Video = new("ySy_k5R3lHg"),
        },
        new("yoga_triangle", "Triangle Pose", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Abs, Quads],
            Category = Yoga,
            Summary = "Triangle Pose (Utthita Trikonasana): with both legs straight, you reach sideways over the front leg to stretch the hamstrings, groin and side of the trunk.",
            Steps =
            [
                "Stand with your feet wide, front foot pointing forward and back foot turned in slightly.",
                "Extend your arms at shoulder height.",
                "Reach forward over the front leg, then lower that hand to your shin, a block or the floor.",
                "Reach the top arm to the ceiling, open your chest and hold for 5–8 breaths per side.",
            ],
            Tips =
            [
                "Lengthen both sides of the waist; don't collapse sideways to reach the floor.",
                "Keep a micro-bend in the front knee rather than locking it.",
            ],
            Video = new("_1124fj0BeQ"),
        },
        new("yoga_extended_side_angle", "Extended Side Angle", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Abs],
            Category = Yoga,
            Summary = "Extended Side Angle (Utthita Parsvakonasana): from Warrior II you lean over the bent front leg and reach the top arm overhead in one long diagonal line.",
            Steps =
            [
                "Start in Warrior II.",
                "Lean over the front leg and rest your forearm on the thigh, or your hand on a block or the floor inside the foot.",
                "Reach the top arm over your ear so there's a straight line from the back heel to the fingertips.",
                "Hold for 5–8 breaths per side.",
            ],
            Tips =
            [
                "Keep the front knee over the ankle and press it back against the arm.",
                "Rotate your chest up rather than letting it face the floor.",
            ],
            Video = new("0k0A_N_FA7k"),
        },
        new("yoga_crescent_lunge", "Crescent Lunge", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Abs],
            Category = Yoga,
            Hold = true,
            Summary = "Crescent Lunge (Ashta Chandrasana): a high lunge with the back heel lifted and arms overhead, building leg strength and balance while stretching the back hip.",
            Steps =
            [
                "From standing, step one foot back into a long lunge with the back heel lifted.",
                "Bend the front knee over the ankle and keep the back leg straight.",
                "Lift your torso upright and raise your arms overhead.",
                "Hold for 5–8 breaths per side.",
            ],
            Tips =
            [
                "Draw the tailbone down to feel the stretch at the front of the back hip.",
                "Keep your hips level and pointing forward.",
            ],
            Video = new("GN_7jhHeSsY"),
        },
        new("yoga_tree", "Tree Pose", Glutes, Bodyweight, Compound)
        {
            Secondary = [Calves, Abs],
            Category = Yoga,
            Hold = true,
            Summary = "Tree Pose (Vrksasana): a standing balance with the sole of one foot pressed into the inner standing leg.",
            Steps =
            [
                "Stand tall and shift your weight onto one foot.",
                "Place the sole of the other foot on the inner ankle, calf or thigh of the standing leg.",
                "Press the foot and leg into each other and bring your hands to your chest or overhead.",
                "Fix your gaze on a still point and hold for 5–10 breaths per side.",
            ],
            Tips =
            [
                "Never press the foot against the side of the knee.",
                "Keep the standing hip from jutting out to the side.",
            ],
            Video = new("wdln9qWYloU"),
        },
        new("yoga_eagle", "Eagle Pose", Glutes, Bodyweight, Compound)
        {
            Secondary = [Quads, Shoulders],
            Category = Yoga,
            Level = Intermediate,
            Hold = true,
            Summary = "Eagle Pose (Garudasana): a one-legged balance with legs and arms wrapped, strengthening the standing leg and stretching the upper back and outer hips.",
            Steps =
            [
                "Stand with soft knees and cross your right thigh over your left, hooking the right foot behind the left calf if you can.",
                "Cross your left arm over your right at the elbows and bring the palms together.",
                "Sink your hips as if into a chair and lift your elbows to shoulder height.",
                "Hold for 5 breaths, then switch sides.",
            ],
            Tips =
            [
                "If the foot won't hook, rest the toes on the floor beside the standing foot.",
                "Keep your chest lifted and shoulders away from the ears.",
            ],
            Video = new("fC9XQWc6ukk"),
        },
        new("yoga_half_moon", "Half Moon Pose", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Abs],
            Category = Yoga,
            Level = Intermediate,
            Hold = true,
            Summary = "Half Moon (Ardha Chandrasana): balancing on one leg and one hand with the body opened sideways and the top leg lifted parallel to the floor.",
            Steps =
            [
                "From Triangle, bend the front knee and place your front hand on the floor or a block about a foot ahead of the foot.",
                "Straighten the front leg as you lift the back leg to hip height.",
                "Stack your top hip over the bottom and reach the top arm to the ceiling.",
                "Hold for 5 breaths per side.",
            ],
            Tips =
            [
                "Use a block under the hand; it makes the pose far more stable.",
                "Flex the lifted foot and reach through the heel.",
            ],
            Video = new("rvOcLOu3f7s"),
        },
        new("yoga_standing_forward_fold", "Standing Forward Fold", Hamstrings, Bodyweight, Isolation)
        {
            Secondary = [LowerBack, Calves],
            Category = Yoga,
            Summary = "Standing Forward Fold (Uttanasana): folding forward from the hips with the head hanging to stretch the hamstrings and lengthen the spine.",
            Steps =
            [
                "Stand with your feet hip-width apart.",
                "Hinge from the hips and fold forward, letting your head and arms hang.",
                "Rest your hands on your shins, the floor or blocks, or hold opposite elbows.",
                "Hold for 5–10 breaths, then roll up slowly.",
            ],
            Tips =
            [
                "Bend your knees as much as you need to fold from the hips.",
                "Shift your weight slightly toward the balls of your feet.",
            ],
            Video = new("jGr-yIl9rtg"),
        },
        new("yoga_garland", "Garland Pose", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves, LowerBack],
            Category = Yoga,
            Summary = "Garland Pose (Malasana): a deep yogi squat with palms together at the chest, opening the hips, groin and ankles.",
            Steps =
            [
                "Stand with your feet a little wider than hip-width, toes turned out.",
                "Squat down as low as you can, heels down if possible.",
                "Bring your palms together at your chest and press your elbows against the insides of your knees.",
                "Lengthen your spine and hold for 5–10 breaths.",
            ],
            Tips =
            [
                "Put a rolled mat or blanket under your heels if they lift.",
                "Sit on a block if you can't reach the bottom comfortably.",
            ],
            Video = new("s9fCNtpNxck"),
        },

        // Yoga: lunges and hip openers
        new("yoga_low_lunge", "Low Lunge", Abs, Bodyweight, Compound)
        {
            Secondary = [Quads, Glutes],
            Category = Yoga,
            Summary = "Low Lunge (Anjaneyasana): a lunge with the back knee down and arms overhead, a deep stretch for the back hip flexor and thigh.",
            Steps =
            [
                "From Downward-Facing Dog, step one foot between your hands and lower the back knee to the mat.",
                "Rest the top of the back foot on the mat.",
                "Lift your torso and raise your arms overhead.",
                "Let your hips sink forward and down and hold for 5–8 breaths per side.",
            ],
            Tips =
            [
                "Pad the back knee with a folded mat or blanket.",
                "Lift up out of the lower back rather than collapsing into it.",
            ],
            Video = new("qMPN9cCDCq8"),
        },
        new("yoga_pigeon", "Pigeon Pose", Glutes, Bodyweight, Compound)
        {
            Secondary = [Abs],
            Category = Yoga,
            Level = Intermediate,
            Summary = "Pigeon Pose (Eka Pada Rajakapotasana prep): the front shin across the mat and back leg long behind, torso upright, opening the outer hip of the front leg and the hip flexor of the back leg.",
            Steps =
            [
                "From Downward-Facing Dog, bring one knee forward behind that wrist and angle the shin across the mat.",
                "Lower the back leg to the mat, straight behind you with the top of the foot down.",
                "Square your hips and walk your hands back to lift your chest upright.",
                "Hold for 5–10 breaths per side, or fold forward to rest.",
            ],
            Tips =
            [
                "Put a block or blanket under the front hip so the pelvis stays level.",
                "Bring the front heel closer to your body if the knee complains.",
            ],
            Video = new("lqCqETr7Q0g"),
        },
        new("yoga_happy_baby", "Happy Baby", Quads, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, LowerBack, Glutes],
            Category = Yoga,
            Summary = "Happy Baby (Ananda Balasana): lying on your back holding your feet with the knees wide, stretching the inner groin and releasing the lower back.",
            Steps =
            [
                "Lie on your back and draw your knees toward your armpits.",
                "Hold the outside edges of your feet, ankles or shins, soles facing the ceiling.",
                "Gently pull the knees down toward the floor beside your ribs.",
                "Hold for 5–10 breaths, rocking gently side to side if you like.",
            ],
            Tips =
            [
                "Keep your tailbone and shoulders on the mat.",
                "Stack your ankles over your knees.",
            ],
            Video = new("Rg8L0_ZDick"),
        },

        // Yoga: seated and resting poses
        new("yoga_childs_pose", "Child's Pose", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Back, Glutes],
            Category = Yoga,
            Summary = "Child's Pose (Balasana): kneeling and folding forward over the thighs with the arms reaching ahead, a resting pose that gently stretches the back and hips.",
            Steps =
            [
                "Kneel with your big toes touching and knees together or wider than your hips.",
                "Sit your hips back toward your heels.",
                "Fold forward and rest your forehead on the mat, arms reaching forward or resting by your sides.",
                "Breathe slowly into your back for 5–10 breaths or longer.",
            ],
            Tips =
            [
                "Widen the knees to make room for the belly and deepen the hip stretch.",
                "Put a cushion between your hips and heels if they don't meet.",
            ],
            Video = new("zStvLHeRnVQ"),
        },
        new("yoga_puppy", "Puppy Pose", Back, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Chest],
            Category = Yoga,
            Summary = "Puppy Pose (Uttana Shishosana): a cross between Child's Pose and Downward-Facing Dog, with hips high over the knees and chest melting toward the mat to stretch the lats and shoulders.",
            Steps =
            [
                "Start on all fours.",
                "Walk your hands forward while keeping your hips stacked over your knees.",
                "Lower your chest and forehead toward the mat, arms straight.",
                "Hold for 5–8 breaths.",
            ],
            Tips =
            [
                "Keep a slight arch in the lower back rather than letting the hips sit back.",
                "Rest your forehead on a block if your chest doesn't reach.",
            ],
            Video = new("i1-bFRjloeI"),
        },
        new("yoga_seated_forward_fold", "Seated Forward Fold", Hamstrings, Bodyweight, Isolation)
        {
            Secondary = [LowerBack, Calves],
            Category = Yoga,
            Summary = "Seated Forward Fold (Paschimottanasana): sitting with both legs straight and folding forward over them to stretch the whole back of the body.",
            Steps =
            [
                "Sit with your legs straight in front of you, feet flexed.",
                "Sit tall and reach your arms overhead.",
                "Hinge forward from the hips and hold your shins, ankles or feet.",
                "Lengthen your spine on each breath in, fold a little deeper on each breath out, for 5–10 breaths.",
            ],
            Tips =
            [
                "Sit on a folded blanket and bend your knees if your back rounds early.",
                "Use a strap around your feet rather than pulling yourself down by rounding.",
            ],
            Video = new("1CkumUyO7tE"),
        },
        new("yoga_legs_up_the_wall", "Legs Up the Wall", Hamstrings, Bodyweight, Isolation)
        {
            Secondary = [LowerBack],
            Category = Yoga,
            Summary = "Legs Up the Wall (Viparita Karani): a restorative pose lying on your back with your legs resting up a wall, gently stretching the hamstrings and calming the body.",
            Steps =
            [
                "Sit side-on to a wall with one hip touching it.",
                "Swing your legs up the wall as you lie back on the floor.",
                "Shuffle your hips as close to the wall as is comfortable and rest your arms by your sides.",
                "Stay for 3–10 minutes, breathing slowly.",
            ],
            Tips =
            [
                "Move your hips away from the wall if your hamstrings feel tight.",
                "A folded blanket under the hips makes it more restful.",
            ],
            Video = new("do_1LisFah0"),
        },
        new("yoga_corpse", "Corpse Pose", Abs, Bodyweight, Isolation)
        {
            Category = Yoga,
            Summary = "Corpse Pose (Savasana): lying still on your back in complete relaxation, usually at the end of a practice, with slow, easy breathing.",
            Steps =
            [
                "Lie on your back with your legs extended and slightly apart, feet falling out.",
                "Rest your arms a little away from your body, palms up.",
                "Close your eyes and let your whole body grow heavy.",
                "Breathe naturally and stay for 3–10 minutes.",
            ],
            Tips =
            [
                "Put a cushion under your knees if your lower back is uncomfortable.",
                "Let go of controlling the breath; just notice it.",
            ],
            Video = new("clk_xP1S9ZE"),
        },

        // Yoga: backbends
        new("yoga_cobra", "Cobra Pose", LowerBack, Bodyweight, Isolation)
        {
            Secondary = [Back, Glutes],
            Category = Yoga,
            Summary = "Cobra Pose (Bhujangasana): lying face down and lifting the chest with the back muscles, hands lightly supporting, a gentle backbend.",
            Steps =
            [
                "Lie face down with your legs together, tops of the feet down and hands under your shoulders.",
                "Press your pubic bone and feet into the floor.",
                "Breathe in and lift your chest, using your back more than your arms, elbows bent and close to your sides.",
                "Hold for 3–5 breaths, then lower.",
            ],
            Tips =
            [
                "Draw your shoulders back and down away from your ears.",
                "Keep the back of the neck long; don't crank your head back.",
            ],
            Video = new("zgvolE4NAH0"),
        },
        new("yoga_upward_dog", "Upward-Facing Dog", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Triceps, Abs, Shoulders],
            Category = Yoga,
            Summary = "Upward-Facing Dog (Urdhva Mukha Svanasana): a backbend on straight arms with the thighs lifted off the floor, opening the chest and front of the body.",
            Steps =
            [
                "Lie face down with the tops of your feet on the mat and hands beside your lower ribs.",
                "Press into your hands and straighten your arms, lifting your chest forward and up.",
                "Lift your thighs and knees off the mat so only your hands and the tops of your feet touch it.",
                "Hold for 1–5 breaths.",
            ],
            Tips =
            [
                "Keep your shoulders stacked over your wrists and pulled away from your ears.",
                "Press firmly through the tops of the feet to keep the legs active.",
                "Use Cobra instead if this pinches your lower back.",
            ],
            Video = new("tbh0qyLJRaI"),
        },
        new("yoga_locust", "Locust Pose", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Glutes, Back, Hamstrings],
            Category = Yoga,
            Hold = true,
            Summary = "Locust Pose (Salabhasana): lying face down and lifting the chest, arms and legs off the floor to strengthen the whole back of the body.",
            Steps =
            [
                "Lie face down with your arms by your sides, palms down, forehead on the mat.",
                "Breathe in and lift your head, chest, arms and legs off the floor.",
                "Reach back through your fingers and toes, keeping the legs straight.",
                "Hold for 3–5 breaths, then lower and rest.",
            ],
            Tips =
            [
                "Keep your gaze down so the neck stays long.",
                "Lengthen out rather than straining up.",
            ],
            Video = new("1jflO_tZg_M"),
        },
        new("yoga_bow", "Bow Pose", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Quads, Chest, Shoulders],
            Category = Yoga,
            Level = Intermediate,
            Summary = "Bow Pose (Dhanurasana): lying face down and holding your ankles, you kick into your hands to lift the chest and thighs, a deep backbend.",
            Steps =
            [
                "Lie face down and bend your knees, reaching back to hold the outsides of your ankles.",
                "Keep your knees about hip-width apart.",
                "Breathe in and kick your feet back into your hands, lifting your chest and thighs off the mat.",
                "Hold for 3–5 breaths, then release slowly.",
            ],
            Tips =
            [
                "Let the legs do the lifting; the arms just hold on.",
                "Don't let the knees splay wide.",
            ],
            Video = new("ZYvDKZm40M8"),
        },
        new("yoga_camel", "Camel Pose", Abs, Bodyweight, Compound)
        {
            Secondary = [Quads, Chest, LowerBack],
            Category = Yoga,
            Level = Intermediate,
            Summary = "Camel Pose (Ustrasana): a kneeling backbend reaching back for the heels, opening the front of the hips, abs and chest.",
            Steps =
            [
                "Kneel with your knees hip-width apart, thighs vertical and hands on your lower back.",
                "Lift your chest and press your hips forward.",
                "Lean back and, if you can, reach your hands to your heels one at a time.",
                "Hold for 3–5 breaths, then come up slowly with your hands on your lower back.",
            ],
            Tips =
            [
                "Keep your hips over your knees as you lean back.",
                "Tuck your toes under to bring the heels closer.",
                "Keep the neck neutral or let the head go back only if it's comfortable.",
            ],
            Video = new("8q7GxnIFsQo"),
        },
        new("yoga_bridge", "Bridge Pose", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, LowerBack],
            Category = Yoga,
            Hold = true,
            Summary = "Bridge Pose (Setu Bandha Sarvangasana): lying on your back and lifting the hips high, strengthening the glutes and opening the chest and hip flexors.",
            Steps =
            [
                "Lie on your back with your knees bent, feet flat and hip-width apart, close to your hips.",
                "Press into your feet and lift your hips toward the ceiling.",
                "Interlace your hands under your back and roll your shoulders under, or keep your arms by your sides.",
                "Hold for 5–8 breaths, then lower one vertebra at a time.",
            ],
            Tips =
            [
                "Keep your knees pointing forward, not falling out or in.",
                "Don't turn your head while you're in the pose.",
            ],
            Video = new("vJTnQ6Yq4Fg"),
        },
        new("yoga_wheel", "Wheel Pose", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Glutes, Triceps],
            Category = Yoga,
            Level = Advanced,
            Hold = true,
            Summary = "Wheel Pose (Urdhva Dhanurasana): a full backbend pressing up onto hands and feet, demanding shoulder and spine mobility and strength.",
            Steps =
            [
                "Lie on your back with knees bent and feet flat, hip-width apart.",
                "Place your hands beside your ears, fingers pointing toward your shoulders.",
                "Press into your feet and hands and lift onto the crown of your head, then straighten your arms to lift fully.",
                "Hold for 3–5 breaths, then tuck your chin and lower slowly.",
            ],
            Tips =
            [
                "Warm up thoroughly with Bridge and shoulder openers first.",
                "Keep your elbows and knees from splaying out.",
                "Don't attempt it with a back or shoulder injury.",
            ],
            Video = new("4F4lTh09Z5E"),
        },

        // Yoga: arm balances and core
        new("yoga_downward_dog", "Downward-Facing Dog", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Calves, Shoulders, Back],
            Category = Yoga,
            Summary = "Downward-Facing Dog (Adho Mukha Svanasana): an inverted V with hips high, stretching the hamstrings and calves while strengthening the shoulders.",
            Steps =
            [
                "Start on all fours with hands slightly ahead of your shoulders and toes tucked.",
                "Lift your knees and send your hips up and back.",
                "Straighten your legs as much as you can and reach your heels toward the floor.",
                "Press the floor away and hold for 5–10 breaths.",
            ],
            Tips =
            [
                "Prioritise a long spine over straight legs; bend the knees if your back rounds.",
                "Spread your fingers and press through the knuckles at the base of the index finger.",
            ],
            Video = new("ljYDGXjyKNA"),
        },
        new("yoga_plank", "Plank Pose", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Chest],
            Category = Yoga,
            Hold = true,
            Summary = "Plank Pose (Phalakasana): a high plank on straight arms with the body in one line, building core and shoulder strength.",
            Steps =
            [
                "Start on all fours and step your feet back so your body forms a straight line.",
                "Stack your shoulders over your wrists.",
                "Engage your legs and draw your belly in.",
                "Hold for 5–10 breaths.",
            ],
            Tips =
            [
                "Push the floor away so your upper back doesn't sag between the shoulder blades.",
                "Don't let your hips drop or pike up.",
            ],
            Video = new("v_8rMn6jxqc"),
        },
        new("yoga_side_plank", "Side Plank Pose", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Glutes],
            Category = Yoga,
            Level = Intermediate,
            Hold = true,
            Summary = "Side Plank (Vasisthasana): balancing on one straight arm and the side of the feet, strengthening the obliques, shoulders and outer hips.",
            Steps =
            [
                "From Plank Pose, shift onto one hand and roll onto the outer edge of that foot.",
                "Stack your feet and hips and reach the top arm to the ceiling.",
                "Lift your hips so your body forms a straight line.",
                "Hold for 3–5 breaths per side.",
            ],
            Tips =
            [
                "Drop the bottom knee to the mat for an easier version.",
                "Press the floor away so you don't sink into the supporting shoulder.",
            ],
            Video = new("8qI2SzNHoO0"),
        },
        new("yoga_chaturanga", "Chaturanga", Triceps, Bodyweight, Compound)
        {
            Secondary = [Chest, Shoulders, Abs],
            Category = Yoga,
            Level = Intermediate,
            Summary = "Chaturanga (Chaturanga Dandasana), four-limbed staff pose: lowering from plank to hover just above the floor with the elbows hugging the ribs.",
            Steps =
            [
                "Start in Plank Pose and shift slightly forward onto your toes.",
                "Bend your elbows straight back, keeping them close to your sides.",
                "Lower until your upper arms are parallel to the floor, body in one straight line.",
                "Hover briefly, then move on to Upward-Facing Dog or lower to the mat.",
            ],
            Tips =
            [
                "Stop at elbow height; dipping lower strains the front of the shoulders.",
                "Drop your knees to the mat until you can keep a straight line.",
            ],
            Video = new("rTVWSjYAZ2Y"),
        },
        new("yoga_boat", "Boat Pose", Abs, Bodyweight, Compound)
        {
            Secondary = [LowerBack],
            Category = Yoga,
            Level = Intermediate,
            Hold = true,
            Summary = "Boat Pose (Navasana): balancing on your sit bones with the legs and chest lifted into a V, a strong core hold.",
            Steps =
            [
                "Sit with your knees bent and feet flat, hands behind your thighs.",
                "Lean back slightly, lift your chest and raise your feet so your shins are parallel to the floor.",
                "Reach your arms forward and, if you can, straighten your legs.",
                "Hold for 5 breaths, rest, and repeat 3 times.",
            ],
            Tips =
            [
                "Keep your chest lifted and spine long; don't round back.",
                "Keep the knees bent until you can straighten the legs with a long spine.",
            ],
            Video = new("tFOy7am1_-8"),
        },
        new("yoga_crow", "Crow Pose", Abs, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders, Forearms],
            Category = Yoga,
            Level = Advanced,
            Hold = true,
            Summary = "Crow Pose (Bakasana): balancing on your hands with the knees resting on the backs of the upper arms, the classic first arm balance.",
            Steps =
            [
                "Squat with your feet together and plant your hands shoulder-width apart in front of you, fingers spread.",
                "Bend your elbows and place your knees high on the backs of your upper arms.",
                "Lean forward, shifting your weight into your hands, and lift one foot, then the other.",
                "Hold for 3–5 breaths, looking slightly ahead of your hands.",
            ],
            Tips =
            [
                "Round your upper back and pull your belly in to feel lighter.",
                "Put a cushion in front of your face while you learn.",
            ],
            Video = new("zrbodlL8aDA"),
        },

        // Yoga: flows
        new("yoga_sun_salutation_a", "Sun Salutation A", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Triceps, LowerBack],
            Category = Yoga,
            Summary = "Sun Salutation A (Surya Namaskar A): the classic flowing sequence linking Mountain, Forward Fold, Plank, Chaturanga, Upward Dog and Downward Dog with the breath.",
            Steps =
            [
                "From Mountain Pose, breathe in and sweep your arms overhead; breathe out and fold forward.",
                "Breathe in to a half lift with a flat back; breathe out, step or jump back to Plank and lower through Chaturanga.",
                "Breathe in to Upward-Facing Dog (or Cobra); breathe out to Downward-Facing Dog and hold for 5 breaths.",
                "Step or jump to your hands, breathe in to a half lift, breathe out to fold, then breathe in to rise with arms overhead and return to Mountain.",
                "Repeat 3–5 rounds.",
            ],
            Tips =
            [
                "One movement per breath; let the breath set the pace.",
                "Lower to your knees in Chaturanga and use Cobra until you're strong enough for the full version.",
            ],
            Video = new("VT609I8OlCs"),
        },
    ];
}
