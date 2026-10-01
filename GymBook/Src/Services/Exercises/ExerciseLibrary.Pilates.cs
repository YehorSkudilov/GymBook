using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] PilatesExercises() =>
    [
        // Classical mat order
        new("pilates_hundred", "The Hundred", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders],
            Category = Pilates,
            Summary = "The classical warm-up: a held abdominal curl with legs extended while the arms pump in time with the breath for 100 counts.",
            Steps =
            [
                "Lie on your back, bring your knees to tabletop and curl your head and shoulders up to the tips of your shoulder blades.",
                "Reach your arms long by your sides a few inches off the mat and extend your legs to about 45° (keep tabletop if your back lifts).",
                "Pump your arms up and down in a small, brisk range from the shoulders.",
                "Breathe in for 5 pumps and out for 5 pumps; repeat 10 times for 100 counts.",
                "Bend your knees in and lower your head to finish.",
            ],
            Tips =
            [
                "Keep your gaze toward your thighs and your abdominals drawn in and scooped, not domed.",
                "Lower the legs only as far as you can keep your lower back steady on the mat.",
                "Don't strain your neck; rest your head down if it fatigues before your abs do.",
            ],
            Video = new("tYJJyYUrK4c", 43, 77),
        },
        new("pilates_roll_up", "Roll-Up", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "A slow articulation of the spine from lying to a forward reach over the legs and back down, one vertebra at a time.",
            Steps =
            [
                "Lie on your back with legs together and long, feet flexed, arms reaching overhead.",
                "Inhale as you float your arms to the ceiling and curl your head and chest up.",
                "Exhale as you peel the spine off the mat and reach forward over your legs in a C-curve.",
                "Inhale to begin rolling back, exhale as you lay the spine down bone by bone and return the arms overhead.",
            ],
            Tips =
            [
                "Keep the legs heavy and the abdominals scooped so you roll rather than jerk up.",
                "Don't use momentum or let the feet fly up; bend the knees slightly if you can't roll smoothly yet.",
            ],
            Video = new("H_-JE2yN1W0"),
        },
        new("pilates_roll_over", "Roll-Over", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "Lift the legs overhead and roll the spine up off the mat, then roll it down with control while the legs stay long.",
            Steps =
            [
                "Lie on your back with arms by your sides, palms down, legs together lifted to 90°.",
                "Exhale as you lift your hips and roll the legs over your head until they are parallel to the floor.",
                "Flex your feet and open the legs to hip width.",
                "Inhale, then exhale as you roll down one vertebra at a time, keeping the legs close to you.",
                "Lower the legs to just above the mat, bring them together and repeat; reverse the leg position after 3.",
            ],
            Tips =
            [
                "Weight stays across your shoulder blades, never on your neck; don't turn your head.",
                "Press the backs of the arms into the mat to control the roll-down.",
                "Skip this if you have neck or disc problems.",
            ],
            Video = new("Y5YdoRMCCKM", 28, 135),
        },
        new("pilates_single_leg_circles", "Single-Leg Circles", Abs, Bodyweight, Isolation)
        {
            Secondary = [Quads, Glutes],
            Category = Pilates,
            Summary = "Trace circles with one straight leg toward the ceiling while the pelvis stays perfectly still, training hip control and core stability.",
            Steps =
            [
                "Lie on your back with arms by your sides, one leg long on the mat and the other reaching to the ceiling.",
                "Cross the raised leg over your body, circle it down and around, and sweep it back up to the start.",
                "Inhale for the first half of the circle and exhale to finish it.",
                "Do 5 circles, reverse for 5, then switch legs.",
            ],
            Tips =
            [
                "Keep the circles only as big as you can manage without the hips rocking.",
                "Anchor the long leg and the backs of the arms into the mat.",
                "Bend the bottom knee if your hamstrings are tight.",
            ],
            Video = new("bVmm1XVgHfU"),
        },
        new("pilates_rolling_like_a_ball", "Rolling Like a Ball", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Summary = "Balance in a tight ball and roll back to the shoulder blades and up again, massaging the spine and teaching abdominal control.",
            Steps =
            [
                "Sit near the front of the mat, hug your shins with heels close to your seat and balance just behind your sit bones.",
                "Tuck your chin and round your back into a tight C-curve, feet just off the mat.",
                "Inhale as you roll back to the tips of your shoulder blades.",
                "Exhale as you roll up and balance again without your feet touching down.",
            ],
            Tips =
            [
                "Keep the same ball shape the whole time; don't open up or kick the legs to get back up.",
                "Never roll onto your neck or head.",
            ],
            Video = new("elkcXFPyaW8"),
        },
        new("pilates_single_leg_stretch", "Single-Leg Stretch", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Summary = "The first of the abdominal series: hold a chest curl while drawing one knee in and extending the other leg, switching rhythmically.",
            Steps =
            [
                "Lie on your back, curl your head and shoulders up and draw your right knee to your chest.",
                "Place your right hand on your right ankle and left hand on your right knee; extend the left leg at about 45°.",
                "Switch legs and hands, drawing the left knee in as the right leg extends.",
                "Inhale for two switches and exhale for two; do 5–10 sets.",
            ],
            Tips =
            [
                "Keep the torso completely still while the legs work.",
                "Pull the knee in toward the shoulder, not across the body, and keep the elbows wide.",
                "Don't let your lower back arch as the leg extends; raise the leg if it does.",
            ],
            Video = new("Ad4lgW4ieAM"),
        },
        new("pilates_double_leg_stretch", "Double-Leg Stretch", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "From a tight ball in a chest curl, reach the arms and legs away in opposite directions, then circle the arms back and hug in.",
            Steps =
            [
                "Lie on your back, curl your head and shoulders up and hug both knees to your chest.",
                "Inhale as you reach your arms overhead and extend your legs to about 45°, keeping the curl.",
                "Exhale as you circle the arms out to the sides and draw the knees back in to hug.",
                "Repeat 6–10 times.",
            ],
            Tips =
            [
                "Keep your head and chest lifted as the arms go back; don't drop your head.",
                "Reach long from the centre and keep the back pressed into the mat.",
            ],
            Video = new("qD58bbjzcm0"),
        },
        new("pilates_single_straight_leg_stretch", "Single Straight-Leg Stretch", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "Often called scissors on the mat: hold a chest curl and scissor straight legs, pulsing one toward you as the other hovers.",
            Steps =
            [
                "Lie on your back, curl up and reach one straight leg toward your face, the other hovering just off the mat.",
                "Hold the raised leg at the calf or ankle and pulse it toward you twice.",
                "Switch legs with a quick scissor and pulse the other leg twice.",
                "Inhale for one switch, exhale for the next; do 5–10 sets.",
            ],
            Tips =
            [
                "Lengthen the legs rather than pulling hard; keep the upper body lifted and still.",
                "Keep the low leg only as low as you can hold a steady pelvis.",
            ],
            Video = new("6D0loyquu1g"),
        },
        new("pilates_double_straight_leg_stretch", "Double Straight-Leg Stretch", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "Also known as the lower lift: hands behind the head in a chest curl, lower both straight legs together and lift them back up.",
            Steps =
            [
                "Lie on your back, hands stacked behind your head, elbows wide, and curl your head and shoulders up.",
                "Reach both legs to the ceiling, squeezed together in Pilates stance.",
                "Inhale as you lower the legs toward the mat, only as far as your back stays down.",
                "Exhale as you draw the legs back up to vertical; repeat 6–10 times.",
            ],
            Tips =
            [
                "Lower slowly and lift a little faster, keeping the abdominals scooped.",
                "Don't pull on your head or let your lower back arch.",
            ],
            Video = new("woAkp7x9vB8"),
        },
        new("pilates_criss_cross", "Criss-Cross", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "The last of the abdominal series: rotate the curled torso toward the bent knee as the other leg extends, working the obliques.",
            Steps =
            [
                "Lie on your back, hands behind your head, curl up and bring the legs to tabletop.",
                "Extend your right leg and rotate your ribs toward your left knee, reaching the right elbow toward it.",
                "Hold the twist briefly, then switch legs and rotate to the other side.",
                "Exhale as you rotate, inhale as you pass through centre; do 5–10 sets.",
            ],
            Tips =
            [
                "Rotate from the ribs; the elbows stay wide and the head stays in your hands.",
                "Keep the pelvis level and still; don't rock side to side.",
            ],
            Video = new("fQiCxMwfjX0", 26, 54),
        },
        new("pilates_spine_stretch_forward", "Spine Stretch Forward", Abs, Bodyweight, Compound)
        {
            Secondary = [LowerBack],
            Category = Pilates,
            Summary = "Sit tall with legs apart and round forward over a deep abdominal scoop, articulating the spine down and back up to vertical.",
            Steps =
            [
                "Sit tall with legs straight and a little wider than the mat, feet flexed, arms reaching forward at shoulder height.",
                "Inhale to grow tall through the crown of the head.",
                "Exhale as you nod your chin and round forward, drawing the navel back as if over a beach ball.",
                "Inhale as you roll back up, stacking one vertebra at a time to sitting tall; repeat 3–5 times.",
            ],
            Tips =
            [
                "Keep your sit bones heavy; the stretch comes from lifting up and over, not collapsing.",
                "Sit on a folded towel or bend the knees if your hamstrings round your lower back.",
            ],
            Video = new("XZGuNaEV-nM"),
        },
        new("pilates_open_leg_rocker", "Open-Leg Rocker", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "Balance with straight legs in a V holding your ankles, then rock back to the shoulder blades and up again without losing the shape.",
            Steps =
            [
                "Sit behind your sit bones holding your ankles, legs extended in a V about shoulder width.",
                "Lift your chest and find your balance with arms straight.",
                "Inhale as you round the spine and roll back to the shoulder blades.",
                "Exhale as you roll back up and balance in the V; repeat 6 times.",
            ],
            Tips =
            [
                "Initiate the roll from the abdominals, not by throwing the head back.",
                "Keep the distance between your legs and chest the same throughout.",
                "Practise the balance with bent knees before straightening the legs.",
            ],
            Video = new("E4FSgzPlUcs"),
        },
        new("pilates_corkscrew", "Corkscrew", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "With legs glued together toward the ceiling, circle them side to side around the centre line, challenging the obliques and pelvic stability.",
            Steps =
            [
                "Lie on your back, arms by your sides, legs together reaching to the ceiling.",
                "Inhale as you shift the legs to the right and circle them down.",
                "Exhale as you sweep them across and back up to centre on the left.",
                "Reverse direction each time; do 3 each way.",
            ],
            Tips =
            [
                "Keep the circle small and the shoulders anchored; one hip lifts only slightly.",
                "In the advanced version the hips roll up off the mat between circles; master this one first.",
            ],
            Video = new("yssUvNuQowE"),
        },
        new("pilates_saw", "Saw", Abs, Bodyweight, Compound)
        {
            Secondary = [LowerBack],
            Category = Pilates,
            Summary = "Sitting with legs apart, twist and reach the little finger past the opposite little toe, sawing it off, to train rotation and flexion together.",
            Steps =
            [
                "Sit tall with legs wider than hip width, feet flexed, arms out to the sides at shoulder height.",
                "Inhale to twist your torso to the right, keeping both sit bones down.",
                "Exhale as you round forward and reach your left little finger past your right little toe, the back arm reaching up behind you.",
                "Inhale as you roll back up, still twisted, then return to centre; alternate sides 3–5 times each.",
            ],
            Tips =
            [
                "Rotate first, then flex; keep the hips square and the opposite sit bone anchored.",
                "Breathe out fully as you saw, as if wringing the air out of the lungs.",
            ],
            Video = new("Sb0SG1cXgEY"),
        },
        new("pilates_swan_dive", "Swan Dive", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Glutes, Hamstrings],
            Category = Pilates,
            Level = Advanced,
            Summary = "Press into a full back extension, then release the hands and rock forward and back on the front of the body in a strong arched shape.",
            Steps =
            [
                "Lie face down with hands under your shoulders and legs together.",
                "Inhale as you press up into extension, lengthening through the crown of the head.",
                "Release the hands, reach the arms forward and rock forward onto the chest as the legs lift.",
                "Rock back, lifting the chest as the legs lower; continue rocking with the breath 6 times.",
            ],
            Tips =
            [
                "Keep the abdominals engaged and lengthen the spine so the arch is long, not pinched in the lower back.",
                "Build up with Swan Prep first; skip this if extension hurts your back.",
            ],
            Video = new("mjZZ22GLcDc"),
        },
        new("pilates_single_leg_kick", "Single-Leg Kick", Hamstrings, Bodyweight, Isolation)
        {
            Secondary = [Glutes],
            Category = Pilates,
            Summary = "Lift onto the forearms in a sphinx position and kick each heel toward the seat with a double beat, working the hamstrings while the trunk stays lifted.",
            Steps =
            [
                "Lie face down propped on your forearms, elbows under shoulders, fists together, legs long.",
                "Lift your abdominals off the mat and press your pubic bone down.",
                "Kick your right heel toward your seat twice with a pointed then flexed foot.",
                "Switch legs and kick the left heel twice; alternate 6–8 times each, exhaling on the kicks.",
            ],
            Tips =
            [
                "Keep the chest lifted away from the hands and don't sink into the shoulders.",
                "Keep the knees together and the hips down on the mat.",
            ],
            Video = new("X79gMUknrVs"),
        },
        new("pilates_double_leg_kick", "Double-Leg Kick", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Glutes, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Lying face down with hands clasped high on the back, kick both heels toward the seat, then extend the legs and lift the chest as the arms reach back.",
            Steps =
            [
                "Lie face down with your head turned to one side and hands clasped high on your back, elbows on the mat.",
                "Kick both heels toward your seat three times.",
                "Inhale as you extend the legs along the mat, reach the clasped hands toward your feet and lift your chest.",
                "Exhale as you lower and turn your head to the other side; repeat 4–6 times each side.",
            ],
            Tips =
            [
                "Keep the legs on the mat as you lift the chest; the lift comes from the upper back.",
                "Draw the shoulder blades down as the arms reach back.",
            ],
            Video = new("eQgIt5Ikb1g"),
        },
        new("pilates_neck_pull", "Neck Pull", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "A harder Roll-Up with hands behind the head: curl up, round forward, sit tall, then hinge back and roll down slowly.",
            Steps =
            [
                "Lie on your back with hands stacked behind your head, elbows wide, legs hip width with feet flexed.",
                "Inhale to curl the head and chest up; exhale to roll up and round forward over your legs.",
                "Inhale to stack the spine up to sitting tall.",
                "Hinge back a few degrees with a long spine, then exhale to tuck the pelvis and roll down one vertebra at a time; repeat 3–5 times.",
            ],
            Tips =
            [
                "Don't pull on your neck; the hands only support the head.",
                "Bend the knees or anchor the feet under a strap until you can roll up without jerking.",
            ],
            Video = new("wVaSdDSxKwY"),
        },
        new("pilates_scissors", "Pilates Scissors", Abs, Bodyweight, Compound)
        {
            Secondary = [LowerBack],
            Category = Pilates,
            Level = Advanced,
            Summary = "Lifted into a supported shoulder stand, split the legs forward and back in a scissor, training control and hip mobility while inverted.",
            Steps =
            [
                "Lie on your back and roll your legs up toward the ceiling, lifting the hips and supporting the pelvis with your hands, elbows under you.",
                "Reach the legs long so your body forms a straight diagonal from shoulders to toes.",
                "Split the legs, bringing one toward your face and the other away, and pulse twice.",
                "Switch with a scissor action and pulse the other side; do 5–6 sets, then roll down slowly.",
            ],
            Tips =
            [
                "Weight on the upper back and elbows, never on the neck; keep the head still.",
                "Keep the hips lifted and the pelvis level as the legs change.",
                "Avoid inversions with neck problems, glaucoma or uncontrolled blood pressure.",
            ],
            Video = new("CO9_19vfCf4"),
        },
        new("pilates_bicycle", "Pilates Bicycle", Abs, Bodyweight, Compound)
        {
            Secondary = [LowerBack],
            Category = Pilates,
            Level = Advanced,
            Summary = "From the supported shoulder stand of Scissors, pedal the legs in a large backward bicycle, adding knee bend to the split.",
            Steps =
            [
                "Set up as for Scissors: hips lifted and supported by your hands, legs reaching long.",
                "Split the legs and bend the back knee, reaching the heel toward the floor.",
                "Draw the bent leg up and over toward your face as the front leg sweeps down and bends.",
                "Keep pedalling in long, smooth circles for 5–6 cycles, then reverse and roll down.",
            ],
            Tips =
            [
                "Make the movement as large as you can without the pelvis dropping.",
                "Weight stays on the shoulders and elbows, never the neck.",
            ],
            Video = new("zKzRqV3cjgQ"),
        },
        new("pilates_shoulder_bridge", "Shoulder Bridge", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Abs],
            Category = Pilates,
            Level = Advanced,
            Summary = "Hold a high bridge on one leg while the other kicks up to the ceiling and lowers with a flexed foot, testing pelvic stability.",
            Steps =
            [
                "Lie on your back with knees bent, feet hip width, and lift your hips into a bridge; support the pelvis with your hands if needed.",
                "Extend your right leg along the line of your thigh.",
                "Inhale as you kick the leg up to the ceiling with a pointed foot.",
                "Exhale as you flex the foot and lower the leg to hip height; do 3–5 kicks, then switch legs.",
            ],
            Tips =
            [
                "Keep the hips level and lifted; don't let the standing-side hip drop.",
                "Press through the heel of the standing foot to keep the glutes working.",
                "Master Pelvic Curl first.",
            ],
            Video = new("QFv_Fex3Mko"),
        },
        new("pilates_spine_twist", "Spine Twist", Abs, Bodyweight, Isolation)
        {
            Secondary = [LowerBack],
            Category = Pilates,
            Summary = "Sit tall with arms out to the sides and twist the spine with a double pulse each way, growing taller as you rotate.",
            Steps =
            [
                "Sit tall with legs together and straight, feet flexed, arms out to the sides at shoulder height.",
                "Inhale to lengthen up.",
                "Exhale as you rotate the torso to the right in two pulses, going a little further on the second.",
                "Inhale to return to centre, then repeat to the left; do 3–5 each side.",
            ],
            Tips =
            [
                "The legs and hips stay still; the twist comes from the waist and ribs.",
                "Keep the arms in line with the shoulders and the head turning with the chest.",
                "Sit on a towel or cross the legs if you can't sit upright with straight legs.",
            ],
            Video = new("PPFkp7Aa3Rg"),
        },
        new("pilates_jackknife", "Pilates Jackknife", Abs, Bodyweight, Compound)
        {
            Secondary = [LowerBack, Triceps],
            Category = Pilates,
            Level = Advanced,
            Summary = "Roll the legs overhead, then shoot them straight up to the ceiling with the hips lifted and roll down with control.",
            Steps =
            [
                "Lie on your back with arms by your sides, palms down, legs together at 90°.",
                "Exhale as you roll the legs over until they are parallel to the floor.",
                "Inhale as you press the arms down and lift the legs and hips up toward the ceiling, body as vertical as you can.",
                "Exhale as you roll down one vertebra at a time, keeping the legs vertical, then lower them to 90°; repeat 3–5 times.",
            ],
            Tips =
            [
                "Weight on the shoulders and arms, not the neck; keep the head still.",
                "Reach the legs up, not back over your face.",
                "Not for anyone with neck or disc problems.",
            ],
            Video = new("SXYp9AJ1uWM"),
        },

        // Side Kick Series
        new("pilates_side_kick_front_back", "Side Kick Front and Back", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Abs, Hamstrings],
            Category = Pilates,
            Summary = "Lying on your side, kick the top leg forward with a double pulse and sweep it back, keeping the torso still to work the hips and core.",
            Steps =
            [
                "Lie on your side along the back edge of the mat, head propped on your hand, legs angled slightly forward.",
                "Lift the top leg to hip height.",
                "Kick it forward with two pulses as you inhale, foot flexed.",
                "Exhale as you sweep it back behind you with a pointed foot; repeat 8–10 times, then switch sides.",
            ],
            Tips =
            [
                "Keep the waist lifted off the mat and the hips stacked; only the leg moves.",
                "Don't arch the lower back as the leg goes behind; stop where the pelvis stays still.",
            ],
            Video = new("nG9JfDHJJlY"),
        },
        new("pilates_side_kick_up_down", "Side Kick Up and Down", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Abs],
            Category = Pilates,
            Summary = "In the side kick position, kick the top leg up to the ceiling and lower it slowly with resistance, working the outer hip.",
            Steps =
            [
                "Lie on your side, head propped on your hand, legs slightly forward of your body.",
                "Turn the top leg out slightly and inhale as you kick it up toward the ceiling with a pointed foot.",
                "Flex the foot and exhale as you lower it slowly, as if pressing against resistance.",
                "Repeat 6–8 times, then switch sides.",
            ],
            Tips =
            [
                "Keep the hips stacked and the bottom waist lifted; don't roll back.",
                "Lower slowly; the lowering is where the work is.",
            ],
            Video = new("8DRDo8PVsLU"),
        },
        new("pilates_side_kick_circles", "Side Kick Small Circles", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Abs],
            Category = Pilates,
            Summary = "Hold the top leg at hip height and draw small, precise circles, building endurance in the outer hip.",
            Steps =
            [
                "Lie on your side, head propped on your hand, and lift the top leg to just above hip height.",
                "Draw small, brisk circles forward with the whole leg, breathing naturally.",
                "Do 8–10, then reverse for 8–10.",
                "Switch sides.",
            ],
            Tips =
            [
                "Keep the circles small and the torso completely still.",
                "Reach the leg long out of the hip rather than gripping it up.",
            ],
            Video = new("J1aGomkLcS0"),
        },

        // Classical mat order (continued)
        new("pilates_teaser", "Teaser", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Level = Intermediate,
            Summary = "Roll up into a V-sit balance with legs and arms reaching on the diagonal, then roll down with control: Pilates' signature test of core strength.",
            Steps =
            [
                "Lie on your back with legs together extended at about 45° and arms reaching overhead.",
                "Inhale as you float the arms up, then exhale as you roll up through the spine, reaching your hands toward your toes.",
                "Balance in a V with the chest lifted and the spine long.",
                "Inhale to hold, then exhale as you roll down one vertebra at a time, keeping the legs up; repeat 3–5 times.",
            ],
            Tips =
            [
                "Use the abdominals to articulate up, not a swing of the arms.",
                "Start with Teaser with one leg extended or knees bent before going to both legs straight.",
                "Lift the chest at the top so you're not slumped on the tailbone.",
            ],
            Video = new("9WFOlfrqWo8"),
        },
        new("pilates_hip_circles", "Pilates Hip Circles", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders],
            Category = Pilates,
            Level = Advanced,
            Summary = "Lean back on straight arms and circle straight legs together in a large arc, challenging the abdominals and shoulder stability.",
            Steps =
            [
                "Sit and lean back on your hands behind you, fingers pointing back, legs lifted together in a high V.",
                "Inhale as you circle the legs to the right and down.",
                "Exhale as you sweep them around and back up to the top on the left.",
                "Reverse direction each time; do 3 each way.",
            ],
            Tips =
            [
                "Keep the chest lifted and the shoulders away from the ears; don't sink into the wrists.",
                "Make the circle only as big as you can keep the back from arching.",
            ],
            Video = new("dN1elhfKzYo"),
        },
        new("pilates_swimming", "Pilates Swimming", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Glutes, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Lying face down with arms and legs lifted, flutter opposite arm and leg quickly as if swimming, strengthening the back extensors.",
            Steps =
            [
                "Lie face down with arms overhead and legs long, hip width.",
                "Lift your head, chest, arms and legs off the mat, lengthening in both directions.",
                "Flutter opposite arm and leg up and down in a small, quick range.",
                "Inhale for 5 counts and exhale for 5; continue for 2–3 rounds, then rest back in child's pose.",
            ],
            Tips =
            [
                "Keep the abdominals lifted and reach long rather than high so the lower back isn't crunched.",
                "Keep your gaze down to the mat to protect the neck.",
            ],
            Video = new("bY6ZyiO_7ek"),
        },
        new("pilates_leg_pull_front", "Leg Pull Front", Abs, Bodyweight, Compound)
        {
            Secondary = [Glutes, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Hold a full plank and lift one leg up and down at a time, rocking the heels, to train the core, shoulders and hip extensors.",
            Steps =
            [
                "Come into a plank on straight arms, hands under shoulders, legs together.",
                "Inhale as you lift one leg up with a pointed foot.",
                "Flex the foot and rock back through the standing heel as you exhale.",
                "Point and lower the leg back down; alternate legs 3–5 times each.",
            ],
            Tips =
            [
                "Keep the pelvis level and the spine long from head to heels; don't sag or pike.",
                "Push the floor away so the shoulder blades don't wing.",
            ],
            Video = new("wOldVi15tGs"),
        },
        new("pilates_leg_pull_back", "Leg Pull Back", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Triceps, Shoulders],
            Category = Pilates,
            Level = Advanced,
            Summary = "Hold a reverse plank facing up and kick one straight leg to the ceiling at a time, working the back of the body and the shoulders.",
            Steps =
            [
                "Sit with legs long, hands behind your hips with fingers toward your feet, and lift your hips into a reverse plank.",
                "Inhale as you kick one leg up toward the ceiling with a pointed foot.",
                "Exhale as you flex the foot and lower it back down.",
                "Alternate legs 3–5 times each, then lower your hips.",
            ],
            Tips =
            [
                "Keep the hips high and the body straight; don't let them drop as the leg lifts.",
                "Keep the chest open and the shoulders down away from the ears.",
                "Skip it if you have wrist or shoulder pain in this position.",
            ],
            Video = new("f_gzE5tOPQw"),
        },
        new("pilates_kneeling_side_kick", "Kneeling Side Kick", Glutes, Bodyweight, Isolation)
        {
            Secondary = [Abs, Shoulders],
            Category = Pilates,
            Level = Advanced,
            Summary = "From kneeling with one hand on the mat and the other behind the head, kick the top leg forward and back at hip height.",
            Steps =
            [
                "Kneel up, then side bend to place one hand on the mat under your shoulder; put the other hand behind your head.",
                "Extend the top leg out to hip height.",
                "Inhale as you kick it forward, exhale as you sweep it back.",
                "Repeat 4–6 times, then switch sides.",
            ],
            Tips =
            [
                "Keep the body in one long line from the kneeling knee to the head; don't let the hips sag forward.",
                "Use a pad under the kneeling knee.",
            ],
            Video = new("hgLDMHCcw4k"),
        },
        new("pilates_side_bend", "Pilates Side Bend", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders],
            Category = Pilates,
            Level = Advanced,
            Summary = "From a side sit, lift into a full side plank with the top arm arching overhead, then lower with control, working the obliques.",
            Steps =
            [
                "Sit on one hip with knees bent, top foot crossed in front of the bottom foot, and the supporting hand under your shoulder.",
                "Inhale as you press up into a side plank, straightening the legs and sweeping the top arm overhead into an arch.",
                "Exhale as you lower the hips back toward the mat, bringing the top arm down by your side.",
                "Repeat 3–5 times, then switch sides.",
            ],
            Tips =
            [
                "Lift from the underside of the waist and push the floor away with the supporting hand.",
                "Don't let the top shoulder roll forward; stay in one plane.",
            ],
            Video = new("zprsJDjeONM"),
        },
        new("pilates_boomerang", "Boomerang", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders],
            Category = Pilates,
            Level = Advanced,
            Summary = "A flowing advanced sequence: roll over with crossed legs, switch them, roll up into teaser, sweep the arms behind and fold forward.",
            Steps =
            [
                "Sit tall with legs straight and crossed at the ankles, hands by your hips.",
                "Exhale as you roll back and lift the legs over your head; switch the cross of the ankles.",
                "Inhale as you roll up into a Teaser balance with arms reaching forward.",
                "Hold the balance as you clasp the hands behind your back, then lower the legs and fold forward, circling the arms around to the start.",
                "Repeat 4–6 times, alternating the cross.",
            ],
            Tips =
            [
                "Keep each phase smooth and controlled, like a single movement.",
                "Learn Roll-Over and Teaser well before attempting this.",
            ],
            Video = new("SRonJI25raE", 0, 108),
        },
        new("pilates_seal", "Seal", Abs, Bodyweight, Compound)
        {
            Category = Pilates,
            Summary = "A playful rolling exercise: hold your ankles from the inside, clap the feet three times, roll back and up again in balance.",
            Steps =
            [
                "Sit with knees open and soles of the feet together, arms threaded inside the legs to hold the outsides of your ankles.",
                "Lift the feet and balance just behind your sit bones in a C-curve.",
                "Clap the feet together three times.",
                "Inhale as you roll back to the shoulder blades and clap three times; exhale as you roll up to balance and clap again; repeat 6–8 times.",
            ],
            Tips =
            [
                "Keep the head and spine in the same rounded shape throughout.",
                "Never roll onto the neck.",
            ],
            Video = new("G5zO03AJlwU"),
        },
        new("pilates_control_balance", "Control Balance", Abs, Bodyweight, Compound)
        {
            Secondary = [Glutes, Hamstrings],
            Category = Pilates,
            Level = Advanced,
            Summary = "From a Roll-Over position, hold one foot overhead and lift the other leg to the ceiling, switching legs with control.",
            Steps =
            [
                "Lie on your back and roll the legs over your head until the toes touch the mat behind you, arms reaching overhead.",
                "Hold one ankle with both hands and lift the other leg straight up to the ceiling.",
                "Switch legs in a scissor action, exhaling as you change.",
                "Do 3–4 each side, then roll down slowly.",
            ],
            Tips =
            [
                "Keep the weight on the shoulder blades and the hips stacked over the shoulders.",
                "Reach the lifted leg long and up; don't let the hips sink.",
                "Avoid with any neck problem.",
            ],
            Video = new("jy4GQNfbmfQ"),
        },
        new("pilates_push_up", "Pilates Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Abs, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "The closing exercise of the mat: roll down from standing, walk out to plank, do push-ups with the elbows hugging in, then walk back and roll up.",
            Steps =
            [
                "Stand tall in Pilates stance, then nod and roll down through the spine until your hands reach the mat.",
                "Walk the hands out to a plank.",
                "Do 3 push-ups, bending the elbows straight back alongside the ribs.",
                "Lift the hips, walk the hands back to the feet and roll up to standing; repeat 3 times.",
            ],
            Tips =
            [
                "Keep the body in one straight line in the plank; don't sag or pike.",
                "Bend the knees as you roll down if your hamstrings are tight.",
            ],
            Video = new("4wS_wf12Ezk"),
        },

        // Contemporary mat
        new("pilates_pelvic_curl", "Pelvic Curl", Glutes, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Abs],
            Category = Pilates,
            Summary = "Peel the spine up off the mat into a bridge one vertebra at a time and lay it back down, teaching spinal articulation and glute control.",
            Steps =
            [
                "Lie on your back with knees bent, feet hip width and flat, arms by your sides.",
                "Exhale as you tilt the pelvis to flatten the low back, then curl the tailbone, hips and spine up off the mat.",
                "Inhale at the top with a straight line from knees to shoulders.",
                "Exhale as you roll down from the upper back to the tailbone; repeat 6–10 times.",
            ],
            Tips =
            [
                "Lead with the pelvis and peel up bone by bone, not as a flat block.",
                "Don't arch the lower back or push the ribs up at the top.",
            ],
            Video = new("fOOypTBlke8"),
        },
        new("pilates_chest_lift", "Chest Lift", Abs, Bodyweight, Isolation)
        {
            Category = Pilates,
            Summary = "The foundational Pilates abdominal curl: lift the head and shoulders on the exhale with a scooped belly and lower on the inhale.",
            Steps =
            [
                "Lie on your back with knees bent, feet flat, hands stacked behind your head and elbows wide.",
                "Inhale to prepare.",
                "Exhale as you nod the chin and curl the head and shoulders up to the base of the shoulder blades.",
                "Inhale as you lower with control; repeat 8–10 times.",
            ],
            Tips =
            [
                "Draw the navel down toward the spine as you lift; the belly should hollow, not bulge.",
                "Keep a small space between chin and chest and don't pull on the head.",
            ],
            Video = new("DPcjqmCGKKI"),
        },
        new("pilates_toe_taps", "Pilates Toe Taps", Abs, Bodyweight, Isolation)
        {
            Category = Pilates,
            Summary = "From tabletop, lower one foot at a time to tap the mat while the pelvis stays still, training deep abdominal control.",
            Steps =
            [
                "Lie on your back with your spine in neutral and legs in tabletop, knees over hips at 90°.",
                "Inhale to prepare.",
                "Exhale as you lower one foot to tap the mat, keeping the knee angle the same.",
                "Inhale as you return it to tabletop; alternate legs 8–10 times each.",
            ],
            Tips =
            [
                "Keep the pelvis level and the lower back still; the movement is from the hip only.",
                "Progress to both legs together only when one leg is effortless.",
            ],
            Video = new("KAs2Cgp85LU"),
        },
        new("pilates_clam", "Pilates Clam", Glutes, Bodyweight, Isolation)
        {
            Category = Pilates,
            Summary = "Lying on your side with knees bent, open the top knee like a clamshell while the feet stay together, targeting the glute medius.",
            Steps =
            [
                "Lie on your side with hips and knees bent, heels in line with your seat, head resting on your arm.",
                "Stack the hips and lift the bottom waist slightly off the mat.",
                "Exhale as you open the top knee, keeping the feet together.",
                "Inhale as you close it with control; repeat 10–15 times, then switch sides.",
            ],
            Tips =
            [
                "Don't let the top hip roll back; open only as far as the pelvis stays still.",
                "Add a band around the thighs for more resistance.",
            ],
            Video = new("udZmLr8Fly4"),
        },
        new("pilates_side_lying_double_leg_lift", "Side-Lying Double Leg Lift", Abs, Bodyweight, Compound)
        {
            Secondary = [Glutes, LowerBack],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Lying on your side, lift both legs together off the mat and lower them, working the obliques and side of the trunk.",
            Steps =
            [
                "Lie on your side in a straight line, head resting on your extended bottom arm, top hand on the mat in front.",
                "Squeeze the legs together.",
                "Exhale as you lift both legs off the mat a few inches.",
                "Inhale as you lower them without touching down; repeat 8–10 times, then switch sides.",
            ],
            Tips =
            [
                "Lift from the waist; keep the hips stacked and don't roll forward or back.",
                "Keep the lift small and controlled rather than swinging.",
            ],
            Video = new("3YcYZvnAYz4"),
        },
        new("pilates_side_lying_inner_thigh_lift", "Side-Lying Inner Thigh Lift", Quads, Bodyweight, Isolation)
        {
            Category = Pilates,
            Summary = "With the top leg crossed in front, lift and lower the straight bottom leg to work the inner thigh (adductors).",
            Steps =
            [
                "Lie on your side, head propped on your hand, and place the top foot on the mat in front of the bottom knee.",
                "Straighten the bottom leg with the foot flexed.",
                "Exhale as you lift the bottom leg off the mat.",
                "Inhale as you lower it without fully resting; repeat 10–15 times, then switch sides.",
            ],
            Tips =
            [
                "Reach the leg long from the hip and keep the torso still.",
                "Lead with the inner heel rather than the toes.",
            ],
            Video = new("_BR4ZodkJ2M"),
        },
        new("pilates_swan_prep", "Swan Prep", LowerBack, Bodyweight, Compound)
        {
            Secondary = [Back, Glutes],
            Category = Pilates,
            Summary = "A gentle back extension from lying face down, lifting the head and chest with the hands lightly on the mat.",
            Steps =
            [
                "Lie face down with hands by your shoulders, elbows bent close to the body, legs hip width.",
                "Draw the abdominals up off the mat and lengthen the legs.",
                "Inhale as you lengthen the crown of the head forward and lift the head and chest, lightly pressing the hands.",
                "Exhale as you lower with control; repeat 6–8 times.",
            ],
            Tips =
            [
                "Lift from the upper back and keep the lower back long; don't push up with the arms alone.",
                "Keep the neck in line with the spine and the shoulders away from the ears.",
            ],
            Video = new("KAotX1bDGps"),
        },
        new("pilates_mermaid", "Mermaid", Abs, Bodyweight, Compound)
        {
            Secondary = [Back],
            Category = Pilates,
            Summary = "A seated side bend in a Z-sit with the arm reaching overhead, stretching and strengthening the sides of the body.",
            Steps =
            [
                "Sit in a Z-sit with both knees bent to one side, holding your ankles with the near hand.",
                "Inhale as you reach the other arm up and over into a side bend away from your feet.",
                "Exhale to return to tall, then place that hand on the mat and reach the first arm overhead to bend the other way.",
                "Repeat 3–5 times, then switch the legs to the other side.",
            ],
            Tips =
            [
                "Keep both sit bones as grounded as you can and bend sideways in one plane.",
                "Lift up before you bend over so you're not collapsing into the waist.",
            ],
            Video = new("msZN5n5J69A"),
        },

        // Reformer
        new("pilates_reformer_footwork", "Reformer Footwork", Quads, Other, Compound)
        {
            Secondary = [Glutes, Hamstrings, Calves],
            Category = Pilates,
            Summary = "The reformer warm-up: lying on the carriage with feet on the footbar, press out and return in a series of foot positions (toes, arches, heels, V).",
            Steps =
            [
                "Lie on the carriage with your head on the headrest, shoulders against the blocks and feet on the footbar.",
                "Place the feet in the first position: heels together, toes on the bar in a small V (toes, arches, heels and wide positions follow).",
                "Exhale as you press the carriage out until the legs are straight.",
                "Inhale as you control it back in; do 10 in each foot position.",
            ],
            Tips =
            [
                "Keep the pelvis neutral and still; don't let the tailbone curl up as the carriage returns.",
                "Straighten the legs fully without locking the knees.",
                "Resist the springs on the way in; don't let the carriage slam home.",
            ],
            Video = new("QDBzjlhKsco"),
        },
        new("pilates_reformer_hundred", "Reformer Hundred", Abs, Other, Compound)
        {
            Secondary = [Back, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "The Hundred lying on the carriage holding the straps, so the arms pump against spring resistance.",
            Steps =
            [
                "Lie on the carriage holding the straps, arms reaching up toward the ceiling, legs in tabletop.",
                "Exhale as you press the arms down by your sides, curl the head and chest up and extend the legs to about 45°.",
                "Pump the arms up and down in a small range, inhaling for 5 and exhaling for 5.",
                "Complete 10 breath cycles, then bend the knees in and lower the head.",
            ],
            Tips =
            [
                "Keep the straps taut and the shoulders down.",
                "Lower the legs only as far as the lower back stays steady.",
            ],
            Video = new("vx4DdBWY6kQ", 166, 302),
        },
        new("pilates_reformer_frog", "Reformer Frog", Quads, Other, Compound)
        {
            Secondary = [Glutes, Hamstrings],
            Category = Pilates,
            Summary = "Lying with feet in the straps, heels together and knees open, press the legs out on the diagonal and draw them back in, working the inner thighs and hips.",
            Steps =
            [
                "Lie on the carriage with the straps around the arches of your feet, heels together and knees wide.",
                "Keep the pelvis neutral and the back of the ribs heavy.",
                "Exhale as you press the legs out on a diagonal until they're straight, squeezing the heels and inner thighs together.",
                "Inhale as you bend back to frog with control; repeat 8–10 times.",
            ],
            Tips =
            [
                "Keep the knees no wider than you can control and the tailbone down.",
                "Keep the legs at a height where your back doesn't arch.",
            ],
            Video = new("B1PoTakYxgY", 45, 82),
        },
        new("pilates_reformer_leg_circles", "Reformer Leg Circles", Quads, Other, Isolation)
        {
            Secondary = [Glutes, Hamstrings],
            Category = Pilates,
            Summary = "With both feet in the straps, circle the straight legs together against the springs, working the hips and inner thighs with a stable pelvis.",
            Steps =
            [
                "Lie on the carriage with straps on the arches of both feet, legs straight up toward the ceiling.",
                "Open the legs wide and lower them down.",
                "Bring them together at the bottom and lift them back up to the start.",
                "Do 5–8 circles, then reverse.",
            ],
            Tips =
            [
                "Keep the pelvis still and the lower back steady; make the circle smaller if it rocks.",
                "Keep tension in the straps throughout.",
            ],
            Video = new("C3yKCk386MQ"),
        },
        new("pilates_reformer_short_spine", "Reformer Short Spine", Abs, Other, Compound)
        {
            Secondary = [Hamstrings, Glutes],
            Category = Pilates,
            Level = Intermediate,
            Summary = "With feet in the straps, lift the legs over and roll the spine up off the carriage, bend the knees, then roll down bone by bone.",
            Steps =
            [
                "Lie on the carriage with straps on your feet and the legs extended at about 45°.",
                "Exhale as you lift the legs overhead and roll the hips and spine up until the legs are parallel to the floor.",
                "Bend the knees toward your shoulders, heels together, knees apart.",
                "Inhale, then exhale as you roll the spine down one vertebra at a time, keeping the heels close to your seat, and extend the legs to the start; repeat 4–6 times.",
            ],
            Tips =
            [
                "Keep the weight on the shoulder blades, never the neck.",
                "Articulate slowly on the way down; that's where the spinal mobility work is.",
                "Skip with neck or disc problems.",
            ],
            Video = new("AOUX2a-in0I"),
        },
        new("pilates_reformer_long_stretch", "Reformer Long Stretch", Abs, Other, Compound)
        {
            Secondary = [Shoulders, Chest, Triceps],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Hold a plank with hands on the footbar and feet against the shoulder blocks, pushing the carriage back and pulling it forward from the shoulders.",
            Steps =
            [
                "Kneel on the carriage, place your hands on the footbar and step back into a plank with the balls of the feet against the shoulder blocks.",
                "Inhale as you push the carriage back by moving at the shoulders, keeping the body in one line.",
                "Exhale as you draw the carriage forward until the shoulders are over the wrists.",
                "Repeat 4–6 times, then step down carefully.",
            ],
            Tips =
            [
                "Keep the body as one rigid plank; the movement comes only from the shoulders.",
                "Don't let the hips sag or pike.",
            ],
            Video = new("BfRY99Ka-zQ"),
        },
        new("pilates_reformer_elephant", "Reformer Elephant", Abs, Other, Compound)
        {
            Secondary = [Hamstrings, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Standing on the carriage in a pike with hands on the footbar, push the carriage back and forth with the legs while the upper body stays still.",
            Steps =
            [
                "Stand on the carriage with heels against the shoulder blocks and hands on the footbar, hips lifted into an inverted V.",
                "Round the back slightly and draw the abdominals in.",
                "Inhale as you push the carriage back with the legs.",
                "Exhale as you pull it forward using the abdominals; repeat 6–8 times.",
            ],
            Tips =
            [
                "The upper body and hips stay where they are; only the legs move the carriage.",
                "Bend the knees slightly if your hamstrings are tight.",
            ],
            Video = new("OxdhCdThIcg"),
        },
        new("pilates_reformer_knee_stretches", "Reformer Knee Stretches", Abs, Other, Compound)
        {
            Secondary = [Glutes, Quads],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Kneeling on the carriage with hands on the footbar, drive the carriage back and forth with the legs while the trunk holds a fixed shape (round, flat or knees off).",
            Steps =
            [
                "Kneel on the carriage with your feet against the shoulder blocks and hands on the footbar.",
                "Round your spine into a C-curve with the abdominals drawn in.",
                "Push the carriage back with the legs and draw it in again, quickly and rhythmically, exhaling as you push.",
                "Do 8–10, then repeat with a flat back, and with the knees lifted for the advanced version.",
            ],
            Tips =
            [
                "The upper body stays still; the movement is from the hips.",
                "Don't let the carriage slam back into the stopper.",
            ],
            Video = new("JM9SMOrIzJg"),
        },
        new("pilates_reformer_rowing", "Reformer Rowing", Back, Other, Compound)
        {
            Secondary = [Shoulders, Abs],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Seated on the carriage holding the straps, a series of arm patterns (from the chest, from the hips, shaving, hug) that combine spinal flexion and extension with upper-body strength.",
            Steps =
            [
                "Sit tall on the carriage facing away from the footbar, legs straight, holding the straps at your chest.",
                "Exhale as you round back slightly and press the hands forward and down.",
                "Inhale as you sit tall and circle the arms up and around to the sides.",
                "Return to the start; repeat 4–5 times, then move through the other rowing variations.",
            ],
            Tips =
            [
                "Keep the shoulders down and the movement led by the trunk, not just the arms.",
                "Use a light spring; this is about control, not load.",
            ],
            Video = new("2GQzUj38xhs"),
        },
        new("pilates_reformer_stomach_massage", "Reformer Stomach Massage", Abs, Other, Compound)
        {
            Secondary = [Quads, Calves],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Seated on the carriage with feet on the footbar, push out and back while holding a rounded, then flat, then twisted trunk position.",
            Steps =
            [
                "Sit on the front of the carriage, toes on the footbar, hands on the edge of the frame behind you, spine rounded.",
                "Exhale as you press the carriage out by straightening the legs.",
                "Lower the heels under the bar, lift them again and bend to return.",
                "Do 8–10, then repeat with hands back and chest lifted, and with a twist.",
            ],
            Tips =
            [
                "Hold the trunk position the whole time; only the legs move.",
                "Draw the abdominals in deeply in the rounded version.",
            ],
            Video = new("Wt_horWSoA4"),
        },
        new("pilates_reformer_side_splits", "Reformer Side Splits", Quads, Other, Isolation)
        {
            Secondary = [Glutes, Abs],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Standing with one foot on the frame and one on the carriage, slide the legs apart and pull them back together, working the inner and outer thighs.",
            Steps =
            [
                "Stand side-on with one foot on the standing platform and the other on the carriage against the shoulder block, arms out to the sides.",
                "Find a tall spine with weight even between both feet.",
                "Inhale as you press the carriage out by opening both legs.",
                "Exhale as you squeeze the inner thighs to bring it back in; repeat 8–10 times, then turn and repeat facing the other way.",
            ],
            Tips =
            [
                "Keep the hips level and stacked over the feet.",
                "Use a light spring and open only as far as you can control the return.",
            ],
            Video = new("OirVxTic1B4", 5.5380001, 50.759998),
        },
        new("pilates_reformer_mermaid", "Reformer Mermaid", Abs, Other, Compound)
        {
            Secondary = [Back, Shoulders],
            Category = Pilates,
            Summary = "Sitting sideways on the carriage with one hand on the footbar, push the carriage out into a long side bend, then stretch the other way.",
            Steps =
            [
                "Sit sideways on the carriage in a Z-sit or cross-legged, with the near hand on the footbar.",
                "Inhale as you push the carriage out and reach the other arm overhead into a long side bend toward the bar.",
                "Exhale as you return to sitting tall.",
                "Then bend away from the bar, keeping the hand on it; repeat 3–5 times and switch sides.",
            ],
            Tips =
            [
                "Bend sideways in one plane; don't twist or collapse forward.",
                "Keep the bottom sit bones grounded as long as you can.",
            ],
            Video = new("h6RX7NeQXQA"),
        },
        new("pilates_reformer_chest_expansion", "Reformer Chest Expansion", Back, Other, Isolation)
        {
            Secondary = [Triceps, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Kneeling on the carriage facing the straps, pull the arms straight back past the hips, turn the head side to side and return with control.",
            Steps =
            [
                "Kneel on the carriage facing the straps, holding them in front of you with straight arms, body upright.",
                "Inhale as you pull the arms back past your hips.",
                "Hold the breath as you look right, then left, then centre.",
                "Exhale as you return the arms forward with control; repeat 4–6 times.",
            ],
            Tips =
            [
                "Kneel tall from knees to head; don't lean back to help the arms.",
                "Keep the chest open and the shoulders down.",
            ],
            Video = new("ILOap3w7A1A"),
        },
        new("pilates_reformer_arms_supine", "Reformer Arms in Straps", Back, Other, Isolation)
        {
            Secondary = [Triceps, Chest],
            Category = Pilates,
            Summary = "Lying on the carriage holding the straps, press the arms down to the sides and through other patterns (circles, triceps, adduction) with a stable trunk.",
            Steps =
            [
                "Lie on the carriage holding the straps, arms reaching up toward the ceiling, legs in tabletop or bent with feet on the headrest.",
                "Exhale as you press the arms down by your hips.",
                "Inhale as you let them rise back to vertical with control.",
                "Do 8–10, then move on to arm circles, triceps presses and side presses.",
            ],
            Tips =
            [
                "Keep the ribs heavy and the back still; don't arch as the arms rise.",
                "Keep the straps in line with your body and the shoulders down.",
            ],
            Video = new("qtBU9R5cxGk"),
        },
        new("pilates_reformer_running", "Reformer Running", Calves, Other, Isolation)
        {
            Secondary = [Quads],
            Category = Pilates,
            Summary = "Part of the footwork series: with the legs straight and the carriage out, alternately lower one heel under the footbar as the other knee bends, like running in place.",
            Steps =
            [
                "Lie on the carriage, press out with the balls of both feet on the footbar until the legs are straight.",
                "Lower one heel under the bar while bending the other knee.",
                "Switch, keeping the carriage still.",
                "Alternate rhythmically for 20–30 steps, then bend both knees to return.",
            ],
            Tips =
            [
                "The carriage stays still; the pelvis stays level.",
                "Get a full stretch of the calf on the straight leg each time.",
            ],
            Video = new("7vQScVpLNY8"),
        },
        new("pilates_reformer_bridging", "Reformer Bridging", Glutes, Other, Compound)
        {
            Secondary = [Hamstrings, Abs],
            Category = Pilates,
            Summary = "With feet on the footbar, roll up into a bridge and then press the carriage out and in from the bridge, working the glutes and hamstrings.",
            Steps =
            [
                "Lie on the carriage with the balls of the feet or heels on the footbar, hip width.",
                "Exhale as you curl the pelvis and spine up into a bridge.",
                "Hold the bridge as you press the carriage out and draw it back in 6–8 times.",
                "Roll the spine down one vertebra at a time.",
            ],
            Tips =
            [
                "Keep the hips high and level as the carriage moves.",
                "Draw the carriage in with the hamstrings; don't let it pull you.",
            ],
            Video = new("NzlmrqN8nCc"),
        },
        new("pilates_reformer_pulling_straps", "Reformer Pulling Straps", Back, Other, Compound)
        {
            Secondary = [LowerBack, Shoulders, Triceps],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Lying face down on the long box holding the straps, pull the straight arms back alongside the body as the chest lifts into extension.",
            Steps =
            [
                "Place the long box on the carriage and lie on it face down, chest just past the front edge, holding the straps with straight arms.",
                "Lengthen the legs and draw the abdominals up.",
                "Inhale as you pull the straps back alongside your body and lift the head and chest.",
                "Exhale as you lower and return the arms forward with control; repeat 6–8 times.",
            ],
            Tips =
            [
                "Lift from the upper back; keep the lower back long and the legs on the box.",
                "Keep the arms straight and the shoulders away from the ears.",
            ],
            Video = new("lmNMMDW90Bc"),
        },
        new("pilates_reformer_backstroke", "Reformer Backstroke", Abs, Other, Compound)
        {
            Secondary = [Shoulders, Back],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Seated on the long box in a ball holding the straps, reach the arms and legs to the ceiling, open them wide and circle back to the ball.",
            Steps =
            [
                "Sit on the long box facing the footbar, roll back into a ball with knees to your chest and hands holding the straps by your forehead.",
                "Inhale as you reach the arms and legs toward the ceiling.",
                "Exhale as you open the arms and legs wide and circle them down and around.",
                "Draw back into the ball; repeat 4–6 times.",
            ],
            Tips =
            [
                "Keep the head and chest curled up throughout.",
                "Lower the legs only as far as your back stays still.",
            ],
            Video = new("VzMG_pzUEmA", 17.629999, 65),
        },
        new("pilates_reformer_teaser", "Reformer Teaser", Abs, Other, Compound)
        {
            Category = Pilates,
            Level = Advanced,
            Summary = "Teaser on the reformer: balancing in a V-sit on the carriage or box, holding the straps, and rolling down and up against the springs.",
            Steps =
            [
                "Sit on the carriage or long box holding the straps and roll up into a Teaser V with legs at about 45°.",
                "Exhale as you roll the spine down partway, keeping the legs still.",
                "Inhale to hold, then exhale to roll back up to the V with arms reaching toward the feet.",
                "Repeat 3–5 times, then lower the legs and sit up.",
            ],
            Tips =
            [
                "Use a light spring; the springs make the roll-down feel heavier than on the mat.",
                "Keep the legs still and the chest lifted at the top.",
            ],
            Video = new("6j_r7O7FgRs", 24.959999, 104.36),
        },

        // Cadillac / tower
        new("pilates_cadillac_roll_down", "Cadillac Roll-Down Bar", Abs, Other, Compound)
        {
            Secondary = [Back],
            Category = Pilates,
            Summary = "Sitting facing the roll-down bar, roll back through the spine against the spring and return, an assisted way to learn the Roll-Up.",
            Steps =
            [
                "Sit facing the tower with knees bent, feet against the poles and hands on the roll-down bar, spine tall.",
                "Exhale as you tuck the pelvis and roll back one vertebra at a time to the base of the shoulder blades.",
                "Inhale to hold the curl.",
                "Exhale as you curl forward and roll back up to sitting; repeat 6–8 times.",
            ],
            Tips =
            [
                "Let the spring support you but keep the abdominals doing the articulating.",
                "Keep the arms straight and the shoulders down.",
            ],
            Video = new("7ZFi1eX9z_0"),
        },
        new("pilates_cadillac_leg_springs", "Cadillac Leg Springs", Hamstrings, Other, Compound)
        {
            Secondary = [Glutes, Quads, Abs],
            Category = Pilates,
            Summary = "Lying on the Cadillac with feet in loops attached to springs, work the legs through lowers, frogs, circles and walking against the springs.",
            Steps =
            [
                "Lie on your back with your head toward the tower, feet in the loops of the leg springs, hands on the poles.",
                "Start with straight legs toward the ceiling.",
                "Exhale as you press the legs down toward the mat, only as low as the pelvis stays still.",
                "Inhale as you let them rise to vertical with control; continue with frog, circles and walking.",
            ],
            Tips =
            [
                "Keep the pelvis neutral and the lower back steady throughout.",
                "Control the return; don't let the springs pull the legs up.",
            ],
            Video = new("nK1wjbGY-5k"),
        },
        new("pilates_cadillac_push_through", "Cadillac Push-Through", Back, Other, Compound)
        {
            Secondary = [Abs, LowerBack, Shoulders],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Sitting facing the tower, push the bar forward and down into a spine stretch, then roll up and lift the bar overhead into extension.",
            Steps =
            [
                "Sit facing the tower with legs straight in a V, holding the push-through bar overhead.",
                "Exhale as you round forward and push the bar forward and down.",
                "Inhale as you roll back up to sitting tall, letting the bar rise overhead.",
                "Lift the chest and extend slightly back with the bar, then return to tall; repeat 4–6 times.",
            ],
            Tips =
            [
                "Keep the sit bones grounded and the shoulders away from the ears.",
                "Check the spring and safety strap are secure before you start.",
            ],
            Video = new("NST6Oe7a51A"),
        },

        // Wunda chair
        new("pilates_chair_footwork", "Chair Footwork", Quads, Other, Compound)
        {
            Secondary = [Glutes, Calves],
            Category = Pilates,
            Summary = "Seated tall on the Wunda chair, press the pedal down and up with the feet in several positions, training the legs and an upright spine.",
            Steps =
            [
                "Sit tall on the front edge of the chair with the balls of the feet on the pedal, knees together.",
                "Exhale as you press the pedal down to the floor.",
                "Inhale as you control it back up.",
                "Do 10 in each foot position: toes, heels, V, and wide.",
            ],
            Tips =
            [
                "Keep the spine tall and still; don't lean back to press.",
                "Resist the spring on the way up.",
            ],
            Video = new("OSU3gDjHSgA"),
        },
        new("pilates_chair_pike", "Chair Pike", Abs, Other, Compound)
        {
            Secondary = [Shoulders, Triceps],
            Category = Pilates,
            Level = Advanced,
            Summary = "Standing behind the chair with hands on the pedal, lift the hips high into a pike as the pedal rises, then lower with control.",
            Steps =
            [
                "Stand behind the chair, place your hands on the pedal and press it down, hips over your shoulders.",
                "Round slightly and draw the abdominals in deeply.",
                "Exhale as you lift the hips higher and the feet off the floor into a pike, letting the pedal rise.",
                "Inhale as you lower with control and return the feet to the floor; repeat 4–6 times.",
            ],
            Tips =
            [
                "Lift from the abdominals, not by jumping.",
                "Keep the arms straight and the shoulders stable over the hands.",
            ],
            Video = new("S82d3pR6pG0", 65, 121),
        },
        new("pilates_chair_swan", "Chair Swan", LowerBack, Other, Compound)
        {
            Secondary = [Back, Shoulders, Triceps],
            Category = Pilates,
            Level = Intermediate,
            Summary = "Lying face down on the chair seat with hands on the pedal, lift the chest into extension as the pedal rises and lower as you press it down.",
            Steps =
            [
                "Lie face down on the chair seat with your hips on the edge, legs long behind you and hands on the pedal.",
                "Press the pedal down with straight arms and lengthen the spine.",
                "Inhale as you let the pedal rise and lift the head and chest into extension.",
                "Exhale as you press the pedal down and return to a long spine; repeat 4–6 times.",
            ],
            Tips =
            [
                "Lengthen through the crown before lifting; don't crunch into the lower back.",
                "Keep the shoulders down and the abdominals engaged.",
            ],
            Video = new("nfSnHYetY3w"),
        },

        // Magic circle and small ball
        new("pilates_circle_inner_thigh_squeeze", "Magic Circle Inner Thigh Squeeze", Quads, Other, Isolation)
        {
            Secondary = [Abs],
            Category = Pilates,
            Summary = "Squeeze a Pilates ring between the thighs or ankles and release with control, working the inner thighs (adductors) and pelvic floor.",
            Steps =
            [
                "Lie on your back with knees bent, feet flat, and the ring between your inner thighs just above the knees.",
                "Inhale to prepare.",
                "Exhale as you squeeze the ring, drawing the pelvic floor and lower abdominals in.",
                "Inhale as you release slowly without letting it drop; repeat 10–15 times.",
            ],
            Tips =
            [
                "Keep the pelvis neutral and still; don't tuck as you squeeze.",
                "Add it to a bridge or chest lift for a harder version.",
            ],
            Video = new("-l1p2iLgu0A"),
        },
        new("pilates_circle_chest_press", "Magic Circle Chest Press", Chest, Other, Isolation)
        {
            Secondary = [Shoulders],
            Category = Pilates,
            Summary = "Hold a Pilates ring between the palms at chest height and press it, working the chest and the connection between the arms and core.",
            Steps =
            [
                "Sit or stand tall holding the ring between your palms at chest height, elbows slightly bent.",
                "Inhale to prepare.",
                "Exhale as you squeeze the ring, keeping the shoulders down.",
                "Inhale as you release with control; repeat 10–15 times, also at overhead and low positions.",
            ],
            Tips =
            [
                "Press with the heels of the hands, not the fingers.",
                "Don't let the shoulders hike up toward your ears.",
            ],
            Video = new("87OLIcCO8WU"),
        },
        new("pilates_small_ball_roll_back", "Small Ball Roll-Back", Abs, Other, Compound)
        {
            Category = Pilates,
            Summary = "Sitting with a small soft ball behind the lower back, roll back onto it into a curl and return, building abdominal control with support.",
            Steps =
            [
                "Sit with knees bent and feet flat, a small soft ball behind your lower back, arms reaching forward.",
                "Exhale as you tuck the pelvis and roll back into the ball, keeping a C-curve.",
                "Inhale to hold, lightly sinking into the ball.",
                "Exhale as you curl forward and return to sitting; repeat 8–10 times, adding a rotation to work the obliques.",
            ],
            Tips =
            [
                "Keep the abdominals scooped; the ball supports, it doesn't do the work.",
                "Keep the feet grounded and the neck relaxed.",
            ],
            Video = new("RRLwOuTbYKI"),
        },
    ];
}
