using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] ChestExercises() =>
    [
        // Flat pressing
        new("bench_press", "Bench Press", Chest, Barbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "The classic barbell press for chest, front delts and triceps, done lying on a flat bench.",
            Steps =
            [
                "Lie on the bench with your eyes under the bar, feet flat and shoulder blades pulled back and down.",
                "Grip the bar a little wider than your shoulders and unrack it over your shoulders.",
                "Lower it under control to your lower chest, elbows about 45° from your body.",
                "Press it back up and slightly back until your arms are straight.",
            ],
            Tips =
            [
                "Keep your upper back tight and your glutes on the bench throughout.",
                "Drive your feet into the floor to stay stable.",
                "Don't bounce the bar off your chest or flare your elbows to 90°.",
                "Use a spotter or safety arms when working near your limit.",
            ],
            Video = new("_FkbD0FhgVE"),
        },
        new("db_bench_press", "Dumbbell Bench Press", Chest, Dumbbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A flat bench press with two dumbbells, which allows a deeper stretch than a barbell and makes each side work on its own.",
            Steps =
            [
                "Sit on the end of a flat bench with the dumbbells on your thighs, then lie back and bring them to your chest.",
                "Set your shoulder blades back and down, feet flat, dumbbells just outside your chest.",
                "Press the dumbbells up over your shoulders until your arms are straight.",
                "Lower them slowly until your elbows sit just below the bench and you feel a stretch across your chest.",
            ],
            Tips =
            [
                "Keep your forearms vertical and your elbows about 45° from your body.",
                "Let the dumbbells come slightly together at the top, but don't clank them.",
                "Don't drop your elbows far below the bench if your shoulders complain.",
            ],
            Video = new("4_QuyfOCI5U"),
        },
        new("smith_bench_press", "Smith Machine Bench Press", Chest, Machine, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A flat bench press on a Smith machine; the fixed bar path makes it easy to push hard safely without a spotter.",
            Steps =
            [
                "Set a flat bench so the bar lines up with your lower chest when lowered.",
                "Lie back, grip the bar a little wider than your shoulders and twist it off the hooks.",
                "Lower the bar under control to your lower chest.",
                "Press it back up to straight arms, then twist it back onto the hooks when done.",
            ],
            Tips =
            [
                "Set the safety stops just below your chest height.",
                "Keep your shoulder blades pinned back as you would on a free bench.",
                "Don't set the bench so the bar lands on your neck or upper chest.",
            ],
            Video = new("AfcBc_nSHIc"),
        },
        new("machine_chest_press", "Machine Chest Press", Chest, Machine, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A seated, guided press that trains the chest with no balance demand, making it easy to learn and to push close to failure.",
            Steps =
            [
                "Adjust the seat so the handles line up with your mid-chest.",
                "Sit with your back against the pad, shoulder blades back and feet flat.",
                "Press the handles forward until your arms are almost straight.",
                "Return slowly until you feel a stretch across your chest.",
            ],
            Tips =
            [
                "Keep your chest up and your shoulders down away from your ears.",
                "Don't let your shoulders roll forward off the pad as you press.",
                "Don't let the weight stack slam between reps.",
            ],
            Video = new("sqNwDkUU_Ps"),
        },
        new("floor_press", "Barbell Floor Press", Chest, Barbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A bench press done lying on the floor; the floor cuts the range short, taking strain off the shoulders and loading the lockout and triceps hard.",
            Steps =
            [
                "Set the bar in a rack at about forearm height above the floor, or use safety arms set low.",
                "Lie under it with your eyes below the bar, knees bent or legs straight, and grip a little wider than your shoulders.",
                "Unrack and lower the bar until your upper arms rest on the floor.",
                "Pause briefly, then press back up to straight arms.",
            ],
            Tips =
            [
                "Let your arms touch down softly; don't bounce your elbows off the floor.",
                "Keep your elbows about 45° from your body.",
                "Keep your shoulder blades squeezed together against the floor.",
            ],
            Video = new("9vYCwtHkWgI"),
        },
        new("db_floor_press", "Dumbbell Floor Press", Chest, Dumbbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A dumbbell press lying on the floor, a shoulder-friendly option that needs no bench and stresses the top half of the press.",
            Steps =
            [
                "Sit on the floor with a dumbbell on each thigh, then lie back with your knees bent and bring them to your chest.",
                "Press the dumbbells up over your shoulders until your arms are straight.",
                "Lower them until your upper arms touch the floor.",
                "Pause for a moment, then press back up.",
            ],
            Tips =
            [
                "Lower your arms to the floor gently instead of dropping onto your elbows.",
                "Keep your wrists stacked over your elbows.",
                "Don't flare your elbows straight out to the sides.",
            ],
            Video = new("T0Y3OBF1bNI"),
        },
        new("neutral_grip_db_press", "Neutral-Grip Dumbbell Press", Chest, Dumbbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A flat dumbbell press with palms facing each other, which keeps the elbows tucked and is often easier on the shoulders.",
            Steps =
            [
                "Lie on a flat bench with a dumbbell in each hand, palms facing each other at the sides of your chest.",
                "Set your shoulder blades back and down and your feet flat.",
                "Press the dumbbells straight up until your arms are straight.",
                "Lower them slowly back to the sides of your chest, elbows close to your body.",
            ],
            Tips =
            [
                "Keep your elbows about 30° from your torso.",
                "Keep the dumbbells parallel; don't let them rotate as you press.",
                "Don't let your shoulders shrug up as the weights come down.",
            ],
            Video = new("Oq5MMAbUE2o"),
        },
        new("single_arm_db_bench_press", "Single-Arm Dumbbell Bench Press", Chest, Dumbbell, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Intermediate,
            Summary = "A flat bench press with one dumbbell at a time, which trains each side alone and makes your core fight to stop you rolling off the bench.",
            Steps =
            [
                "Lie on a flat bench holding one dumbbell at the side of your chest; grip the bench with your free hand or rest it on your stomach.",
                "Plant your feet wide and brace your core.",
                "Press the dumbbell up over your shoulder until your arm is straight.",
                "Lower it slowly to your chest, finish the set, then switch sides.",
            ],
            Tips =
            [
                "Keep your hips and shoulders square to the ceiling.",
                "Start lighter than your usual dumbbell press.",
                "Don't let your body twist toward the weight.",
            ],
            Video = new("d6Ba29xFBnw"),
        },
        new("db_squeeze_press", "Dumbbell Squeeze Press", Chest, Dumbbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A flat press with the dumbbells squeezed hard together the whole time, which keeps the chest working through the full rep. Also called a crush press.",
            Steps =
            [
                "Lie on a flat bench holding two dumbbells together over your chest, palms facing each other.",
                "Squeeze the dumbbells into each other as hard as you can.",
                "Keeping that squeeze, lower them until they touch your chest.",
                "Press them back up, still pressing them together.",
            ],
            Tips =
            [
                "Use lighter weights than your normal dumbbell press; the squeeze is the point.",
                "Keep your elbows tucked close to your body.",
                "Don't let the dumbbells drift apart at the bottom.",
            ],
            Video = new("Aq_wwRslKas"),
        },
        new("cable_chest_press", "Standing Cable Chest Press", Chest, Cable, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Summary = "A standing press with two cables, which keeps tension on the chest through the whole range and makes your core stabilise you.",
            Steps =
            [
                "Set both pulleys at about chest height and take a handle in each hand.",
                "Step forward into a split stance, handles beside your chest, elbows bent.",
                "Press the handles forward and slightly together until your arms are straight.",
                "Return slowly until your hands are back beside your chest.",
            ],
            Tips =
            [
                "Brace your core and keep your torso still; your arms do the moving.",
                "Lean forward a little from the ankles, not by bending at the waist.",
                "Don't let the cables pull your shoulders forward at the start.",
            ],
            Video = new("A12UEw70jio"),
        },
        new("band_chest_press", "Band Chest Press", Chest, Band, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A standing chest press against a resistance band anchored behind you, a simple home or travel option.",
            Steps =
            [
                "Anchor the band at chest height behind you, or wrap it across your upper back, and hold one end in each hand.",
                "Stand in a split stance with your hands beside your chest and elbows bent.",
                "Press your hands forward until your arms are straight.",
                "Bring them back slowly to your chest.",
            ],
            Tips =
            [
                "Step further from the anchor to make it harder.",
                "Squeeze your chest at full extension, where the band is tightest.",
                "Don't let the band snap your hands back; control the return.",
            ],
            Video = new("6-86jEAXA08"),
        },
        new("landmine_chest_press", "Landmine Chest Press", Chest, Barbell, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Summary = "A standing two-handed press of a barbell anchored in a landmine, which presses up and forward on an incline path that hits the upper chest.",
            Steps =
            [
                "Set one end of a barbell in a landmine or corner and load the other end.",
                "Stand facing the bar and hold the end with both hands interlaced at your chest.",
                "Lean slightly forward, brace and press the bar up and away until your arms are straight.",
                "Lower it slowly back to your chest.",
            ],
            Tips =
            [
                "Squeeze your palms together to bring the chest in more.",
                "Keep your ribs down; don't lean back to finish the press.",
                "Don't let the bar drift off to one side.",
            ],
            Video = new("6sH6vyWduNU"),
        },
        new("paused_bench_press", "Paused Bench Press", Chest, Barbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A bench press with a dead-still pause on the chest each rep, as in powerlifting, which builds strength off the chest and removes any bounce.",
            Steps =
            [
                "Set up as for a normal bench press: eyes under the bar, shoulder blades back and down, feet planted.",
                "Unrack the bar and lower it under control to your lower chest.",
                "Hold it motionless on your chest for 1–3 seconds while staying tight.",
                "Press it back up and slightly back to straight arms.",
            ],
            Tips =
            [
                "Keep pressure on the bar during the pause; don't relax and let it sink into your chest.",
                "Expect to use about 5–10% less than your touch-and-go bench.",
                "Don't lift your hips or heave the bar to start the press.",
            ],
            Video = new("i-ylz1zdp5s"),
        },
        new("larsen_press", "Larsen Press", Chest, Barbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A bench press with your legs straight out and feet off the floor, so there is no leg drive and your upper body must stay stable on its own.",
            Steps =
            [
                "Lie on the bench with shoulder blades pulled back, then straighten your legs and lift your feet just off the floor.",
                "Unrack the bar and steady it over your shoulders.",
                "Lower it under control to your lower chest without letting your body tip.",
                "Press it back up to straight arms, keeping your legs still.",
            ],
            Tips =
            [
                "Use lighter weight than your normal bench; balance is the limiter.",
                "Keep your glutes on the bench and a small, controlled arch.",
                "Don't let the bar drift unevenly or your hips twist.",
                "Use a spotter or safety arms; you can't use your legs to bail.",
            ],
            Video = new("Ilt0GuTwB_Y"),
        },
        new("spoto_press", "Spoto Press", Chest, Barbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A bench press paused 2–5 cm above the chest without touching it, named after Eric Spoto, to build control and strength just off the chest.",
            Steps =
            [
                "Set up as for a normal bench press and unrack the bar.",
                "Lower it under control toward your chest.",
                "Stop it an inch or two above your chest and hold it still for a second.",
                "Press it back up to straight arms.",
            ],
            Tips =
            [
                "Pause in the same spot every rep, just above where the bar would touch.",
                "Stay tight and keep your elbows tucked during the pause.",
                "Don't let the bar drift toward your face or touch your chest.",
            ],
            Video = new("gModkRG2uu4", 64.647),
        },
        new("reverse_grip_bench_press", "Reverse-Grip Bench Press", Chest, Barbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A barbell bench press with an underhand grip, which tucks the elbows and shifts more work onto the upper chest and triceps with less shoulder stress.",
            Steps =
            [
                "Lie on the bench and grip the bar underhand, palms facing your head, about shoulder width.",
                "Have a spotter help you unrack it and steady it over your shoulders.",
                "Lower it with tucked elbows to your lower chest, just below the nipples.",
                "Press it back up and slightly back toward your face to straight arms.",
            ],
            Tips =
            [
                "Squeeze the bar hard and keep your wrists stacked over your forearms.",
                "Start light; the grip feels unstable at first.",
                "Use a spotter for the lift-off and heavy sets; the bar can roll out of an underhand grip.",
                "Don't use a thumbless grip.",
            ],
            Video = new("G0uxeMB8_xg", 63.72, 112.6),
        },
        new("guillotine_press", "Guillotine Press", Chest, Barbell, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Level = Advanced,
            Summary = "Vince Gironda's neck press: a wide-grip bench press with flared elbows, lowering the bar toward your neck to stretch the upper chest. Light weight only.",
            Steps =
            [
                "Lie on a flat bench and take a grip a little wider than for a normal bench press.",
                "Unrack the bar and hold it over your upper chest, elbows pointing out.",
                "Lower it slowly toward the base of your neck, stopping just above it.",
                "Press it back up over your upper chest to straight arms.",
            ],
            Tips =
            [
                "Use light weight, a controlled tempo and a spotter or safety arms set just above your neck.",
                "Stop short of touching your neck and keep your upper back tight.",
                "Don't go heavy or chase failure; the flared position is hard on the shoulders.",
                "Skip it if you have shoulder pain.",
            ],
            Video = new("y90T12sTukg"),
        },

        // Incline pressing
        new("incline_bench_press", "Incline Bench Press", Chest, Barbell, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Level = Intermediate,
            Summary = "A barbell press on a 30–45° bench, shifting the work toward the upper chest and front delts.",
            Steps =
            [
                "Set the bench to 30–45° and lie back with your eyes under the bar.",
                "Grip a little wider than your shoulders, pull your shoulder blades back and unrack.",
                "Lower the bar under control to your upper chest, just below your collarbones.",
                "Press it up and slightly back until your arms are straight over your shoulders.",
            ],
            Tips =
            [
                "A lower incline (around 30°) keeps more of the work on the chest.",
                "Keep your hips on the seat and your feet planted.",
                "Don't arch so hard that the incline becomes a flat press.",
            ],
            Video = new("98HWfiRonkE"),
        },
        new("incline_db_press", "Incline Dumbbell Press", Chest, Dumbbell, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Summary = "A dumbbell press on a 30–45° bench for the upper chest, with a deeper stretch than the barbell version.",
            Steps =
            [
                "Set the bench to 30–45°, sit back and bring the dumbbells up to your shoulders.",
                "Pull your shoulder blades back and down, feet flat.",
                "Press the dumbbells up over your upper chest until your arms are straight.",
                "Lower them slowly to the sides of your upper chest until you feel a stretch.",
            ],
            Tips =
            [
                "Keep your forearms vertical and your elbows about 45° from your body.",
                "Press toward a point above your chin, not out over your face.",
                "Don't let your shoulders roll forward at the top.",
            ],
            Video = new("8fXfwG4ftaQ"),
        },
        new("incline_smith_press", "Incline Smith Machine Press", Chest, Machine, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Summary = "An incline press on a Smith machine, a stable way to load the upper chest without a spotter.",
            Steps =
            [
                "Set an incline bench at 30–45° under the Smith bar so it lowers to your upper chest.",
                "Lie back, grip a little wider than your shoulders and twist the bar off the hooks.",
                "Lower the bar under control to just below your collarbones.",
                "Press it back up to straight arms.",
            ],
            Tips =
            [
                "Check the bench position with an empty bar first.",
                "Set the safety stops just above your chest.",
                "Don't let your shoulders shrug up toward your ears.",
            ],
            Video = new("8urE8Z8AMQ4"),
        },
        new("incline_machine_press", "Incline Machine Press", Chest, Machine, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Summary = "A seated machine press that pushes up and forward on an incline path to target the upper chest.",
            Steps =
            [
                "Adjust the seat so the handles start at about upper-chest height.",
                "Sit with your back against the pad and shoulder blades pulled back.",
                "Press the handles up and forward until your arms are almost straight.",
                "Lower them slowly until you feel a stretch across your upper chest.",
            ],
            Tips =
            [
                "Keep your chest high and your back flat on the pad.",
                "Control the lowering; don't let the stack crash.",
                "Don't let your shoulders roll forward off the pad.",
            ],
            Video = new("TrTSvn5-MTk"),
        },

        // Decline pressing
        new("decline_bench_press", "Decline Bench Press", Chest, Barbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A barbell press on a bench angled head-down, which emphasises the lower chest and usually allows heavy loads with less shoulder stress.",
            Steps =
            [
                "Set a decline bench to about 15–30° and hook your feet under the pads.",
                "Lie back, grip a little wider than your shoulders and unrack with a spotter's help.",
                "Lower the bar under control to your lower chest.",
                "Press it back up until your arms are straight over your shoulders.",
            ],
            Tips =
            [
                "Use a spotter; getting out from under a stuck bar is harder head-down.",
                "Keep your shoulder blades pulled back.",
                "Don't lower the bar toward your neck.",
            ],
            Video = new("FFyGwcLnDYc", 34.398998, 54.132999),
        },
        new("decline_db_press", "Decline Dumbbell Press", Chest, Dumbbell, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A dumbbell press on a decline bench for the lower chest, with each arm working on its own.",
            Steps =
            [
                "Sit on a decline bench with your feet hooked and the dumbbells on your thighs.",
                "Lie back carefully, bringing the dumbbells to the sides of your lower chest.",
                "Press them up until your arms are straight over your chest.",
                "Lower them slowly to the sides of your lower chest.",
            ],
            Tips =
            [
                "Have someone hand you the dumbbells if they're heavy.",
                "At the end of the set, bring the dumbbells to your chest and sit up with them, or lower them to the floor carefully.",
                "Don't drop the dumbbells from the bottom position.",
            ],
            Video = new("Pf1nDoqx_1A"),
        },

        // Flyes and crossovers
        new("db_fly", "Dumbbell Fly", Chest, Dumbbell, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A flat bench fly that stretches and squeezes the chest by opening and closing the arms in a wide arc.",
            Steps =
            [
                "Lie on a flat bench holding two dumbbells over your chest, palms facing each other and elbows slightly bent.",
                "Open your arms out to the sides in a wide arc, keeping the elbow bend fixed.",
                "Stop when your upper arms are about level with the bench and you feel a deep stretch.",
                "Squeeze your chest to bring the dumbbells back together over your chest.",
            ],
            Tips =
            [
                "Think of hugging a big tree; the elbows stay soft but don't change angle.",
                "Use much lighter weights than you press.",
                "Don't lower past where your shoulders feel comfortable.",
            ],
            Video = new("vJh-4hRLH-o"),
        },
        new("incline_db_fly", "Incline Dumbbell Fly", Chest, Dumbbell, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A dumbbell fly on a 30–45° bench that focuses on the upper chest.",
            Steps =
            [
                "Set the bench to 30–45° and lie back holding two dumbbells over your upper chest, palms facing each other.",
                "With a slight bend in your elbows, open your arms out wide in an arc.",
                "Stop when you feel a stretch across your upper chest.",
                "Squeeze the dumbbells back together over your upper chest.",
            ],
            Tips =
            [
                "Keep your shoulder blades pinned back against the bench.",
                "Keep the elbow angle the same through the whole rep.",
                "Don't turn it into a press by bending your elbows more at the bottom.",
            ],
            Video = new("ozAhti8BK6s"),
        },
        new("decline_db_fly", "Decline Dumbbell Fly", Chest, Dumbbell, Isolation)
        {
            Secondary = [Shoulders],
            Level = Intermediate,
            Summary = "A dumbbell fly on a decline bench, which shifts the emphasis toward the lower chest.",
            Steps =
            [
                "Lie on a decline bench with your feet hooked, holding two dumbbells over your lower chest.",
                "With a slight bend in your elbows, open your arms out to the sides.",
                "Stop when you feel a stretch across your chest.",
                "Squeeze the dumbbells back together over your lower chest.",
            ],
            Tips =
            [
                "Go light; it's awkward to get in and out of position with heavy dumbbells.",
                "Keep the arc wide and the elbows soft.",
                "Don't let the weights drop below shoulder level.",
            ],
            Video = new("7NlIkLqcgnw"),
        },
        new("cable_fly", "Cable Fly", Chest, Cable, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A standing fly between two cables set at shoulder height, keeping constant tension on the chest as you bring your hands together.",
            Steps =
            [
                "Set both pulleys at about shoulder height and take a handle in each hand.",
                "Step forward into a split stance with your arms out wide and elbows slightly bent.",
                "Bring your hands together in front of your chest in a wide hugging arc.",
                "Squeeze for a moment, then open back out slowly until you feel a stretch.",
            ],
            Tips =
            [
                "Keep your chest up and your elbow bend fixed.",
                "Lean slightly forward and stay still; only your arms move.",
                "Don't let the cables pull your arms back further than your shoulders like.",
            ],
            Video = new("I-Ue34qLxc4"),
        },
        new("high_to_low_cable_fly", "High-to-Low Cable Fly", Chest, Cable, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "The classic cable crossover: the cables pull from above and you sweep your hands down and together, emphasising the lower chest.",
            Steps =
            [
                "Set both pulleys high and take a handle in each hand.",
                "Step forward into a split stance with your arms out wide and slightly above shoulder height.",
                "Sweep your hands down and together until they meet in front of your hips or lower chest.",
                "Return slowly along the same arc until you feel a stretch.",
            ],
            Tips =
            [
                "Keep a slight bend in your elbows that doesn't change.",
                "Lean your torso slightly forward from the hips.",
                "Don't turn it into a pushdown by bending and straightening your elbows.",
            ],
            Video = new("8Um35Es-ROE", 39, 103),
        },
        new("low_to_high_cable_fly", "Low-to-High Cable Fly", Chest, Cable, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A cable fly from low pulleys, sweeping the hands up and together to target the upper chest.",
            Steps =
            [
                "Set both pulleys at the bottom and take a handle in each hand.",
                "Stand between them with your arms down and slightly out to the sides, palms facing forward.",
                "Sweep your hands up and together until they meet in front of your upper chest.",
                "Lower them slowly back along the same arc.",
            ],
            Tips =
            [
                "Keep your elbows soft and fixed throughout.",
                "Finish at about chin height, not overhead.",
                "Don't shrug your shoulders up as your hands rise.",
            ],
            Video = new("KFl3Re5UbPo"),
        },
        new("incline_cable_fly", "Incline Cable Fly", Chest, Cable, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A fly lying on an incline bench between two low cables, which keeps tension on the upper chest even at the top where dumbbells go slack.",
            Steps =
            [
                "Place an incline bench at 30–45° between two low pulleys and take a handle in each hand.",
                "Lie back with your arms over your upper chest, palms facing each other.",
                "Open your arms out wide in an arc with a slight bend in your elbows.",
                "Squeeze your chest to bring the handles back together over your upper chest.",
            ],
            Tips =
            [
                "Keep your shoulder blades back against the bench.",
                "Keep the elbow angle fixed.",
                "Don't let the cables pull your hands below the bench.",
            ],
            Video = new("LGDCjwO-hFg"),
        },
        new("single_arm_cable_fly", "Single-Arm Cable Fly", Chest, Cable, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A cable fly done one arm at a time, which lets you bring the hand across the body for a fuller contraction.",
            Steps =
            [
                "Set one pulley at shoulder height and stand side-on to it, holding the handle in the near hand.",
                "Step away until your arm is stretched out to the side with a slight elbow bend.",
                "Sweep your hand across in front of your body until it passes your midline.",
                "Return slowly to the stretch, finish the set, then switch sides.",
            ],
            Tips =
            [
                "Keep your hips and shoulders square; don't twist to move the weight.",
                "Hold a fixed upright with your free hand if it helps you stay still.",
                "Don't bend your elbow to shorten the lever.",
            ],
            Video = new("E_mT1JWOp90"),
        },
        new("pec_deck", "Pec Deck", Chest, Machine, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A seated machine fly that brings the arms together in front of the chest along a fixed path. Also called the machine fly.",
            Steps =
            [
                "Adjust the seat so the handles or pads sit at about chest height.",
                "Sit tall with your back against the pad and take the handles with a slight bend in your elbows.",
                "Bring the handles together in front of your chest.",
                "Squeeze briefly, then let them open slowly until you feel a stretch.",
            ],
            Tips =
            [
                "Keep your shoulders down and back against the pad.",
                "Set the start position so the stretch is comfortable for your shoulders.",
                "Don't let the weight stack yank your arms back.",
            ],
            Video = new("O-OBCfyh9Fw"),
        },
        new("band_fly", "Band Chest Fly", Chest, Band, Isolation)
        {
            Secondary = [Shoulders],
            Summary = "A standing fly against a resistance band anchored behind you, a home version of the cable fly.",
            Steps =
            [
                "Anchor the band at chest height behind you and hold one end in each hand.",
                "Step forward until there's tension, with your arms out wide and elbows slightly bent.",
                "Bring your hands together in front of your chest in a wide arc.",
                "Open back out slowly.",
            ],
            Tips =
            [
                "Squeeze at the end, where the band is tightest.",
                "Keep your elbow bend fixed.",
                "Don't let the band pull your arms back past your shoulders.",
            ],
            Video = new("yVcEkvgymt8"),
        },
        new("svend_press", "Svend Press", Chest, Other, Compound)
        {
            Secondary = [Shoulders],
            Summary = "A standing press with one or two weight plates squeezed together between your palms, pushed straight out from the chest to keep the inner chest working hard.",
            Steps =
            [
                "Stand tall holding one or two weight plates pressed together between your flat palms at your chest.",
                "Squeeze the plates hard so your chest switches on.",
                "Keeping the squeeze, press the plates straight out in front of you until your arms are straight.",
                "Bring them back slowly to your chest.",
            ],
            Tips =
            [
                "The squeeze matters more than the weight; light plates are plenty.",
                "Keep your shoulders down and your ribs down.",
                "Don't let the plates sag toward the floor as you press.",
            ],
            Video = new("cIoUZOnypS8"),
        },

        // Push-ups
        new("push_up", "Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Summary = "The fundamental bodyweight press: lower your chest to the floor from a plank and push back up.",
            Steps =
            [
                "Start in a high plank with your hands just wider than your shoulders and your body in a straight line from head to heels.",
                "Brace your core and squeeze your glutes.",
                "Bend your elbows to lower your chest to just above the floor, elbows about 45° from your body.",
                "Push the floor away to return to the top.",
            ],
            Tips =
            [
                "Move as one solid piece; your hips and chest rise together.",
                "Keep your neck neutral and look at the floor slightly ahead of your hands.",
                "Don't let your hips sag or pike up.",
                "Don't flare your elbows straight out to the sides.",
            ],
            Video = new("-Oa88QNL1bc"),
        },
        new("wall_push_up", "Wall Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A push-up against a wall, the easiest version and a good starting point for building up to floor push-ups.",
            Steps =
            [
                "Stand an arm's length from a wall and place your hands on it at shoulder height, a little wider than your shoulders.",
                "Lean in with your body straight and heels lifting slightly.",
                "Bend your elbows to bring your chest close to the wall.",
                "Push back to straight arms.",
            ],
            Tips =
            [
                "Step your feet further back to make it harder.",
                "Keep your body in one straight line.",
                "Don't let your hips push toward the wall first.",
            ],
            Video = new("wIPJvBQs7RA"),
        },
        new("incline_push_up", "Incline Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Summary = "A push-up with your hands raised on a bench or box, which lightens the load and is a common step toward full push-ups.",
            Steps =
            [
                "Place your hands on a bench, box or bar, just wider than your shoulders.",
                "Walk your feet back until your body is straight from head to heels.",
                "Lower your chest to the edge of the bench, elbows about 45° from your body.",
                "Push back up to straight arms.",
            ],
            Tips =
            [
                "The higher the surface, the easier it gets; lower it as you get stronger.",
                "Keep your core braced so your hips don't sag.",
                "Don't reach your chin forward to touch the bench.",
            ],
            Video = new("Me9bHFAxnCs"),
        },
        new("knee_push_up", "Knee Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A push-up done from the knees, which shortens the lever so beginners can work the full range.",
            Steps =
            [
                "Kneel on a mat with your hands on the floor just wider than your shoulders.",
                "Walk your hands forward until your body is straight from head to knees.",
                "Lower your chest toward the floor, elbows about 45° from your body.",
                "Push back up to straight arms.",
            ],
            Tips =
            [
                "Keep your hips in line with your shoulders and knees, not bent up behind you.",
                "Pad your knees if the floor is hard.",
                "Don't cut the range short; get your chest close to the floor.",
            ],
            Video = new("PDr5B2jLUOw"),
        },
        new("wide_push_up", "Wide Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Triceps, Abs],
            Summary = "A push-up with the hands set wide, which puts more of the work on the chest and less on the triceps.",
            Steps =
            [
                "Start in a high plank with your hands about one and a half shoulder-widths apart.",
                "Brace your core and keep your body in a straight line.",
                "Lower your chest toward the floor between your hands.",
                "Push back up to straight arms.",
            ],
            Tips =
            [
                "Turn your fingers slightly outward if your wrists or shoulders prefer it.",
                "Keep a little bend toward your body in the elbows rather than flaring them fully.",
                "Don't go so wide that your shoulders ache at the bottom.",
            ],
            Video = new("m3FTgWtJsFE"),
        },
        new("decline_push_up", "Decline Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Triceps, Abs],
            Level = Intermediate,
            Summary = "A push-up with your feet raised on a bench or box, which adds load and shifts the work toward the upper chest and front delts.",
            Steps =
            [
                "Place your feet on a bench or box and your hands on the floor just wider than your shoulders.",
                "Set your body in a straight line from head to heels.",
                "Lower your chest toward the floor, elbows about 45° from your body.",
                "Push back up to straight arms.",
            ],
            Tips =
            [
                "The higher your feet, the harder it is and the more your shoulders work.",
                "Brace hard; your hips will want to sag.",
                "Don't drop your head toward the floor first.",
            ],
            Video = new("aq2xZxfrQlM"),
        },
        new("deficit_push_up", "Deficit Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Intermediate,
            Summary = "A push-up with the hands on blocks, plates or handles so the chest can sink below hand level for a deeper stretch.",
            Steps =
            [
                "Place two push-up handles, blocks or plates just wider than your shoulders and grip them.",
                "Set up in a high plank with your body straight.",
                "Lower your chest below the level of your hands until you feel a deep stretch.",
                "Push back up to straight arms.",
            ],
            Tips =
            [
                "Start with a small deficit and add height over time.",
                "Make sure the blocks or handles can't slide or tip.",
                "Don't sink deeper than your shoulders can control.",
            ],
            Video = new("ZCYPSaEjsWc", 22.379999, 66.599998),
        },
        new("band_push_up", "Banded Push-Up", Chest, Band, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Intermediate,
            Summary = "A push-up with a resistance band across your upper back, adding resistance that grows as you push to the top.",
            Steps =
            [
                "Loop a band across your upper back and pin each end under your hands.",
                "Set up in a high plank with your hands just wider than your shoulders.",
                "Lower your chest toward the floor.",
                "Push hard against the band back to straight arms.",
            ],
            Tips =
            [
                "Keep the band flat across your shoulder blades so it doesn't roll up your neck.",
                "Choke up on the band to make it harder.",
                "Don't let the extra resistance break your straight body line.",
            ],
            Video = new("7Xu3D-TKKAw"),
        },
        new("weighted_push_up", "Weighted Push-Up", Chest, Other, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Intermediate,
            Summary = "A push-up with a weight plate on your upper back or a weighted vest, for when bodyweight reps get too easy.",
            Steps =
            [
                "Put on a weighted vest, or get into a high plank and have a partner place a plate across your upper back.",
                "Set your hands just wider than your shoulders and brace your whole body.",
                "Lower your chest toward the floor.",
                "Push back up to straight arms.",
            ],
            Tips =
            [
                "Keep the plate high on your shoulder blades, not on your lower back.",
                "Add weight in small jumps.",
                "Don't let the load make your hips sag.",
            ],
            Video = new("8sa2qDm8a6k", 47.080002, 83.839996),
        },
        new("archer_push_up", "Archer Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Advanced,
            Summary = "A very wide push-up where you shift your weight over one arm while the other stays nearly straight, a step toward the one-arm push-up.",
            Steps =
            [
                "Start in a high plank with your hands about twice shoulder-width apart, fingers turned out.",
                "Shift your weight toward one hand and bend that elbow, keeping the other arm straight.",
                "Lower your chest toward the bent arm's hand.",
                "Push back to the middle and repeat to the other side.",
            ],
            Tips =
            [
                "Let the straight arm slide or roll onto the side of the hand as needed.",
                "Keep your hips square to the floor.",
                "Don't rush; control the shift to each side.",
            ],
            Video = new("MxVbNel13Ek", 133),
        },
        new("ring_push_up", "Ring Push-Up", Chest, Other, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Intermediate,
            Summary = "A push-up on gymnastic rings or a suspension trainer; the unstable handles make the chest and shoulders work harder to stay steady.",
            Steps =
            [
                "Set the rings a few inches off the floor and take one in each hand.",
                "Walk your feet back into a plank with your arms straight and the rings close to your sides.",
                "Lower your chest between the rings, keeping them tight to your body.",
                "Push back up and turn your palms slightly forward at the top.",
            ],
            Tips =
            [
                "Raise the rings to make it easier.",
                "Keep the rings from drifting out wide.",
                "Don't let your shoulders shrug toward your ears.",
            ],
            Video = new("95NOIGw4K5E"),
        },
        new("one_arm_push_up", "One-Arm Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Advanced,
            Summary = "A push-up on a single arm with the feet set wide, demanding serious pressing strength and full-body tension.",
            Steps =
            [
                "Start in a high plank with your feet wider than your hips and one hand under the middle of your chest.",
                "Put your free hand behind your back or on your thigh.",
                "Brace hard and lower your chest toward the floor, elbow close to your body.",
                "Push back up to a straight arm, then switch sides.",
            ],
            Tips =
            [
                "A wider stance makes it easier to balance.",
                "Allow a slight twist, but keep your hips from dropping.",
                "Build up with archer and incline one-arm push-ups first.",
            ],
            Video = new("IlWN4SNqaEM"),
        },
        new("hindu_push_up", "Hindu Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Level = Intermediate,
            Summary = "A flowing push-up (the wrestler's dand) that swoops from a pike down through the hands and up into a back arch, working the chest, shoulders and triceps while mobilising the spine.",
            Steps =
            [
                "Start in a pike with your hands shoulder-width apart, feet a bit wider and hips high.",
                "Bend your elbows and swoop your chest down close to the floor between your hands.",
                "Keep sliding forward and press up into straight arms with your hips low and chest lifted.",
                "Push your hips back up and return to the pike, then repeat.",
            ],
            Tips =
            [
                "Move smoothly and breathe out as you rise into the arch.",
                "Keep your elbows close to your body on the swoop.",
                "Don't crank your lower back; only arch as far as is comfortable.",
            ],
            Video = new("CdH5dT12axE"),
        },
        new("spiderman_push_up", "Spiderman Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders, Abs],
            Level = Intermediate,
            Summary = "A push-up where you bring one knee out toward the elbow on the same side as you lower, adding core and hip work.",
            Steps =
            [
                "Start in a high plank with your hands just wider than your shoulders.",
                "As you lower your chest, lift one foot and bring that knee out to the side toward your elbow.",
                "Push back up and return the foot to the floor.",
                "Repeat on the other side, alternating each rep.",
            ],
            Tips =
            [
                "Keep your hips level and your body in one line as the knee travels.",
                "Lower all the way; don't shorten the push-up to make room for the knee.",
                "Don't let your hips sag or twist.",
            ],
            Video = new("O4ykWemt47k"),
        },
        new("pseudo_planche_push_up", "Pseudo Planche Push-Up", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Chest, Triceps, Abs],
            Level = Advanced,
            Summary = "A push-up with your hands turned out by your hips and your body leaned forward past them, loading the front delts heavily as a planche progression.",
            Steps =
            [
                "Get into a push-up position with your hands by your lower ribs or hips, fingers turned out or back.",
                "Lean forward until your shoulders are well in front of your hands, arms straight and shoulder blades pushed apart.",
                "Keeping the lean, bend your elbows and lower your chest toward the floor, elbows tight to your sides.",
                "Press back to straight arms without losing the forward lean.",
            ],
            Tips =
            [
                "The further you lean, the harder it gets; keep the lean the same through the rep.",
                "Keep your body hollow and your glutes squeezed.",
                "Build wrist strength first and warm the wrists up well.",
                "Don't shift your shoulders back over your hands as you press.",
            ],
            Video = new("TZ63httkob4", 22.16, 83.119),
        },

        // Dips
        new("dips", "Chest Dip", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Intermediate,
            Summary = "A dip on parallel bars with the torso leaning forward, which loads the lower chest along with the triceps and front delts.",
            Steps =
            [
                "Support yourself on parallel bars with straight arms.",
                "Lean your torso forward about 30° and let your legs hang slightly behind you.",
                "Bend your elbows to lower yourself until your shoulders are just below your elbows.",
                "Press back up to straight arms, keeping the forward lean.",
            ],
            Tips =
            [
                "Let your elbows flare a little to keep the chest involved.",
                "Keep your shoulders down away from your ears.",
                "Don't drop too deep if your shoulders feel pinched at the bottom.",
            ],
            Video = new("yN6Q1UI_xkE", 161.28, 286.76001),
        },
        new("assisted_chest_dip", "Assisted Chest Dip", Chest, Machine, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A chest dip on an assisted dip machine, where a counterweight takes off part of your bodyweight so you can learn the movement.",
            Steps =
            [
                "Set the assistance weight; more weight means more help.",
                "Kneel or stand on the platform and grip the dip handles with straight arms.",
                "Lean forward and bend your elbows to lower until your shoulders are just below your elbows.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "Reduce the assistance over time as you get stronger.",
                "Keep the forward lean to keep the work on your chest.",
                "Don't let the platform bounce you out of the bottom.",
            ],
            Video = new("g3o0dC_qCns"),
        },
        new("weighted_chest_dip", "Weighted Chest Dip", Chest, Other, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Advanced,
            Summary = "A chest dip with extra weight hung from a dip belt or held between the feet, for lifters who can already do plenty of bodyweight dips.",
            Steps =
            [
                "Attach a plate or kettlebell to a dip belt, or hold a dumbbell between your feet.",
                "Support yourself on parallel bars with straight arms and lean your torso forward.",
                "Lower yourself until your shoulders are just below your elbows.",
                "Press back up to straight arms.",
            ],
            Tips =
            [
                "Be able to do 10–15 clean bodyweight dips before adding load.",
                "Keep the weight from swinging by moving slowly.",
                "Don't drop into the bottom; control every rep.",
            ],
            Video = new("ZDOrGNvRdM0"),
        },
        new("ring_dip", "Ring Dip", Chest, Other, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Level = Advanced,
            Summary = "A dip on gymnastic rings, much harder than bar dips because you have to stop the rings swinging out while you press.",
            Steps =
            [
                "Jump or press up to support on the rings with straight arms, rings tight to your sides.",
                "Lean slightly forward and lower yourself, keeping the rings close to your body.",
                "Descend until your shoulders are just below your elbows.",
                "Press back up and turn your palms slightly forward at the top.",
            ],
            Tips =
            [
                "Master a steady ring support hold first.",
                "Keep the rings tucked in; letting them drift out strains the shoulders.",
                "Don't go deeper than you can control.",
            ],
            Video = new("-pChKm_jMYY"),
        },

        // Pullover
        new("db_pullover", "Dumbbell Pullover", Chest, Dumbbell, Isolation)
        {
            Secondary = [Back, Triceps],
            Level = Intermediate,
            Summary = "A lying movement that swings a dumbbell from over your chest to behind your head and back, working the chest and lats together through a big stretch.",
            Steps =
            [
                "Lie on a bench, or across it with your upper back supported, holding one dumbbell over your chest with both hands cupping the top end.",
                "With a slight bend in your elbows, lower the dumbbell back in an arc behind your head.",
                "Stop when your arms are about in line with your torso and you feel a stretch through your chest and lats.",
                "Pull the dumbbell back over your chest along the same arc.",
            ],
            Tips =
            [
                "Keep the elbow angle fixed; the movement is at the shoulders.",
                "Keep your ribs down so your lower back doesn't arch.",
                "Don't go further back than your shoulders comfortably allow.",
            ],
            Video = new("tcHaHIQStsk", 427.5, 583.67999),
        },

        // Serratus
        new("push_up_plus", "Push-Up Plus", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Summary = "A push-up with an extra push at the top to spread the shoulder blades apart, training the serratus anterior that holds the shoulder blade to the ribs.",
            Steps =
            [
                "Start in a high plank, or on your knees, with your hands under your shoulders.",
                "Do a normal push-up.",
                "At the top, keep your arms straight and push the floor further away so your upper back rounds slightly.",
                "Hold for a moment, then relax the shoulder blades and start the next rep.",
            ],
            Tips =
            [
                "The plus is a small movement; feel your shoulder blades wrap around your ribs.",
                "Keep your hips and lower back still during the plus.",
                "Don't shrug your shoulders up toward your ears.",
            ],
            Video = new("RFbjeyq_ZPc"),
        },
        new("scapular_push_up", "Scapular Push-Up", Chest, Bodyweight, Isolation)
        {
            Summary = "A small plank movement where only the shoulder blades move, squeezing together and then spreading apart, to train the serratus anterior and shoulder blade control.",
            Steps =
            [
                "Start in a high plank, or on your knees, with your hands under your shoulders and arms straight.",
                "Keeping your arms locked, let your chest sink so your shoulder blades squeeze together.",
                "Push the floor away so your shoulder blades spread apart and your upper back rounds slightly.",
                "Repeat slowly through the full range.",
            ],
            Tips =
            [
                "Your elbows never bend; the movement is all in the shoulder blades.",
                "Keep your body straight and your hips still.",
                "Don't let your head drop as your chest sinks.",
            ],
            Video = new("NKekqeudgWs"),
        },
        new("serratus_punch", "Dumbbell Serratus Punch", Chest, Dumbbell, Isolation)
        {
            Summary = "A lying punch toward the ceiling with a dumbbell that trains the serratus anterior, a common physio exercise for shoulder blade control.",
            Steps =
            [
                "Lie on your back on a bench or floor, holding a light dumbbell straight up over your shoulder.",
                "Keeping your elbow straight, reach the dumbbell toward the ceiling so your shoulder blade lifts off the surface.",
                "Hold for a moment at the top.",
                "Lower your shoulder blade back down slowly without bending your elbow.",
            ],
            Tips =
            [
                "The range is only a few centimetres; that's correct.",
                "Keep your arm straight and vertical throughout.",
                "Don't shrug toward your ear or twist your body to reach higher.",
            ],
            Video = new("y1FPkIS85kY"),
        },
        new("wall_slide_lift_off", "Wall Slide with Lift-Off", Chest, Bodyweight, Isolation)
        {
            Secondary = [Traps, Shoulders],
            Summary = "A wall slide with the forearms on the wall followed by lifting the hands off at the top, training the serratus anterior and lower traps that rotate the shoulder blade upward.",
            Steps =
            [
                "Face a wall with your forearms on it at shoulder height, elbows shoulder-width apart; a foam roller under your forearms is optional.",
                "Push gently into the wall to spread your shoulder blades.",
                "Slide your forearms up the wall into a Y until your arms are nearly straight.",
                "Lift your hands a few centimetres off the wall, hold briefly, then put them back and slide down slowly.",
            ],
            Tips =
            [
                "Keep pressing into the wall as you slide; that's what works the serratus.",
                "Keep your ribs down and your lower back neutral.",
                "Don't shrug your shoulders up as your arms rise.",
            ],
            Video = new("3blA9Ba2TFI"),
        },
    ];
}
