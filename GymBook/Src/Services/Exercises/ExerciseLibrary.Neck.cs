using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] NeckExercises() =>
    [
        // Plate work on a bench
        new("plate_neck_flexion", "Plate Neck Flexion", Neck, Other, Isolation)
        {
            Summary = "Strengthens the front of the neck: lying face up on a bench with your head off the end, you curl your chin toward your chest against a light plate held on your forehead.",
            Steps =
            [
                "Lie on your back on a flat bench with your shoulders at the end and your head and neck off it.",
                "Fold a towel over your forehead, place a light plate on it and hold it in place with both hands.",
                "Let your head lower back slowly until your face points at the wall behind you, within a comfortable range.",
                "Tuck your chin and curl your head up until your chin nears your chest.",
                "Pause, then lower for 2–3 seconds.",
            ],
            Tips =
            [
                "Start with a very light plate (or none) and add weight slowly; the neck adapts slower than bigger muscles.",
                "Lead with a chin tuck so the deep neck flexors work, not just the jutting SCM.",
                "Don't drop into the bottom or bounce; stay in a pain-free range.",
                "Stop if you feel dizziness, tingling or pain running into your arms.",
            ],
            Video = new("o78kjeBJUBQ"),
        },
        new("plate_neck_extension", "Plate Neck Extension", Neck, Other, Isolation)
        {
            Secondary = [Traps],
            Summary = "Strengthens the back of the neck: lying face down on a bench with your head off the end, you raise your head against a light plate held on the back of your head.",
            Steps =
            [
                "Lie face down on a flat bench with your shoulders at the end and your head off it.",
                "Fold a towel over the back of your head, place a light plate on it and hold it with both hands.",
                "Let your chin lower slowly toward your chest.",
                "Raise your head until it's in line with your spine, or just above.",
                "Pause, then lower for 2–3 seconds.",
            ],
            Tips =
            [
                "Keep the chin slightly tucked as you lift so you extend the whole neck, not just crank the head back.",
                "Don't go far past neutral at the top or let the plate slide down your neck.",
                "Use slow, controlled reps with a light plate; stop for dizziness, tingling or sharp pain.",
            ],
            Video = new("_qC3YtnRE2k"),
        },
        new("plate_lateral_neck_flexion", "Plate Lateral Neck Flexion", Neck, Other, Isolation)
        {
            Secondary = [Traps],
            Summary = "Strengthens the sides of the neck: lying on your side on a bench with your head off the end, you lift your ear toward your shoulder against a light plate.",
            Steps =
            [
                "Lie on your side on a flat bench with your bottom shoulder at the end and your head off it.",
                "Fold a towel over the side of your head, place a light plate on it and hold it with your top hand.",
                "Let your head lower slowly toward the floor.",
                "Lift your ear up toward your top shoulder, keeping your face pointing forward.",
                "Lower for 2–3 seconds, finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your nose pointing straight ahead; don't turn your face up or down.",
                "Don't shrug the top shoulder to meet your ear.",
                "Use a light plate and a pain-free range; stop for dizziness or tingling.",
            ],
            Video = new("kydwdUTpmBE"),
        },

        // Neck harness
        new("harness_neck_extension", "Neck Harness Extension", Neck, Other, Isolation)
        {
            Secondary = [Traps],
            Level = Intermediate,
            Summary = "The classic loaded neck extension: wearing a head harness with a weight hanging from it, you lean forward, seated or standing, and raise your head against the load.",
            Steps =
            [
                "Put on a neck harness and attach a light plate or kettlebell to the chain.",
                "Sit on the end of a bench, or stand, and lean forward with your hands on your knees and your back flat.",
                "Let the weight lower your chin slowly toward your chest.",
                "Raise your head until it's in line with your spine.",
                "Pause, then lower for 2–3 seconds.",
            ],
            Tips =
            [
                "Keep your torso still; the movement happens only at the neck.",
                "Don't swing the weight or throw your head back past neutral.",
                "Start light and add weight in small steps; neck strength builds over months, not weeks.",
                "Stop for dizziness, tingling or pain into the arms.",
            ],
            Video = new("VLdIkr2Gfdc"),
        },
        new("harness_band_neck_extension", "Harness Band Neck Extension", Neck, Band, Isolation)
        {
            Secondary = [Traps],
            Summary = "A neck harness clipped to a resistance band instead of a weight, giving smooth, joint-friendly tension for neck extension that's easy to set up at home or ringside.",
            Steps =
            [
                "Put on a neck harness and attach a light band anchored low in front of you, or stand on the band.",
                "Stand with your knees soft and hinge forward slightly with a flat back.",
                "Let the band draw your chin toward your chest under control.",
                "Raise your head until it's in line with your spine.",
                "Return slowly and repeat.",
            ],
            Tips =
            [
                "Turn around (anchor behind) or sideways to train flexion and side bending with the same setup.",
                "Don't let the band snap your head forward; control the way down.",
                "Keep reps slow and the range pain-free.",
            ],
            Video = new("hHv2oKifZ8A"),
        },

        // Cable with harness
        new("cable_neck_extension", "Cable Neck Extension", Neck, Cable, Isolation)
        {
            Secondary = [Traps],
            Level = Intermediate,
            Summary = "Neck extension with a head harness clipped to a low cable, which keeps constant tension through the whole range.",
            Steps =
            [
                "Clip a neck harness to a low pulley and face the stack.",
                "Step back, hinge forward with a flat back and your hands on your thighs.",
                "Let the cable lower your chin toward your chest.",
                "Raise your head until it's in line with your spine.",
                "Return slowly and repeat.",
            ],
            Tips =
            [
                "Use the lightest plate on the stack to start.",
                "Keep your torso still and move only your head.",
                "Don't jerk the weight or lift past neutral.",
            ],
            Video = new("HdwTo4VQjrw"),
        },
        new("cable_neck_flexion", "Cable Neck Flexion", Neck, Cable, Isolation)
        {
            Level = Intermediate,
            Summary = "Neck flexion with a head harness clipped to a cable behind you; you nod your chin toward your chest against the pull.",
            Steps =
            [
                "Clip a neck harness to a pulley set at head height and stand facing away from the stack.",
                "Step forward until there's tension and brace your trunk.",
                "Let the cable draw your head back to neutral or slightly beyond.",
                "Tuck your chin and bring your head forward toward your chest.",
                "Return slowly and repeat.",
            ],
            Tips =
            [
                "Start each rep with a chin tuck rather than poking your chin forward.",
                "Keep your shoulders still; don't crunch your upper back to help.",
                "Use a light weight and stop for dizziness or tingling.",
            ],
            Video = new("ty4WRrutIGg"),
        },
        new("cable_lateral_neck_flexion", "Cable Lateral Neck Flexion", Neck, Cable, Isolation)
        {
            Secondary = [Traps],
            Level = Intermediate,
            Summary = "Side bending of the neck against a cable attached to a head harness, standing side-on to the stack.",
            Steps =
            [
                "Clip a neck harness to a pulley at head height and stand side-on to the stack.",
                "Step away until there's tension and stand tall.",
                "Let the cable draw your ear toward the stack, within a comfortable range.",
                "Bend your head away, bringing your far ear toward your far shoulder.",
                "Return slowly, finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your face pointing forward; don't rotate.",
                "Don't shrug or lean your torso to cheat the weight.",
                "Light load, slow reps, pain-free range.",
            ],
            Video = new("0hffNv8Eyl0"),
        },

        // Bands
        new("band_neck_flexion", "Band Neck Flexion", Neck, Band, Isolation)
        {
            Summary = "Strengthens the front of the neck with a light band anchored behind you and looped around your forehead.",
            Steps =
            [
                "Anchor a light band behind you at head height and loop it around your forehead over a folded towel.",
                "Step forward until there's tension and stand or kneel tall.",
                "Tuck your chin and bring your head forward toward your chest against the band.",
                "Return slowly to neutral.",
            ],
            Tips =
            [
                "Lead with a chin tuck; don't jut the chin forward.",
                "Keep your torso still; only the head moves.",
                "Don't let the band pull your head back past neutral.",
            ],
            Video = new("-4ZXyLnxIPk"),
        },
        new("band_neck_extension", "Band Neck Extension", Neck, Band, Isolation)
        {
            Secondary = [Traps],
            Summary = "Strengthens the back of the neck with a light band anchored in front of you and looped around the back of your head.",
            Steps =
            [
                "Anchor a light band in front of you at head height and loop it around the back of your head.",
                "Step back until there's tension and stand or kneel tall.",
                "With your chin slightly tucked, press your head back until it's in line with your spine.",
                "Return slowly and repeat.",
            ],
            Tips =
            [
                "Keep your chin level; don't look up at the ceiling.",
                "Don't arch your lower back to help.",
                "Slow, controlled reps in a pain-free range.",
            ],
            Video = new("hHv2oKifZ8A"),
        },
        new("band_lateral_neck_flexion", "Band Lateral Neck Flexion", Neck, Band, Isolation)
        {
            Secondary = [Traps],
            Summary = "Strengthens the sides of the neck with a light band anchored beside you and looped around the side of your head.",
            Steps =
            [
                "Anchor a light band at head height beside you and loop it around the side of your head over a towel.",
                "Step away until there's tension and stand tall.",
                "Bend your head away from the anchor, ear toward the far shoulder.",
                "Return slowly to neutral, finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your face pointing straight ahead.",
                "Don't shrug the far shoulder up to meet your ear.",
                "Keep it slow and stop for any tingling into the arm.",
            ],
            Video = new("tTXPFPt1pio"),
        },
        new("band_neck_rotation", "Band Neck Rotation", Neck, Band, Isolation)
        {
            Level = Intermediate,
            Summary = "Trains the neck rotators by turning your head against a light band, useful for athletes who need to resist twisting forces.",
            Steps =
            [
                "Anchor a light band at head height beside you and loop it around your forehead over a towel.",
                "Step away until there's light tension and stand tall with your chin slightly tucked.",
                "Turn your head away from the anchor as if looking over your shoulder.",
                "Return slowly to facing forward, finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Use a very light band; rotation is the most sensitive direction.",
                "Keep your head level; don't tilt or nod while you turn.",
                "Don't let the band whip your head back; control the return.",
                "Stop for dizziness, visual changes or tingling.",
            ],
            Video = new("bhR3--amjnQ"),
        },
        new("band_chin_tuck", "Band Chin Tuck", Neck, Band, Isolation)
        {
            Summary = "A chin tuck against a band around the back of your head, training the deep neck muscles that hold your head back over your shoulders.",
            Steps =
            [
                "Anchor a light band in front of you at head height and loop it around the back of your head.",
                "Step back until there's light tension and stand tall.",
                "Draw your head straight back, making a double chin, without tilting up or down.",
                "Hold 2–3 seconds, then let the band ease your head forward to neutral.",
            ],
            Tips =
            [
                "Think of gliding the head back on a shelf, not nodding.",
                "Keep your eyes level throughout.",
                "Don't let the band pull your head forward past neutral.",
            ],
            Video = new("6fRJARjVVjI"),
        },

        // 4-way neck machine
        new("neck_machine", "4-Way Neck Machine", Neck, Machine, Isolation)
        {
            Secondary = [Traps],
            Summary = "A pad-and-stack machine that loads the neck in all four directions: forward, back and both sides.",
            Steps =
            [
                "Adjust the seat so the pad sits on your forehead, the back or the side of your head.",
                "Hold the handles and sit tall with your torso still.",
                "Move your head through a comfortable range against the pad.",
                "Return slowly and repeat, then reposition and train the other directions.",
            ],
            Tips =
            [
                "Train all four directions for balanced strength.",
                "Use a light weight and a 2–3 second lowering phase.",
                "Don't use your torso or jerk the weight; stop for dizziness or tingling.",
            ],
            Video = new("4SfJZoy_1vA"),
        },
        new("machine_neck_flexion", "Machine Neck Flexion", Neck, Machine, Isolation)
        {
            Summary = "Forward neck flexion on a 4-way neck machine, with the pad on your forehead.",
            Steps =
            [
                "Sit facing the pad and adjust the seat so it meets your forehead.",
                "Hold the handles and sit tall.",
                "Tuck your chin and push the pad forward and down toward your chest.",
                "Return slowly to neutral.",
            ],
            Tips =
            [
                "Lead with a chin tuck.",
                "Don't curl your upper back forward to move the pad.",
                "Keep the load light and the reps slow.",
            ],
            Video = new("O2eZBv8U3Zg"),
        },
        new("machine_neck_extension", "Machine Neck Extension", Neck, Machine, Isolation)
        {
            Secondary = [Traps],
            Summary = "Neck extension on a 4-way neck machine, with the pad on the back of your head.",
            Steps =
            [
                "Sit facing away from the pad and adjust the seat so it meets the back of your head.",
                "Hold the handles and sit tall.",
                "Push the pad back until your head is in line with your spine, or just past.",
                "Return slowly until your chin nears your chest.",
            ],
            Tips =
            [
                "Keep your chin level rather than looking up.",
                "Don't lean your torso back to move the weight.",
                "Use slow reps in a pain-free range.",
            ],
            Video = new("2amJzoyxuNw"),
        },
        new("machine_lateral_neck_flexion", "Machine Lateral Neck Flexion", Neck, Machine, Isolation)
        {
            Secondary = [Traps],
            Summary = "Side bending on a 4-way neck machine, with the pad on the side of your head.",
            Steps =
            [
                "Sit side-on to the pad and adjust the seat so it meets the side of your head above the ear.",
                "Hold the handles and sit tall.",
                "Push the pad sideways, bringing your ear toward your shoulder.",
                "Return slowly to neutral, finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your face pointing forward.",
                "Don't lean your torso or shrug.",
                "Keep it light and slow.",
            ],
            Video = new("vXDGG5S4ww4"),
        },

        // Self- and partner-resisted
        new("neck_isometric_front_back", "Isometric Neck Hold (Front and Back)", Neck, Bodyweight, Isolation)
        {
            Hold = true,
            Summary = "Hand-resisted isometric holds for the front and back of the neck: you press your head into your hands while they stop it moving.",
            Steps =
            [
                "Sit or stand tall with your chin slightly tucked.",
                "Put your palm on your forehead and push your head forward into it while your hand resists, so nothing moves.",
                "Build pressure over 2 seconds, hold 5–10 seconds, then release slowly.",
                "Clasp your hands behind your head and repeat, pushing back into them.",
            ],
            Tips =
            [
                "Ramp the pressure up and down gradually; never jolt into it.",
                "Start at about half effort and build over weeks.",
                "Keep breathing; don't hold your breath.",
            ],
            Video = new("PSwHo-kcfhc"),
        },
        new("neck_isometric_sides", "Isometric Neck Hold (Sides)", Neck, Bodyweight, Isolation)
        {
            Hold = true,
            Secondary = [Traps],
            Summary = "Hand-resisted isometric holds for the sides of the neck: you press your head sideways into your hand while it stops it moving.",
            Steps =
            [
                "Sit or stand tall with one palm against the side of your head above the ear.",
                "Push your head sideways into your hand while the hand resists, so your head stays still.",
                "Build pressure over 2 seconds, hold 5–10 seconds, then release slowly.",
                "Repeat, then switch sides.",
            ],
            Tips =
            [
                "Keep your shoulders down and level.",
                "Ramp the pressure gradually; don't shove.",
                "Stop if you feel tingling into the arm.",
            ],
            Video = new("D9xdxnUL9uA"),
        },
        new("neck_isometric_rotation", "Isometric Neck Hold (Rotation)", Neck, Bodyweight, Isolation)
        {
            Hold = true,
            Summary = "Hand-resisted isometric hold for the neck rotators: you try to turn your head into your hand while it stops it moving.",
            Steps =
            [
                "Sit or stand tall and place one palm on the side of your forehead, by the temple.",
                "Try to turn your head toward your hand while the hand resists, so nothing moves.",
                "Build pressure over 2 seconds, hold 5–10 seconds, then release slowly.",
                "Repeat, then switch sides.",
            ],
            Tips =
            [
                "Use light pressure; rotation needs less force than the other directions.",
                "Keep your head level, not tilted.",
                "Stop for dizziness or visual changes.",
            ],
            Video = new("E5ad2c2z_Bc"),
        },
        new("partner_neck_isometrics", "Partner Neck Isometrics", Neck, Bodyweight, Isolation)
        {
            Hold = true,
            Secondary = [Traps],
            Level = Intermediate,
            Summary = "A partner pushes on your head from the front, back and sides while you hold your head still, the way wrestlers and fighters build neck strength.",
            Steps =
            [
                "Kneel or sit tall with your chin slightly tucked; your partner stands beside you.",
                "Your partner places a hand on your forehead and gradually pushes while you resist, keeping your head still.",
                "Hold 5–10 seconds, then your partner eases off slowly.",
                "Repeat from the back and each side.",
            ],
            Tips =
            [
                "The partner builds and releases pressure gradually; no sudden pushes or letting go.",
                "Agree a signal to stop instantly.",
                "Start well below maximum effort and build over sessions.",
            ],
            Video = new("UxBctfcxZ7E"),
        },
        new("partner_manual_neck_resistance", "Partner Manual Neck Resistance", Neck, Bodyweight, Isolation)
        {
            Secondary = [Traps],
            Level = Intermediate,
            Summary = "A partner applies hand resistance as you move your head through each direction, a staple of wrestling and rugby neck training that needs no equipment.",
            Steps =
            [
                "Lie face up on a bench with your head off the end, or kneel; your partner places a hand on your forehead.",
                "Curl your head up while your partner resists just enough that it takes 2–3 seconds.",
                "Your partner then pushes your head back slowly while you resist on the way down.",
                "Repeat for the back (face down) and each side (side-lying).",
            ],
            Tips =
            [
                "Resistance should be smooth and steady, matched to your strength through the range.",
                "Communicate constantly; the lifter controls the effort.",
                "Never push the head beyond a comfortable range or bounce it.",
            ],
            Video = new("0TFHA73fXQc"),
        },

        // Swiss ball isometrics
        new("swiss_ball_neck_hold_front", "Swiss Ball Neck Hold (Front)", Neck, Other, Isolation)
        {
            Hold = true,
            Summary = "An isometric hold for the front of the neck: you lean your forehead into a Swiss ball pinned against a wall.",
            Steps =
            [
                "Hold a Swiss ball against a wall at head height and face it.",
                "Place your forehead on the ball and walk your feet back a little so you lean in.",
                "Keep your chin slightly tucked and your head in line with your body.",
                "Hold 10–30 seconds, then step in to release.",
            ],
            Tips =
            [
                "Walk the feet further back to make it harder.",
                "Keep your body straight like a plank; don't sag at the hips.",
                "Don't let your head tip back or forward.",
            ],
            Video = new("CJ7nf2F4Gls"),
        },
        new("swiss_ball_neck_hold_back", "Swiss Ball Neck Hold (Back)", Neck, Other, Isolation)
        {
            Hold = true,
            Secondary = [Traps],
            Summary = "An isometric hold for the back of the neck: you lean the back of your head into a Swiss ball pinned against a wall.",
            Steps =
            [
                "Stand with your back to a wall and a Swiss ball between the wall and the back of your head.",
                "Walk your feet forward a little so you lean back into the ball.",
                "Keep your chin slightly tucked and your head in line with your body.",
                "Hold 10–30 seconds, then step back to release.",
            ],
            Tips =
            [
                "Small lean to start; increase the angle over weeks.",
                "Don't look up or let your chin poke out.",
                "Keep your body straight and breathe normally.",
            ],
            Video = new("u-XKSux8Nt8"),
        },
        new("swiss_ball_neck_hold_side", "Swiss Ball Neck Hold (Side)", Neck, Other, Isolation)
        {
            Hold = true,
            Secondary = [Traps],
            Summary = "An isometric hold for the side of the neck: you lean the side of your head into a Swiss ball pinned against a wall.",
            Steps =
            [
                "Stand side-on to a wall with a Swiss ball between it and the side of your head.",
                "Walk your feet away a little so you lean into the ball.",
                "Keep your head in line with your body and your face pointing forward.",
                "Hold 10–30 seconds, then switch sides.",
            ],
            Tips =
            [
                "Keep your shoulders level; don't shrug.",
                "Increase the lean gradually.",
                "Don't rotate your face toward or away from the wall.",
            ],
            Video = new("RKbIybrihZM"),
        },

        // Bodyweight on a bench
        new("lying_neck_curl", "Lying Neck Curl", Neck, Bodyweight, Isolation)
        {
            Summary = "A bodyweight neck flexion lying face up with your head off the end of a bench, the first step before adding a plate.",
            Steps =
            [
                "Lie on your back on a flat bench with your head and neck off the end.",
                "Let your head lower slowly back within a comfortable range.",
                "Tuck your chin and curl your head up toward your chest.",
                "Pause, then lower for 2–3 seconds.",
            ],
            Tips =
            [
                "Lead with the chin tuck.",
                "Don't swing your head or bounce at the bottom.",
                "Stop for dizziness, especially when lifting your head after the lowered position.",
            ],
            Video = new("aM657RnN9XE", 6.4640002, 24.24),
        },
        new("lying_neck_extension", "Lying Neck Extension", Neck, Bodyweight, Isolation)
        {
            Secondary = [Traps],
            Summary = "A bodyweight neck extension lying face down with your head off the end of a bench.",
            Steps =
            [
                "Lie face down on a flat bench with your head off the end.",
                "Let your chin lower slowly toward your chest.",
                "Raise your head until it's in line with your spine.",
                "Pause, then lower slowly.",
            ],
            Tips =
            [
                "Keep the chin slightly tucked as you lift.",
                "Don't crank your head back past neutral.",
                "Slow and smooth, in a pain-free range.",
            ],
            Video = new("AUtvjVUIBt4"),
        },
        new("side_lying_neck_raise", "Side-Lying Neck Raise", Neck, Bodyweight, Isolation)
        {
            Secondary = [Traps],
            Summary = "A bodyweight side bend of the neck lying on your side, with your head off the end of a bench or lifted off the floor.",
            Steps =
            [
                "Lie on your side on a bench with your head off the end, or on the floor with your head resting on your arm.",
                "Let your head lower toward the floor.",
                "Lift your ear up toward your top shoulder, face pointing forward.",
                "Lower slowly, finish your reps, then switch sides.",
            ],
            Tips =
            [
                "Keep your nose pointing straight ahead.",
                "Don't shrug your top shoulder.",
                "Move slowly; add a plate only when 20 easy reps feel smooth.",
            ],
            Video = new("duOfvQUQfWQ"),
        },

        // Neck bridges
        new("wall_neck_bridge", "Wall Neck Bridge", Neck, Bodyweight, Isolation)
        {
            Hold = true,
            Level = Intermediate,
            Summary = "A leaning bridge with your head on a padded wall, the safe progression toward floor neck bridges; done facing the wall and with your back to it.",
            Steps =
            [
                "Fold a towel or pad and hold it on a wall at head height.",
                "Face the wall, place your forehead on the pad and walk your feet back so you lean in at a slight angle.",
                "Keep your body straight and your head in line with it.",
                "Hold 10–30 seconds, then turn around and repeat with the back of your head on the pad.",
            ],
            Tips =
            [
                "Steepen the angle only a little each week before moving to the floor.",
                "Keep your hands near the wall to catch yourself.",
                "Don't let your neck bend; it should stay neutral the whole time.",
                "Stop for dizziness, tingling or pain.",
            ],
            Video = new("iGIYFQePeFs"),
        },
        new("wrestlers_bridge", "Wrestler's Bridge", Neck, Bodyweight, Compound)
        {
            Hold = true,
            Secondary = [Traps, LowerBack, Glutes],
            Level = Advanced,
            Summary = "The wrestler's back bridge: lying face up, you arch and lift your hips so your weight rests on your feet and the top of your head, building a strong, resilient back of the neck.",
            Steps =
            [
                "Lie on your back on a thick mat with your knees bent and feet flat, wide apart.",
                "Place your hands by your head, drive through your feet and lift your hips, rolling onto the top of your head.",
                "Arch your back and push your hips high so your nose points toward the mat behind you.",
                "Take some weight with your hands at first, then hold with your hands crossed on your chest.",
                "Hold 10–30 seconds, then lower your hips and roll back down with control.",
            ],
            Tips =
            [
                "Only attempt this after months of solid neck training and wall bridges; it loads the cervical spine heavily.",
                "Always use a thick mat and keep your hands ready to take weight.",
                "Don't roll suddenly or let your head collapse; move in and out slowly.",
                "Stop immediately for tingling, numbness, dizziness or sharp pain.",
            ],
            Video = new("5SJOsWxFOKQ"),
        },
        new("front_neck_bridge", "Front Neck Bridge", Neck, Bodyweight, Compound)
        {
            Hold = true,
            Secondary = [Traps, Abs],
            Level = Advanced,
            Summary = "The face-down bridge: with your forehead on the mat and your hips raised, you hold your weight on your head and feet, building strength in the front of the neck.",
            Steps =
            [
                "Kneel on a thick mat and place your forehead on it, hands on the mat either side of your head.",
                "Straighten your legs and lift your hips so your weight is shared between your feet, forehead and hands.",
                "Gradually take your hands away or keep them lightly on the mat.",
                "Hold 10–30 seconds with your neck still, then lower your knees and lift your head.",
            ],
            Tips =
            [
                "Progress from kneeling with hands down to feet only over many weeks.",
                "Keep the weight on your forehead, not the top of your head or your nose.",
                "Don't rock into the hold; get in and out slowly.",
                "Stop for tingling, numbness, dizziness or sharp pain.",
            ],
            Video = new("4gMDsf7nDlY"),
        },
        new("neck_bridge_rocks", "Neck Bridge Rocks", Neck, Bodyweight, Compound)
        {
            Secondary = [Traps, LowerBack, Glutes],
            Level = Advanced,
            Summary = "Slow rocks forward and back in a wrestler's bridge, rolling from the top of your head toward your forehead to strengthen the neck through its range.",
            Steps =
            [
                "Set up in a wrestler's bridge on a thick mat, hands by your head to support.",
                "Push with your feet to rock slowly forward toward your forehead.",
                "Rock back onto the top of your head and slightly toward the back of it.",
                "Keep the rocks small at first and do 5–10 slow reps, then lower down with control.",
            ],
            Tips =
            [
                "Master a steady wrestler's bridge hold before adding rocks.",
                "Keep your hands down to take weight as needed.",
                "Small, slow range only; never bounce or roll fast.",
                "Stop immediately for tingling, numbness, dizziness or sharp pain.",
            ],
            Video = new("kIq7oCcaY9I"),
        },

        // Chin tucks and deep neck flexors
        new("chin_tuck", "Chin Tuck", Neck, Bodyweight, Isolation)
        {
            Summary = "A small backward glide of the head that trains the deep neck flexors and counters forward-head posture.",
            Steps =
            [
                "Sit or stand tall with your eyes level.",
                "Draw your head straight back, making a double chin, without tilting up or down.",
                "Hold 3–5 seconds, feeling a gentle stretch at the base of the skull.",
                "Relax back to neutral and repeat.",
            ],
            Tips =
            [
                "Place a finger on your chin and move your head away from it to get the direction right.",
                "Don't nod your head down; it's a glide, not a bow.",
                "Keep your shoulders relaxed.",
            ],
            Video = new("w4kf1ceqyQs"),
        },
        new("wall_chin_tuck", "Wall Chin Tuck", Neck, Bodyweight, Isolation)
        {
            Summary = "A chin tuck standing with your back against a wall, using the wall as a guide and as light resistance.",
            Steps =
            [
                "Stand with your heels, buttocks and shoulders against a wall.",
                "Draw your chin in and press the back of your head gently into the wall.",
                "Hold 3–5 seconds, keeping your eyes level.",
                "Relax and repeat.",
            ],
            Tips =
            [
                "Keep your chin down; don't tip your face up to reach the wall.",
                "If your head can't reach the wall comfortably, don't force it.",
                "Gentle pressure only.",
            ],
            Video = new("TtTu4arbMmM"),
        },
        new("lying_chin_nod", "Lying Chin Nod", Neck, Bodyweight, Isolation)
        {
            Summary = "A gentle nodding movement lying face up with your head on the floor (craniocervical flexion), the physio's first exercise for the deep neck flexors.",
            Steps =
            [
                "Lie on your back with your knees bent and a thin folded towel under your head.",
                "Gently nod as if saying \"yes\", sliding the back of your head up the towel.",
                "Hold 5–10 seconds without lifting your head.",
                "Relax and repeat.",
            ],
            Tips =
            [
                "The front of the neck should stay soft; if the cords stand out, nod less.",
                "Don't lift your head off the floor.",
                "Breathe normally throughout.",
            ],
            Video = new("WJ-iTfZY7aM", 11, 20),
        },
        new("supine_chin_tuck_head_lift", "Supine Chin Tuck Head Lift", Neck, Bodyweight, Isolation)
        {
            Level = Intermediate,
            Summary = "Lying face up, you tuck your chin and lift your head just off the floor, training the deep neck flexors for strength and endurance.",
            Steps =
            [
                "Lie on your back with your knees bent and your arms by your sides.",
                "Tuck your chin as in a lying chin nod.",
                "Keeping the tuck, lift your head about 2–3 cm off the floor.",
                "Hold 5–10 seconds, lower slowly and relax.",
            ],
            Tips =
            [
                "The chin stays tucked the whole time; if it pokes up, lower down.",
                "Build toward longer holds as endurance improves.",
                "Don't strain or hold your breath.",
            ],
            Video = new("7OgvJ653oxE"),
        },
        new("prone_chin_tuck_head_lift", "Prone Chin Tuck Head Lift", Neck, Bodyweight, Isolation)
        {
            Summary = "Lying face down, you tuck your chin and lift your forehead just off the floor, training the deep neck extensors.",
            Steps =
            [
                "Lie face down with your forehead on a folded towel and your arms by your sides.",
                "Tuck your chin gently.",
                "Lift your forehead a few centimetres, keeping your eyes on the floor.",
                "Hold 3–5 seconds, lower slowly and relax.",
            ],
            Tips =
            [
                "Look at the floor, not forward; the back of your neck stays long.",
                "Don't lift your chest or squeeze your shoulders up.",
                "Small range is enough.",
            ],
            Video = new("4L30y5fa72k"),
        },
        new("quadruped_neck_extension", "Quadruped Neck Extension", Neck, Bodyweight, Isolation)
        {
            Summary = "On hands and knees, you lower and raise your head with your chin tucked, a controlled way to train the neck extensors through range.",
            Steps =
            [
                "Kneel on all fours with your hands under your shoulders and your back flat.",
                "Tuck your chin slightly and let your head lower so you look back toward your knees.",
                "Raise your head back to neutral, keeping the chin tucked.",
                "Repeat slowly.",
            ],
            Tips =
            [
                "Stop at neutral; don't look up at the wall.",
                "Keep your spine still; only the head and neck move.",
                "Move slowly and smoothly.",
            ],
            Video = new("7ZsaS27hJqM"),
        },

        // Mobility
        new("neck_cars", "Neck CARs", Neck, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "Controlled articular rotations for the neck: slow, deliberate circles through the largest pain-free range to keep the joints moving well.",
            Steps =
            [
                "Sit or stand tall and brace lightly so only your head moves.",
                "Tuck your chin toward your chest.",
                "Slowly roll your ear toward one shoulder, then tip your head back with control, then to the other shoulder and back to the chin.",
                "Do 3–5 slow circles each direction.",
            ],
            Tips =
            [
                "Go slowly, about 10 seconds per circle.",
                "Keep the backward part small if it pinches.",
                "Don't swing or roll fast; stop for dizziness.",
            ],
            Video = new("dA6RKcIL74w"),
        },
        new("neck_rotation", "Controlled Neck Rotation", Neck, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "Slowly turning the head from side to side to maintain rotation range.",
            Steps =
            [
                "Sit or stand tall with your chin slightly tucked.",
                "Turn your head slowly to look over one shoulder as far as is comfortable.",
                "Pause for a breath, then turn to the other side.",
                "Repeat 5–10 times each way.",
            ],
            Tips =
            [
                "Keep your head level; don't tilt as you turn.",
                "Keep your shoulders facing forward.",
                "Stop for dizziness or visual changes.",
            ],
            Video = new("PruXF-NE2zI"),
        },
        new("neck_side_tilt", "Neck Side Tilt", Neck, Bodyweight, Isolation)
        {
            Category = Mobility,
            Summary = "Slowly tilting your ear toward each shoulder to keep side-bending range in the neck.",
            Steps =
            [
                "Sit or stand tall with your shoulders relaxed.",
                "Tilt your ear toward one shoulder as far as is comfortable.",
                "Pause for a breath, return to centre and tilt to the other side.",
                "Repeat 5–10 times each way.",
            ],
            Tips =
            [
                "Keep your face pointing forward.",
                "Don't lift your shoulder to meet your ear.",
                "Move slowly and smoothly.",
            ],
            Video = new("CroDcDQll4E"),
        },

        // Stretches
        new("upper_trap_stretch", "Upper Trap Stretch", Neck, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Secondary = [Traps],
            Summary = "A lateral neck stretch for the side of the neck and upper traps: you tilt your ear toward one shoulder and gently assist with your hand.",
            Steps =
            [
                "Sit tall and hold the edge of the seat with one hand to keep that shoulder down.",
                "Tilt your head away, ear toward the other shoulder.",
                "Rest your free hand on your head and let its weight deepen the stretch gently.",
                "Hold 20–30 seconds, then switch sides.",
            ],
            Tips =
            [
                "Use only the weight of your hand; don't pull.",
                "Keep your face pointing forward.",
                "Stop if you feel tingling into the arm.",
            ],
            Video = new("uwLcpgIqpnU"),
        },
        new("levator_scapulae_stretch", "Levator Scapulae Stretch", Neck, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Secondary = [Traps],
            Summary = "Stretches the levator scapulae, the muscle from the neck to the top of the shoulder blade, by turning your nose toward your armpit and looking down.",
            Steps =
            [
                "Sit tall and hold the edge of the seat with one hand.",
                "Turn your head about 45° away from that side and look down toward your opposite armpit.",
                "Rest your free hand on the back of your head and let it add gentle pressure.",
                "Hold 20–30 seconds, then switch sides.",
            ],
            Tips =
            [
                "You should feel it at the back and side of the neck toward the shoulder blade.",
                "Keep the anchored shoulder down.",
                "Don't yank with your hand.",
            ],
            Video = new("0_RO8NbdKBc"),
        },
        new("scm_stretch", "SCM Stretch", Neck, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Stretches the sternocleidomastoid at the front and side of the neck by tilting your head away and looking slightly up.",
            Steps =
            [
                "Sit tall and place one hand flat on your collarbone on the side you want to stretch.",
                "Tilt your head away from that side.",
                "Turn your face slightly up toward the ceiling until you feel a stretch along the front of the neck.",
                "Hold 20–30 seconds, then switch sides.",
            ],
            Tips =
            [
                "Keep the extension small; don't drop your head far back.",
                "Hold the collarbone down gently rather than pressing on the throat.",
                "Stop for dizziness.",
            ],
            Video = new("fAC8CwPLKXg"),
        },
        new("scalene_stretch", "Scalene Stretch", Neck, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Stretches the scalenes on the side of the neck by anchoring your collarbone and tilting your head away and slightly back.",
            Steps =
            [
                "Sit tall and press both hands, one on top of the other, onto one collarbone.",
                "Tilt your head away from that side.",
                "Tip your chin very slightly up and back until you feel the stretch low on the side of the neck.",
                "Hold 20–30 seconds, then switch sides.",
            ],
            Tips =
            [
                "Small movements; the stretch comes on quickly.",
                "Stop if you feel tingling or numbness into the arm or hand.",
                "Breathe slowly and relax your shoulders.",
            ],
            Video = new("K4RT5VWhptg"),
        },
        new("neck_extensor_stretch", "Neck Extensor Stretch", Neck, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Secondary = [Traps],
            Summary = "Stretches the back of the neck by lowering your chin toward your chest with gentle help from your hands.",
            Steps =
            [
                "Sit tall with your shoulders relaxed.",
                "Tuck your chin and lower it toward your chest.",
                "Clasp your hands lightly on the back of your head and let their weight add a gentle stretch.",
                "Hold 20–30 seconds, then lift your head slowly.",
            ],
            Tips =
            [
                "Let the hands rest; don't pull your head down.",
                "Keep your back upright; don't round your upper back.",
                "Stop if you feel tingling down your spine or into your arms.",
            ],
            Video = new("nG10kGCsos0"),
        },
        new("suboccipital_release", "Suboccipital Release", Neck, Bodyweight, Isolation)
        {
            Category = ExerciseCategory.Stretch,
            Summary = "Eases tension in the small muscles at the base of the skull with a gentle chin nod, lying with your head supported on your fingertips or two tennis balls.",
            Steps =
            [
                "Lie on your back with your knees bent.",
                "Place your fingertips, or two tennis balls in a sock, under the base of your skull either side of the spine.",
                "Let your head relax onto them, then make a small chin nod to lengthen the back of the neck.",
                "Hold 30–60 seconds, breathing slowly.",
            ],
            Tips =
            [
                "Pressure should feel like a dull release, never sharp.",
                "Keep the balls off the spine itself.",
                "Stop for headache, dizziness or tingling.",
            ],
            Video = new("P3RIddl_rlY"),
        },
    ];
}
