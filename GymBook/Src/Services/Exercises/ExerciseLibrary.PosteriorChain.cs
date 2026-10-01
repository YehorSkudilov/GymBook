using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] PosteriorChainExercises() =>
    [
        // Deadlifts
        new("deadlift", "Deadlift", LowerBack, Barbell, Compound)
        {
            Secondary = [Glutes, Hamstrings, Back],
            Level = Intermediate,
            Summary = "The conventional barbell deadlift: lift the bar from the floor to standing with a flat back, training the whole posterior chain and grip.",
            Steps =
            [
                "Stand with your feet hip-width apart and the bar over your mid-foot.",
                "Hinge down and grip the bar just outside your shins, then bend your knees until your shins touch the bar.",
                "Flatten your back, pull the slack out of the bar and brace your trunk hard.",
                "Push the floor away and stand up, keeping the bar against your legs, until your hips and knees are locked.",
                "Lower it by pushing your hips back first, then bending your knees once the bar passes them.",
            ],
            Tips =
            [
                "Keep the bar in contact with your legs the whole way; it should travel in a straight vertical line.",
                "Lats tight, as if bending the bar around your shins.",
                "Don't let your lower back round, and don't lean back or shrug at lockout.",
                "Don't jerk the bar off the floor; build tension first, then push.",
            ],
            Video = new("ZaTM37cfiDs"),
        },
        new("sumo_deadlift", "Sumo Deadlift", Glutes, Barbell, Compound)
        {
            Secondary = [Quads, Hamstrings, LowerBack],
            Level = Intermediate,
            Summary = "A wide-stance deadlift with the hands inside the knees, giving a more upright torso and more work for the glutes, adductors and quads.",
            Steps =
            [
                "Take a wide stance with your toes turned out about 30–45° and the bar over your mid-foot.",
                "Grip the bar shoulder-width, arms hanging straight down inside your knees.",
                "Drop your hips, push your knees out over your toes and lift your chest until your back is flat.",
                "Brace and push the floor apart with your feet to stand up, bringing your hips through to lockout.",
                "Lower the bar under control along the same path.",
            ],
            Tips =
            [
                "Keep your knees tracking over your toes the whole lift.",
                "Get your hips as close to the bar as your mobility allows.",
                "Don't let your knees cave in or your hips shoot up first.",
            ],
            Video = new("HBmLwb9IcaI"),
        },
        new("trap_bar_deadlift", "Trap Bar Deadlift", Glutes, Barbell, Compound)
        {
            Secondary = [Quads, Hamstrings, LowerBack],
            Summary = "A deadlift with a hexagonal trap bar, standing inside the frame. The load sits in line with your body, so it's easier on the lower back and more quad-heavy.",
            Steps =
            [
                "Stand in the middle of the trap bar with your feet hip-width apart.",
                "Hinge and bend your knees to grip the handles at their centre.",
                "Flatten your back, chest up, and brace.",
                "Drive through your whole foot to stand up straight.",
                "Sit back down with the same hinge-and-bend to return the bar to the floor.",
            ],
            Tips =
            [
                "Use the high handles if you can't reach the low ones with a flat back.",
                "Stand centred so the bar doesn't tip forward or back.",
                "Don't round your back or let your knees cave in.",
            ],
            Video = new("kpyCkyVIxjI"),
        },
        new("deficit_deadlift", "Deficit Deadlift", LowerBack, Barbell, Compound)
        {
            Secondary = [Glutes, Hamstrings, Quads],
            Level = Advanced,
            Summary = "A conventional deadlift standing on a 2–10 cm plate or platform, lengthening the range to build strength off the floor.",
            Steps =
            [
                "Stand on a stable plate or low platform with the bar over your mid-foot.",
                "Hinge and bend your knees further than usual to grip the bar.",
                "Flatten your back, pull the slack out and brace.",
                "Push the floor away and stand up to full lockout.",
                "Lower under control back to the plates.",
            ],
            Tips =
            [
                "Start with a small deficit and lighter weight than your normal deadlift.",
                "Only go as deep as you can while keeping a flat back.",
                "Don't let your hips rise faster than your shoulders off the floor.",
            ],
            Video = new("wvyi2o_bAyw"),
        },
        new("rack_pull", "Rack Pull", LowerBack, Barbell, Compound)
        {
            Secondary = [Glutes, Traps, Back],
            Level = Intermediate,
            Summary = "A partial deadlift from pins or blocks at about knee height, overloading the lockout and the upper back.",
            Steps =
            [
                "Set the bar on the rack pins or on blocks, usually just below or above the knee.",
                "Stand close with the bar against your legs, hinge and grip it just outside your thighs.",
                "Flatten your back, set your lats and brace.",
                "Drive your hips forward to stand up straight.",
                "Lower the bar back to the pins under control.",
            ],
            Tips =
            [
                "Squeeze your glutes to finish; don't lean back at the top.",
                "Lower the bar rather than dropping it on the pins.",
                "Don't let the heavier load round your upper back.",
            ],
            Video = new("Y99s0dNWpUw"),
        },
        new("stiff_leg_deadlift", "Stiff-Leg Deadlift", Hamstrings, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Intermediate,
            Summary = "A hinge with nearly straight legs where each rep starts from the floor, putting a big stretch on the hamstrings.",
            Steps =
            [
                "Stand with your feet hip-width apart and the bar over your mid-foot.",
                "With only a slight bend in your knees, hinge at the hips and grip the bar.",
                "Flatten your back and brace.",
                "Drive your hips forward to stand up, keeping your knees nearly straight.",
                "Hinge back down and touch the bar to the floor before the next rep.",
            ],
            Tips =
            [
                "Keep the bar close to your legs the whole way.",
                "If you can't reach the floor with a flat back, lift from blocks.",
                "Don't round your lower back to get the bar down.",
            ],
            Video = new("m8m4eaag87k"),
        },
        new("snatch_grip_deadlift", "Snatch-Grip Deadlift", LowerBack, Barbell, Compound)
        {
            Secondary = [Back, Traps, Glutes],
            Level = Advanced,
            Summary = "A deadlift with a very wide grip that makes you start lower, building the upper back, grip and strength off the floor.",
            Steps =
            [
                "Stand with your feet hip-width apart and the bar over your mid-foot.",
                "Grip the bar very wide, about where the bar sits in your hip crease when you stand.",
                "Sit your hips lower than in a normal deadlift, flatten your back and brace.",
                "Push the floor away and stand up, keeping the bar close.",
                "Lower under control.",
            ],
            Tips =
            [
                "Use straps if your grip gives out before your back.",
                "Keep your chest up and your upper back tight.",
                "Don't let your shoulders roll forward under the wider grip.",
            ],
            Video = new("r5eT5DUfzww"),
        },
        new("paused_deadlift", "Paused Deadlift", LowerBack, Barbell, Compound)
        {
            Secondary = [Glutes, Hamstrings, Quads],
            Level = Advanced,
            Summary = "A deadlift with a 1–3 second pause just after the bar leaves the floor or at the knee, teaching position and control.",
            Steps =
            [
                "Set up as for a conventional deadlift and brace.",
                "Lift the bar a few centimetres off the floor, or to just below the knee.",
                "Hold that position for 1–3 seconds without moving.",
                "Finish the lift to lockout, then lower under control.",
            ],
            Tips =
            [
                "Use 60–80% of your normal deadlift weight.",
                "During the pause your back should stay flat and the bar against your legs.",
                "Don't let your hips drift up or the bar drift forward while you hold.",
            ],
            Video = new("UUa2WhA4eA8"),
        },
        new("kb_deadlift", "Kettlebell Deadlift", Glutes, Kettlebell, Compound)
        {
            Secondary = [Hamstrings, Quads, LowerBack],
            Summary = "A deadlift with a kettlebell between your feet, the easiest way to learn the hip hinge.",
            Steps =
            [
                "Stand with your feet a little wider than hip-width and the kettlebell between your mid-feet.",
                "Push your hips back and bend your knees to grip the handle with both hands.",
                "Flatten your back and pull your shoulders back.",
                "Drive through your feet and stand up tall, squeezing your glutes.",
                "Push your hips back to lower the bell to the floor.",
            ],
            Tips =
            [
                "Think hips back, not chest down.",
                "Keep your arms straight; they are just hooks.",
                "Don't round your back to reach the bell.",
            ],
            Video = new("eFkeIXvhSFs"),
        },

        // Romanian deadlifts
        new("romanian_deadlift", "Romanian Deadlift", Hamstrings, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Intermediate,
            Summary = "A top-down barbell hinge with soft knees, lowering the bar along your legs until the hamstrings are fully stretched.",
            Steps =
            [
                "Stand tall holding the bar at hip height with an overhand grip, feet hip-width apart.",
                "Unlock your knees slightly and push your hips back, sliding the bar down your thighs.",
                "Lower until you feel a strong hamstring stretch, usually around mid-shin.",
                "Drive your hips forward to stand back up, squeezing your glutes at the top.",
            ],
            Tips =
            [
                "Keep your knee bend the same throughout; the movement is all in the hips.",
                "Keep the bar touching your legs.",
                "Don't round your back to go lower; stop where your hamstrings stop you.",
            ],
            Video = new("7j-2w4-P14I"),
        },
        new("db_romanian_deadlift", "Dumbbell Romanian Deadlift", Hamstrings, Dumbbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Summary = "The Romanian deadlift with a dumbbell in each hand, hinging until the hamstrings are fully stretched.",
            Steps =
            [
                "Stand tall with a dumbbell in each hand in front of your thighs.",
                "Soften your knees and push your hips back, letting the dumbbells slide down your legs.",
                "Lower until you feel a strong stretch in your hamstrings.",
                "Drive your hips forward to stand up and squeeze your glutes.",
            ],
            Tips =
            [
                "Keep the dumbbells close to your legs, not hanging out in front.",
                "Keep your neck in line with your back; look at the floor a little ahead.",
                "Don't turn it into a squat by bending your knees more as you go down.",
            ],
            Video = new("wiekN4aIJ0g"),
        },
        new("single_leg_rdl", "Single-Leg RDL", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Summary = "A bodyweight Romanian deadlift on one leg that trains the hamstrings, glutes and balance.",
            Steps =
            [
                "Stand on one leg with a slight bend in the knee.",
                "Hinge forward at the hip while your free leg extends straight behind you.",
                "Lower until your torso and back leg are close to parallel with the floor, or you feel a hamstring stretch.",
                "Drive your hip forward to return to standing.",
            ],
            Tips =
            [
                "Keep your hips square to the floor; point the back toes down.",
                "Reach your arms forward or hold a wall if balance limits you.",
                "Don't let your hip open out to the side as you hinge.",
            ],
            Video = new("Zfr6wizR8rs"),
        },
        new("single_leg_db_rdl", "Single-Leg Dumbbell RDL", Hamstrings, Dumbbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Intermediate,
            Summary = "A single-leg Romanian deadlift holding a dumbbell, loading each hamstring and glute on its own.",
            Steps =
            [
                "Stand on one leg holding a dumbbell in the opposite hand.",
                "Soften the standing knee and hinge forward as your free leg reaches straight back.",
                "Lower the dumbbell toward the floor until you feel a strong hamstring stretch.",
                "Drive your standing hip forward to return to tall standing.",
            ],
            Tips =
            [
                "Keep your hips level and square; don't rotate open.",
                "Move slowly, especially on the way down.",
                "Don't round your back to reach the floor.",
            ],
            Video = new("FQfMko8XXPg"),
        },
        new("b_stance_rdl", "B-Stance Dumbbell RDL", Hamstrings, Dumbbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Level = Intermediate,
            Summary = "A Romanian deadlift with most of your weight on the front leg and the back foot as a kickstand, a stable step toward single-leg work.",
            Steps =
            [
                "Hold a dumbbell in each hand and step one foot back so its toes are level with the front heel.",
                "Put about 80% of your weight on the front leg, back heel raised.",
                "Hinge at the hips, lowering the dumbbells along the front leg until the hamstring stretches.",
                "Drive the front hip forward to stand back up.",
            ],
            Tips =
            [
                "The back leg only balances you; don't push with it.",
                "Keep your hips square and your back flat.",
                "Don't let the front knee bend more as you lower.",
            ],
            Video = new("edRCGi28KE4"),
        },
        new("landmine_rdl", "Landmine RDL", Hamstrings, Barbell, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Summary = "A Romanian deadlift holding the end of a landmine-anchored barbell, whose arc makes the hinge easy to learn.",
            Steps =
            [
                "Stand facing the free end of a landmine barbell, holding it with both hands at your hips.",
                "Soften your knees and push your hips back, lowering the bar along its arc.",
                "Lower until your hamstrings are fully stretched with a flat back.",
                "Drive your hips forward to stand up tall.",
            ],
            Tips =
            [
                "Stand close enough that the bar stays near your body.",
                "It works on one leg too, holding the bar in the opposite hand.",
                "Don't let your back round at the bottom.",
            ],
            Video = new("LHBk19GsUuw"),
        },

        // Leg curls
        new("lying_leg_curl", "Lying Leg Curl", Hamstrings, Machine, Isolation)
        {
            Secondary = [Calves],
            Summary = "A machine curl lying face down, bending your knees to bring the pad toward your glutes.",
            Steps =
            [
                "Lie face down with your knees just off the end of the bench and the pad just above your heels.",
                "Hold the handles and press your hips into the bench.",
                "Curl the pad up toward your glutes as far as you can.",
                "Lower it slowly until your legs are almost straight.",
            ],
            Tips =
            [
                "Line your knees up with the machine's pivot.",
                "Take 2–3 seconds to lower.",
                "Don't lift your hips off the pad to finish the rep.",
            ],
            Video = new("d6sg829PgNs"),
        },
        new("seated_leg_curl", "Seated Leg Curl", Hamstrings, Machine, Isolation)
        {
            Secondary = [Calves],
            Summary = "A machine curl sitting upright, which trains the hamstrings in a lengthened position.",
            Steps =
            [
                "Sit with your knees lined up with the pivot and the lower pad behind your ankles.",
                "Lock the thigh pad down firmly above your knees.",
                "Curl your legs down and back as far as you can.",
                "Let the pad come back up slowly until your legs are almost straight.",
            ],
            Tips =
            [
                "Lean your torso forward slightly for a deeper hamstring stretch.",
                "Keep the thigh pad tight so your legs can't lift.",
                "Don't let the weight stack slam between reps.",
            ],
            Video = new("t9sTSr-JYSs"),
        },
        new("standing_leg_curl", "Standing Leg Curl", Hamstrings, Machine, Isolation)
        {
            Secondary = [Calves],
            Summary = "A one-leg-at-a-time machine curl done standing, letting you focus on each hamstring.",
            Steps =
            [
                "Stand at the machine with your thigh against the pad and the roller behind your ankle.",
                "Hold the handles and stand tall on your other leg.",
                "Curl your heel up toward your glutes.",
                "Lower slowly to a straight leg, then switch sides after your reps.",
            ],
            Tips =
            [
                "Keep your hips still against the pad.",
                "Squeeze for a moment at the top.",
                "Don't swing your hips or arch your back to lift more.",
            ],
            Video = new("Sx2j5pyrFdA"),
        },
        new("cable_leg_curl", "Cable Standing Leg Curl", Hamstrings, Cable, Isolation)
        {
            Summary = "A standing single-leg curl with an ankle strap on a low cable, for when there's no leg curl machine.",
            Steps =
            [
                "Attach an ankle strap to a low pulley and put it on one ankle.",
                "Face the machine and hold it for support, stepping back until there's tension.",
                "Keeping your thigh still, curl your heel up toward your glutes.",
                "Lower slowly to a straight leg, then switch sides.",
            ],
            Tips =
            [
                "Keep your knee pointing at the floor throughout.",
                "Hinge forward slightly at the hips for a better line of pull.",
                "Don't swing the leg forward to cheat the rep.",
            ],
            Video = new("bkwW1flKchc"),
        },
        new("db_leg_curl", "Dumbbell Lying Leg Curl", Hamstrings, Dumbbell, Isolation)
        {
            Summary = "A leg curl lying face down on a bench with a dumbbell held between your feet.",
            Steps =
            [
                "Have a partner place a dumbbell vertically between your feet, or set it there before lying down.",
                "Lie face down on a flat bench with your knees just off the end.",
                "Squeeze the dumbbell between your feet and curl your heels toward your glutes.",
                "Lower slowly until your legs are almost straight.",
            ],
            Tips =
            [
                "Use a light dumbbell until you trust your grip with your feet.",
                "Stop short of vertical so the weight stays loaded.",
                "Don't let the dumbbell tip or slip at the bottom.",
            ],
            Video = new("aPUtiouhcQQ"),
        },
        new("swiss_ball_leg_curl", "Swiss Ball Leg Curl", Hamstrings, Other, Isolation)
        {
            Secondary = [Glutes],
            Summary = "A hamstring curl lying on your back with your heels on a stability ball, rolling it in while holding your hips up.",
            Steps =
            [
                "Lie on your back with your heels on a stability ball and your arms flat on the floor.",
                "Lift your hips until your body is straight from shoulders to heels.",
                "Curl your heels toward your glutes, rolling the ball in while your hips stay high.",
                "Straighten your legs slowly to roll the ball back out.",
            ],
            Tips =
            [
                "Keep your glutes squeezed so your hips don't drop.",
                "Do it one leg at a time once two is easy.",
                "Don't let your hips sag as the ball rolls out.",
            ],
            Video = new("XkESHgkTdFw"),
        },
        new("slider_leg_curl", "Slider Leg Curl", Hamstrings, Other, Isolation)
        {
            Secondary = [Glutes],
            Level = Intermediate,
            Summary = "A bridge-position hamstring curl sliding your heels on gliders or a towel on a smooth floor.",
            Steps =
            [
                "Lie on your back with your heels on sliders (or a towel on a smooth floor), knees bent.",
                "Lift your hips into a bridge.",
                "Slide your heels away slowly until your legs are nearly straight, keeping your hips up.",
                "Pull your heels back in toward your glutes.",
            ],
            Tips =
            [
                "The slow slide out is the hard part; control it.",
                "Keep your hips up the whole time.",
                "Don't push through your lower back; tuck your pelvis slightly.",
            ],
            Video = new("kkkTfzi2gj4"),
        },
        new("nordic_curl", "Nordic Curl", Hamstrings, Bodyweight, Isolation)
        {
            Level = Advanced,
            Summary = "An eccentric hamstring exercise: kneel with your ankles anchored and lower your body toward the floor as slowly as you can.",
            Steps =
            [
                "Kneel on a pad with your ankles held down by a partner, a Nordic bench or a secure bar.",
                "Keep your body straight from knees to head, arms ready in front of you.",
                "Lower your body forward as slowly as you can, resisting with your hamstrings.",
                "Catch yourself with your hands, then push off lightly and pull back up.",
            ],
            Tips =
            [
                "Keep your hips extended; your body moves as one straight line.",
                "Build up slowly: it causes heavy soreness at first.",
                "Don't bend at the hips to make the lowering easier.",
            ],
            Video = new("DAwJCKwMUIg"),
        },
        new("glute_ham_raise", "Glute-Ham Raise", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves],
            Level = Advanced,
            Summary = "Done on a glute-ham developer (GHD): extend your hips and then curl your body up with your hamstrings.",
            Steps =
            [
                "Set the GHD so your knees sit just behind the pad and your feet are locked against the plate.",
                "Start with your torso hanging down toward the floor.",
                "Raise your torso until your body is straight, then keep going by bending your knees.",
                "Curl up until your body is upright, then lower slowly the same way.",
            ],
            Tips =
            [
                "Push your toes into the footplate to bring the hamstrings in.",
                "Use a band or your hands on the pad for help at first.",
                "Don't arch your lower back as you come up.",
            ],
            Video = new("blX1If7ScxY"),
        },
        new("razor_curl", "Razor Curl", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Glutes],
            Level = Advanced,
            Summary = "A Nordic curl variant where you hinge at the hips as you lower and then curl your body back up, making it easier to come back up.",
            Steps =
            [
                "Kneel on a pad with your ankles anchored, as for a Nordic curl.",
                "Lower forward with your body straight until your thighs are close to parallel with the floor.",
                "Bend at the hips, bringing your torso down and back toward your knees.",
                "Pull with your hamstrings to curl back up to kneeling.",
            ],
            Tips =
            [
                "Keep the move smooth; the hip hinge is what makes the way up possible.",
                "Use a band or partner for help if needed.",
                "Don't collapse forward; stay in control the whole range.",
            ],
            Video = new("wTl9IeOeA6w"),
        },

        // Hip thrusts
        new("hip_thrust", "Barbell Hip Thrust", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings],
            Summary = "With your upper back on a bench, drive a barbell up with your hips. One of the best loaded glute exercises.",
            Steps =
            [
                "Sit on the floor with your upper back against a bench and a padded bar over your hips.",
                "Plant your feet flat, about hip-width, so your shins are vertical at the top.",
                "Tuck your chin and drive through your heels to lift your hips until your body is straight from shoulders to knees.",
                "Squeeze your glutes for a moment, then lower your hips toward the floor.",
            ],
            Tips =
            [
                "Keep your ribs down and pelvis tucked at the top.",
                "Use a pad or towel under the bar.",
                "Don't overextend your lower back to get the hips higher.",
            ],
            Video = new("-GEVlyzVbcg"),
        },
        new("db_hip_thrust", "Dumbbell Hip Thrust", Glutes, Dumbbell, Compound)
        {
            Secondary = [Hamstrings],
            Summary = "A hip thrust with a dumbbell resting across your hips, ideal for lighter loads or home training.",
            Steps =
            [
                "Sit with your upper back on a bench and hold a dumbbell across your hip crease.",
                "Plant your feet flat and hip-width apart.",
                "Drive through your heels to lift your hips until your body is straight from shoulders to knees.",
                "Squeeze your glutes, then lower under control.",
            ],
            Tips =
            [
                "Hold the dumbbell in place with both hands.",
                "Keep your chin tucked and your eyes forward.",
                "Don't push through your toes or arch your back.",
            ],
            Video = new("29OfN4ztW_g"),
        },
        new("smith_hip_thrust", "Smith Machine Hip Thrust", Glutes, Machine, Compound)
        {
            Secondary = [Hamstrings],
            Summary = "A hip thrust under a Smith machine bar, which keeps the bar path fixed and is easy to set up.",
            Steps =
            [
                "Put a bench perpendicular to the Smith machine and sit with your upper back on it.",
                "Set the padded bar over your hip crease and plant your feet flat.",
                "Unhook the bar and drive your hips up until your body is straight from shoulders to knees.",
                "Squeeze your glutes, lower under control and re-hook the bar when done.",
            ],
            Tips =
            [
                "Set the safeties just below your bottom position.",
                "Keep your shins vertical at the top.",
                "Don't overarch your lower back at lockout.",
            ],
            Video = new("eUNTpG5W6Ts"),
        },
        new("machine_hip_thrust", "Machine Hip Thrust", Glutes, Machine, Compound)
        {
            Secondary = [Hamstrings],
            Summary = "A hip thrust on a dedicated hip thrust or glute drive machine, with a belt or pad over your hips.",
            Steps =
            [
                "Sit in the machine with your upper back on the pad and the belt or pad over your hips.",
                "Place your feet on the platform so your shins are vertical at the top.",
                "Drive your hips up until they are fully extended.",
                "Squeeze, then lower under control.",
            ],
            Tips =
            [
                "Keep your chin tucked and ribs down.",
                "Pause at the top on each rep.",
                "Don't bounce out of the bottom.",
            ],
            Video = new("X8dG8g2hrqM"),
        },
        new("single_leg_hip_thrust", "Single-Leg Hip Thrust", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings],
            Level = Intermediate,
            Summary = "A bodyweight hip thrust on one leg with your upper back on a bench, training each glute alone.",
            Steps =
            [
                "Sit with your upper back on a bench, one foot flat on the floor and the other leg raised.",
                "Drive through the planted heel to lift your hips until they are level and fully extended.",
                "Hold for a moment, squeezing the glute.",
                "Lower under control and repeat, then switch legs.",
            ],
            Tips =
            [
                "Keep your hips level; don't let one side drop.",
                "Add a dumbbell on the working hip once it's easy.",
                "Don't push off the floor with the raised leg.",
            ],
            Video = new("xizbNw9IrjE"),
        },
        new("b_stance_hip_thrust", "B-Stance Hip Thrust", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings],
            Level = Intermediate,
            Summary = "A barbell hip thrust with one foot forward as a kickstand, putting most of the load on one glute.",
            Steps =
            [
                "Set up as for a barbell hip thrust.",
                "Step one foot forward so only its heel touches the floor, the other foot planted under your knee.",
                "Drive mainly through the planted heel to lift your hips to full extension.",
                "Lower under control, then switch sides after your reps.",
            ],
            Tips =
            [
                "The front foot only balances you.",
                "Keep the bar level across your hips.",
                "Don't twist your pelvis toward the working side.",
            ],
            Video = new("b3SOY5ljRsM"),
        },

        // Glute bridges
        new("glute_bridge", "Glute Bridge", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "Lying on your back with knees bent, lift your hips by squeezing your glutes. The basic glute exercise.",
            Steps =
            [
                "Lie on your back with your knees bent, feet flat and hip-width apart.",
                "Press your lower back gently into the floor and squeeze your glutes.",
                "Push through your heels to lift your hips until your body is straight from shoulders to knees.",
                "Hold for a moment, then lower slowly.",
            ],
            Tips =
            [
                "Keep your ribs down; the movement comes from the hips.",
                "Bring your feet closer if you feel it mostly in your hamstrings.",
                "Don't arch your back at the top.",
            ],
            Video = new("X_IGw8U_e38"),
        },
        new("single_leg_glute_bridge", "Single-Leg Glute Bridge", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "A glute bridge on one leg, with the other leg extended or held up.",
            Steps =
            [
                "Lie on your back with one knee bent, foot flat, and the other leg straight or held with knee to chest.",
                "Push through the planted heel to lift your hips.",
                "Keep your hips level at the top and squeeze.",
                "Lower slowly, finish your reps, then switch legs.",
            ],
            Tips =
            [
                "Hugging the free knee to your chest stops you arching your back.",
                "Keep both hips at the same height.",
                "Don't let the pelvis rotate toward the free leg.",
            ],
            Video = new("_K_di6h2-Wg"),
        },
        new("barbell_glute_bridge", "Barbell Glute Bridge", Glutes, Barbell, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "A glute bridge from the floor with a barbell over your hips, a shorter-range alternative to the hip thrust.",
            Steps =
            [
                "Lie on your back and roll a padded barbell over your hip crease.",
                "Bend your knees with your feet flat and hip-width apart.",
                "Drive through your heels to lift your hips until fully extended.",
                "Squeeze your glutes, then lower until the plates touch the floor.",
            ],
            Tips =
            [
                "Use bumper plates so the bar rolls easily into position.",
                "Hold the bar with both hands so it doesn't roll.",
                "Don't arch your lower back to finish.",
            ],
            Video = new("ylpfCk3i-0Y"),
        },
        new("banded_glute_bridge", "Banded Glute Bridge", Glutes, Band, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "A glute bridge with a mini band just above your knees, adding glute medius work as you push your knees out.",
            Steps =
            [
                "Put a mini band just above your knees and lie on your back with your knees bent, feet flat.",
                "Push your knees out against the band.",
                "Drive through your heels to lift your hips, keeping the knees pressed out.",
                "Squeeze at the top, then lower slowly.",
            ],
            Tips =
            [
                "Keep the band tight for the whole set.",
                "Keep your knees in line with your feet.",
                "Don't let your knees cave in as you lift.",
            ],
            Video = new("p5N_fok9YCM"),
        },
        new("frog_pump", "Frog Pump", Glutes, Bodyweight, Isolation)
        {
            Summary = "A short glute bridge with the soles of your feet together and knees open, usually done for high reps.",
            Steps =
            [
                "Lie on your back, put the soles of your feet together and let your knees fall open.",
                "Pull your heels in toward your glutes.",
                "Squeeze your glutes to lift your hips off the floor.",
                "Lower and repeat quickly, keeping tension on the glutes.",
            ],
            Tips =
            [
                "Prop your upper back on your elbows if that helps you feel it.",
                "Add a dumbbell on your hips once bodyweight is easy.",
                "Don't arch your lower back to get your hips higher.",
            ],
            Video = new("NNOUE6uAV0E"),
        },

        // Kickbacks
        new("cable_kickback", "Cable Glute Kickback", Glutes, Cable, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "A standing kickback with an ankle strap on a low cable, extending the hip against resistance.",
            Steps =
            [
                "Attach an ankle strap to a low pulley and face the machine, holding it for support.",
                "Hinge forward slightly with your weight on the standing leg.",
                "Kick the strapped leg back and up by squeezing the glute.",
                "Return slowly until your foot is just behind the standing leg, then switch sides after your reps.",
            ],
            Tips =
            [
                "Keep your torso and hips still; only the leg moves.",
                "Stop when your hip is fully extended.",
                "Don't arch your lower back to kick higher.",
            ],
            Video = new("n-cgsNePyFo"),
        },
        new("machine_kickback", "Machine Glute Kickback", Glutes, Machine, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "A kickback on a glute kickback machine, pressing a pad or platform back with one leg.",
            Steps =
            [
                "Set up on the machine with your forearms or chest on the pad and one foot on the platform.",
                "Brace your trunk.",
                "Press the platform back until your hip is fully extended.",
                "Return slowly, then switch sides after your reps.",
            ],
            Tips =
            [
                "Drive through your heel to bring in the glute.",
                "Keep your hips square to the machine.",
                "Don't arch your lower back to finish the rep.",
            ],
            Video = new("3fBptAH0Rnw"),
        },
        new("band_kickback", "Band Glute Kickback", Glutes, Band, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "A kickback against a resistance band, done on all fours or standing, for home training.",
            Steps =
            [
                "Get on all fours with a band looped around your foot and held under your hands, or anchored low in front of you if standing.",
                "Brace your trunk and keep your back flat.",
                "Push your foot back and up until your hip is fully extended.",
                "Return slowly and repeat, then switch sides.",
            ],
            Tips =
            [
                "Squeeze the glute at the top of each rep.",
                "Keep your hips square to the floor.",
                "Don't let your back sag as the leg extends.",
            ],
            Video = new("2Az9INnAF20"),
        },
        new("donkey_kick", "Donkey Kick", Glutes, Bodyweight, Isolation)
        {
            Summary = "On all fours, drive one bent leg up toward the ceiling, sole first, to work the glute.",
            Steps =
            [
                "Get on all fours with your hands under your shoulders and knees under your hips.",
                "Keeping the knee bent at 90°, lift one foot toward the ceiling.",
                "Lift until your thigh is in line with your body, squeezing your glute.",
                "Lower back down without resting the knee, and switch sides after your reps.",
            ],
            Tips =
            [
                "Keep your back flat and your core braced.",
                "Add an ankle weight or band once it's easy.",
                "Don't swing the leg or arch your back to get higher.",
            ],
            Video = new("YoOlLusFMYU"),
        },
        new("quadruped_hip_extension", "Quadruped Hip Extension", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Hamstrings],
            Summary = "On all fours, extend one straight leg back to hip height, training the glute and hamstring.",
            Steps =
            [
                "Get on all fours with your hands under your shoulders and knees under your hips.",
                "Straighten one leg behind you with the toes lightly touching the floor.",
                "Lift the straight leg until it's in line with your body.",
                "Lower slowly and repeat, then switch sides.",
            ],
            Tips =
            [
                "Keep your pelvis level and your trunk still.",
                "Reach the heel long behind you rather than up.",
                "Don't lift past hip height or arch your back.",
            ],
            Video = new("7h0DxNkoNuM"),
        },

        // Hip abduction (glute medius)
        new("hip_abduction_machine", "Hip Abduction Machine", Glutes, Machine, Isolation)
        {
            Summary = "A seated machine where you push your legs apart against the pads, training the glute medius and outer hip.",
            Steps =
            [
                "Sit in the machine with the pads against the outside of your knees.",
                "Hold the handles and sit tall.",
                "Push your knees out as far as you can.",
                "Bring them back together slowly without letting the stack touch.",
            ],
            Tips =
            [
                "Leaning forward slightly puts more work on the upper glutes.",
                "Pause for a moment in the open position.",
                "Don't use momentum or rock your torso.",
            ],
            Video = new("tn-ABeb1QAM"),
        },
        new("cable_hip_abduction", "Cable Hip Abduction", Glutes, Cable, Isolation)
        {
            Summary = "A standing side leg raise with an ankle strap on a low cable, training the glute medius.",
            Steps =
            [
                "Attach an ankle strap to a low pulley and put it on the ankle farthest from the machine.",
                "Stand side-on, holding the machine for support.",
                "Lift the strapped leg out to the side, away from the machine.",
                "Lower slowly back across, then switch sides after your reps.",
            ],
            Tips =
            [
                "Keep your toes pointing forward, not up.",
                "Keep your torso upright; the leg moves, not your body.",
                "Don't lean away from the machine to lift higher.",
            ],
            Video = new("pvnR8CDb4BU"),
        },
        new("side_lying_hip_abduction", "Side-Lying Hip Abduction", Glutes, Bodyweight, Isolation)
        {
            Summary = "Lying on your side, lift your straight top leg toward the ceiling to train the glute medius.",
            Steps =
            [
                "Lie on your side with your legs straight and stacked, head resting on your arm.",
                "Bend the bottom knee slightly for balance if you like.",
                "Lift the top leg about 30–45°, leading with the heel.",
                "Lower slowly and repeat, then switch sides.",
            ],
            Tips =
            [
                "Keep the top leg slightly behind your body and the toes pointing forward.",
                "Keep your hips stacked; don't roll back.",
                "Don't swing the leg up high; height isn't the goal.",
            ],
            Video = new("g9FtnmsIYgI"),
        },
        new("clamshell", "Clamshell", Glutes, Bodyweight, Isolation)
        {
            Summary = "Lying on your side with knees bent, open your top knee like a clam shell to train the glute medius.",
            Steps =
            [
                "Lie on your side with your hips and knees bent and your feet together.",
                "Rest your head on your arm and stack your hips.",
                "Keeping your feet touching, lift your top knee as far as you can without rolling your pelvis.",
                "Lower slowly, finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Add a mini band above your knees once it's easy.",
                "Put your top hand on your hip to check it doesn't roll back.",
                "Don't rush; the slow return matters.",
            ],
            Video = new("gFyIjunfbbg"),
        },
        new("banded_lateral_walk", "Banded Lateral Walk", Glutes, Band, Isolation)
        {
            Summary = "Sidesteps in a half-squat with a mini band around your legs, a common glute medius warm-up.",
            Steps =
            [
                "Put a mini band just above your knees or around your ankles.",
                "Stand with your feet hip-width apart and sit into a quarter squat.",
                "Step sideways with one foot, then follow with the other without letting the band go slack.",
                "Take 10–15 steps one way, then go back the other way.",
            ],
            Tips =
            [
                "Keep your toes pointing forward and your chest up.",
                "Stay at the same height throughout.",
                "Don't let your knees cave in or your feet click together.",
            ],
            Video = new("6eoK_yxY8Ak"),
        },
        new("monster_walk", "Monster Walk", Glutes, Band, Isolation)
        {
            Summary = "Walking forward and backward in wide diagonal steps with a mini band around your legs, working the glute medius.",
            Steps =
            [
                "Put a mini band above your knees or around your ankles and sit into a quarter squat.",
                "Step forward and out at about 45° with one foot.",
                "Step forward and out with the other foot, keeping your feet wide.",
                "Walk 10–15 steps forward, then walk backward the same way.",
            ],
            Tips =
            [
                "Keep tension on the band at every step.",
                "Keep your knees pushed out over your toes.",
                "Don't waddle or rock your torso side to side.",
            ],
            Video = new("snbNxUIUQPc"),
        },
        new("fire_hydrant", "Fire Hydrant", Glutes, Bodyweight, Isolation)
        {
            Summary = "On all fours, lift one bent leg out to the side, training the glute medius and hip rotators.",
            Steps =
            [
                "Get on all fours with your hands under your shoulders and knees under your hips.",
                "Keeping the knee bent at 90°, lift one leg out to the side.",
                "Lift until your thigh is close to parallel with the floor, without shifting your weight.",
                "Lower slowly and repeat, then switch sides.",
            ],
            Tips =
            [
                "Keep your trunk still and your hips level.",
                "Add a mini band above your knees for more resistance.",
                "Don't lean away from the working leg to lift it higher.",
            ],
            Video = new("IRkRgk2Gc1E"),
        },
        new("side_plank_hip_abduction", "Side Plank Hip Abduction", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Abs],
            Level = Intermediate,
            Summary = "Holding a side plank, lift your top leg, training the glute medius of both legs and the obliques.",
            Steps =
            [
                "Get into a side plank on your forearm, elbow under your shoulder, body straight and legs stacked.",
                "Hold your hips high.",
                "Raise your top leg 30–45° without letting your hips drop.",
                "Lower it with control and repeat, then switch sides.",
            ],
            Tips =
            [
                "Do it from your knees to make it easier.",
                "Keep your body in one straight line from head to feet.",
                "Don't let your hips sag or roll back.",
            ],
            Video = new("8JZUOtcp0Js"),
        },
        new("banded_seated_abduction", "Banded Seated Hip Abduction", Glutes, Band, Isolation)
        {
            Summary = "Seated on a bench with a mini band above your knees, push your knees out. A band version of the abduction machine.",
            Steps =
            [
                "Sit on the edge of a bench with a mini band just above your knees and your feet hip-width apart.",
                "Sit tall or lean forward slightly.",
                "Push your knees out against the band as far as you can.",
                "Return slowly, keeping some tension in the band.",
            ],
            Tips =
            [
                "Keep your feet flat and still.",
                "Do a set upright and a set leaning forward to work different fibres.",
                "Don't let the band snap your knees back in.",
            ],
            Video = new("yFxE57RAqWo"),
        },

        // Lower back
        new("back_extension", "Back Extension", LowerBack, Bodyweight, Isolation)
        {
            Secondary = [Glutes, Hamstrings],
            Summary = "On a horizontal (90°) back extension bench or roman chair, lower your torso and raise it back in line with your legs.",
            Steps =
            [
                "Set the bench so the pad sits just below your hip bones and lock your ankles under the rollers.",
                "Cross your arms over your chest or hold a plate to it.",
                "Bend at the hips to lower your torso toward the floor.",
                "Raise your torso until it's in line with your legs.",
            ],
            Tips =
            [
                "Move slowly and stop when your body is straight.",
                "Hold a plate to your chest for extra resistance.",
                "Don't swing up or hyperextend past straight.",
            ],
            Video = new("hs1Tj_y1dIY"),
        },
        new("back_extension_45", "45° Back Extension", LowerBack, Bodyweight, Isolation)
        {
            Secondary = [Glutes, Hamstrings],
            Summary = "A back extension on the angled 45° bench, the hyperextension bench most gyms have.",
            Steps =
            [
                "Set the pad just below your hip bones and lock your ankles under the rollers.",
                "Cross your arms over your chest or hold a plate to it.",
                "Bend at the hips to lower your torso toward the floor.",
                "Rise until your body forms a straight line from head to heels.",
            ],
            Tips =
            [
                "Rounding your upper back and pushing your hips into the pad puts more work on the glutes.",
                "Keep a flat back and stop at straight to focus on the lower back.",
                "Don't jerk up or arch past a straight line.",
            ],
            Video = new("VH4Bqn1FUhM"),
        },
        new("back_extension_machine", "Back Extension Machine", LowerBack, Machine, Isolation)
        {
            Secondary = [Glutes],
            Summary = "A seated selectorised machine where you extend your back against a pad behind your shoulders.",
            Steps =
            [
                "Sit in the machine with the pad across your upper back and your feet on the footrest.",
                "Fasten the belt if there is one and cross your arms over your chest.",
                "Push back against the pad until your torso is upright or slightly past.",
                "Return slowly to the start.",
            ],
            Tips =
            [
                "Start light; this is not a max-effort machine.",
                "Move smoothly through a range that feels comfortable.",
                "Don't throw yourself back with momentum.",
            ],
            Video = new("P489_62b8JU"),
        },
        new("reverse_hyper", "Reverse Hyperextension", LowerBack, Machine, Isolation)
        {
            Secondary = [Glutes, Hamstrings],
            Summary = "Lying face down on a reverse hyper machine with your legs hanging, swing your legs up behind you, training the lower back and glutes.",
            Steps =
            [
                "Lie face down on the machine with your hips at the edge of the pad and hold the handles.",
                "Put your ankles in the strap, legs hanging straight down.",
                "Squeeze your glutes to lift your legs until they're in line with your body.",
                "Lower under control and let your legs swing slightly under the pad before the next rep.",
            ],
            Tips =
            [
                "No machine? Do it lying face down on a high bench or a GHD.",
                "Lift with your glutes and keep the movement smooth.",
                "Don't kick up into an aggressive overarch.",
            ],
            Video = new("5Z2iTnhb5qM"),
        },
        new("superman", "Superman", LowerBack, Bodyweight, Isolation)
        {
            Secondary = [Glutes, Shoulders],
            Summary = "Lying face down, lift your arms, chest and legs off the floor together, training the back extensors.",
            Steps =
            [
                "Lie face down with your arms stretched overhead and legs straight.",
                "Squeeze your glutes and lift your arms, chest and legs a few centimetres off the floor.",
                "Hold for 1–2 seconds at the top.",
                "Lower slowly back to the floor.",
            ],
            Tips =
            [
                "Keep your neck long and look at the floor.",
                "Lift a little; it's a small movement.",
                "Don't crank your head up or force a deep arch.",
            ],
            Video = new("ZH0FS5Gp_eg"),
        },
        new("good_morning", "Good Morning", LowerBack, Barbell, Compound)
        {
            Secondary = [Hamstrings, Glutes],
            Level = Intermediate,
            Summary = "With a barbell on your upper back, hinge forward at the hips with soft knees and stand back up.",
            Steps =
            [
                "Set the bar on your upper back as for a back squat, feet hip-width apart.",
                "Brace and unlock your knees slightly.",
                "Push your hips back and lower your torso until it's close to parallel, or until your hamstrings stop you.",
                "Drive your hips forward to stand back up.",
            ],
            Tips =
            [
                "Start light; this loads the lower back heavily.",
                "Keep your back flat and the bar over your mid-foot.",
                "Don't round your back to go lower.",
            ],
            Video = new("nWyx81AfTos"),
        },
        new("banded_good_morning", "Banded Good Morning", LowerBack, Band, Compound)
        {
            Secondary = [Hamstrings, Glutes],
            Summary = "A good morning with a resistance band under your feet and around your neck or upper back, for learning the hinge at home.",
            Steps =
            [
                "Stand on a long band with your feet hip-width apart and loop the other end behind your neck or upper back.",
                "Soften your knees and brace.",
                "Push your hips back and lower your torso until you feel a hamstring stretch.",
                "Drive your hips forward to stand tall against the band.",
            ],
            Tips =
            [
                "Hold the band at your shoulders so it doesn't press on your neck.",
                "Keep your back flat through the whole rep.",
                "Don't let the band snap you up; control it.",
            ],
            Video = new("fJA39ZOVaEQ"),
        },
        new("cable_pull_through", "Cable Pull-Through", Glutes, Cable, Compound)
        {
            Secondary = [Hamstrings, LowerBack],
            Summary = "Facing away from a low cable with a rope between your legs, hinge and drive your hips forward to pull it through.",
            Steps =
            [
                "Attach a rope to a low pulley, face away and hold it between your legs with both hands.",
                "Walk forward a couple of steps and stand with your feet a little wider than hip-width.",
                "Hinge at the hips, letting the rope pull your hands back between your legs.",
                "Drive your hips forward to stand up tall, squeezing your glutes.",
            ],
            Tips =
            [
                "Keep your arms straight; the hips do the work.",
                "Knees bend slightly but this is a hinge, not a squat.",
                "Don't lean back at the top.",
            ],
            Video = new("IU-ERkjTKXA"),
        },
        new("jefferson_curl", "Jefferson Curl", LowerBack, Dumbbell, Isolation)
        {
            Secondary = [Hamstrings],
            Level = Advanced,
            Summary = "A slow, light-weight curl down through the spine one segment at a time, for spinal flexion strength and hamstring flexibility. Only for people without back problems.",
            Steps =
            [
                "Stand on a box or step holding a light dumbbell or kettlebell, legs straight.",
                "Tuck your chin and slowly roll down one vertebra at a time, letting the weight hang.",
                "Go as low as is comfortable, letting the weight travel below the box.",
                "Roll back up slowly from the bottom of your spine, stacking your head last.",
            ],
            Tips =
            [
                "Start with no weight or a very light one and add it slowly over weeks.",
                "Take 5 seconds or more each way.",
                "Skip it if you have a disc problem or back pain, and stop if anything feels sharp.",
                "Don't bounce or push for range.",
            ],
            Video = new("1dGnvWcwnR8"),
        },
    ];
}
