// Validates the hand-written exercise library: ids, names, enum values, required ids.
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
const dir = "C:/GitHub/GymBook/GymBook/Src/Services/Exercises/";
const M = new Set("Chest Back Traps Shoulders Biceps Triceps Forearms Abs LowerBack Glutes Quads Hamstrings Calves Neck".split(" "));
const E = new Set("Barbell Dumbbell Machine Cable Bodyweight Kettlebell EzBar Band Other".split(" "));
const K = new Set(["Compound", "Isolation"]);
const C = new Set("Strength Plyometric Olympic Pilates Yoga Mobility Stretch Cardio".split(" "));
const L = new Set(["Beginner", "Intermediate", "Advanced"]);
const all = [];
const problems = [];
for (const f of readdirSync(dir).filter(f => f.startsWith("ExerciseLibrary.") && f.endsWith(".cs"))) {
  const src = readFileSync(dir + f, "utf8");
  const re = /new\("([^"]+)", "((?:[^"\\]|\\.)*)", (\w+), (\w+), (\w+)\)\s*\{([\s\S]*?)\n        \},/g;
  let m, n = 0;
  while ((m = re.exec(src))) {
    n++;
    const [, id, name, p, e, k, body] = m;
    const sec = (/Secondary = \[([^\]]*)\]/.exec(body)?.[1] ?? "").split(",").map(s => s.trim()).filter(Boolean);
    const cat = /Category = (?:ExerciseCategory\.)?(\w+)/.exec(body)?.[1] ?? "Strength";
    const lvl = /Level = (\w+)/.exec(body)?.[1] ?? "Beginner";
    const hold = /Hold = true/.test(body);
    const video = /Video = new\("([^"]+)"(?:, ([\d.]+|null))?(?:, ([\d.]+))?\)/.exec(body);
    if (!M.has(p)) problems.push(`${f} ${id}: bad primary ${p}`);
    if (!E.has(e)) problems.push(`${f} ${id}: bad equipment ${e}`);
    if (!K.has(k)) problems.push(`${f} ${id}: bad mechanic ${k}`);
    if (!C.has(cat)) problems.push(`${f} ${id}: bad category ${cat}`);
    if (!L.has(lvl)) problems.push(`${f} ${id}: bad level ${lvl}`);
    for (const s of sec) if (!M.has(s)) problems.push(`${f} ${id}: bad secondary ${s}`);
    if (sec.includes(p)) problems.push(`${f} ${id}: primary also secondary`);
    if (!/Summary = "/.test(body)) problems.push(`${f} ${id}: no summary`);
    if (!/^[a-z0-9_]+$/.test(id)) problems.push(`${f} ${id}: id not snake_case`);
    all.push({ file: f, id, name, primary: p, equipment: e, mechanic: k, secondary: sec, category: cat, level: lvl, hold, video: video ? [video[1], video[2], video[3]] : null });
  }
  const declared = (src.match(/\n        new\("/g) || []).length;
  if (declared !== n) problems.push(`${f}: ${declared} entries declared but ${n} parsed (format drift)`);
}
const dup = (key) => {
  const seen = new Map();
  for (const x of all) { const k = key(x).toLowerCase(); if (seen.has(k)) problems.push(`duplicate ${k}: ${seen.get(k)} and ${x.file}`); else seen.set(k, x.file); }
};
dup(x => x.id); dup(x => x.name);
const required = `bench_press incline_bench_press db_bench_press incline_db_press machine_chest_press cable_fly db_fly pec_deck push_up dips pull_up chin_up lat_pulldown barbell_row db_row seated_cable_row t_bar_row chest_supported_row inverted_row straight_arm_pulldown band_row deadlift barbell_shrug db_shrug plate_lateral_neck_flexion band_neck_flexion band_neck_extension neck_machine plate_neck_flexion plate_neck_extension harness_neck_extension neck_isometric_front_back neck_isometric_sides overhead_press db_shoulder_press machine_shoulder_press arnold_press lateral_raise cable_lateral_raise band_lateral_raise face_pull reverse_fly rear_delt_machine pike_push_up barbell_curl db_curl hammer_curl ez_bar_curl cable_curl preacher_curl incline_db_curl band_curl triceps_pushdown overhead_triceps_extension skull_crusher close_grip_bench db_overhead_extension bench_dip diamond_push_up band_pushdown wrist_curl reverse_curl crunch hanging_leg_raise cable_crunch leg_raise ab_wheel back_extension good_morning hip_thrust db_hip_thrust glute_bridge cable_kickback kb_swing back_squat front_squat leg_press hack_squat leg_extension goblet_squat db_lunge bulgarian_split_squat step_up bw_squat walking_lunge romanian_deadlift db_romanian_deadlift lying_leg_curl seated_leg_curl nordic_curl single_leg_rdl standing_calf_raise seated_calf_raise db_calf_raise bw_calf_raise box_jump broad_jump tuck_jump med_ball_chest_pass plyo_push_up med_ball_slam power_clean hang_clean push_press`.split(" ");
const ids = new Set(all.map(x => x.id));
for (const r of required) if (!ids.has(r)) problems.push(`missing required id ${r}`);
writeFileSync(new URL("./library.json", import.meta.url), JSON.stringify(all, null, 1));
const byFile = {}; for (const x of all) byFile[x.file] = (byFile[x.file] ?? 0) + 1;
console.log(byFile, "total", all.length, "with video", all.filter(x => x.video).length);
console.log(problems.length ? problems.join("\n") : "no problems");
