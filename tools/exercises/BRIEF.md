# Writing a region of GymBook's exercise library

GymBook is a .NET MAUI workout tracker. Its exercise library is being rewritten by hand, one C# file per region,
replacing a scraped dataset whose names were awful. You write ONE region file. Quality and accuracy matter more
than anything: a lifter, a physio and a Pilates instructor should all nod at every entry.

Read first: `C:\GitHub\GymBook\GymBook\Src\Services\Exercises\ExerciseLibrary.cs` (the `Def` type) and
`C:\GitHub\GymBook\GymBook\Src\Models\ExerciseDetails.cs` (categories, levels). The old hand-written library is at
`<scratchpad>\OldExerciseLibrary.cs` for reference (ids you must keep are listed in your task).

Do NOT build, run tests, or touch any other file. Write only your file.

## File format (exactly this shape)

```csharp
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
                "Don't bounce the bar off your chest or flare your elbows to 90°.",
            ],
        },
    ];
}
```

The method name is given in your task. Group entries with short `// Section` comments. Category defaults to
Strength and Level to Beginner, so only set them when different. `Hold = true` only for exercises held still
(plank, wall sit, dead hang, isometric neck holds, static stretches are NOT Hold - see Stretch below).

## The enums (no other values exist; don't invent any)

- MuscleGroup: Chest, Back, Traps, Shoulders, Biceps, Triceps, Forearms, Abs, LowerBack, Glutes, Quads, Hamstrings, Calves, Neck
- Equipment: Barbell, Dumbbell, Machine, Cable, Bodyweight, Kettlebell, EzBar, Band, Other
- Mechanic: Compound (more than one joint moves), Isolation (one joint)
- ExerciseCategory: Strength, Plyometric, Olympic, Pilates, Yoga, Mobility, Stretch, Cardio
- ExerciseLevel: Beginner, Intermediate, Advanced

Muscles the enum doesn't have go to the nearest group: obliques, transverse abdominis, hip flexors (iliopsoas)
and pelvic floor → Abs; serratus anterior → Chest; lats, rhomboids, teres major, mid back → Back; upper and middle
traps → Traps; rotator cuff and all three delt heads → Shoulders; brachialis → Biceps; brachioradialis, wrist and
grip muscles → Forearms; erector spinae and QL → LowerBack; glute medius/minimus and abductors → Glutes;
adductors → Quads; tibialis anterior and soleus → Calves; SCM, scalenes, neck flexors and extensors → Neck.

Primary = the muscle doing most of the work (what a coach would say it trains). Secondary = only muscles that
contribute meaningfully (usually 0–3), most important first. Never list the primary as a secondary. Stabilisers that
barely work don't count.

Equipment = what the user needs beyond their body. Bodyweight also covers a mat, a bench, a step or box, a
pull-up bar, dip bars, a wall, a chair, an ab wheel. Smith machine and every plate-loaded or selectorised machine →
Machine. Trap bar and landmine → Barbell. Pilates reformer, Cadillac, chair, ring, stability ball, medicine ball,
weight plate, sled, battle ropes, rowing machine, bike, rings, suspension trainer, foam roller, sandbag, neck
harness → Other (say what's needed in the name or Summary when it's not obvious). Cardio machines → Machine.

## Writing rules

- **Ids**: lowercase snake_case, short, stable, unique across the WHOLE library. Use the prefix given in your task
  if one is given. Keep the existing ids listed in your task EXACTLY (you may improve their names and content).
- **Names**: what people at a good gym or studio actually call it, Title Case, clean and short. No underscores,
  no " - ", no grip-width essays. Put the equipment first when it tells two variants apart
  ("Dumbbell Bench Press", "Cable Lateral Raise"). Names must be unique across the library; if you think another
  region might use the same name, make yours specific.
- **Summary**: 1–2 sentences, plain English, what it trains and the gist of the movement.
- **Steps**: 3–6 short imperative steps, setup → movement → return. Second person, no fluff.
- **Tips**: 2–4 items: the cues that matter most, then the common mistakes ("Don't ..."). Safety notes where
  real (spine, neck, knees).
- British/US spelling doesn't matter, but use ° for angles and – for ranges.
- Accuracy: the muscles, the mechanic and the level must be right. If you aren't sure an exercise is real and
  commonly done, leave it out. Better 60 excellent entries than 100 sloppy ones.
- No duplicates of the same movement under two names. Distinct variants (grip, angle, equipment, stance) that
  change what the exercise does or what equipment it needs are separate entries.
- Coverage: be thorough. Every common gym, home, band and bodyweight variant in your region, plus the well-known
  specialist ones. Aim for the count in your task.

## Category specifics

- Plyometric: jumps, bounds, hops, clap push-ups, throws; Olympic: snatch/clean/jerk and their variants.
- Pilates: classical mat repertoire by its proper names (The Hundred, Roll-Up, Single-Leg Stretch, ...), plus
  reformer and other apparatus work (Equipment Other). Mechanic per movement.
- Yoga: poses by their common English name with Sanskrit in the Summary ("Downward-Facing Dog (Adho Mukha Svanasana)").
- Stretch: static stretches; Hold stays false (the app treats stretches separately), say how long to hold in the steps.
- Mobility: controlled joint-range drills (CARs, 90/90 switches, cat-cow, thoracic rotations ...).
- Cardio: machines, running, rope, conditioning drills.

When done, reply with only: the file path, the number of entries, and any id you were told to keep but couldn't
place (there should be none).
