using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] PowerExercises() =>
    [
        // Jumps
        new("box_jump", "Box Jump", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves, Hamstrings],
            Category = Plyometric,
            Summary = "An explosive jump onto a box that trains leg power while keeping landing impact low.",
            Steps =
            [
                "Stand about a foot from a sturdy box, feet hip-width apart.",
                "Dip quickly into a quarter squat and swing your arms back.",
                "Swing your arms forward and jump, pulling your knees up to land softly on the box.",
                "Land with both feet flat, knees over toes, in a half squat, then stand up tall.",
                "Step back down one foot at a time rather than jumping off.",
            ],
            Tips =
            [
                "Choose a box you can land on in the same squat depth you took off from; higher isn't better.",
                "Land quietly with your knees tracking over your toes.",
                "Don't let your knees cave in on take-off or landing.",
                "Keep reps low and rest fully so every jump is maximal.",
            ],
            Video = new("YLPQsdRDmB0", 30.66, 73.799),
        },
        new("seated_box_jump", "Seated Box Jump", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Hamstrings, Calves],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "A box jump started from sitting on a bench, removing the countermovement to build starting power.",
            Steps =
            [
                "Sit on a bench or low box facing a jump box, feet flat and hip-width apart.",
                "Swing your arms back while staying seated.",
                "Drive your arms forward and explode off the bench, jumping onto the box.",
                "Land softly in a half squat with both feet flat, then stand up.",
                "Step down and reset on the bench.",
            ],
            Tips =
            [
                "Start from a dead stop; don't rock forward to create momentum.",
                "Use a lower box than for a standing box jump.",
                "Don't land with your knees caving in or your heels off the box.",
            ],
            Video = new("64OAo8kINPQ"),
        },
        new("depth_jump", "Depth Jump", Quads, Bodyweight, Compound)
        {
            Secondary = [Calves, Glutes, Hamstrings],
            Category = Plyometric,
            Level = Advanced,
            Summary = "A shock-method plyometric: step off a box, land, and rebound into a maximal jump as fast as possible.",
            Steps =
            [
                "Stand on the edge of a box about 30–60 cm high.",
                "Step off (don't jump off) and land on both feet at once.",
                "As soon as your feet touch, jump straight up as high as you can.",
                "Land softly, reset and climb back onto the box.",
            ],
            Tips =
            [
                "Keep ground contact short; think of the floor as hot.",
                "If you sink deep or your heels crash down, the box is too high.",
                "Don't do these fatigued or without a base of regular jumping and strength training.",
                "Keep total contacts low, around 20–40 per session.",
            ],
            Video = new("bMHL5xqKn3E"),
        },
        new("vertical_jump", "Countermovement Jump", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves, Hamstrings],
            Category = Plyometric,
            Summary = "The standard vertical jump: a quick dip and arm swing straight into a maximal jump.",
            Steps =
            [
                "Stand with feet hip-width apart and arms by your sides.",
                "Dip quickly into a quarter squat while swinging your arms back.",
                "Reverse straight away, swinging your arms up and jumping as high as you can.",
                "Land softly on the balls of your feet, sinking into your hips and knees.",
                "Reset fully before the next rep.",
            ],
            Tips =
            [
                "Make the dip fast; a slow, deep squat loses the stretch reflex.",
                "Reach overhead at the top to finish the extension.",
                "Don't land stiff-legged or let your knees cave in.",
            ],
            Video = new("QcQCihNGteE"),
        },
        new("broad_jump", "Broad Jump", Glutes, Bodyweight, Compound)
        {
            Secondary = [Quads, Hamstrings, Calves],
            Category = Plyometric,
            Summary = "A standing long jump for horizontal power through the hips.",
            Steps =
            [
                "Stand with feet hip-width apart behind a line.",
                "Swing your arms back as you hinge and bend your knees.",
                "Swing your arms forward and jump out as far as you can, driving through your hips.",
                "Bring your feet forward and land softly in a half squat, holding the landing.",
                "Walk back and reset.",
            ],
            Tips =
            [
                "Jump out at roughly 45°, not straight up or flat.",
                "Stick the landing for a second before standing; if you fall forward, jump shorter.",
                "Don't land with straight knees or let them cave in.",
            ],
            Video = new("lPM07R25-c0"),
        },
        new("tuck_jump", "Tuck Jump", Quads, Bodyweight, Compound)
        {
            Secondary = [Abs, Calves, Glutes],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "A vertical jump with the knees pulled to the chest at the top, done in quick repeated reps.",
            Steps =
            [
                "Stand with feet hip-width apart, arms bent in front of you.",
                "Dip and jump straight up.",
                "At the top, pull your knees up toward your chest.",
                "Extend your legs to land softly on the balls of your feet and go straight into the next jump.",
            ],
            Tips =
            [
                "Bring your knees up rather than folding your chest down to them.",
                "Keep ground contact short and landings quiet.",
                "Don't continue once landings get heavy or your knees start caving in.",
            ],
            Video = new("-bnJGikRGsM"),
        },
        new("split_jump", "Split Jump", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Hamstrings, Calves],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "A jumping lunge: explode up from a split stance and switch legs in the air.",
            Steps =
            [
                "Start in a lunge with your front thigh near parallel and back knee just above the floor.",
                "Drive up explosively through both legs, swinging your arms.",
                "Switch your legs in mid-air.",
                "Land softly in a lunge with the other leg in front and go straight into the next rep.",
            ],
            Tips =
            [
                "Keep your torso upright and your front knee over your mid-foot.",
                "Land with control before the next jump; don't let your back knee slam the floor.",
                "Don't let your front knee collapse inward on landing.",
            ],
            Video = new("_5kDxC0flg0"),
        },
        new("single_leg_hop", "Single-Leg Hop", Quads, Bodyweight, Compound)
        {
            Secondary = [Calves, Glutes, Hamstrings],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "Hops forward or in place on one leg to build single-leg power, stiffness and landing control.",
            Steps =
            [
                "Stand on one leg with a slight bend in the knee.",
                "Dip and hop forward (or straight up), using your arms.",
                "Land on the same leg with a soft knee and hip, holding briefly.",
                "Hop again, then switch legs after the set.",
            ],
            Tips =
            [
                "Start with a pause after each landing; only link hops once landings are stable.",
                "Keep your knee tracking over your toes and your pelvis level.",
                "Don't let your knee drift inward when you land.",
            ],
            Video = new("7WgzHOQGgYw", 33.042, 84.989998),
        },
        new("lateral_bound", "Lateral Bound", Glutes, Bodyweight, Compound)
        {
            Secondary = [Quads, Calves],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "A powerful sideways jump from one leg to the other, stuck on landing, for lateral power.",
            Steps =
            [
                "Stand on your right leg, knee and hip slightly bent.",
                "Load the hip, then push off hard to jump as far as you can to the left.",
                "Land on your left leg only, sinking into the hip and holding the landing for a second.",
                "Bound back to the right in the same way.",
            ],
            Tips =
            [
                "Push off through the whole foot and drive the hip sideways.",
                "Stick each landing balanced over the landing leg before the next bound.",
                "Don't let the landing knee cave in or your torso fold over.",
            ],
            Video = new("ZkYORFHgRTw"),
        },
        new("skater_jump", "Skater Jump", Glutes, Bodyweight, Compound)
        {
            Secondary = [Quads, Calves],
            Category = Plyometric,
            Summary = "Rhythmic side-to-side single-leg jumps, like a speed skater's stride.",
            Steps =
            [
                "Stand on your right leg, knee bent and chest slightly forward.",
                "Jump sideways to the left, landing on your left leg.",
                "Let your right leg sweep behind your left ankle as you land, swinging your arms across.",
                "Jump straight back to the right and keep alternating.",
            ],
            Tips =
            [
                "Land softly in a quarter squat with your knee over your toes.",
                "Increase the distance and speed once landings are steady.",
                "Don't let the landing knee drift inward.",
            ],
            Video = new("AMFCdEFDkjg"),
        },
        new("pogo_hop", "Pogo Hop", Calves, Bodyweight, Isolation)
        {
            Secondary = [Quads],
            Category = Plyometric,
            Summary = "Small, quick, continuous hops off the balls of the feet that train ankle stiffness and reactivity.",
            Steps =
            [
                "Stand tall with feet hip-width apart and knees nearly straight.",
                "Hop a few centimetres off the floor using mainly your ankles.",
                "Land on the balls of your feet and bounce straight back up.",
                "Keep a fast, steady rhythm for the set.",
            ],
            Tips =
            [
                "Spend as little time on the ground as possible.",
                "Keep your heels just off the floor and your legs springy, not locked.",
                "Don't bend your knees much; the bounce comes from the ankles.",
            ],
            Video = new("c2LBofIzUqs"),
        },
        new("hurdle_hop", "Hurdle Hop", Quads, Bodyweight, Compound)
        {
            Secondary = [Calves, Glutes, Hamstrings],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "Consecutive two-footed jumps over a row of mini hurdles, keeping ground contact short.",
            Steps =
            [
                "Set out 4–6 low hurdles about a stride apart.",
                "Stand behind the first with feet hip-width apart.",
                "Jump over it, pulling your knees up, and land on the balls of your feet.",
                "Rebound straight away over the next hurdle and continue to the end.",
            ],
            Tips =
            [
                "Start with low hurdles and raise them only if contacts stay quick.",
                "Stay tall and land in the same spot each time.",
                "Don't stop and reset between hurdles; it should be one continuous rhythm.",
            ],
            Video = new("MRieBuzWWG8"),
        },

        // Upper-body plyometrics
        new("plyo_push_up", "Plyo Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "An explosive push-up where you push hard enough for your hands to leave the floor.",
            Steps =
            [
                "Start in a push-up position with hands just wider than your shoulders.",
                "Lower your chest toward the floor under control.",
                "Push up explosively so your hands leave the floor.",
                "Land with soft elbows and go straight into the next rep.",
            ],
            Tips =
            [
                "Keep your body in a straight line from head to heels.",
                "Elevate your hands on a bench to make it easier.",
                "Don't land with locked elbows or let your hips sag.",
            ],
            Video = new("MH4gcTKQiEc"),
        },
        new("clap_push_up", "Clap Push-Up", Chest, Bodyweight, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Category = Plyometric,
            Level = Advanced,
            Summary = "A plyo push-up with enough height to clap your hands before landing.",
            Steps =
            [
                "Start in a push-up position with hands just wider than your shoulders.",
                "Lower your chest toward the floor.",
                "Push up explosively, clap your hands quickly in front of your chest.",
                "Get your hands back under your shoulders and land with soft elbows.",
            ],
            Tips =
            [
                "Master plyo push-ups first; you need real height to clap safely.",
                "Keep your core braced so your body moves as one piece.",
                "Don't land on straight arms or let your face drop toward the floor.",
            ],
            Video = new("EYwWCgM198U", 23.1),
        },

        // Medicine-ball throws
        new("med_ball_chest_pass", "Medicine Ball Chest Pass", Chest, Other, Compound)
        {
            Secondary = [Triceps, Shoulders],
            Category = Plyometric,
            Summary = "An explosive two-handed throw from the chest into a wall or to a partner, using a medicine ball.",
            Steps =
            [
                "Stand facing a wall about 2–3 m away, holding a medicine ball at your chest.",
                "Set your feet in a split or square stance with knees slightly bent.",
                "Push the ball forward explosively, extending your arms fully.",
                "Catch the rebound with soft arms and reset at your chest.",
            ],
            Tips =
            [
                "Throw as hard as you can on every rep; this is a power drill.",
                "Step into the throw to add your legs.",
                "Don't let your elbows flare high; push straight from the chest.",
            ],
            Video = new("e-zHTwXA8mE"),
        },
        new("med_ball_slam", "Medicine Ball Slam", Abs, Other, Compound)
        {
            Secondary = [Back, Shoulders, Glutes],
            Category = Plyometric,
            Summary = "Lift a slam ball overhead and drive it hard into the floor, for full-body power and conditioning.",
            Steps =
            [
                "Stand with feet shoulder-width apart, holding a slam ball at your waist.",
                "Rise onto your toes as you lift the ball overhead, arms extended.",
                "Brace and slam the ball into the floor in front of you, hinging at the hips.",
                "Squat to pick it up and go straight into the next rep.",
            ],
            Tips =
            [
                "Use a dead-bounce slam ball, not a rubber medicine ball that rebounds at your face.",
                "Pull down with your lats and abs, not just your arms.",
                "Pick the ball up with a flat back; don't round over to grab it.",
            ],
            Video = new("CkO1mfSBvv4"),
        },
        new("med_ball_rotational_scoop_toss", "Medicine Ball Rotational Scoop Toss", Abs, Other, Compound)
        {
            Secondary = [Glutes, Shoulders],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "A low-to-high rotational throw into a wall that trains hip-driven rotational power.",
            Steps =
            [
                "Stand side-on to a wall about 1–2 m away, holding a medicine ball at your back hip.",
                "Sink into your back hip with knees bent.",
                "Drive your back hip toward the wall and rotate, scooping the ball up and into the wall.",
                "Catch the rebound, reset, then switch sides after the set.",
            ],
            Tips =
            [
                "The throw starts from the hips; the arms just steer the ball.",
                "Let your back foot pivot as you rotate.",
                "Don't twist through your lower back with your hips stuck.",
            ],
            Video = new("Qq83wji4t2I"),
        },
        new("med_ball_side_throw", "Medicine Ball Rotational Side Throw", Abs, Other, Compound)
        {
            Secondary = [Glutes, Shoulders, Chest],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "A horizontal rotational throw at chest height into a wall, the classic rotational power drill for throwing and striking sports.",
            Steps =
            [
                "Stand side-on to a wall with a medicine ball held at chest height.",
                "Rotate away from the wall, loading your back hip.",
                "Whip your hips, then your torso and arms, throwing the ball flat into the wall.",
                "Catch the rebound and repeat, then switch sides.",
            ],
            Tips =
            [
                "Keep the ball at chest height so it travels on a flat line.",
                "Let your hips lead your shoulders.",
                "Don't throw with only your arms.",
            ],
            Video = new("l0H-L2glg68", 31.039, 48.799999),
        },
        new("med_ball_overhead_backward_throw", "Medicine Ball Overhead Backward Throw", Glutes, Other, Compound)
        {
            Secondary = [Hamstrings, LowerBack, Shoulders],
            Category = Plyometric,
            Level = Intermediate,
            Summary = "A triple-extension throw that sends a medicine ball backward over your head for distance.",
            Steps =
            [
                "Stand in an open area holding a medicine ball at arm's length in front of your hips.",
                "Hinge and bend your knees, lowering the ball between your legs.",
                "Drive your hips forward and extend explosively, swinging the ball up.",
                "Release it overhead so it flies backward, letting yourself finish on your toes.",
            ],
            Tips =
            [
                "Power comes from snapping the hips through, as in a jump.",
                "Check behind you is clear before every throw.",
                "Don't round your back at the bottom of the dip.",
            ],
            Video = new("j2iNY9aLmys"),
        },

        // Snatch family
        new("snatch", "Snatch", Glutes, Barbell, Compound)
        {
            Secondary = [Quads, Hamstrings, Traps],
            Category = Olympic,
            Level = Advanced,
            Summary = "The Olympic lift that takes the bar from the floor to overhead in one movement, caught in a full overhead squat.",
            Steps =
            [
                "Set up over the bar with a wide snatch grip, hips above knees, back flat and shoulders over the bar.",
                "Push the floor away, keeping the bar close as it passes your knees.",
                "At mid-thigh, extend your hips, knees and ankles explosively and shrug.",
                "Pull yourself under the bar, punching your arms straight and catching it in a full squat.",
                "Stand up with the bar locked out overhead.",
            ],
            Tips =
            [
                "Keep the bar close the whole way; let it brush your thighs.",
                "Finish the extension before pulling under.",
                "Don't pull early with your arms or swing the bar out in front.",
                "Learn it with a coach and an empty bar or PVC pipe first.",
            ],
            Video = new("1Lv1IyigIUY"),
        },
        new("power_snatch", "Power Snatch", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings, Quads, Traps],
            Category = Olympic,
            Level = Advanced,
            Summary = "A snatch caught above parallel, demanding a higher, more powerful pull.",
            Steps =
            [
                "Set up over the bar with a snatch grip, back flat and shoulders over the bar.",
                "Lift the bar from the floor with your legs, keeping it close.",
                "Extend explosively at the hips and knees and shrug.",
                "Drop quickly into a partial squat, locking the bar overhead above parallel.",
                "Stand up and lower the bar.",
            ],
            Tips =
            [
                "Catch with your feet moved out to squat width, knees out.",
                "Lock your elbows on arrival, not after.",
                "Don't press the bar out overhead; punch under it.",
            ],
            Video = new("DR41uRTZvz8"),
        },
        new("hang_snatch", "Hang Snatch", Glutes, Barbell, Compound)
        {
            Secondary = [Quads, Hamstrings, Traps],
            Category = Olympic,
            Level = Advanced,
            Summary = "A snatch started from a standing hang above the knees and caught in a full overhead squat.",
            Steps =
            [
                "Stand holding the bar with a snatch grip at your hips.",
                "Hinge, sliding the bar down to just above your knees, shoulders over the bar.",
                "Extend explosively and shrug, keeping the bar close.",
                "Pull under into a full overhead squat with locked arms.",
                "Stand up with the bar overhead.",
            ],
            Tips =
            [
                "Keep your weight mid-foot in the hang; don't drift onto your toes.",
                "Use it to drill a patient, vertical second pull.",
                "Don't bend your arms before the hips finish.",
            ],
            Video = new("Php-RclQ1yU", 0, 65.518997),
        },
        new("hang_power_snatch", "Hang Power Snatch", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings, Quads, Traps],
            Category = Olympic,
            Level = Intermediate,
            Summary = "A snatch from the hang caught above parallel, a common way to train snatch speed and power.",
            Steps =
            [
                "Stand holding the bar with a snatch grip at your hips.",
                "Hinge to bring the bar to just above your knees.",
                "Extend your hips and knees explosively and shrug.",
                "Pull under and lock the bar overhead in a partial squat.",
                "Stand up and lower the bar.",
            ],
            Tips =
            [
                "Keep the bar in contact with your thighs as you extend.",
                "Land with flat feet and knees out.",
                "Don't let the bar loop out in front of you.",
            ],
            Video = new("SpDPcj0W3Yw"),
        },
        new("muscle_snatch", "Muscle Snatch", Shoulders, Barbell, Compound)
        {
            Secondary = [Traps, Glutes, Hamstrings],
            Category = Olympic,
            Level = Intermediate,
            Summary = "A snatch with no re-bend of the knees: you pull and turn the bar over to overhead while standing tall.",
            Steps =
            [
                "Set up as for a snatch with a wide grip.",
                "Lift the bar and extend your hips and knees fully.",
                "Keep pulling with your elbows high and out as you rise onto your toes.",
                "Turn your arms over and press the bar to lockout without dipping.",
            ],
            Tips =
            [
                "Keep the bar close to your body as it turns over.",
                "Use lighter loads than your power snatch.",
                "Don't re-bend your knees; that turns it into a power snatch.",
            ],
            Video = new("nJmtGVutszE"),
        },
        new("snatch_pull", "Snatch Pull", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings, Traps, Quads],
            Category = Olympic,
            Level = Intermediate,
            Summary = "The snatch pull without the catch, used to build pulling strength and the finish position with heavier loads.",
            Steps =
            [
                "Set up with a snatch grip as for the snatch.",
                "Lift the bar from the floor, keeping your back angle and the bar close.",
                "At mid-thigh, extend your hips, knees and ankles explosively.",
                "Finish with a shrug, arms straight, then lower the bar under control.",
            ],
            Tips =
            [
                "Keep your arms long; this trains the legs and hips, not the arms.",
                "Finish tall with shoulders slightly behind the bar.",
                "Don't let the hips shoot up first off the floor.",
            ],
            Video = new("G1QygZ3Kd3w"),
        },
        new("overhead_squat", "Overhead Squat", Quads, Barbell, Compound)
        {
            Secondary = [Shoulders, Glutes, Abs],
            Category = Olympic,
            Level = Advanced,
            Summary = "A full squat with a barbell locked out overhead in a snatch grip, building the snatch receiving position.",
            Steps =
            [
                "Press or jerk the bar overhead with a wide snatch grip.",
                "Lock your elbows and push up into the bar, shoulder blades active.",
                "Squat down between your heels, keeping the bar over your mid-foot.",
                "Drive back up to standing with the bar still locked out.",
            ],
            Tips =
            [
                "Keep the bar over the back of your neck, not out in front.",
                "Brace hard and keep your chest up.",
                "Don't let your elbows soften or your heels lift.",
                "Build up with a PVC pipe; mobility limits this lift.",
            ],
            Video = new("mGvEruE-0rc"),
        },
        new("snatch_balance", "Snatch Balance", Quads, Barbell, Compound)
        {
            Secondary = [Shoulders, Triceps, Abs],
            Category = Olympic,
            Level = Advanced,
            Summary = "From the back rack, dip and drive yourself under the bar into an overhead squat, training speed under the snatch.",
            Steps =
            [
                "Stand with the bar on your back, hands in a snatch grip.",
                "Dip slightly at the knees.",
                "Drive the bar up a little and push yourself down under it, moving your feet to squat width.",
                "Catch it locked out in a full overhead squat, then stand up.",
            ],
            Tips =
            [
                "You go down; the bar barely goes up.",
                "Lock your elbows as you land, not after.",
                "Start very light and build only while the catch stays solid.",
            ],
            Video = new("8KKQTdnxWso"),
        },
        new("db_snatch", "Dumbbell Snatch", Glutes, Dumbbell, Compound)
        {
            Secondary = [Hamstrings, Shoulders, Traps],
            Category = Olympic,
            Level = Intermediate,
            Summary = "A one-arm snatch with a dumbbell from the floor to overhead in one explosive movement.",
            Steps =
            [
                "Stand with a dumbbell between your feet, then squat and hinge to grip it with one hand.",
                "Drive through the floor and extend your hips explosively.",
                "Keep the dumbbell close and pull it up with a high elbow.",
                "Punch under it, locking your arm out overhead in a quarter squat, then stand.",
                "Lower it to your shoulder and then the floor; alternate arms or do all reps on one side.",
            ],
            Tips =
            [
                "The hips launch it; the arm only guides it.",
                "Keep your back flat as you pick it up.",
                "Don't swing it out in a wide arc or press it out at the top.",
            ],
            Video = new("0yd9443Y-fg", 49),
        },

        // Clean family
        new("clean", "Clean", Glutes, Barbell, Compound)
        {
            Secondary = [Quads, Hamstrings, Traps],
            Category = Olympic,
            Level = Advanced,
            Summary = "The Olympic lift that takes the bar from the floor to the front of the shoulders, caught in a full front squat.",
            Steps =
            [
                "Set up with a grip just outside your legs, back flat and shoulders over the bar.",
                "Push the floor away, keeping the bar close as it passes your knees.",
                "At mid-thigh, extend your hips, knees and ankles explosively and shrug.",
                "Pull yourself under, whipping your elbows forward to catch the bar on your shoulders in a full squat.",
                "Stand up with the bar in the front rack.",
            ],
            Tips =
            [
                "Keep the bar close and finish the extension before pulling under.",
                "Get your elbows high and fast in the catch.",
                "Don't reverse-curl the bar or catch it on your hands.",
                "Learn it with a coach before going heavy.",
            ],
            Video = new("oQIaWLrB318"),
        },
        new("power_clean", "Power Clean", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings, Quads, Traps],
            Category = Olympic,
            Level = Intermediate,
            Summary = "A clean caught above parallel, the most common Olympic lift in strength and sports training.",
            Steps =
            [
                "Set up over the bar with a shoulder-width grip, back flat and shoulders over the bar.",
                "Lift the bar from the floor with your legs, keeping it close.",
                "At mid-thigh, extend your hips and knees explosively and shrug.",
                "Drop into a quarter squat, whipping your elbows forward to catch the bar on your shoulders.",
                "Stand up, then lower the bar to your hips and the floor.",
            ],
            Tips =
            [
                "Jump the bar up with your hips; don't lift it with your arms.",
                "Land with flat feet and elbows high.",
                "Don't lean back excessively at the top of the pull.",
            ],
            Video = new("E2z5zK5V-MM"),
        },
        new("hang_clean", "Hang Clean", Glutes, Barbell, Compound)
        {
            Secondary = [Quads, Hamstrings, Traps],
            Category = Olympic,
            Level = Intermediate,
            Summary = "A clean from a standing hang above the knees, caught in a full front squat.",
            Steps =
            [
                "Stand holding the bar at your hips with a shoulder-width grip.",
                "Hinge to slide the bar to just above your knees, shoulders over the bar.",
                "Extend explosively and shrug, keeping the bar close.",
                "Pull under into a full front squat, catching the bar on your shoulders.",
                "Stand up with the bar in the front rack.",
            ],
            Tips =
            [
                "Keep your weight mid-foot in the hang.",
                "Ride the catch down and bounce out of the bottom.",
                "Don't pull with bent arms before your hips extend.",
            ],
            Video = new("uUeV3LwisDI"),
        },
        new("hang_power_clean", "Hang Power Clean", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings, Quads, Traps],
            Category = Olympic,
            Level = Intermediate,
            Summary = "A clean from the hang caught above parallel, a simpler entry to the clean for athletes.",
            Steps =
            [
                "Stand holding the bar at your hips with a shoulder-width grip.",
                "Hinge to bring the bar to just above your knees.",
                "Extend your hips and knees explosively and shrug.",
                "Drop into a quarter squat and catch the bar on your shoulders with elbows high.",
                "Stand up and return the bar to your hips.",
            ],
            Tips =
            [
                "Keep the bar close and brush your thighs.",
                "Catch with flat feet, not on your toes.",
                "Don't swing the bar out with your hips.",
            ],
            Video = new("efHjodEVf9w"),
        },
        new("clean_pull", "Clean Pull", Glutes, Barbell, Compound)
        {
            Secondary = [Hamstrings, Traps, Quads],
            Category = Olympic,
            Level = Intermediate,
            Summary = "The clean pull without the catch, for building pulling strength with loads heavier than your clean.",
            Steps =
            [
                "Set up with a clean grip as for the clean.",
                "Lift the bar from the floor, keeping it close and your back flat.",
                "At mid-thigh, extend your hips, knees and ankles explosively.",
                "Finish with a shrug, arms straight, then lower the bar under control.",
            ],
            Tips =
            [
                "Keep your arms straight; the power comes from the legs and hips.",
                "Hit the same positions as your clean.",
                "Don't round your back to break the bar from the floor.",
            ],
            Video = new("xx8WkFrST2Y"),
        },
        new("high_pull", "Barbell High Pull", Traps, Barbell, Compound)
        {
            Secondary = [Shoulders, Glutes, Hamstrings],
            Category = Olympic,
            Level = Intermediate,
            Summary = "An explosive pull that continues past the shrug, driving the elbows high to bring the bar to chest height.",
            Steps =
            [
                "Hold the bar at your hips with a clean grip, or start from the hang above your knees.",
                "Extend your hips and knees explosively.",
                "As the bar rises, shrug and pull your elbows high and out.",
                "Bring the bar to lower-chest height, then lower it under control.",
            ],
            Tips =
            [
                "Let the hips start the bar moving; the arms just keep it going.",
                "Keep the bar close to your body.",
                "Don't turn it into a slow upright row.",
            ],
            Video = new("2Qv8pEnprpU"),
        },

        // Jerks and push press
        new("push_press", "Push Press", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Quads, Glutes],
            Category = Olympic,
            Level = Intermediate,
            Summary = "An overhead press driven by a quick leg dip, letting you move more weight than a strict press.",
            Steps =
            [
                "Stand with the bar in the front rack, hands just outside your shoulders.",
                "Dip a few centimetres by bending your knees, keeping your torso upright.",
                "Drive up hard through your legs and press the bar overhead as it leaves your shoulders.",
                "Lock your arms out with the bar over the back of your head, then lower to your shoulders.",
            ],
            Tips =
            [
                "Dip straight down with weight on your whole foot, not your toes.",
                "Move your head back to let the bar travel straight up.",
                "Don't re-bend your knees to get under the bar; that's a push jerk.",
            ],
            Video = new("yklSQG1_Ovc"),
        },
        new("push_jerk", "Push Jerk", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Quads, Glutes],
            Category = Olympic,
            Level = Intermediate,
            Summary = "Dip and drive the bar off the shoulders, then drop into a partial squat to catch it locked out overhead.",
            Steps =
            [
                "Stand with the bar in the front rack, feet hip-width apart.",
                "Dip straight down at the knees with an upright torso.",
                "Drive the bar up explosively with your legs.",
                "Push yourself under it into a quarter squat, catching it with locked arms.",
                "Stand up with the bar overhead, then lower it to your shoulders.",
            ],
            Tips =
            [
                "Lock your elbows as your feet land.",
                "Keep the dip short and vertical.",
                "Don't press the bar out slowly; drop under it.",
            ],
            Video = new("Om7vLD6x8W0"),
        },
        new("split_jerk", "Split Jerk", Shoulders, Barbell, Compound)
        {
            Secondary = [Triceps, Quads, Glutes],
            Category = Olympic,
            Level = Advanced,
            Summary = "The competition jerk: drive the bar overhead and split your feet to catch it, allowing the heaviest loads.",
            Steps =
            [
                "Stand with the bar in the front rack, feet hip-width apart.",
                "Dip and drive the bar off your shoulders explosively.",
                "Split your feet, front foot forward and back foot back on its toes, as you punch under the bar.",
                "Catch it with locked arms over your mid-split.",
                "Step the front foot back, then the back foot forward, and lower the bar.",
            ],
            Tips =
            [
                "Land with your front shin vertical and back knee slightly bent.",
                "Keep your hips between your feet in the split.",
                "Don't step back with the back foot first when recovering.",
            ],
            Video = new("2GPA-cjUFnA"),
        },
        new("clean_and_jerk", "Clean and Jerk", Glutes, Barbell, Compound)
        {
            Secondary = [Quads, Shoulders, Triceps],
            Category = Olympic,
            Level = Advanced,
            Summary = "The full Olympic lift: clean the bar to your shoulders, stand, then jerk it overhead.",
            Steps =
            [
                "Clean the bar from the floor into the front rack and stand up.",
                "Reset your breath, grip and feet.",
                "Dip and drive the bar off your shoulders.",
                "Split or push under it to catch it locked out overhead.",
                "Recover to standing with the bar overhead, then lower it.",
            ],
            Tips =
            [
                "Take a breath and settle before the jerk.",
                "Keep the jerk dip vertical even when tired from the clean.",
                "Don't rush the jerk from a poor rack position.",
            ],
            Video = new("bNCXgyosXlc"),
        },

        // Kettlebell
        new("kb_swing", "Kettlebell Swing", Glutes, Kettlebell, Compound)
        {
            Secondary = [Hamstrings, LowerBack],
            Summary = "The two-handed Russian swing: hinge and snap your hips forward to float the kettlebell to chest height.",
            Steps =
            [
                "Stand with feet a little wider than your hips, a kettlebell about a foot in front of you.",
                "Hinge, grab the handle with both hands and hike it back between your thighs.",
                "Snap your hips forward and squeeze your glutes to swing it to chest height.",
                "Let it fall, hinging back to receive it as your forearms meet your thighs.",
                "Repeat, then hike it back and set it down in front of you to finish.",
            ],
            Tips =
            [
                "It's a hinge, not a squat; your shins stay nearly vertical.",
                "Stand tall at the top with ribs down and glutes tight.",
                "Don't lift it with your arms or lean back at the top.",
                "Keep your back flat; hike the bell high between your thighs, not down at your knees.",
            ],
            Video = new("yHxcTn1UeAc"),
        },
        new("kb_single_arm_swing", "Single-Arm Kettlebell Swing", Glutes, Kettlebell, Compound)
        {
            Secondary = [Hamstrings, Abs, Forearms],
            Summary = "A kettlebell swing with one hand, adding an anti-rotation and grip challenge.",
            Steps =
            [
                "Set up as for a two-handed swing and grip the handle with one hand.",
                "Hike the kettlebell back between your thighs.",
                "Snap your hips forward to swing it to chest height.",
                "Hinge to receive it and repeat, then switch hands.",
            ],
            Tips =
            [
                "Keep your shoulders square; don't let the bell twist you.",
                "Pack the working shoulder down, away from your ear.",
                "Don't let your back round as the bell goes between your legs.",
            ],
            Video = new("sxtfgmzhXvE"),
        },
        new("kb_american_swing", "American Kettlebell Swing", Glutes, Kettlebell, Compound)
        {
            Secondary = [Hamstrings, Shoulders, LowerBack],
            Level = Intermediate,
            Summary = "A kettlebell swing taken all the way to overhead, common in CrossFit.",
            Steps =
            [
                "Set up and hike the kettlebell back as for a Russian swing.",
                "Snap your hips forward to drive the bell up.",
                "Keep guiding it up until it's directly overhead with arms straight, bottom of the bell pointing up.",
                "Let it fall and hinge back to receive it.",
            ],
            Tips =
            [
                "Only swing overhead if you can get your arms fully overhead without arching your back.",
                "Keep your ribs down at the top.",
                "Don't let the bell tip past overhead behind you.",
            ],
            Video = new("d94xX-AQZ0A"),
        },
        new("kb_clean", "Kettlebell Clean", Glutes, Kettlebell, Compound)
        {
            Secondary = [Hamstrings, Forearms, Shoulders],
            Level = Intermediate,
            Summary = "A hip-driven move that brings the kettlebell from between the legs to the rack position on your chest.",
            Steps =
            [
                "Hike a kettlebell back between your thighs with one hand.",
                "Snap your hips forward, keeping your elbow close to your body.",
                "Guide the bell up and let your hand slip around it so it lands softly in the rack, resting on your forearm.",
                "Hold briefly with wrist straight, then drop it back into the next hike.",
            ],
            Tips =
            [
                "Keep the bell close, like zipping up a jacket.",
                "Insert your hand around the bell instead of flipping it over.",
                "Don't let the bell bang onto your forearm.",
            ],
            Video = new("xZyGP55NkAo"),
        },
        new("kb_snatch", "Kettlebell Snatch", Glutes, Kettlebell, Compound)
        {
            Secondary = [Hamstrings, Shoulders, Traps],
            Level = Intermediate,
            Summary = "One explosive hip drive takes the kettlebell from between your legs to locked out overhead.",
            Steps =
            [
                "Hike a kettlebell back between your thighs with one hand.",
                "Snap your hips forward and let the bell rise close to your body.",
                "As it passes your chest, punch your hand up through the handle.",
                "Lock out overhead with the bell resting on your forearm, then drop it back into the next hike.",
            ],
            Tips =
            [
                "Punch through at the top so the bell doesn't flip and slam your wrist.",
                "Learn swings and cleans before snatches.",
                "Don't swing the bell in a wide arc.",
            ],
            Video = new("AR6E01ABoHk"),
        },
        new("kb_turkish_get_up", "Turkish Get-Up", Shoulders, Kettlebell, Compound)
        {
            Secondary = [Abs, Glutes, Triceps],
            Level = Intermediate,
            Summary = "A slow, step-by-step move from lying to standing with a kettlebell held overhead, training shoulder stability and the whole body.",
            Steps =
            [
                "Lie on your back holding a kettlebell straight up in your right hand, right knee bent.",
                "Roll onto your left elbow, then your left hand, keeping your arm vertical.",
                "Bridge your hips up and sweep your left leg back under you into a half-kneel.",
                "Lift your torso upright and stand up, the bell locked out overhead.",
                "Reverse each step to return to the floor, then switch sides.",
            ],
            Tips =
            [
                "Keep your eyes on the bell and your arm vertical the whole time.",
                "Move slowly; it's a strength and control drill, not a race.",
                "Practise the first steps unloaded or with a shoe balanced on your fist.",
                "Don't let your wrist bend back or your shoulder shrug up.",
            ],
            Video = new("0bWRPC49-KI"),
        },
        new("kb_windmill", "Kettlebell Windmill", Abs, Kettlebell, Compound)
        {
            Secondary = [Shoulders, Hamstrings, Glutes],
            Level = Intermediate,
            Summary = "With a kettlebell overhead, hinge sideways to reach the floor, training the obliques, hips and overhead stability.",
            Steps =
            [
                "Hold a kettlebell overhead in your right hand, feet wider than your hips and both toes turned slightly left.",
                "Look up at the bell and push your right hip out to the side.",
                "Hinge down, sliding your left hand down your left leg toward the floor.",
                "Reverse to standing with the arm still locked overhead, then switch sides.",
            ],
            Tips =
            [
                "The movement is a hip hinge sideways, not a side bend at the waist.",
                "Keep the overhead arm vertical and your eyes on the bell.",
                "Don't force the depth; go only as far as your hamstrings allow.",
            ],
            Video = new("q8K_Wg8OuV4"),
        },
        new("kb_front_squat", "Double Kettlebell Front Squat", Quads, Kettlebell, Compound)
        {
            Secondary = [Glutes, Abs],
            Summary = "A squat holding two kettlebells in the rack position, which loads the legs and makes your core fight to stay upright.",
            Steps =
            [
                "Clean two kettlebells to the rack, elbows tucked against your ribs.",
                "Stand with feet shoulder-width apart, toes slightly out.",
                "Squat down between your hips, keeping your chest up.",
                "Drive up through your whole foot to standing.",
            ],
            Tips =
            [
                "Keep your elbows down and the bells tight to your chest.",
                "Push your knees out over your toes.",
                "Don't let your upper back round forward at the bottom.",
            ],
            Video = new("6XghYOzny8U"),
        },
        new("kb_halo", "Kettlebell Halo", Shoulders, Kettlebell, Compound)
        {
            Secondary = [Abs, Triceps],
            Summary = "Circle a light kettlebell around your head to warm up and mobilise the shoulders and upper back.",
            Steps =
            [
                "Hold a kettlebell upside down by the horns in front of your chest.",
                "Brace your core and circle it around one side of your head.",
                "Bring it behind your head, close to your neck, and round the other side to the front.",
                "Alternate directions each rep.",
            ],
            Tips =
            [
                "Keep the circle tight around your head.",
                "Keep your ribs down and your torso still.",
                "Don't arch your back as the bell goes behind you.",
            ],
            Video = new("LEjVh4scVy4"),
        },

        // Carries
        new("farmers_walk", "Farmer's Walk", Forearms, Dumbbell, Compound)
        {
            Secondary = [Traps, Abs, Glutes],
            Summary = "Walk with a heavy weight in each hand to build grip, traps and a braced trunk; also done with kettlebells or farmer's handles.",
            Steps =
            [
                "Deadlift a heavy dumbbell or kettlebell in each hand, standing tall.",
                "Brace your core and set your shoulders down and back.",
                "Walk forward with short, quick steps for the set distance or time.",
                "Stop and set the weights down with a flat back.",
            ],
            Tips =
            [
                "Stay tall; don't let the weights pull your shoulders forward.",
                "Grip as hard as you can.",
                "Don't lift the weights from the floor with a rounded back.",
            ],
            Video = new("NH7Xv-7NQNQ", 20, 43),
        },
        new("suitcase_carry", "Suitcase Carry", Abs, Dumbbell, Compound)
        {
            Secondary = [Forearms, Glutes, Traps],
            Summary = "Carry a heavy weight in one hand while staying perfectly upright, training the obliques and anti-lateral flexion.",
            Steps =
            [
                "Pick up a heavy dumbbell or kettlebell in one hand.",
                "Stand tall with your shoulders level.",
                "Walk for the set distance or time without leaning.",
                "Set it down, switch hands and repeat.",
            ],
            Tips =
            [
                "Imagine a weight in the other hand to stay level.",
                "Keep your ribs stacked over your hips.",
                "Don't lean away from the weight or let it pull you sideways.",
            ],
            Video = new("y-hn_Ha1-RE"),
        },
        new("overhead_carry", "Overhead Carry", Shoulders, Kettlebell, Compound)
        {
            Secondary = [Abs, Triceps, Traps],
            Level = Intermediate,
            Summary = "Walk with a kettlebell or dumbbell locked out overhead, for shoulder stability and core control; one-arm is often called a waiter's walk.",
            Steps =
            [
                "Clean and press a kettlebell or dumbbell overhead with one or both arms.",
                "Lock your elbow and keep your bicep by your ear.",
                "Walk slowly for the set distance or time.",
                "Lower the weight under control and switch sides if one-arm.",
            ],
            Tips =
            [
                "Keep your ribs down; don't arch to get the arm overhead.",
                "Push up actively into the weight.",
                "Don't let your elbow bend or the weight drift forward.",
            ],
            Video = new("54POVWkWjEs"),
        },
        new("front_rack_carry", "Front Rack Carry", Abs, Kettlebell, Compound)
        {
            Secondary = [Back, Shoulders, Forearms],
            Summary = "Walk with one or two kettlebells in the rack position, training the core to keep you upright.",
            Steps =
            [
                "Clean one or two kettlebells to the rack position.",
                "Stand tall with elbows tucked and wrists straight.",
                "Walk with controlled steps for the set distance or time.",
                "Lower the bells safely when done.",
            ],
            Tips =
            [
                "Keep your ribs down and breathe behind the brace.",
                "With one bell, don't lean away from it.",
                "Don't let your elbows flare out and the bells slide off your chest.",
            ],
            Video = new("CJ-F_Xg9fJY"),
        },
        new("sandbag_carry", "Sandbag Bear Hug Carry", Abs, Other, Compound)
        {
            Secondary = [Back, Biceps, Quads],
            Summary = "Hug a heavy sandbag to your chest and walk, training the upper back, arms and core; needs a sandbag.",
            Steps =
            [
                "Pick the sandbag up from the floor and bring it to your chest.",
                "Wrap your arms around it and squeeze it tight.",
                "Stand tall and walk with short steps for the set distance.",
                "Lower it by squatting down with a braced back.",
            ],
            Tips =
            [
                "Squeeze the bag hard into your chest so it doesn't slide down.",
                "Breathe in short breaths behind the brace.",
                "Don't lean back excessively to carry it.",
            ],
            Video = new("sAOjLtEdxWs", 34, 130),
        },
        new("keg_carry", "Keg Carry", Abs, Other, Compound)
        {
            Secondary = [Back, Biceps, Forearms],
            Level = Intermediate,
            Summary = "A strongman carry with a keg held against the chest; the shifting load trains grip, upper back and core.",
            Steps =
            [
                "Lift the keg from the floor to your chest by its rims or in a bear hug.",
                "Hold it tight against your chest.",
                "Walk quickly with short steps for the set distance.",
                "Lower it under control by squatting.",
            ],
            Tips =
            [
                "Keep the keg high and tight to your body.",
                "Brace hard; a partly filled keg will slosh and shift.",
                "Don't let your back round when lifting it from the floor.",
            ],
            Video = new("y4i69xtGsak"),
        },

        // Sleds
        new("sled_push", "Sled Push", Quads, Other, Compound)
        {
            Secondary = [Glutes, Calves, Hamstrings],
            Summary = "Drive a loaded sled across the floor with your legs, for leg power and conditioning with little muscle soreness.",
            Steps =
            [
                "Grip the sled's upright handles with arms extended or bent.",
                "Lean in at about 45° with a straight line from head to back heel.",
                "Drive the sled forward with short, powerful steps, pushing through the balls of your feet.",
                "Keep going for the set distance, then rest.",
            ],
            Tips =
            [
                "Keep your hips low and back flat.",
                "Use low handles for a lower, more quad-heavy position.",
                "Don't take long strides or stand up tall.",
            ],
            Video = new("9XRRXaUpnLk"),
        },
        new("sled_drag", "Backward Sled Drag", Quads, Other, Compound)
        {
            Secondary = [Calves],
            Summary = "Walk backward pulling a sled by a strap or handles, a knee-friendly way to build the quads.",
            Steps =
            [
                "Attach a strap or rope to the sled and hold the handles at arm's length.",
                "Sit into a quarter squat facing the sled, leaning back slightly.",
                "Walk backward with small steps, pushing through the balls of your feet.",
                "Keep tension on the strap for the set distance.",
            ],
            Tips =
            [
                "Stay in a quarter squat so your quads do the work.",
                "Check the path behind you is clear.",
                "Don't pull with your arms; they're just hooks.",
            ],
            Video = new("k7JsvdG9sSo"),
        },
        new("sled_pull", "Hand-Over-Hand Sled Pull", Back, Other, Compound)
        {
            Secondary = [Biceps, Forearms, LowerBack],
            Summary = "Stand still and pull a loaded sled toward you with a rope, hand over hand, for back and grip strength.",
            Steps =
            [
                "Attach a long rope to the sled and walk out to its end.",
                "Stand in an athletic stance with knees bent and hips back.",
                "Pull the rope toward you one hand after the other, drawing the sled in.",
                "Continue until the sled reaches you.",
            ],
            Tips =
            [
                "Keep your chest up and back flat throughout.",
                "Pull with your lats, driving your elbows back.",
                "Don't stand up straight and pull only with your arms.",
            ],
            Video = new("ZmQOg00gtSA"),
        },

        // Strongman
        new("sandbag_clean", "Sandbag Clean", Glutes, Other, Compound)
        {
            Secondary = [Hamstrings, LowerBack, Back],
            Level = Intermediate,
            Summary = "Lift a sandbag from the floor to your chest or shoulder with a powerful hip extension.",
            Steps =
            [
                "Stand over the sandbag with feet shoulder-width apart.",
                "Squat and hinge to grip the bag underneath or by its handles.",
                "Deadlift it to your thighs, then drive your hips forward explosively.",
                "Pull it up to your chest or onto one shoulder and stand tall.",
                "Lower it to the floor and repeat.",
            ],
            Tips =
            [
                "The hips do the lifting; the arms just keep the bag close.",
                "Keep your back braced; a slight round is common with sandbags but don't lose your brace.",
                "Don't curl the bag up with your arms.",
            ],
            Video = new("bKupJRL22HM"),
        },
        new("tire_flip", "Tire Flip", Glutes, Other, Compound)
        {
            Secondary = [Hamstrings, Quads, Back],
            Level = Intermediate,
            Summary = "Lift and push a heavy tractor tire end over end, a full-body strongman event.",
            Steps =
            [
                "Squat at the tire with your chest against it and fingers under the tread.",
                "Drive forward and up into the tire with your legs and hips.",
                "As it rises, step in and drive a knee into it.",
                "Flip your hands and push the tire over.",
                "Walk to it and set up for the next flip.",
            ],
            Tips =
            [
                "Push forward into the tire, not straight up.",
                "Keep your chest against it and your back flat.",
                "Don't try to curl the tire up with your arms; that's how biceps tear.",
            ],
            Video = new("aIDjGG_xwHg"),
        },
        new("yoke_walk", "Yoke Walk", Quads, Other, Compound)
        {
            Secondary = [Abs, LowerBack, Traps],
            Level = Advanced,
            Summary = "Carry a heavy frame across your upper back for distance, building total-body strength and bracing.",
            Steps =
            [
                "Step under the yoke and set the crossbar on your upper back as for a back squat.",
                "Grip the uprights, brace hard and stand it up.",
                "Walk forward with short, fast steps.",
                "Set the yoke down at the end by bending your knees.",
            ],
            Tips =
            [
                "Stay tight and keep your steps short so the yoke doesn't sway.",
                "Breathe behind the brace.",
                "Don't take long strides, and set it down if it starts swinging badly.",
            ],
            Video = new("zRsFkNPxaMM", 6, 254),
        },
        new("atlas_stone", "Atlas Stone Load", Glutes, Other, Compound)
        {
            Secondary = [Hamstrings, LowerBack, Biceps],
            Level = Advanced,
            Summary = "Lift a heavy round stone from the floor and load it onto a platform, the signature strongman event.",
            Steps =
            [
                "Straddle the stone and wrap your arms around it, hands underneath.",
                "Deadlift it into your lap, sitting back on your heels.",
                "Re-grip higher and hug it tight to your chest.",
                "Drive your hips forward explosively and roll the stone onto the platform.",
            ],
            Tips =
            [
                "Use tacky and forearm sleeves; they make a big difference.",
                "Squeeze the stone hard with your arms throughout.",
                "Start light; a rounded back is part of the lift, so build tolerance gradually.",
            ],
            Video = new("ItFKJ5b0IX8"),
        },
        new("log_press", "Log Press", Shoulders, Other, Compound)
        {
            Secondary = [Triceps, Quads, Abs],
            Level = Intermediate,
            Summary = "The strongman overhead press with a log and neutral grip, cleaned to the chest and pressed or push-pressed overhead.",
            Steps =
            [
                "Clean the log to your chest with a neutral grip, elbows high.",
                "Dip and drive with your legs, as for a push press.",
                "Press the log overhead, moving your head back and then through.",
                "Lock out, then lower to your chest and repeat.",
            ],
            Tips =
            [
                "Keep your elbows high in the rack.",
                "Brace hard against the log's thickness on your chest.",
                "Don't lean back excessively at lockout.",
            ],
            Video = new("MjKCPd9eJhM", 27),
        },

        // Combat-sport power
        new("landmine_rotational_punch", "Landmine Rotational Punch", Abs, Barbell, Compound)
        {
            Secondary = [Shoulders, Triceps, Glutes],
            Level = Intermediate,
            Summary = "Push a landmine bar up and away from your shoulder with hip rotation, building punching power.",
            Steps =
            [
                "Stand in a fighting stance with the end of a landmine bar held at your back shoulder.",
                "Rotate away slightly to load your back hip.",
                "Drive off your back foot, rotating your hips and torso, and punch the bar up and forward.",
                "Bring it back under control to your shoulder and repeat, then switch stance.",
            ],
            Tips =
            [
                "Let the back foot pivot and the hips lead.",
                "Keep it explosive with a load you can move fast.",
                "Don't just press with your arm.",
            ],
            Video = new("HRzERntj2iI", 0, 64.43),
        },
        new("band_resisted_punch", "Band-Resisted Punch", Shoulders, Band, Compound)
        {
            Secondary = [Chest, Triceps, Abs],
            Summary = "Throw punches against a band anchored behind you to train punching speed and endurance.",
            Steps =
            [
                "Anchor a band at chest height behind you, or loop it around your back, holding an end in each hand.",
                "Step forward into your fighting stance until the band has tension.",
                "Throw straight punches, rotating your hips and extending your arm fully.",
                "Return your hand to your guard quickly after each punch.",
            ],
            Tips =
            [
                "Keep your guard up between punches.",
                "Snap the punch out and back; the band will try to pull your hand back sloppily.",
                "Don't lock your elbow hard at full extension.",
            ],
            Video = new("gYliCzGOsTs"),
        },
        new("sledgehammer_tire_strike", "Sledgehammer Tire Strike", Abs, Other, Compound)
        {
            Secondary = [Back, Shoulders, Forearms],
            Summary = "Swing a sledgehammer overhead into a tire, for rotational and full-body power and conditioning.",
            Steps =
            [
                "Stand facing a large tire, feet shoulder-width apart, holding a sledgehammer.",
                "Slide your top hand up the handle and lift the hammer up over one shoulder.",
                "Swing it down hard onto the tire, sliding your top hand down to meet the other.",
                "Let it bounce up, then switch sides on alternate swings or after each set.",
            ],
            Tips =
            [
                "Bend your knees and hinge into the strike to use your whole body.",
                "Keep a firm grip and watch for the rebound.",
                "Don't round your back at the bottom of each strike.",
            ],
            Video = new("hTo28QSkLZY", 0, 322),
        },
    ];
}
