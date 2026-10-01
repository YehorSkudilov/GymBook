using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] QuadExercises() =>
    [
        // Barbell squats
        new("back_squat", "Back Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Intermediate,
            Summary = "The king of leg exercises: a barbell squat with the bar on top of your traps (high-bar), building the quads, glutes and the whole trunk.",
            Steps =
            [
                "Set the bar in a rack at mid-chest height, step under it and rest it on top of your traps, hands just outside your shoulders.",
                "Unrack, take two or three steps back and set your feet shoulder-width, toes turned out 15–30°.",
                "Brace your core, then sit down between your hips, knees travelling out over your toes.",
                "Go down until your thighs are at least parallel to the floor, keeping your chest up.",
                "Drive up through your whole foot until you're standing tall.",
            ],
            Tips =
            [
                "Take a big breath and brace before every rep; hold it through the hardest part.",
                "Keep the bar over your mid-foot the whole way.",
                "Don't let your knees cave in or your heels lift.",
                "Squat in a rack with the safety arms set just below your bottom position.",
            ],
            Video = new("iZTxa8NJH2g"),
        },
        new("low_bar_squat", "Low-Bar Back Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, Hamstrings, LowerBack],
            Level = Intermediate,
            Summary = "A back squat with the bar lower on the rear delts, which tilts the torso forward and brings in more hips, letting most lifters move the most weight.",
            Steps =
            [
                "Rest the bar on your rear delts just below the spine of your shoulder blades, with a narrower grip and elbows pointing back.",
                "Unrack, step back and set your feet about shoulder-width, toes turned out.",
                "Brace, push your hips back and down while your knees track out, letting your chest lean forward.",
                "Descend to just below parallel, keeping the bar over your mid-foot.",
                "Drive your hips and chest up together to stand.",
            ],
            Tips =
            [
                "Keep your wrists straight; the bar sits on your back, not in your hands.",
                "Raise your chest and hips at the same rate out of the hole.",
                "Don't let your hips shoot up first and turn it into a good morning.",
            ],
            Video = new("Po9CDtfcLJI"),
        },
        new("front_squat", "Front Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, Abs],
            Level = Intermediate,
            Summary = "A squat with the bar racked on the front of your shoulders, forcing an upright torso and putting more of the work on the quads and upper back.",
            Steps =
            [
                "Step under the bar and rest it on your front delts against your throat, fingertips under the bar and elbows high.",
                "Unrack, step back and set your feet shoulder-width, toes slightly out.",
                "Brace and squat straight down, keeping your elbows up and your torso upright.",
                "Go as deep as you can while staying upright, ideally below parallel.",
                "Drive up, leading with your elbows.",
            ],
            Tips =
            [
                "The bar rests on your shoulders, not your hands; two or three fingers under it is enough.",
                "Keep your elbows high so the bar doesn't roll forward.",
                "Don't round your upper back; dump the bar forward if you lose it.",
            ],
            Video = new("6PLvU0rjw18", 196, 281),
        },
        new("box_squat", "Box Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, Hamstrings, LowerBack],
            Level = Intermediate,
            Summary = "A back squat to a box set at parallel or just below, teaching depth and a strong hip drive from a dead stop.",
            Steps =
            [
                "Place a box behind you so your thighs are about parallel when you sit on it.",
                "Unrack the bar on your back and set your feet a little wider than for a normal squat.",
                "Brace, push your hips back and sit down onto the box under control.",
                "Pause briefly on the box while staying tight.",
                "Drive your feet into the floor and stand up.",
            ],
            Tips =
            [
                "Sit back onto the box; don't drop onto it, which jars your spine.",
                "Stay braced on the box: don't relax and rock back.",
                "Keep your shins near vertical and knees pushed out.",
            ],
            Video = new("rMEPHwNhQfo"),
        },
        new("pause_squat", "Pause Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Intermediate,
            Summary = "A back squat with a 2–3 second pause at the bottom, removing the bounce to build strength and control out of the hole.",
            Steps =
            [
                "Unrack the bar on your back and set your normal squat stance.",
                "Brace and squat down to your usual depth.",
                "Hold the bottom position for 2–3 seconds, staying tight.",
                "Drive up hard to standing.",
            ],
            Tips =
            [
                "Use noticeably less weight than your normal squat.",
                "Keep your back tight and your knees out during the pause.",
                "Don't relax or sink lower during the hold.",
            ],
            Video = new("nknf16JJTZo"),
        },
        new("pin_squat", "Pin Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Advanced,
            Summary = "Also called the Anderson squat: a back squat started from a dead stop with the bar resting on the rack's safety pins at bottom depth, building strength out of the hole without any bounce.",
            Steps =
            [
                "Set the safety pins at your bottom squat depth and rest the bar on them.",
                "Squat down under the bar and set it on your upper back in your normal squat position.",
                "Brace hard, grip the bar tightly and press your feet into the floor.",
                "Drive up to standing, then lower the bar back onto the pins and come to a full stop before the next rep.",
            ],
            Tips =
            [
                "Build full-body tension before you push; don't jerk the bar off the pins.",
                "Expect to use noticeably less weight than in your normal squat.",
                "Don't let your hips shoot up first out of the bottom.",
            ],
            Video = new("9UHbrvW_WWM", 55.847, 112.403),
        },
        new("safety_bar_squat", "Safety Bar Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack, Traps],
            Level = Intermediate,
            Summary = "A back squat with a safety squat bar, whose padded yoke and forward handles spare the shoulders and load the upper back and quads hard.",
            Steps =
            [
                "Set the safety bar in a rack, step under the yoke and hold the handles in front of you.",
                "Unrack, step back and set your feet shoulder-width.",
                "Brace and squat down, keeping your chest up against the bar's pull forward.",
                "Reach at least parallel, then drive up to standing.",
            ],
            Tips =
            [
                "Push your upper back up into the pads to stop the bar tipping you forward.",
                "Keep your elbows down and the handles close.",
                "Don't let your upper back round under the load.",
            ],
            Video = new("b2jmZyptN64", 72.799004, 137.03999),
        },
        new("hatfield_squat", "Hatfield Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Advanced,
            Summary = "A safety bar squat done while holding the rack's safety pins or a second bar in front of you, so your arms can steady you and help out of the bottom while your legs handle heavier loads.",
            Steps =
            [
                "Set the rack's safety pins or a second bar at about stomach height in front of where you will squat.",
                "Unrack the safety bar onto your upper back, step back and set your feet shoulder-width.",
                "Hold the pins or bar in front of you lightly with both hands.",
                "Squat down with an upright torso to at least parallel, then drive up with your legs.",
            ],
            Tips =
            [
                "Let your legs do the work; push with your arms only to get past a sticking point.",
                "The support lets you stay very upright, so the quads work especially hard.",
                "Don't haul yourself up with your arms on every rep.",
            ],
            Video = new("qwxnNDzaGBE"),
        },
        new("zercher_squat", "Zercher Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes, Abs, LowerBack],
            Level = Advanced,
            Summary = "A squat with the barbell held in the crooks of your elbows, which hammers the quads, core and upper back.",
            Steps =
            [
                "Set the bar in a rack at about hip height and hook it in the crooks of your elbows, hands together or clasped.",
                "Unrack, step back and set your feet shoulder-width or a little wider.",
                "Brace hard and squat down between your knees, torso upright.",
                "Go as deep as you can while staying tight, then drive up.",
            ],
            Tips =
            [
                "Wrap a pad or towel around the bar if it digs into your arms.",
                "Keep the bar tight to your body and your elbows up.",
                "Don't let your upper back round to cradle the bar.",
            ],
            Video = new("YducYiBtGag"),
        },
        new("landmine_squat", "Landmine Squat", Quads, Barbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A squat holding the end of a landmine-anchored barbell at your chest, whose arc lets you lean into the bar and squat deep with an upright torso.",
            Steps =
            [
                "Wedge one end of a barbell into a landmine or corner and load the other end.",
                "Lift the free end and hold it at your upper chest with both hands cupped around the sleeve.",
                "Step back so you lean slightly into the bar and set your feet shoulder-width.",
                "Squat down between your knees until your thighs are at least parallel, then drive back up.",
            ],
            Tips =
            [
                "Keep the bar tight to your chest; don't let it drift away from you.",
                "Find the foot position where you can sit straight down without your heels lifting.",
                "Don't stand so close that the bar pushes you backward.",
            ],
            Video = new("_mfORB47xMs"),
        },
        new("smith_squat", "Smith Machine Squat", Quads, Machine, Compound)
        {
            Secondary = [Glutes],
            Summary = "A back squat on the Smith machine; the fixed bar path lets you place your feet forward and focus on pushing with the quads.",
            Steps =
            [
                "Set the bar at upper-chest height, step under it and rest it on your upper traps.",
                "Place your feet shoulder-width, slightly in front of the bar.",
                "Unhook the bar and squat down until your thighs are at least parallel.",
                "Drive back up and rehook the bar when you finish.",
            ],
            Tips =
            [
                "Set the safety stops just below your bottom position.",
                "Feet further forward shifts work to the glutes; under the bar keeps it on the quads.",
                "Don't let your lower back round at the bottom.",
            ],
            Video = new("3PpzYOubZ5A"),
        },

        // Machine squats
        new("hack_squat", "Hack Squat", Quads, Machine, Compound)
        {
            Secondary = [Glutes],
            Summary = "A machine squat with your back on an angled pad, letting you squat deep and load the quads without balancing a bar.",
            Steps =
            [
                "Stand on the platform with your back flat on the pad and shoulders under the pads.",
                "Place your feet shoulder-width in the middle of the platform and release the safety handles.",
                "Lower under control until your thighs are at or below parallel.",
                "Drive through your whole foot back to the top without locking your knees hard.",
            ],
            Tips =
            [
                "Keep your back and hips against the pad the whole time.",
                "Lower feet on the platform target the quads more.",
                "Don't let your heels lift or your knees cave in.",
            ],
            Video = new("0tn5K9NlCfo"),
        },
        new("belt_squat", "Belt Squat", Quads, Machine, Compound)
        {
            Secondary = [Glutes],
            Summary = "A squat with the load hung from a hip belt, training the legs hard with no weight on the spine.",
            Steps =
            [
                "Attach the belt around your hips and clip it to the machine or loading pin.",
                "Stand on the platform with your feet shoulder-width and hold the handles lightly.",
                "Release the stop and squat down until your thighs are at least parallel.",
                "Drive up through your whole foot to standing.",
            ],
            Tips =
            [
                "Stay upright; the belt lets you squat deep without your back taking the load.",
                "Use the handles for balance only, not to pull yourself up.",
                "Don't let your knees cave in at the bottom.",
            ],
            Video = new("YgYwEo5WN5o", 20.48, 50.238998),
        },
        new("pendulum_squat", "Pendulum Squat", Quads, Machine, Compound)
        {
            Secondary = [Glutes],
            Summary = "A machine squat on a swinging arm that keeps resistance high at the bottom, giving the quads a deep, heavy stretch.",
            Steps =
            [
                "Step in with your back on the pad and shoulders under the pads, feet together to shoulder-width.",
                "Release the safety and lower as deep as you can with your knees travelling forward.",
                "Pause briefly in the bottom.",
                "Drive back up to just short of lockout.",
            ],
            Tips =
            [
                "Let your knees come well forward over your toes; that's the point.",
                "Keep your hips on the pad throughout.",
                "Don't cut the depth short to use more weight.",
            ],
            Video = new("RE9k04SVWOo"),
        },

        // Dumbbell and kettlebell squats
        new("goblet_squat", "Goblet Squat", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A squat holding a dumbbell at your chest; the front load keeps you upright, making it the best way to learn to squat.",
            Steps =
            [
                "Hold one end of a dumbbell vertically against your chest with both hands.",
                "Stand with your feet shoulder-width, toes slightly out.",
                "Squat down between your knees, keeping your chest up and elbows inside your knees.",
                "Go as deep as you can with a flat back, then drive up.",
            ],
            Tips =
            [
                "Keep the dumbbell touching your chest the whole time.",
                "Push your knees out over your toes.",
                "Don't let your heels lift or your lower back round.",
            ],
            Video = new("7-80HiXX1K8"),
        },
        new("kb_goblet_squat", "Kettlebell Goblet Squat", Quads, Kettlebell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A goblet squat holding a kettlebell by the horns at your chest, training the quads and glutes with an upright torso.",
            Steps =
            [
                "Hold the kettlebell by the horns at chest height, elbows pointing down.",
                "Stand with your feet shoulder-width, toes slightly out.",
                "Squat down between your knees, chest up.",
                "Pause at the bottom, then drive up through your whole foot.",
            ],
            Tips =
            [
                "Keep the bell close to your body.",
                "Use your elbows to gently push your knees out at the bottom.",
                "Don't round your lower back to get deeper.",
            ],
            Video = new("dBnNCOtuGNQ"),
        },
        new("sumo_goblet_squat", "Sumo Goblet Squat", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A wide-stance squat holding a dumbbell between your legs, working the quads, adductors and glutes.",
            Steps =
            [
                "Stand with your feet well wider than your shoulders, toes turned out 30–45°.",
                "Hold a dumbbell by one end with both hands, arms hanging between your legs.",
                "Squat straight down, pushing your knees out in line with your toes.",
                "Lower until your thighs are parallel, then drive up and squeeze your glutes at the top.",
            ],
            Tips =
            [
                "Keep your torso upright and your chest up.",
                "Stand on a pair of plates or benches to let the dumbbell go lower.",
                "Don't let your knees collapse inward past your toes.",
            ],
            Video = new("vM5x2_NIsy8"),
        },
        new("heels_elevated_goblet_squat", "Heels-Elevated Goblet Squat", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A goblet squat with your heels raised on a plate or wedge, letting your knees travel forward to bias the quads and squat deeper.",
            Steps =
            [
                "Stand with your heels on a small plate or wedge, feet hip-width.",
                "Hold a dumbbell vertically against your chest.",
                "Squat straight down, letting your knees travel forward over your toes.",
                "Go as deep as you can with an upright torso, then drive up.",
            ],
            Tips =
            [
                "A narrower stance and slow descent increase the quad focus.",
                "Keep your whole foot in contact with the wedge and floor.",
                "Don't bounce out of the bottom.",
            ],
            Video = new("lmWPn5Q-TSE"),
        },
        new("db_squat", "Dumbbell Squat", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A squat holding a dumbbell in each hand at your sides, a simple way to load the legs without a barbell.",
            Steps =
            [
                "Stand with your feet shoulder-width, a dumbbell in each hand at your sides.",
                "Brace and squat down, keeping your chest up and the dumbbells hanging straight.",
                "Lower until your thighs are about parallel.",
                "Drive up through your whole foot to standing.",
            ],
            Tips =
            [
                "Keep your shoulders back and the weights by your sides.",
                "Push your knees out in line with your toes.",
                "Don't lean forward to let the dumbbells touch the floor.",
            ],
            Video = new("v_c67Omje48"),
        },

        // Bodyweight squats
        new("bw_squat", "Bodyweight Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "The basic air squat: sitting down and standing up with no load, for warm-ups, beginners and high-rep work.",
            Steps =
            [
                "Stand with your feet shoulder-width, toes slightly out, arms at your sides.",
                "Push your hips back and bend your knees, raising your arms in front for balance.",
                "Squat as deep as you can with a flat back and heels down.",
                "Drive up through your whole foot to standing.",
            ],
            Tips =
            [
                "Keep your chest up and knees tracking over your toes.",
                "Squat to the depth you can control; it'll improve with practice.",
                "Don't let your heels lift or your knees cave in.",
            ],
            Video = new("m0GcZ24pK6k"),
        },
        new("cossack_squat", "Cossack Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A deep side-to-side squat onto one leg while the other stays straight, building quads, adductors and hip mobility.",
            Steps =
            [
                "Stand in a very wide stance, toes slightly out.",
                "Shift your weight to one side and squat down on that leg, keeping the other leg straight with its toes pointing up.",
                "Go as deep as you can with your heel down and chest up.",
                "Push back up through the bent leg and shift across to the other side.",
            ],
            Tips =
            [
                "Hold a light weight at your chest as a counterbalance if you tip backward.",
                "Keep the heel of the working leg flat on the floor.",
                "Don't force depth your hips and ankles don't have yet.",
            ],
            Video = new("iPZNB5GsOnM"),
        },
        new("split_squat", "Split Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "A static lunge with both feet planted, one forward and one back, lowering straight down to train one leg at a time.",
            Steps =
            [
                "Stand in a long split stance, front foot flat and back heel raised.",
                "Lower straight down until your back knee is just above the floor.",
                "Keep most of your weight on the front foot.",
                "Drive through the front foot to come back up, then repeat before switching legs.",
            ],
            Tips =
            [
                "Keep your torso upright and your front knee tracking over your toes.",
                "Hold dumbbells at your sides once bodyweight gets easy.",
                "Don't let your front heel lift.",
            ],
            Video = new("hXpGSa5HYqY"),
        },
        new("bulgarian_split_squat", "Bulgarian Split Squat", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A split squat with your rear foot up on a bench, one of the hardest and most effective single-leg exercises for the quads and glutes.",
            Steps =
            [
                "Stand about a stride in front of a bench, holding a dumbbell in each hand.",
                "Place the top of your rear foot on the bench behind you.",
                "Lower straight down until your front thigh is about parallel and your back knee is near the floor.",
                "Drive through your front foot to stand back up. Finish your reps, then switch legs.",
            ],
            Tips =
            [
                "A more upright torso hits the quads; leaning forward slightly brings in the glutes.",
                "Find a foot position where your front heel stays down at the bottom.",
                "Don't push off the back foot; it's only there for balance.",
            ],
            Video = new("IVSrQD9BqSQ"),
        },
        new("ffe_split_squat", "Front-Foot-Elevated Split Squat", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A split squat with the front foot raised on a low step or plate, which lets you sink deeper and works the front leg's quads and glutes through a longer range.",
            Steps =
            [
                "Hold a dumbbell in each hand and place your front foot on a low step or weight plate.",
                "Step the other foot back into a long split stance, back heel raised.",
                "Lower straight down until your back knee is just above the floor, letting your front knee travel forward.",
                "Drive through your front foot to stand back up. Finish your reps, then switch legs.",
            ],
            Tips =
            [
                "A 5–10 cm elevation is plenty.",
                "Keep most of your weight on the front foot.",
                "Don't let your front heel lift at the bottom.",
            ],
            Video = new("wBahkeIwDyA"),
        },
        new("smith_split_squat", "Smith Machine Split Squat", Quads, Machine, Compound)
        {
            Secondary = [Glutes],
            Summary = "A split squat with a Smith machine bar on your upper back, whose fixed path lets you load each leg heavily without having to balance the weight.",
            Steps =
            [
                "Set the bar at shoulder height, step under it and rest it on your upper back.",
                "Unrack it and set your feet in a long split stance, one forward and one back.",
                "Lower straight down until your back knee is just above the floor.",
                "Drive through your front foot to stand back up. Finish your reps, then switch legs.",
            ],
            Tips =
            [
                "Set your feet so your torso can stay upright as the bar moves straight up and down.",
                "A longer stance brings in more glute; a shorter one works the quads more.",
                "Don't push off the back foot.",
            ],
            Video = new("j-TJxRRPu64"),
        },
        new("atg_split_squat", "ATG Split Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "The Knees Over Toes Guy's full-range split squat: the front knee travels far past the toes until the hamstring covers the calf, while the straight back leg gets a deep hip flexor stretch.",
            Steps =
            [
                "Stand in a long split stance with your front foot on the floor or a low step and your back leg nearly straight.",
                "Shift forward, driving your front knee well over your toes as you lower.",
                "Sink until your front hamstring rests on your calf, keeping your front heel down and the back knee off the floor.",
                "Drive through your whole front foot to come back up. Finish your reps, then switch legs.",
            ],
            Tips =
            [
                "Hold a rail or a pair of dumbbells for balance, and raise the front foot while you build range.",
                "Squeeze your back glute at the bottom to deepen the hip flexor stretch.",
                "Don't force depth; progress gradually if your knee complains.",
            ],
            Video = new("LHX34TpJxbQ"),
        },
        new("pistol_squat", "Pistol Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Abs],
            Level = Advanced,
            Summary = "A full single-leg squat with the other leg held straight out in front, demanding strength, balance and mobility.",
            Steps =
            [
                "Stand on one leg with the other leg lifted straight in front and arms reaching forward.",
                "Sit down on the standing leg as deep as you can, keeping the free leg off the floor.",
                "Keep your heel down and your knee tracking over your toes.",
                "Drive back up to standing without letting the free foot touch.",
            ],
            Tips =
            [
                "Holding a light weight out in front makes balancing easier.",
                "Build up with assisted pistols or box pistols first.",
                "Don't let your knee collapse inward.",
            ],
            Video = new("bH3mRwnAN88"),
        },
        new("assisted_pistol_squat", "Assisted Pistol Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A single-leg squat holding a post, doorframe or suspension straps for balance, the stepping stone to a full pistol.",
            Steps =
            [
                "Stand on one leg beside a sturdy post or doorframe and hold it with one or both hands.",
                "Extend the other leg in front of you.",
                "Squat down on the standing leg as deep as you can, using your hands only as much as needed.",
                "Drive back up, helping with your arms as little as possible.",
            ],
            Tips =
            [
                "Reduce the help from your hands over time.",
                "Keep your heel flat and your knee over your toes.",
                "Don't drop into the bottom; lower under control.",
            ],
            Video = new("tiA23NSUm7A"),
        },
        new("shrimp_squat", "Shrimp Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Level = Advanced,
            Summary = "A single-leg squat holding your rear foot behind you and lowering the back knee to the floor, very demanding on the quads.",
            Steps =
            [
                "Stand on one leg and bend the other knee, holding that foot behind you with the same-side hand.",
                "Reach your free arm forward for balance.",
                "Lower under control until your back knee touches the floor softly.",
                "Drive through the standing leg to return to the top.",
            ],
            Tips =
            [
                "Start by lowering onto a pad or folded mat to shorten the range.",
                "Lean forward slightly to stay balanced over your foot.",
                "Don't crash your knee into the floor.",
            ],
            Video = new("P-h6BuU3q78"),
        },
        new("skater_squat", "Skater Squat", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A single-leg squat with the free leg bent behind you, lowering until the back knee touches down; a little more glute and a little less ankle mobility than the pistol squat.",
            Steps =
            [
                "Stand on one leg with the other knee bent and that foot lifted behind you.",
                "Reach your arms forward for balance and let your torso lean forward slightly.",
                "Bend the standing knee and hip to lower until your back knee lightly touches the floor or a pad.",
                "Drive through the standing foot to stand back up without touching down with the free foot.",
            ],
            Tips =
            [
                "Put a pad or yoga block under the back knee to shorten the range while you learn it.",
                "Hold light dumbbells out in front as a counterbalance.",
                "Don't let your standing knee cave inward.",
            ],
            Video = new("YO-247pOeIc"),
        },
        new("sissy_squat", "Sissy Squat", Quads, Bodyweight, Isolation)
        {
            Level = Advanced,
            Summary = "A quad-isolating squat where you lean back and drive your knees forward, keeping your hips extended, for a deep stretch on the front of the thigh.",
            Steps =
            [
                "Stand holding a support with one hand, feet hip-width.",
                "Rise onto the balls of your feet and push your knees forward while leaning your torso back.",
                "Keep a straight line from knees to shoulders as you lower toward the floor.",
                "Use your quads to pull yourself back up to standing.",
            ],
            Tips =
            [
                "Keep your hips straight; don't sit back into a regular squat.",
                "Start with a small range and build depth gradually.",
                "Stop if you feel sharp pain at the knee.",
            ],
            Video = new("JGD_aMeKBJY"),
        },
        new("spanish_squat", "Spanish Squat", Quads, Band, Compound)
        {
            Summary = "A squat with a heavy band looped behind your knees and anchored in front, letting you sit back with vertical shins to load the quads gently; popular for knee pain.",
            Steps =
            [
                "Anchor a heavy band or strap low on a rack and step into it so it sits behind both knees.",
                "Step back until the band is taut and stand with your feet hip-width.",
                "Sit back and down, keeping your shins vertical and torso upright.",
                "Lower to about 90° at the knee, then stand back up, or hold the bottom.",
            ],
            Tips =
            [
                "Let the band take your knees; sit back into it.",
                "Holds of 30–45 seconds are often used for patellar tendon pain.",
                "Don't let your knees travel forward of your toes.",
            ],
            Video = new("3igyh6eqGvc"),
        },
        new("wall_sit", "Wall Sit", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Hold = true,
            Summary = "An isometric squat held with your back against a wall and thighs parallel to the floor, building quad endurance.",
            Steps =
            [
                "Stand with your back against a wall and your feet about two feet in front of you, hip-width.",
                "Slide down until your thighs are parallel to the floor and your knees are at 90°.",
                "Hold the position with your back flat on the wall.",
                "Slide back up to finish.",
            ],
            Tips =
            [
                "Keep your knees over your ankles, not past your toes.",
                "Keep your hands off your thighs.",
                "Don't hold your breath; breathe steadily.",
            ],
            Video = new("eb7vLD6V-iU"),
        },

        // Lunges
        new("bw_lunge", "Bodyweight Lunge", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "The basic forward lunge with no weight: step forward, drop the back knee toward the floor and push back to standing, one leg at a time.",
            Steps =
            [
                "Stand tall with your feet hip-width apart and your hands on your hips.",
                "Take a long step forward and lower until your back knee is just above the floor.",
                "Keep your torso upright and your front knee over your foot.",
                "Push off the front foot to return to standing, then alternate legs.",
            ],
            Tips =
            [
                "Land heel first and keep most of your weight on the front leg.",
                "A longer step works the glutes more; a shorter one works the quads more.",
                "Don't let your front knee cave inward or your back knee slam into the floor.",
            ],
            Video = new("QE_hU8XX48I", 13.92),
        },
        new("db_lunge", "Dumbbell Lunge", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A forward lunge holding a dumbbell in each hand, training each leg in turn with a strong deceleration demand.",
            Steps =
            [
                "Stand tall with a dumbbell in each hand at your sides.",
                "Take a long step forward and lower until your back knee is just above the floor.",
                "Keep your front shin fairly vertical and your torso upright.",
                "Push off the front foot to return to standing, then alternate legs.",
            ],
            Tips =
            [
                "Land heel first and keep your weight on the front foot.",
                "Keep your front knee in line with your toes.",
                "Don't let your back knee slam into the floor.",
            ],
            Video = new("TwEH620Pn6A"),
        },
        new("reverse_lunge", "Reverse Lunge", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "A lunge stepping backward instead of forward, which is easier on the knees and easier to balance.",
            Steps =
            [
                "Stand tall with your feet hip-width.",
                "Step one foot back and lower until your back knee is just above the floor.",
                "Keep your weight on the front foot and your torso upright.",
                "Drive through the front foot to bring the back leg forward to standing, then switch.",
            ],
            Tips =
            [
                "Step back far enough that your front shin stays fairly vertical.",
                "Keep your hips square to the front.",
                "Don't push off the back foot to stand up.",
            ],
            Video = new("ufjvjxrGyFM"),
        },
        new("db_reverse_lunge", "Dumbbell Reverse Lunge", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "A reverse lunge holding dumbbells at your sides, a knee-friendly way to load each leg.",
            Steps =
            [
                "Stand tall with a dumbbell in each hand.",
                "Step one foot back and lower until your back knee nearly touches the floor.",
                "Keep most of your weight on the front foot.",
                "Drive through the front foot to return to standing, then switch legs or finish one side.",
            ],
            Tips =
            [
                "Keep your chest up and the dumbbells hanging straight.",
                "A slight forward lean brings in more glutes.",
                "Don't let your front knee cave inward.",
            ],
            Video = new("MpfeGnBFEo8"),
        },
        new("deficit_reverse_lunge", "Deficit Reverse Lunge", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A reverse lunge with your front foot on a low step or plate, increasing the range of motion for the quads and glutes.",
            Steps =
            [
                "Stand on a step or plate 5–10 cm high, a dumbbell in each hand.",
                "Step one foot back off the platform and lower until your back knee is near the floor.",
                "Sink into the deeper bottom position under control.",
                "Drive through the front foot to step back up onto the platform.",
            ],
            Tips =
            [
                "Start with a low deficit; it adds range quickly.",
                "Keep your front heel down throughout.",
                "Don't bounce out of the bottom.",
            ],
            Video = new("r_UZ5OLa--c"),
        },
        new("barbell_lunge", "Barbell Lunge", Quads, Barbell, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A lunge with a barbell on your upper back, allowing heavier loading than dumbbells but demanding more balance.",
            Steps =
            [
                "Unrack a barbell onto your upper traps and step back from the rack.",
                "Stand tall with your feet hip-width and brace your core.",
                "Step forward or back and lower until your back knee is just above the floor.",
                "Drive back to the start and alternate legs.",
            ],
            Tips =
            [
                "Keep your feet hip-width apart, not in a straight line, for balance.",
                "Keep your torso upright and the bar steady.",
                "Don't use a weight you can't control on a wobbly rep.",
            ],
            Video = new("EWBiNhxDnmQ"),
        },
        new("walking_lunge", "Walking Lunge", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "Continuous forward lunges, stepping through with each rep to travel across the floor.",
            Steps =
            [
                "Stand tall with your hands on your hips or at your sides.",
                "Step forward and lower until your back knee is just above the floor.",
                "Drive through the front foot and bring the back foot forward into the next lunge.",
                "Keep alternating legs as you walk forward.",
            ],
            Tips =
            [
                "Take long enough steps that your front knee stays over your ankle.",
                "Keep your torso upright and core braced.",
                "Don't let your back knee bang the floor.",
            ],
            Video = new("BenhAbJiTsw"),
        },
        new("db_walking_lunge", "Dumbbell Walking Lunge", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "Walking lunges holding a dumbbell in each hand, a classic leg-day finisher for the quads and glutes.",
            Steps =
            [
                "Stand tall with a dumbbell in each hand at your sides.",
                "Step forward into a lunge until your back knee is just above the floor.",
                "Drive through the front foot and step straight through into the next lunge.",
                "Continue alternating legs for the set distance or reps.",
            ],
            Tips =
            [
                "Keep your shoulders back and grip tight.",
                "Control each step; don't fall into the lunge.",
                "Don't let your front knee drift inward.",
            ],
            Video = new("Tc1TsAdoDRo"),
        },
        new("lateral_lunge", "Lateral Lunge", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "A side lunge that trains the quads, glutes and adductors in the side-to-side plane.",
            Steps =
            [
                "Stand with your feet together.",
                "Take a wide step to one side and sit your hips back over that foot, bending the knee.",
                "Keep the other leg straight and both feet pointing forward.",
                "Push off the bent leg to return to the start, then switch sides.",
            ],
            Tips =
            [
                "Keep your chest up and your heel down on the working side.",
                "Hold a dumbbell or kettlebell at your chest to add load.",
                "Don't let your knee cave inward past your toes.",
            ],
            Video = new("MvpBUsQrt_4"),
        },
        new("curtsy_lunge", "Curtsy Lunge", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "A lunge stepping one leg diagonally behind the other, adding glute medius and adductor work to the quads.",
            Steps =
            [
                "Stand with your feet hip-width.",
                "Step one leg back and across behind the other, like a curtsy.",
                "Lower until your front thigh is about parallel.",
                "Drive through the front foot to return to standing, then switch.",
            ],
            Tips =
            [
                "Keep your hips and shoulders facing forward.",
                "Keep your front knee tracking over your toes.",
                "Don't cross back so far that you twist your knee.",
            ],
            Video = new("cVYnf2CFO9M", 30.99),
        },

        // Step-ups and step-downs
        new("step_up", "Dumbbell Step-Up", Quads, Dumbbell, Compound)
        {
            Secondary = [Glutes],
            Summary = "Stepping up onto a box or bench holding dumbbells, training each leg's quads and glutes.",
            Steps =
            [
                "Stand facing a box or bench about knee height, a dumbbell in each hand.",
                "Place one whole foot on the box.",
                "Drive through that foot to stand up on the box, bringing the other foot up.",
                "Step down under control with the trailing leg and repeat.",
            ],
            Tips =
            [
                "Let the top leg do the work; don't push off the bottom foot.",
                "Keep your knee in line with your toes.",
                "Don't use a box so high that your hips sink below your knee.",
            ],
            Video = new("5ksu8nrdVIE"),
        },
        new("box_step_up", "Box Step-Up", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "A bodyweight step-up onto a box or step, a simple single-leg exercise for strength and knee control.",
            Steps =
            [
                "Stand facing a box or step.",
                "Place one whole foot on top.",
                "Drive through that foot to stand tall on the box.",
                "Lower back down slowly with the same leg and repeat.",
            ],
            Tips =
            [
                "Go slowly on the way down for more work.",
                "Keep your torso upright and hips level.",
                "Don't bounce off the bottom foot.",
            ],
            Video = new("EQKg5Gg2PNs"),
        },
        new("barbell_step_up", "Barbell Step-Up", Quads, Barbell, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A step-up with a barbell on your back, allowing heavier single-leg loading for the quads and glutes.",
            Steps =
            [
                "Unrack a barbell onto your upper back and stand facing a box about knee height.",
                "Place one whole foot on the box.",
                "Drive through that foot to stand up on the box.",
                "Step down under control and repeat.",
            ],
            Tips =
            [
                "Use a stable box and a weight you can balance easily.",
                "Keep your chest up and the bar level.",
                "Don't push off the floor with the trailing leg.",
            ],
            Video = new("oHUJbAlLSu4"),
        },
        new("lateral_step_up", "Lateral Step-Up", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "A step-up from the side of a box, training the quads and glute medius together.",
            Steps =
            [
                "Stand side-on to a box or step with your near foot on top.",
                "Drive through the top foot to stand up, lifting the other foot to the box.",
                "Lower the outside foot back to the floor under control.",
                "Finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your knee tracking over your toes.",
                "Hold dumbbells once bodyweight is easy.",
                "Don't let your hips drop or rotate.",
            ],
            Video = new("6g7LnS8Y7jI"),
        },
        new("poliquin_step_up", "Poliquin Step-Up", Quads, Bodyweight, Compound)
        {
            Level = Intermediate,
            Summary = "A short-range, heel-elevated step-down from the side of a low step, popularised by Charles Poliquin, that targets the quads, especially the inner VMO, as the knee travels forward.",
            Steps =
            [
                "Stand side-on on a low step with the working foot on a slant board or wedge, heel raised, and the other foot hanging off the side.",
                "Keeping your torso upright, bend the working knee forward over your toes to lower the free heel toward the floor.",
                "Lightly tap the free heel down without putting weight on it.",
                "Straighten the working knee to rise back up. Finish your reps, then switch legs.",
            ],
            Tips =
            [
                "Move slowly, especially on the way down.",
                "Hold light dumbbells once bodyweight is easy.",
                "Don't let the knee drift inward or push off the floor with the free foot.",
            ],
            Video = new("VJWAb94U4SM", 0, 35.433),
        },
        new("peterson_step_up", "Peterson Step-Up", Quads, Bodyweight, Compound)
        {
            Level = Intermediate,
            Summary = "A knee-strengthening step-up done on the ball of the foot on a low step, driving the knee forward over the toes to load the quads (especially the VMO) through a short range.",
            Steps =
            [
                "Stand with one foot on a low step and the other foot just off its edge.",
                "Rise onto the ball of the working foot so its heel is raised.",
                "Keeping that heel up, bend the knee forward over the toes to lower the free foot toward the floor.",
                "Straighten the knee to rise back up, lowering the working heel as you reach the top. Finish your reps, then switch legs.",
            ],
            Tips =
            [
                "Keep the range short and the tempo slow.",
                "Keep the knee tracking over your second toe.",
                "Don't hinge at the hips; the knee does the work.",
            ],
            Video = new("yuvRE6PsvJw"),
        },
        new("step_down", "Step-Down", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "Slowly lowering one heel to the floor from a step while balancing on the other leg, building eccentric quad strength and knee control.",
            Steps =
            [
                "Stand on a step on one leg, the other leg hanging off the front.",
                "Bend the standing knee and slowly lower until the free heel lightly touches the floor.",
                "Keep your hips level and knee over your toes.",
                "Push back up through the standing leg.",
            ],
            Tips =
            [
                "Take 3 seconds to lower.",
                "Start with a low step and raise it as you get stronger.",
                "Don't let your knee collapse inward or your hip drop.",
            ],
            Video = new("RgTKgtV1ltk"),
        },

        // Leg press and extension
        new("leg_press", "Leg Press", Quads, Machine, Compound)
        {
            Secondary = [Glutes],
            Summary = "Pressing a weighted sled away with your legs, a safe way to load the quads and glutes heavily.",
            Steps =
            [
                "Sit in the machine with your back flat on the pad and feet shoulder-width in the middle of the platform.",
                "Press the platform up and release the safeties.",
                "Lower the platform until your knees reach about 90° or a little deeper.",
                "Press back up without locking your knees out hard.",
            ],
            Tips =
            [
                "Keep your lower back and hips on the pad; stop before they curl up.",
                "Lower feet on the platform target the quads; higher feet bring in glutes.",
                "Don't snap your knees into lockout.",
            ],
            Video = new("cDGOn-yfKJA"),
        },
        new("single_leg_press", "Single-Leg Leg Press", Quads, Machine, Compound)
        {
            Secondary = [Glutes],
            Summary = "A leg press with one leg at a time, evening out strength differences between sides.",
            Steps =
            [
                "Sit in the leg press with one foot in the middle of the platform.",
                "Rest the other foot on the frame or floor.",
                "Lower the platform until your knee reaches about 90°.",
                "Press back up, finish your reps and switch legs.",
            ],
            Tips =
            [
                "Use much less than half your normal leg press weight to start.",
                "Keep your knee tracking over your toes.",
                "Don't let your hips lift off the seat.",
            ],
            Video = new("BsOg0iBKs6A"),
        },
        new("narrow_leg_press", "Narrow-Stance Leg Press", Quads, Machine, Compound)
        {
            Summary = "A leg press with feet close together low on the platform, shifting the work onto the quads.",
            Steps =
            [
                "Sit in the leg press and place your feet hip-width or closer, low on the platform.",
                "Release the safeties.",
                "Lower the platform, letting your knees travel forward, as deep as you can with your hips down.",
                "Press back up without locking out.",
            ],
            Tips =
            [
                "Keep your heels on the platform throughout.",
                "Use a slow lowering for more quad tension.",
                "Don't let your lower back round off the pad.",
            ],
            Video = new("eDgxpUNdbD8"),
        },
        new("leg_extension", "Leg Extension", Quads, Machine, Isolation)
        {
            Summary = "The machine for isolating the quads: straightening your knees against a pad at your ankles.",
            Steps =
            [
                "Sit in the machine with your knees in line with its pivot and the pad on your lower shins.",
                "Hold the handles and sit back against the pad.",
                "Straighten your legs fully and squeeze your quads at the top.",
                "Lower under control until your knees are bent about 90°.",
            ],
            Tips =
            [
                "Pause briefly at the top.",
                "Lower slowly; don't let the stack crash.",
                "Don't swing or lift your hips off the seat.",
            ],
            Video = new("ArOrQ9zicUk"),
        },
        new("single_leg_extension", "Single-Leg Leg Extension", Quads, Machine, Isolation)
        {
            Summary = "A leg extension with one leg at a time, for fixing imbalances and focusing on each quad.",
            Steps =
            [
                "Set up in the leg extension machine with the pad on one shin.",
                "Hold the handles and sit back.",
                "Straighten the working leg fully and squeeze the quad.",
                "Lower under control, finish your reps and switch legs.",
            ],
            Tips =
            [
                "Start with the weaker leg and match the reps on the other.",
                "Keep your hips on the seat.",
                "Don't kick the weight up with momentum.",
            ],
            Video = new("82IuSLk5zNc"),
        },
        new("reverse_nordic", "Reverse Nordic", Quads, Bodyweight, Isolation)
        {
            Level = Intermediate,
            Summary = "Kneeling and leaning back with a straight body from knees to head, loading the quads in a long stretch.",
            Steps =
            [
                "Kneel on a mat with your knees hip-width and your body upright.",
                "Keep your hips straight and lean back slowly as one unit.",
                "Lower as far as you can control.",
                "Use your quads to pull yourself back to upright.",
            ],
            Tips =
            [
                "Squeeze your glutes to keep your hips from bending.",
                "Start with a small range and increase it over weeks.",
                "Use a padded mat to protect your knees.",
            ],
            Video = new("efvU8PmaeC8"),
        },

        // Adductors
        new("adductor_machine", "Hip Adduction Machine", Quads, Machine, Isolation)
        {
            Summary = "A seated machine that trains the inner-thigh adductors by squeezing your legs together against the pads.",
            Steps =
            [
                "Sit in the machine with your back against the pad and the pads on the inside of your knees.",
                "Set the start width to a comfortable stretch.",
                "Squeeze your legs together until the pads meet.",
                "Return slowly to the starting width.",
            ],
            Tips =
            [
                "Control the return; that's where the stretch is.",
                "Sit upright and keep your back on the pad.",
                "Don't set the start so wide that it strains your groin.",
            ],
            Video = new("BmMmt-c9aNM"),
        },
        new("cable_hip_adduction", "Cable Hip Adduction", Quads, Cable, Isolation)
        {
            Summary = "A standing adduction with an ankle strap on a low cable, pulling the leg across the body to train the inner thigh.",
            Steps =
            [
                "Attach an ankle strap to a low cable and fasten it to the ankle nearest the machine.",
                "Stand side-on, a step away, holding the frame for balance.",
                "Pull your strapped leg in front of and across your standing leg.",
                "Return slowly to the start.",
            ],
            Tips =
            [
                "Keep your torso upright and the moving leg straight.",
                "Move only at the hip.",
                "Don't swing the leg with momentum.",
            ],
            Video = new("gdXIIVY8wIY"),
        },
        new("side_lying_adduction", "Side-Lying Hip Adduction", Quads, Bodyweight, Isolation)
        {
            Summary = "Lying on your side and lifting the bottom leg, a simple floor exercise for the inner thigh.",
            Steps =
            [
                "Lie on your side with your bottom leg straight.",
                "Cross your top leg over and place that foot flat on the floor in front of you.",
                "Lift the bottom leg a few inches off the floor.",
                "Lower slowly, finish your reps and switch sides.",
            ],
            Tips =
            [
                "Keep your hips stacked and the bottom leg straight.",
                "Pause at the top of each lift.",
                "Don't roll backward to lift higher.",
            ],
            Video = new("lhwT35sshrI"),
        },
        new("copenhagen_plank", "Copenhagen Plank", Quads, Bodyweight, Compound)
        {
            Secondary = [Abs],
            Level = Intermediate,
            Hold = true,
            Summary = "A side plank with your top leg on a bench, held by the adductors; proven to reduce groin injuries in field sports.",
            Steps =
            [
                "Lie on your side next to a bench and prop yourself up on your forearm.",
                "Place your top leg on the bench, at the knee for an easier version or at the ankle for the full one.",
                "Lift your hips until your body is straight, letting the bottom leg hang or lift to the bench.",
                "Hold, then lower and switch sides.",
            ],
            Tips =
            [
                "Start with the knee on the bench and short holds.",
                "Keep your body in a straight line from head to foot.",
                "Don't let your hips sag or rotate.",
            ],
            Video = new("IXjQrC45D7s"),
        },

        // Calves
        new("standing_calf_raise", "Standing Calf Raise", Calves, Machine, Isolation)
        {
            Summary = "Raising your heels with straight knees under a shoulder-pad machine, the main exercise for the gastrocnemius.",
            Steps =
            [
                "Stand with your shoulders under the pads and the balls of your feet on the edge of the platform.",
                "Straighten your legs to lift the weight.",
                "Lower your heels into a deep stretch below the platform.",
                "Rise as high as you can onto your toes, pause and lower slowly.",
            ],
            Tips =
            [
                "Pause for a second in the stretch to stop bouncing.",
                "Keep your knees straight but not locked hard.",
                "Don't cut the range short to use more weight.",
            ],
            Video = new("n-5T_oYc1oU"),
        },
        new("barbell_calf_raise", "Barbell Calf Raise", Calves, Barbell, Isolation)
        {
            Level = Intermediate,
            Summary = "A standing calf raise with a barbell on the back, for heavy calf work without a calf machine.",
            Steps =
            [
                "Set up in a squat rack with the bar on your upper back, balls of your feet on a plate or step if you have one.",
                "Stand tall with your feet hip-width apart.",
                "Lower your heels as far as you can for a stretch.",
                "Rise up onto your toes as high as you can, pause, and lower slowly.",
            ],
            Tips =
            [
                "Keep your knees straight but not locked.",
                "Use the rack's safety pins; balance gets harder as the calves tire.",
                "Pause in the stretch instead of bouncing out of it.",
            ],
            Video = new("3UWi44yN-wM"),
        },
        new("seated_calf_raise", "Seated Calf Raise", Calves, Machine, Isolation)
        {
            Summary = "A calf raise sitting with bent knees and a pad on your thighs, which targets the soleus.",
            Steps =
            [
                "Sit in the machine with the balls of your feet on the platform and the pad snug on your lower thighs.",
                "Lift the weight and release the safety lever.",
                "Lower your heels into a full stretch.",
                "Raise your heels as high as you can, pause and lower slowly.",
            ],
            Tips =
            [
                "Use a full range and a slow tempo.",
                "Higher reps (12–20) suit the soleus well.",
                "Don't bounce out of the bottom.",
            ],
            Video = new("ORY-ke6vcgk"),
        },
        new("db_calf_raise", "Dumbbell Calf Raise", Calves, Dumbbell, Isolation)
        {
            Summary = "A standing calf raise holding dumbbells, done off a step for a full range.",
            Steps =
            [
                "Stand with the balls of both feet on the edge of a step, a dumbbell in each hand.",
                "Lower your heels below the step into a stretch.",
                "Rise as high as you can onto your toes.",
                "Pause at the top, then lower slowly.",
            ],
            Tips =
            [
                "Keep your knees straight and your body upright.",
                "Hold a rail with one hand if you need balance and the dumbbell in the other.",
                "Don't bounce at the bottom.",
            ],
            Video = new("SRUtMJ0tE2A"),
        },
        new("single_leg_db_calf_raise", "Single-Leg Dumbbell Calf Raise", Calves, Dumbbell, Isolation)
        {
            Summary = "A one-leg calf raise off a step holding a dumbbell, a heavy calf exercise that needs no machine.",
            Steps =
            [
                "Stand on one foot on the edge of a step, holding a dumbbell on the same side and a rail with the other hand.",
                "Lower your heel below the step into a stretch.",
                "Rise as high as you can onto your toes.",
                "Pause, lower slowly, finish your reps and switch legs.",
            ],
            Tips =
            [
                "Use the rail for balance only.",
                "Keep your knee straight.",
                "Don't rush the lowering.",
            ],
            Video = new("E1mG5L9rpFc"),
        },
        new("seated_db_calf_raise", "Seated Dumbbell Calf Raise", Calves, Dumbbell, Isolation)
        {
            Summary = "A seated calf raise with dumbbells on the knees, which works the soleus like the seated calf machine.",
            Steps =
            [
                "Sit on a bench with the balls of your feet on a step or plate and dumbbells resting on your knees.",
                "Lower your heels as far as you can.",
                "Push up onto your toes as high as you can and squeeze.",
                "Lower slowly and repeat.",
            ],
            Tips =
            [
                "Pause at the bottom; the stretch makes it work.",
                "Put a towel between the dumbbells and your knees for comfort.",
            ],
            Video = new("fFWpWJy8ybU"),
        },
        new("band_calf_press", "Band Calf Press", Calves, Band, Isolation)
        {
            Summary = "Sitting with the legs straight and a band looped around the balls of the feet, pointing the toes against the band.",
            Steps =
            [
                "Sit on the floor with your legs straight and loop a band around the balls of your feet.",
                "Hold the ends of the band with your hands, enough to feel tension.",
                "Point your toes away from you as far as you can against the band.",
                "Return slowly and repeat.",
            ],
            Tips =
            [
                "Sit tall and keep the knees straight.",
                "Go slowly: it's easy to rush the return.",
            ],
            Video = new("jkH3WP_a_8E"),
        },
        new("bw_calf_raise", "Single-Leg Calf Raise", Calves, Bodyweight, Isolation)
        {
            Summary = "A bodyweight calf raise on one leg off a step, enough load for most people and a staple of calf and Achilles rehab.",
            Steps =
            [
                "Stand on one foot on the edge of a step, holding a wall or rail.",
                "Lower your heel below the step into a stretch.",
                "Rise as high as you can onto your toes.",
                "Lower slowly, finish your reps and switch legs.",
            ],
            Tips =
            [
                "Take 2–3 seconds to lower.",
                "Rise straight up over your big toe; don't roll to the outside of your foot.",
                "Don't bend the knee to help.",
            ],
            Video = new("qPd73snQfUs"),
        },
        new("bw_double_calf_raise", "Bodyweight Calf Raise", Calves, Bodyweight, Isolation)
        {
            Summary = "Rising onto your toes on both feet with no weight, a beginner calf exercise you can do anywhere.",
            Steps =
            [
                "Stand with your feet hip-width, on the floor or with the balls of your feet on a step.",
                "Hold a wall for balance if needed.",
                "Rise as high as you can onto your toes.",
                "Lower slowly, letting your heels drop below the step if you're on one.",
            ],
            Tips =
            [
                "Pause at the top of each rep.",
                "Move on to single-leg raises once you can do 25 easily.",
                "Don't bounce through the reps.",
            ],
            Video = new("eMTy3qylqnE"),
        },
        new("leg_press_calf_raise", "Leg Press Calf Raise", Calves, Machine, Isolation)
        {
            Summary = "A calf raise done on the leg press, pushing the platform with the balls of your feet.",
            Steps =
            [
                "Sit in the leg press and press the platform up with your legs straight.",
                "Slide your feet down so only the balls of your feet are on the bottom edge.",
                "Let the platform push your toes toward you into a stretch.",
                "Press through the balls of your feet to point your toes, then return slowly.",
            ],
            Tips =
            [
                "Keep the safety catches engaged where the machine allows.",
                "Keep your knees straight but not locked.",
                "Don't let your feet slip off the platform; use a controlled tempo.",
            ],
            Video = new("sluiSitVePs"),
        },
        new("smith_calf_raise", "Smith Machine Calf Raise", Calves, Machine, Isolation)
        {
            Summary = "A standing calf raise with a Smith machine bar on your back, often done standing on a plate or step.",
            Steps =
            [
                "Place a step or plate under the bar and set the bar at shoulder height.",
                "Step under the bar and stand with the balls of your feet on the edge of the step.",
                "Unhook the bar and lower your heels into a stretch.",
                "Rise as high as you can onto your toes, pause and lower slowly.",
            ],
            Tips =
            [
                "Keep your body upright and knees straight.",
                "Pause at both the top and bottom.",
                "Don't bounce in the stretch.",
            ],
            Video = new("8YuFuc9gqYg"),
        },
        new("donkey_calf_raise", "Donkey Calf Raise", Calves, Machine, Isolation)
        {
            Summary = "A calf raise bent forward at the hips with the load on your lower back, giving the calves a deep stretch.",
            Steps =
            [
                "Set up in a donkey calf machine with the pad on your lower back and hips, bent forward at the hips.",
                "Place the balls of your feet on the edge of the platform and hold the handles.",
                "Lower your heels into a deep stretch.",
                "Rise as high as you can onto your toes, pause and lower slowly.",
            ],
            Tips =
            [
                "Keep your knees straight and your back flat.",
                "Use a full range rather than heavy partials.",
                "Don't round your lower back under the pad.",
            ],
            Video = new("watMaxAQBCU"),
        },

        // Shins
        new("tibialis_raise", "Tibialis Raise", Calves, Bodyweight, Isolation)
        {
            Summary = "Lifting your toes while leaning against a wall to train the tibialis anterior on the front of the shin.",
            Steps =
            [
                "Stand with your back against a wall and your heels about a foot in front of it.",
                "Keep your legs straight and heels on the floor.",
                "Lift your toes as high as you can toward your shins.",
                "Lower slowly until your toes nearly touch the floor and repeat.",
            ],
            Tips =
            [
                "Step your feet further from the wall to make it harder.",
                "Expect a burn; high reps work well.",
                "Don't let your toes slap down.",
            ],
            Video = new("OPEuhclsTUQ"),
        },
        new("seated_tibialis_raise", "Seated Tibialis Raise", Calves, Machine, Isolation)
        {
            Summary = "Lifting your toes against a padded load on a tibialis machine or tib bar while seated, a loadable way to train the front of the shin.",
            Steps =
            [
                "Sit on the machine or a bench with the pad or tib bar over the tops of your feet.",
                "Let your toes drop toward the floor into a stretch.",
                "Pull your toes up toward your shins as far as you can.",
                "Pause, then lower slowly.",
            ],
            Tips =
            [
                "Start light; the tibialis is a small muscle.",
                "Keep your heels fixed and move only at the ankle.",
                "Don't jerk the weight up.",
            ],
            Video = new("jI2ahvUAGqs"),
        },
    ];
}
