// Builds GymBook/Src/Services/ExerciseCatalog.json from free-exercise-db (public domain, Unlicense):
// https://github.com/yuhonas/free-exercise-db
//
//   node tools/import-exercises.mjs
//
// The dataset is pinned to a commit so image URLs baked into the app never move. To update, change COMMIT,
// re-run, and review the diff. The app's own curated exercises (ExerciseLibrary.cs) keep their ids and
// classification; CURATED below links each to its dataset entry for images and step-by-step instructions.

import { writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const COMMIT = "a859101d633a01c4a1a920d6a8ce41dabba0705f";
const DATA_URL = `https://raw.githubusercontent.com/yuhonas/free-exercise-db/${COMMIT}/dist/exercises.json`;
const IMAGE_BASE = `https://raw.githubusercontent.com/yuhonas/free-exercise-db/${COMMIT}/exercises/`;
const OUT = fileURLToPath(new URL("../GymBook/Src/Services/ExerciseCatalog.json", import.meta.url));

// Curated id -> dataset id. Curated exercises missing here have no close match in the dataset.
const CURATED = {
  bench_press: "Barbell_Bench_Press_-_Medium_Grip",
  incline_bench_press: "Barbell_Incline_Bench_Press_-_Medium_Grip",
  db_bench_press: "Dumbbell_Bench_Press",
  incline_db_press: "Incline_Dumbbell_Press",
  machine_chest_press: "Leverage_Chest_Press",
  cable_fly: "Cable_Crossover",
  db_fly: "Dumbbell_Flyes",
  pec_deck: "Butterfly",
  push_up: "Pushups",
  dips: "Dips_-_Chest_Version",
  pull_up: "Pullups",
  chin_up: "Chin-Up",
  lat_pulldown: "Wide-Grip_Lat_Pulldown",
  barbell_row: "Bent_Over_Barbell_Row",
  db_row: "One-Arm_Dumbbell_Row",
  seated_cable_row: "Seated_Cable_Rows",
  t_bar_row: "T-Bar_Row_with_Handle",
  chest_supported_row: "Lying_T-Bar_Row",
  inverted_row: "Inverted_Row",
  straight_arm_pulldown: "Straight-Arm_Pulldown",
  deadlift: "Barbell_Deadlift",
  barbell_shrug: "Barbell_Shrug",
  db_shrug: "Dumbbell_Shrug",
  overhead_press: "Standing_Military_Press",
  db_shoulder_press: "Dumbbell_Shoulder_Press",
  machine_shoulder_press: "Machine_Shoulder_Military_Press",
  arnold_press: "Arnold_Dumbbell_Press",
  lateral_raise: "Side_Lateral_Raise",
  cable_lateral_raise: "Cable_Seated_Lateral_Raise",
  band_lateral_raise: "Lateral_Raise_-_With_Bands",
  face_pull: "Face_Pull",
  reverse_fly: "Reverse_Flyes",
  rear_delt_machine: "Reverse_Machine_Flyes",
  barbell_curl: "Barbell_Curl",
  db_curl: "Dumbbell_Bicep_Curl",
  hammer_curl: "Hammer_Curls",
  ez_bar_curl: "EZ-Bar_Curl",
  cable_curl: "Standing_Biceps_Cable_Curl",
  preacher_curl: "Preacher_Curl",
  incline_db_curl: "Incline_Dumbbell_Curl",
  triceps_pushdown: "Triceps_Pushdown",
  overhead_triceps_extension: "Cable_Rope_Overhead_Triceps_Extension",
  skull_crusher: "EZ-Bar_Skullcrusher",
  close_grip_bench: "Close-Grip_Barbell_Bench_Press",
  db_overhead_extension: "Standing_Dumbbell_Triceps_Extension",
  bench_dip: "Bench_Dips",
  diamond_push_up: "Push-Ups_-_Close_Triceps_Position",
  wrist_curl: "Seated_Dumbbell_Palms-Up_Wrist_Curl",
  reverse_curl: "Reverse_Barbell_Curl",
  crunch: "Crunches",
  hanging_leg_raise: "Hanging_Leg_Raise",
  cable_crunch: "Cable_Crunch",
  leg_raise: "Flat_Bench_Lying_Leg_Raise",
  ab_wheel: "Ab_Roller",
  back_extension: "Hyperextensions_Back_Extensions",
  good_morning: "Good_Morning",
  hip_thrust: "Barbell_Hip_Thrust",
  glute_bridge: "Butt_Lift_Bridge",
  cable_kickback: "One-Legged_Cable_Kickback",
  kb_swing: "One-Arm_Kettlebell_Swings",
  back_squat: "Barbell_Full_Squat",
  front_squat: "Front_Barbell_Squat",
  leg_press: "Leg_Press",
  hack_squat: "Hack_Squat",
  leg_extension: "Leg_Extensions",
  goblet_squat: "Goblet_Squat",
  db_lunge: "Dumbbell_Lunges",
  bulgarian_split_squat: "Split_Squat_with_Dumbbells",
  step_up: "Dumbbell_Step_Ups",
  bw_squat: "Bodyweight_Squat",
  walking_lunge: "Bodyweight_Walking_Lunge",
  romanian_deadlift: "Romanian_Deadlift",
  db_romanian_deadlift: "Stiff-Legged_Dumbbell_Deadlift",
  lying_leg_curl: "Lying_Leg_Curls",
  seated_leg_curl: "Seated_Leg_Curl",
  nordic_curl: "Natural_Glute_Ham_Raise",
  standing_calf_raise: "Standing_Calf_Raises",
  seated_calf_raise: "Seated_Calf_Raise",
  db_calf_raise: "Standing_Dumbbell_Calf_Raise",
};

// Stretches, cardio and foam rolling don't fit set/rep logging.
const SKIP_CATEGORIES = new Set(["stretching", "cardio"]);
const SKIP_EQUIPMENT = new Set(["foam roll"]);

// Dataset muscles -> GymBook's MuscleGroup (which has no separate lats, adductors, abductors or neck).
const MUSCLES = {
  abdominals: "Abs", abductors: "Glutes", adductors: "Quads", biceps: "Biceps", calves: "Calves",
  chest: "Chest", forearms: "Forearms", glutes: "Glutes", hamstrings: "Hamstrings", lats: "Back",
  "lower back": "LowerBack", "middle back": "Back", neck: "Traps", quadriceps: "Quads",
  shoulders: "Shoulders", traps: "Traps", triceps: "Triceps",
};

// Dataset equipment -> GymBook's Equipment. Unlisted equipment is almost always bodyweight in the dataset.
const EQUIPMENT = {
  barbell: "Barbell", dumbbell: "Dumbbell", machine: "Machine", cable: "Cable", "body only": "Bodyweight",
  kettlebells: "Kettlebell", "e-z curl bar": "EzBar", bands: "Band",
  other: "Other", "medicine ball": "Other", "exercise ball": "Other",
};

const cap = s => s && s[0].toUpperCase() + s.slice(1);

const data = await (await fetch(DATA_URL)).json();
const byId = new Map(data.map(e => [e.id, e]));

const details = e => ({
  level: cap(e.level),
  force: cap(e.force),
  category: cap(e.category),
  steps: e.instructions.map(s => s.trim()).filter(Boolean),
  images: e.images,
});

const out = [];

for (const [id, sourceId] of Object.entries(CURATED)) {
  const e = byId.get(sourceId);
  if (!e) throw new Error(`Curated ${id} maps to missing dataset entry ${sourceId}`);
  out.push({ id, ...details(e) });
}

const linked = new Set(Object.values(CURATED));
for (const e of data) {
  if (linked.has(e.id) || SKIP_CATEGORIES.has(e.category) || SKIP_EQUIPMENT.has(e.equipment))
    continue;
  const muscles = [...e.primaryMuscles, ...e.secondaryMuscles].map(m => MUSCLES[m]);
  if (muscles.length === 0 || muscles.includes(undefined))
    throw new Error(`Unmapped muscle in ${e.id}: ${[...e.primaryMuscles, ...e.secondaryMuscles]}`);
  const primary = muscles[0];
  const equipment = e.equipment == null ? "Bodyweight" : EQUIPMENT[e.equipment];
  if (!equipment)
    throw new Error(`Unmapped equipment in ${e.id}: ${e.equipment}`);
  out.push({
    id: e.id,
    name: e.name,
    primary,
    secondary: [...new Set(muscles.slice(1))].filter(m => m !== primary),
    equipment,
    mechanic: e.mechanic === "isolation" ? "Isolation" : "Compound",
    ...details(e),
  });
}

writeFileSync(OUT, JSON.stringify({ source: `free-exercise-db@${COMMIT}`, imageBase: IMAGE_BASE, exercises: out }) + "\n");
console.log(`Wrote ${out.length} entries (${Object.keys(CURATED).length} curated, ${out.length - Object.keys(CURATED).length} new) to ${OUT}`);
