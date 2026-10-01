using GymBook.Models;
using static GymBook.Models.Equipment;
using static GymBook.Models.ExerciseCategory;
using static GymBook.Models.ExerciseLevel;
using static GymBook.Models.Mechanic;
using static GymBook.Models.MuscleGroup;

namespace GymBook.Services;

public static partial class ExerciseLibrary
{
    static Def[] CardioExercises() =>
    [
        // Cardio machines
        new("treadmill_run", "Treadmill Run", Quads, Machine, Compound)
        {
            Secondary = [Hamstrings, Glutes, Calves],
            Category = Cardio,
            Summary = "Running on a motorised treadmill at a steady pace or in intervals, a controllable way to build aerobic fitness.",
            Steps =
            [
                "Step onto the side rails, start the belt at a walking speed and step on.",
                "Raise the speed gradually to your running pace and set a 1% incline to mimic outdoor running.",
                "Run tall with a relaxed upper body, landing with your foot under your hips.",
                "Slow to a walk for a few minutes to cool down before stopping the belt.",
            ],
            Tips =
            [
                "Stay in the middle of the belt and look ahead, not at your feet.",
                "Take short, quick strides rather than long reaching ones.",
                "Don't hold the handrails or the console while running.",
            ],
            Video = new("UIyZUHq2UdU"),
        },
        new("incline_treadmill_walk", "Incline Treadmill Walk", Glutes, Machine, Compound)
        {
            Secondary = [Calves, Hamstrings, Quads],
            Category = Cardio,
            Summary = "Brisk walking on a steep treadmill incline, a low-impact way to raise your heart rate and work the glutes and calves.",
            Steps =
            [
                "Start walking on a flat belt at an easy pace to warm up.",
                "Raise the incline to 8–15% and set a speed you can walk briskly without running, usually 4–6 km/h.",
                "Walk tall with a full stride, pushing through your whole foot and swinging your arms naturally.",
                "Lower the incline and speed for the last few minutes to cool down.",
            ],
            Tips =
            [
                "Lean only slightly into the hill from your ankles, not by bending at the waist.",
                "Don't hang on to the handrails; if you need them, lower the incline or speed.",
            ],
            Video = new("HwXYMPGjlUg"),
        },
        new("curved_treadmill_run", "Curved Treadmill Run", Quads, Machine, Compound)
        {
            Secondary = [Hamstrings, Glutes, Calves],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Running on a self-powered curved treadmill, where your own stride drives the belt; great for sprints and harder than a motorised belt at the same pace.",
            Steps =
            [
                "Hold the rails and step onto the low middle of the curve.",
                "Let go and start walking; move forward up the curve to speed up and drift back to slow down.",
                "Run on the balls of your feet with a slight forward lean, driving your knees forward.",
                "Drift back to the bottom of the curve to slow to a walk before stepping off.",
            ],
            Tips =
            [
                "Land under your hips on the front of your foot; heel striking fights the belt.",
                "Start with short intervals, as the effort builds much faster than on a normal treadmill.",
                "Don't step off while the belt is still moving quickly.",
            ],
            Video = new("vHBdtRm0ZlU"),
        },
        new("rowing_machine", "Rowing Machine", Quads, Machine, Compound)
        {
            Secondary = [Back, Glutes, Hamstrings],
            Category = Cardio,
            Summary = "Full-body cardio on an indoor rower; most of the power comes from the leg drive, finished by the hips leaning back and the arms pulling.",
            Steps =
            [
                "Strap your feet in so the strap crosses the widest part of your foot and take the handle with an overhand grip.",
                "Start at the catch: shins vertical, arms straight, leaning slightly forward from the hips.",
                "Drive with your legs first, then swing your torso back slightly, then pull the handle to your lower ribs.",
                "Return in reverse order: arms out, lean forward from the hips, then bend your knees to slide back to the catch.",
                "Keep a steady rhythm, roughly one count to drive and two to recover.",
            ],
            Tips =
            [
                "Think legs, body, arms on the drive and arms, body, legs on the recovery.",
                "Keep your back long and avoid rounding over at the catch.",
                "Don't pull early with your arms or shoot your hips back without moving the handle.",
                "A damper of 3–5 suits most people; higher isn't a harder workout, just a heavier stroke.",
            ],
            Video = new("4zWu1yuJ0_g"),
        },
        new("stationary_bike", "Stationary Bike", Quads, Machine, Compound)
        {
            Secondary = [Glutes, Hamstrings, Calves],
            Category = Cardio,
            Summary = "Steady or interval cycling on an upright exercise bike, a low-impact way to build aerobic fitness.",
            Steps =
            [
                "Set the seat so your knee is slightly bent at the bottom of the pedal stroke.",
                "Sit tall with a light grip on the handlebars and your feet in the straps or clipped in.",
                "Pedal at a smooth cadence, around 80–100 rpm, adjusting the resistance to the effort you want.",
                "Ease the resistance down for the last few minutes to cool down.",
            ],
            Tips =
            [
                "Push and pull through the whole circle rather than stamping down.",
                "Keep your knees tracking in line with your feet.",
                "Don't set the seat so low that your knees come up high or so high that your hips rock.",
            ],
            Video = new("fW-gDFOLaCk"),
        },
        new("recumbent_bike", "Recumbent Bike", Quads, Machine, Compound)
        {
            Secondary = [Glutes, Hamstrings],
            Category = Cardio,
            Summary = "Cycling in a reclined, backed seat with the pedals in front of you; easy on the back and joints and good for beginners and rehab.",
            Steps =
            [
                "Sit back against the seat and slide it so your knees stay slightly bent when the pedal is furthest away.",
                "Hold the side handles lightly and place your feet in the pedal straps.",
                "Pedal at a smooth, steady cadence, adjusting the resistance to the effort you want.",
                "Lower the resistance at the end to cool down.",
            ],
            Tips =
            [
                "Keep your back against the seat and your shoulders relaxed.",
                "Don't lock your knees out at the far end of the stroke.",
            ],
            Video = new("ckJRUKyJ8cI"),
        },
        new("spin_bike", "Spin Bike", Quads, Machine, Compound)
        {
            Secondary = [Glutes, Hamstrings, Calves],
            Category = Cardio,
            Summary = "Indoor cycling on a heavy-flywheel spin bike, with seated climbs, standing efforts and sprints as in a spin class.",
            Steps =
            [
                "Set the saddle level with your hip bone and the handlebars at saddle height or higher.",
                "Clip in or tighten the cages and start pedalling with light resistance to warm up.",
                "Ride seated, adding resistance for climbs and spinning faster for sprints.",
                "For standing efforts, add resistance first, then rise with your hips over the pedals.",
                "Finish with a few minutes of easy spinning.",
            ],
            Tips =
            [
                "Always keep some resistance on; spinning a free flywheel fast can hurt your knees.",
                "Use the brake or resistance knob to stop; the pedals keep turning with the flywheel.",
                "Don't bounce in the saddle; if you do, add resistance.",
            ],
            Video = new("gWosN1CY4bg"),
        },
        new("air_bike", "Air Bike", Quads, Machine, Compound)
        {
            Secondary = [Shoulders, Glutes, Back],
            Category = Cardio,
            Level = Intermediate,
            Summary = "A fan bike (Assault Bike, Echo Bike) with moving handles, so your arms push and pull while your legs pedal; the harder you go, the harder it gets.",
            Steps =
            [
                "Set the seat so your knee is slightly bent at the bottom of the stroke and grip the handles.",
                "Push and pull the handles in rhythm with the pedals, opposite arm to leg.",
                "Hold a steady pace for longer pieces, or go all-out for short sprints of 10–30 seconds.",
                "Ease off gradually rather than stopping dead after hard sprints.",
            ],
            Tips =
            [
                "Use your arms as hard as your legs on sprints; both drive the fan.",
                "Keep your chest up and core braced so you don't rock side to side.",
                "Don't go all-out for long; pace yourself, as the effort rises sharply with speed.",
            ],
            Video = new("Sg3Id7Lu8XU"),
        },
        new("ski_erg", "Ski Erg", Back, Machine, Compound)
        {
            Secondary = [Triceps, Abs, Shoulders],
            Category = Cardio,
            Summary = "Cardio on a ski ergometer, pulling two handles down from overhead like a double-pole ski stroke, driven by the lats, triceps and a hip hinge.",
            Steps =
            [
                "Stand facing the machine, feet hip-width apart, holding a handle in each hand with arms raised.",
                "Pull the handles down while hinging at the hips and bending your knees slightly.",
                "Finish with your hands past your thighs and your torso leaning forward.",
                "Stand back up and let your arms rise with the cords to the start.",
            ],
            Tips =
            [
                "Start each stroke by engaging your lats with your arms nearly straight, then crunch and hinge.",
                "Keep your back flat as you hinge.",
                "Don't just squat down; the hinge and the pull do the work.",
            ],
            Video = new("14Rs6KJOTaY"),
        },
        new("elliptical", "Elliptical", Quads, Machine, Compound)
        {
            Secondary = [Glutes, Hamstrings, Shoulders],
            Category = Cardio,
            Summary = "Low-impact cardio on an elliptical trainer, gliding your feet in an oval path while pushing and pulling the moving handles.",
            Steps =
            [
                "Step onto the pedals, hold the moving handles and start striding forward.",
                "Set a resistance and incline that let you keep a smooth, steady rhythm.",
                "Push through your whole foot and drive the handles with your arms.",
                "Lower the resistance for the last few minutes to cool down.",
            ],
            Tips =
            [
                "Stand upright with your weight over your feet, not leaning on the handles.",
                "Keep your heels down to work the glutes and hamstrings more.",
                "Don't let momentum do the work at very low resistance.",
            ],
            Video = new("3pCdU1XkNm8"),
        },
        new("stair_climber", "Stair Climber", Quads, Machine, Compound)
        {
            Secondary = [Glutes, Calves, Hamstrings],
            Category = Cardio,
            Summary = "Climbing a revolving staircase at a steady pace, a demanding low-impact cardio that works the legs and glutes hard.",
            Steps =
            [
                "Step onto the stairs, hold the rails lightly and start at a slow speed.",
                "Climb with your whole foot on each step, standing tall.",
                "Raise the speed to a pace you can keep without hanging on.",
                "Slow it down for the last few minutes before stepping off.",
            ],
            Tips =
            [
                "Push through your heel and drive up with your glutes.",
                "Use the rails only for balance.",
                "Don't lean on the rails with straight arms; it takes the load off your legs.",
            ],
            Video = new("LM9lgFK4mkk"),
        },
        new("versaclimber", "VersaClimber", Quads, Machine, Compound)
        {
            Secondary = [Shoulders, Back, Glutes],
            Category = Cardio,
            Level = Intermediate,
            Summary = "A vertical climbing machine where your arms and legs move in opposition, like climbing a ladder; intense full-body cardio with no impact.",
            Steps =
            [
                "Strap your feet into the pedals and grip the handles at about shoulder height.",
                "Push one foot down while reaching up with the opposite hand.",
                "Alternate sides in a smooth rhythm, using long strokes for endurance or short fast ones for sprints.",
                "Slow the pace gradually to finish.",
            ],
            Tips =
            [
                "Keep your hips close to the machine and your torso upright.",
                "Pull down with your arms as well as reaching up; both sides of the stroke count.",
                "Don't take tiny strokes all the time; full range builds the most fitness.",
            ],
            Video = new("XTsFZ4dhzkY"),
        },
        new("arm_ergometer", "Arm Bike", Shoulders, Machine, Compound)
        {
            Secondary = [Back, Triceps, Biceps],
            Category = Cardio,
            Summary = "Cardio on an upper-body ergometer, cranking the handles in circles with your arms; useful when you need to rest your legs.",
            Steps =
            [
                "Sit or stand so the crank axle is at shoulder height and your arms are almost straight at the far point.",
                "Grip the handles and start cranking at light resistance.",
                "Keep a smooth, steady cadence, pushing and pulling through the whole circle.",
                "Raise the resistance for intervals and lower it again to cool down.",
            ],
            Tips =
            [
                "Sit tall and keep your shoulders down away from your ears.",
                "Try cranking backwards for some of the time to balance the work.",
                "Don't hunch forward over the handles.",
            ],
            Video = new("Kt3PwXFuCgI"),
        },

        // Running, walking and outdoor
        new("easy_run", "Easy Run", Quads, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Glutes, Calves],
            Category = Cardio,
            Summary = "A relaxed, conversational-pace run that builds your aerobic base and makes up most of a runner's weekly distance.",
            Steps =
            [
                "Walk or jog slowly for 5 minutes to warm up.",
                "Settle into a pace at which you can still speak in full sentences.",
                "Run tall with relaxed shoulders, arms swinging forward and back, landing with your foot under your hips.",
                "Finish with a few minutes of easy walking.",
            ],
            Tips =
            [
                "If you can't talk comfortably, slow down; easy runs should feel easy.",
                "Increase your weekly distance gradually, by about 10% at a time.",
                "Don't overstride or slam your heels down far in front of you.",
            ],
            Video = new("M9U9pk6yOIU"),
        },
        new("sprints", "Sprints", Quads, Bodyweight, Compound)
        {
            Secondary = [Hamstrings, Glutes, Calves],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Short all-out runs, usually 10–100 m, with full or partial rest between, to build speed, power and anaerobic fitness.",
            Steps =
            [
                "Warm up thoroughly with easy jogging, drills and a few build-up runs.",
                "Start from a staggered stance with a slight forward lean.",
                "Drive hard with your arms and knees, building to top speed.",
                "Run tall at full speed through the finish, then slow down gradually.",
                "Walk back and rest fully before the next sprint.",
            ],
            Tips =
            [
                "Run on the balls of your feet and push the ground behind you.",
                "Keep your face and shoulders relaxed, even at full effort.",
                "Don't sprint cold; hamstring strains usually happen without a proper warm-up.",
            ],
            Video = new("6jxlev8ZlyU"),
        },
        new("hill_sprints", "Hill Sprints", Glutes, Bodyweight, Compound)
        {
            Secondary = [Quads, Hamstrings, Calves],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Short, hard sprints up a moderate hill; the slope builds power and makes sprinting easier on the hamstrings.",
            Steps =
            [
                "Find a moderate hill of about 5–10% and warm up with easy jogging.",
                "Sprint up for 8–20 seconds, leaning slightly into the hill from your ankles.",
                "Drive your knees up and pump your arms hard.",
                "Walk back down slowly to recover before the next rep.",
            ],
            Tips =
            [
                "Take short, powerful strides and stay on the balls of your feet.",
                "Keep your head up and look up the hill, not at your feet.",
                "Don't run back down hard; the walk down is your rest.",
            ],
            Video = new("VzHQUh5jzsI", 27, 88),
        },
        new("shuttle_runs", "Shuttle Runs", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Hamstrings, Calves],
            Category = Cardio,
            Summary = "Repeated sprints back and forth between two markers, usually 10–25 m apart, training speed, acceleration and turning.",
            Steps =
            [
                "Set two markers 10–25 m apart and start at one of them.",
                "Sprint to the far marker and touch the line or the ground beside it.",
                "Plant your outside foot, drop your hips and turn back.",
                "Sprint back and repeat for the set number of lengths.",
            ],
            Tips =
            [
                "Slow down in the last few steps and stay low into each turn.",
                "Alternate the hand you touch with so you turn both ways.",
                "Don't turn upright on a straight leg; it strains your knees and slows you down.",
            ],
            Video = new("9pb6JX2ulDU"),
        },
        new("stair_sprints", "Stair Sprints", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves, Hamstrings],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Running up a flight of stairs or stadium steps as fast as you can, then walking down to recover.",
            Steps =
            [
                "Warm up by walking up and down the stairs a few times.",
                "Run up one step at a time, landing on the balls of your feet.",
                "Pump your arms hard and keep your chest up.",
                "Walk back down carefully and repeat.",
            ],
            Tips =
            [
                "Once you're comfortable, take two steps at a time for more glute and quad work.",
                "Use the handrail on the way down.",
                "Don't run down the stairs; most falls and knee pain happen on the descent.",
            ],
            Video = new("2OwzD_UPL30"),
        },
        new("brisk_walk", "Brisk Walk", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves, Hamstrings],
            Category = Cardio,
            Summary = "Walking at a quick, purposeful pace that raises your heart rate; the simplest cardio there is.",
            Steps =
            [
                "Start at an easy pace for a few minutes.",
                "Pick up the pace until you're breathing harder but can still talk.",
                "Walk tall, roll from heel to toe and swing your arms from the shoulders.",
                "Slow down for the last few minutes.",
            ],
            Tips =
            [
                "Take quicker steps rather than longer ones to walk faster.",
                "Look ahead, not down, and keep your shoulders relaxed.",
            ],
            Video = new("nmvVfgrExAg"),
        },
        new("rucking", "Rucking", Quads, Other, Compound)
        {
            Secondary = [Glutes, Traps, LowerBack],
            Category = Cardio,
            Summary = "Brisk walking with a weighted backpack (a ruck), which turns an ordinary walk into strength-endurance training.",
            Steps =
            [
                "Load a backpack with 10–20% of your body weight, packing the weight high and close to your back.",
                "Tighten the shoulder straps and the hip belt so the pack doesn't bounce.",
                "Walk at a brisk pace, standing tall with your core braced.",
                "Build the distance and the load gradually over weeks.",
            ],
            Tips =
            [
                "Keep your chest up rather than leaning forward under the pack.",
                "Wear supportive shoes and start on flat ground.",
                "Don't jump to heavy loads or long distances; your feet and back need time to adapt.",
            ],
            Video = new("tBE6_Cixeyc"),
        },
        new("freestyle_swim", "Freestyle Swim", Back, Other, Compound)
        {
            Secondary = [Shoulders, Triceps, Abs],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Front crawl in a pool, a no-impact full-body cardio led by the lats and shoulders, with a steady flutter kick.",
            Steps =
            [
                "Push off the wall face down with your body long and flat near the surface.",
                "Reach one arm forward, then pull it down and back under your body to your hip.",
                "Recover the arm over the water while the other arm reaches and pulls.",
                "Kick from your hips with fairly straight legs and relaxed ankles.",
                "Turn your head to the side to breathe as an arm recovers, every 2–3 strokes.",
            ],
            Tips =
            [
                "Rotate your body with each stroke instead of swimming flat.",
                "Breathe out steadily under water so you only need to breathe in when you turn.",
                "Don't lift your head forward to breathe; it sinks your hips.",
            ],
            Video = new("NFHanLS9g5k"),
        },
        new("outdoor_cycling", "Outdoor Cycling", Quads, Other, Compound)
        {
            Secondary = [Glutes, Hamstrings, Calves],
            Category = Cardio,
            Summary = "Riding a road, gravel or mountain bike outdoors, a low-impact endurance cardio you can do for hours.",
            Steps =
            [
                "Set the saddle height so your knee is slightly bent at the bottom of the stroke and put your helmet on.",
                "Start easy for the first 10 minutes to warm up.",
                "Pedal at a smooth cadence, shifting gears to keep it steady on hills and flats.",
                "Finish with a few minutes of easy spinning.",
            ],
            Tips =
            [
                "Shift to an easier gear before a climb, not halfway up.",
                "Keep your upper body relaxed with a slight bend in your elbows.",
                "Don't grind a heavy gear at low cadence for long; it strains your knees.",
            ],
            Video = new("4ssLDk1eX9w"),
        },

        // Jump rope
        new("jump_rope", "Jump Rope", Calves, Other, Compound)
        {
            Secondary = [Quads, Shoulders, Forearms],
            Category = Cardio,
            Summary = "Basic two-foot bounce with a skipping rope; simple, portable cardio that builds coordination and springy ankles.",
            Steps =
            [
                "Stand on the middle of the rope; the handles should reach your armpits.",
                "Hold the handles at hip height with your elbows close to your sides.",
                "Turn the rope with your wrists and hop a few centimetres off the floor as it passes under you.",
                "Land softly on the balls of your feet and keep a steady rhythm.",
            ],
            Tips =
            [
                "Jump just high enough to clear the rope.",
                "Turn the rope from your wrists, not by swinging your whole arms.",
                "Don't land flat-footed or on your heels.",
            ],
            Video = new("WA1xNoWA7yA"),
        },
        new("boxer_skip", "Boxer Skip", Calves, Other, Compound)
        {
            Secondary = [Quads, Shoulders, Forearms],
            Category = Cardio,
            Summary = "Jump rope with a light shift of weight from foot to foot on each turn, the relaxed rhythm boxers use for long rounds.",
            Steps =
            [
                "Start turning the rope with a basic bounce to find your rhythm.",
                "Shift your weight onto one foot as the rope passes, tapping the other lightly.",
                "Switch your weight to the other foot on the next turn.",
                "Keep it light and rhythmic for rounds of 1–3 minutes.",
            ],
            Tips =
            [
                "Stay relaxed and bouncy; it should feel like a dance, not a series of jumps.",
                "Keep your hands low and close to your hips.",
                "Don't kick your feet back or lift them high.",
            ],
            Video = new("fV73Ce3RiiQ"),
        },
        new("high_knee_jump_rope", "High-Knee Jump Rope", Quads, Other, Compound)
        {
            Secondary = [Calves, Abs, Shoulders],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Running on the spot over the rope, driving one knee to hip height on each turn; a hard, fast conditioning drill.",
            Steps =
            [
                "Start turning the rope with a basic bounce.",
                "Lift one knee to hip height as the rope passes under you.",
                "Alternate knees on every turn, like sprinting on the spot.",
                "Keep the rope turning fast and stay on the balls of your feet.",
            ],
            Tips =
            [
                "Stay tall and keep your core braced; don't lean back.",
                "Speed up the rope to match your feet.",
                "Don't let your knees drop below hip height as you tire; slow down instead.",
            ],
            Video = new("bAGA6CRS7Cw"),
        },
        new("double_unders", "Double-Unders", Calves, Other, Compound)
        {
            Secondary = [Quads, Forearms, Shoulders],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Jump rope where the rope passes under your feet twice per jump, demanding fast wrists, timing and a slightly higher jump.",
            Steps =
            [
                "Start with a few basic bounces to find your rhythm.",
                "Jump a little higher than usual, keeping your legs nearly straight.",
                "Flick your wrists fast so the rope passes under you twice before you land.",
                "Land softly on the balls of your feet and go straight into the next jump.",
            ],
            Tips =
            [
                "Use a light speed rope and keep your hands just in front of your hips.",
                "Practise single–single–double patterns before linking doubles.",
                "Don't pike or kick your feet back to buy time; jump straight up.",
            ],
            Video = new("ZmqgcpcM-SI"),
        },

        // Bodyweight conditioning
        new("burpee", "Burpee", Quads, Bodyweight, Compound)
        {
            Secondary = [Chest, Shoulders, Triceps],
            Category = Cardio,
            Summary = "Squat down, jump your feet back to a plank, do a push-up, jump your feet in and jump up: a full-body conditioning staple.",
            Steps =
            [
                "Stand with your feet shoulder-width apart.",
                "Squat down and place your hands on the floor in front of your feet.",
                "Jump your feet back into a plank and do a push-up, chest to the floor.",
                "Jump your feet back in under your hips.",
                "Jump up and clap your hands overhead, then go straight into the next rep.",
            ],
            Tips =
            [
                "Keep your core tight in the plank so your hips don't sag.",
                "Land softly with bent knees on the jump.",
                "Don't flop down in a heap; control the drop to the floor.",
            ],
            Video = new("fZx6nxKMq4E"),
        },
        new("burpee_box_jump_over", "Burpee Box Jump-Over", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Chest, Calves],
            Category = Cardio,
            Level = Intermediate,
            Summary = "A burpee beside a box followed by jumping onto or over it, then another burpee on the far side.",
            Steps =
            [
                "Stand facing the long side of a box about 50–60 cm high.",
                "Do a burpee, bringing your chest to the floor.",
                "Jump your feet in, then jump with both feet onto the box or clear over it.",
                "Land on the far side, turn to face the box and repeat.",
            ],
            Tips =
            [
                "Step down from the box instead of jumping when you're tired.",
                "Land softly with your knees tracking over your toes.",
                "Don't rush the jump when fatigued; clipping the box causes most shin injuries.",
            ],
            Video = new("DgFuYUOAcF8"),
        },
        new("squat_thrust", "Squat Thrust", Quads, Bodyweight, Compound)
        {
            Secondary = [Abs, Shoulders],
            Category = Cardio,
            Summary = "The burpee without the push-up or the jump: squat, kick your feet back to a plank and jump them back in.",
            Steps =
            [
                "Stand tall with your feet shoulder-width apart.",
                "Squat down and place your hands on the floor in front of you.",
                "Jump your feet back into a high plank.",
                "Jump your feet back in and stand up.",
            ],
            Tips =
            [
                "Land in a solid plank with your hips level, not piked or sagging.",
                "Keep your hands under your shoulders.",
                "Don't round your back as you squat down; bend your knees.",
            ],
            Video = new("fysU2ldlXSY"),
        },
        new("sprawl", "Sprawl", Quads, Bodyweight, Compound)
        {
            Secondary = [Abs, Shoulders, Glutes],
            Category = Cardio,
            Summary = "The wrestler's takedown defence: drop your hands to the floor, shoot your legs back and drive your hips to the ground, then pop back up.",
            Steps =
            [
                "Stand in an athletic stance with your knees bent and hands up.",
                "Drop your hands to the floor and shoot your legs back and wide.",
                "Drive your hips down to the floor with your chest up.",
                "Snap your feet back under you and return to your stance.",
            ],
            Tips =
            [
                "Keep your chest and head up as your hips hit the floor.",
                "Make it explosive in both directions.",
                "Don't land with your hips in the air; the point is to flatten them.",
            ],
            Video = new("pgZ7qaEhyWk"),
        },
        new("mountain_climbers", "Mountain Climbers", Abs, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Quads],
            Category = Cardio,
            Summary = "From a high plank, drive your knees towards your chest one after the other at a running pace.",
            Steps =
            [
                "Start in a high plank with your hands under your shoulders and your body in a straight line.",
                "Drive one knee towards your chest.",
                "Switch legs quickly, bringing the other knee in as the first goes back.",
                "Keep alternating at a fast, steady rhythm.",
            ],
            Tips =
            [
                "Keep your hips level with your shoulders.",
                "Hold your shoulders over your hands throughout.",
                "Don't bounce your hips up and down or let your back sag.",
            ],
            Video = new("WN1ZemcpIck"),
        },
        new("jumping_jacks", "Jumping Jacks", Calves, Bodyweight, Compound)
        {
            Secondary = [Shoulders, Glutes, Quads],
            Category = Cardio,
            Summary = "Jump your feet out wide while swinging your arms overhead, then jump back in; a classic warm-up and conditioning move.",
            Steps =
            [
                "Stand with your feet together and arms at your sides.",
                "Jump your feet out wider than your shoulders while raising your arms overhead.",
                "Jump your feet back together as your arms come down.",
                "Keep a quick, steady rhythm.",
            ],
            Tips =
            [
                "Stay on the balls of your feet and land softly.",
                "Keep a slight bend in your knees.",
                "Don't let your knees cave in as your feet land wide.",
            ],
            Video = new("Q4QnlZs9PqI"),
        },
        new("star_jumps", "Star Jumps", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves, Shoulders],
            Category = Cardio,
            Summary = "From a quarter squat, jump explosively and spread your arms and legs into a star shape in the air, then land back together.",
            Steps =
            [
                "Stand with your feet together and drop into a quarter squat with your arms down.",
                "Jump up as high as you can, spreading your arms and legs wide into an X.",
                "Bring your arms and legs back together before you land.",
                "Land softly in a quarter squat and go straight into the next jump.",
            ],
            Tips =
            [
                "Land through the balls of your feet with your knees bent.",
                "Swing your arms hard to help you jump higher.",
                "Don't land stiff-legged.",
            ],
            Video = new("lkYtECQYrWo"),
        },
        new("high_knees", "High Knees", Quads, Bodyweight, Compound)
        {
            Secondary = [Abs, Calves],
            Category = Cardio,
            Summary = "Running on the spot, driving each knee up to hip height as fast as you can.",
            Steps =
            [
                "Stand tall with your feet hip-width apart and your arms bent.",
                "Drive one knee up to hip height while hopping off the other foot.",
                "Switch legs quickly, pumping your arms in time.",
                "Keep going at a fast, steady rhythm.",
            ],
            Tips =
            [
                "Stay on the balls of your feet and stay tall.",
                "Hold your hands out at hip height as targets for your knees.",
                "Don't lean back to get your knees higher.",
            ],
            Video = new("WofWmk-4qU4"),
        },
        new("butt_kicks", "Butt Kicks", Hamstrings, Bodyweight, Compound)
        {
            Secondary = [Calves, Quads],
            Category = Cardio,
            Summary = "Jogging on the spot or forwards while kicking your heels up towards your glutes; a common running warm-up drill.",
            Steps =
            [
                "Stand tall with your arms bent at your sides.",
                "Jog lightly, flicking one heel up towards your glutes.",
                "Switch legs quickly, keeping your knees pointing down.",
                "Keep a fast, light rhythm.",
            ],
            Tips =
            [
                "Keep your thighs roughly vertical; the movement is at the knee.",
                "Stay on the balls of your feet.",
                "Don't lean forward or let your knees drift forwards.",
            ],
            Video = new("q1Z_tTeXCVE"),
        },
        new("lateral_shuffle", "Lateral Shuffle", Quads, Bodyweight, Compound)
        {
            Secondary = [Glutes, Calves],
            Category = Cardio,
            Summary = "Quick side-to-side shuffling in a low athletic stance, a staple agility and conditioning drill for court and field sports.",
            Steps =
            [
                "Stand in an athletic stance: feet wider than your hips, knees bent, chest up.",
                "Push off your trailing foot and step sideways with your lead foot.",
                "Bring your trailing foot in without letting your feet touch.",
                "Shuffle 5–10 m, then change direction and shuffle back.",
            ],
            Tips =
            [
                "Stay low the whole time; don't bob up and down.",
                "Push from the outside edge of your trailing foot.",
                "Don't cross or click your feet together.",
            ],
            Video = new("W6wgZU8Ioos"),
        },
        new("bear_crawl", "Bear Crawl", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Abs, Quads, Triceps],
            Category = Cardio,
            Summary = "Crawling forwards on your hands and feet with your knees hovering just off the floor, working the shoulders and core.",
            Steps =
            [
                "Start on all fours with your hands under your shoulders and knees under your hips.",
                "Lift your knees a few centimetres off the floor.",
                "Step forward with one hand and the opposite foot together.",
                "Repeat with the other hand and foot, keeping your back flat.",
            ],
            Tips =
            [
                "Keep your hips low and level, about the height of your shoulders.",
                "Take small steps and move slowly at first.",
                "Don't let your hips rise into a pike.",
            ],
            Video = new("vg5hegN6pk0"),
        },
        new("crab_walk", "Crab Walk", Triceps, Bodyweight, Compound)
        {
            Secondary = [Glutes, Shoulders, Hamstrings],
            Category = Cardio,
            Summary = "Walking on your hands and feet facing upwards with your hips lifted, working the triceps, shoulders and glutes.",
            Steps =
            [
                "Sit on the floor with your knees bent and hands behind you, fingers pointing towards your feet.",
                "Lift your hips off the floor.",
                "Walk forwards or backwards by moving one hand and the opposite foot together.",
                "Keep your hips up the whole way.",
            ],
            Tips =
            [
                "Squeeze your glutes to keep your hips lifted.",
                "Keep your shoulders pulled down and back.",
                "Don't let your hips sag towards the floor.",
            ],
            Video = new("VbxDvCXaJw8"),
        },

        // Equipment conditioning
        new("battle_rope_waves", "Battle Rope Alternating Waves", Shoulders, Other, Compound)
        {
            Secondary = [Forearms, Abs, Back],
            Category = Cardio,
            Summary = "Whipping the ends of a heavy battle rope up and down one arm after the other to make alternating waves, a hard shoulder-led conditioning drill.",
            Steps =
            [
                "Hold an end of the rope in each hand and step back until there's a little slack.",
                "Stand in a half squat with your feet shoulder-width apart and chest up.",
                "Raise and lower your arms quickly in turn, sending alternating waves down the rope.",
                "Keep the waves going for 20–40 seconds.",
            ],
            Tips =
            [
                "Keep your knees bent and core braced so the work stays in your arms and shoulders.",
                "Make the waves travel all the way to the anchor.",
                "Don't stand upright with locked knees.",
            ],
            Video = new("uyLccdETUEw"),
        },
        new("battle_rope_double_waves", "Battle Rope Double Waves", Shoulders, Other, Compound)
        {
            Secondary = [Forearms, Abs, Back],
            Category = Cardio,
            Summary = "Both arms raising and lowering together to send matching waves down a battle rope.",
            Steps =
            [
                "Hold an end of the rope in each hand and step back until there's a little slack.",
                "Stand in a half squat with your chest up and arms in front of you.",
                "Raise both arms to shoulder height and drive them down together, sending a big wave down the rope.",
                "Keep a fast, steady rhythm for 20–40 seconds.",
            ],
            Tips =
            [
                "Use your hips and legs to add power to each wave.",
                "Keep your back flat as you work.",
                "Don't round forward over the rope.",
            ],
            Video = new("Q4En4X5NUNE"),
        },
        new("battle_rope_slams", "Battle Rope Slams", Shoulders, Other, Compound)
        {
            Secondary = [Back, Abs, Triceps],
            Category = Cardio,
            Summary = "Lifting both ends of a battle rope overhead and slamming them into the floor as hard as you can.",
            Steps =
            [
                "Hold an end of the rope in each hand and stand in a half squat.",
                "Rise up onto your toes and lift both arms overhead.",
                "Slam the ropes into the floor, dropping back into the half squat.",
                "Lift again straight away and repeat.",
            ],
            Tips =
            [
                "Use your whole body: stand tall to lift and drop your hips to slam.",
                "Keep your core braced and back flat at the bottom.",
                "Don't just use your arms; the power comes from your hips and lats.",
            ],
            Video = new("zJVbXefebRI"),
        },
        new("wall_ball", "Wall Ball", Quads, Other, Compound)
        {
            Secondary = [Shoulders, Glutes, Triceps],
            Category = Cardio,
            Summary = "A full squat holding a soft medicine ball, then driving up and throwing it to a target high on a wall, catching it into the next squat.",
            Steps =
            [
                "Stand about an arm's length from a wall holding a soft medicine ball at your chest.",
                "Squat down until your hips go below your knees.",
                "Drive up hard and throw the ball to a target about 3 m high.",
                "Catch the ball as it comes down and sink straight into the next squat.",
            ],
            Tips =
            [
                "Use the drive from your legs to throw; your arms just guide the ball.",
                "Keep your chest up and elbows under the ball.",
                "Don't stand too close or too far from the wall, or the ball will come back behind or in front of you.",
            ],
            Video = new("0dozPCsvQDI"),
        },
        new("med_ball_burpee", "Medicine Ball Burpee", Quads, Other, Compound)
        {
            Secondary = [Chest, Shoulders, Abs],
            Category = Cardio,
            Level = Intermediate,
            Summary = "A burpee with your hands on a medicine ball, finished by pressing the ball overhead as you stand or jump.",
            Steps =
            [
                "Stand holding a medicine ball at your chest.",
                "Squat down and place the ball on the floor, hands on its sides.",
                "Jump your feet back to a plank with your hands on the ball.",
                "Jump your feet back in, lift the ball and stand up.",
                "Press or throw the ball overhead, then repeat.",
            ],
            Tips =
            [
                "Use a ball that won't roll easily and keep your shoulders over it in the plank.",
                "Keep your core tight so your hips don't sag.",
                "Don't round your back as you pick the ball up; squat to it.",
            ],
            Video = new("VfmeazwSY2I"),
        },
        new("sled_sprint", "Sled Sprint", Quads, Other, Compound)
        {
            Secondary = [Glutes, Calves, Hamstrings],
            Category = Cardio,
            Level = Intermediate,
            Summary = "Pushing a lightly loaded sled as fast as you can over 10–30 m, building acceleration and leg power with no lowering phase.",
            Steps =
            [
                "Load the sled lightly enough that you can still move fast.",
                "Grip the high or low handles with your arms extended and lean forward about 45°.",
                "Drive the sled forward with short, powerful steps, pushing through the balls of your feet.",
                "Sprint to the finish, then rest before the next run.",
            ],
            Tips =
            [
                "Keep a straight line from your head to your back heel.",
                "Drive your knees forward and push the ground behind you.",
                "Don't load it so heavily that you slow to a walk; that's a different drill.",
            ],
            Video = new("YJbKlXj4WhI"),
        },

        // Combat conditioning
        new("shadow_boxing", "Shadow Boxing", Shoulders, Bodyweight, Compound)
        {
            Secondary = [Abs, Calves],
            Category = Cardio,
            Summary = "Throwing punch combinations and moving on your feet against an imaginary opponent, building conditioning, rhythm and technique.",
            Steps =
            [
                "Stand in your boxing stance: feet staggered, knees soft, hands up guarding your chin.",
                "Throw straight punches, hooks and uppercuts in combinations of 2–4.",
                "Move between combinations: step in, out, side to side, and slip or roll.",
                "Work in rounds of 2–3 minutes with short rests.",
            ],
            Tips =
            [
                "Rotate your hips and pivot your feet to put your body behind each punch.",
                "Bring each hand straight back to your guard after it punches.",
                "Don't fully lock out your elbows on punches thrown into the air.",
            ],
            Video = new("rdi5rX_rd58"),
        },
        new("heavy_bag_rounds", "Heavy Bag Rounds", Shoulders, Other, Compound)
        {
            Secondary = [Abs, Chest, Triceps],
            Category = Cardio,
            Summary = "Timed rounds of punching combinations on a heavy bag, with wraps and gloves, for conditioning and power.",
            Steps =
            [
                "Wrap your hands, put on bag gloves and stand in your stance an arm's length from the bag.",
                "Throw combinations of 2–4 punches, then move around the bag.",
                "Mix in bursts of fast punches with steadier work.",
                "Work in rounds of 2–3 minutes with about a minute's rest.",
            ],
            Tips =
            [
                "Punch with a straight wrist and land with the first two knuckles.",
                "Keep your hands up and your chin down between combinations.",
                "Don't punch without wraps and gloves; wrist and knuckle injuries are common.",
            ],
            Video = new("yWSVnvthdII"),
        },
        new("agility_ladder_drills", "Agility Ladder Drills", Calves, Other, Compound)
        {
            Secondary = [Quads, Glutes],
            Category = Cardio,
            Summary = "Quick footwork patterns through a flat agility ladder on the floor (one foot in each box, two feet in, in-and-out, lateral steps), training speed and coordination.",
            Steps =
            [
                "Lay the agility ladder flat on the floor and stand at one end.",
                "Pick a pattern, such as two feet in each box, and walk it through slowly.",
                "Run the pattern as fast as you can while still hitting every box cleanly.",
                "Jog back to the start and repeat, changing patterns between sets.",
            ],
            Tips =
            [
                "Stay on the balls of your feet with your knees slightly bent.",
                "Pump your arms in time with your feet.",
                "Don't look down at your feet the whole way; glance ahead once you know the pattern.",
            ],
            Video = new("z1OhMhCZa8I"),
        },
    ];
}
