// Offline exercise animations: a short looping silent video per exercise, shown on the exercise page instead of the YouTube
// video when there's no connection, in the same style as the exercise thumbnails (thumbs.mjs).
//
// How one is made:
// 1. OpenAI's image API draws a 3×2 storyboard: six keyframes of the movement, one way from start to end, from one
//    fixed camera angle.
// 2. A vision model checks it (same angle, same figure, poses in order, the right exercise); a storyboard that fails
//    is drawn again (--attempts). Ones that never pass are kept but listed in anim-review.txt to look at.
// 3. The panels are cut out and lined up on each other (the model never places the figure in quite the same spot),
//    then played start → end → start with a pause at each end, and ffmpeg's motion interpolation adds the frames in
//    between, so it moves like a video rather than stepping through poses. It's saved as a small H.264 MP4 (a GIF
//    of the same frames is about ten times bigger).
//
//   OPENAI_API_KEY=key[,key...] node anims.mjs [--only id,id] [--force] [--attempts 3] [--hint "pose detail"]
//       [--size 288] [--fps 24] [--crf 23] [--step 250] [--pause 450] [--per-minute 5] [--concurrency 1] [--from-sheets]
// Several keys (comma separated, e.g. one per OpenAI project) are each paced on their own, so they add up.
// Output: GymBook/Resources/Raw/exercise-animations/<id>.mp4, plus the storyboard in anim-sheets/<id>.webp so the
// videos can be rebuilt (other --size, --fps, --crf, --step, --pause) without paying again: --from-sheets does only that.
// Needs `sharp` (npm install sharp) and ffmpeg on the PATH. Skips exercises that already have a video unless --force.
import { readFileSync, writeFileSync, appendFileSync, existsSync, mkdirSync, mkdtempSync, readdirSync, rmSync } from "node:fs";
import { createRequire } from "node:module";
import { execFileSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const args = process.argv.slice(2);
const opt = (name, fallback) => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : fallback; };
const path = rel => new URL(rel, import.meta.url).pathname.replace(/^\/(\w:)/, "$1");
const model = opt("model", "gpt-image-2");
const quality = opt("quality", "low");
const reviewer = opt("reviewer", "gpt-5.4-mini");
const attempts = +opt("attempts", "3");
const out = opt("out", path("../../GymBook/Resources/Raw/exercise-animations/"));
const sheets = opt("sheets", path("./anim-sheets/"));
const reviewLog = path("./anim-review.txt");
const only = opt("only", null)?.split(",");
const force = args.includes("--force");
const fromSheets = args.includes("--from-sheets");
const hint = opt("hint", null);
// 288 px is sharp in the exercise page's ~360 pt box on a phone.
const size = +opt("size", "288");
// Playback: each move between keyframes takes `step` ms, shown at `fps` frames a second (the in-betweens are
// interpolated), with a `pause` ms hold at both ends of the rep.
const fps = +opt("fps", "24");
// Video quality: lower is better and bigger (H.264 CRF).
const crf = +opt("crf", "23");
const step = +opt("step", "250");
const pause = +opt("pause", "450");
const concurrency = +opt("concurrency", "1");
// OpenAI limits images per minute by account tier (5 at the lowest): each key starts requests no faster than this.
const perMinute = +opt("per-minute", "5");
const require = createRequire(opt("sharp-from", process.cwd() + "/"));
const sharp = require("sharp");
const keys = (process.env.OPENAI_API_KEY ?? "").split(",").map(k => k.trim()).filter(Boolean);
if (!fromSheets && keys.length === 0) throw new Error("Set OPENAI_API_KEY (several keys may be comma separated).");
execFileSync("ffmpeg", ["-version"], { stdio: "ignore" });

// Storyboard layout: columns × rows panels, read left to right, top to bottom.
const columns = 3, rows = 2;

// Name, summary, steps and whether it's a hold, from the region files and library.json (run check.mjs first).
const dir = new URL("../../GymBook/Src/Services/Exercises/", import.meta.url);
const lib = new Map(JSON.parse(readFileSync(new URL("./library.json", import.meta.url), "utf8")).map(x => [x.id, x]));
const unescape = s => s.replace(/\\"/g, '"');
const items = [];
for (const f of readdirSync(dir).filter(f => /^ExerciseLibrary\.\w+\.cs$/.test(f) && !f.includes("Aliases"))) {
  const text = readFileSync(new URL(f, dir), "utf8");
  for (const entry of text.split(/(?=new\("[a-z0-9_]+", ")/).slice(1)) {
    const head = /^new\("([a-z0-9_]+)", "([^"]+)"/.exec(entry);
    const summary = /Summary = "((?:[^"\\]|\\.)*)"/.exec(entry);
    if (!head || !summary || !lib.has(head[1]))
      continue;
    const stepsBlock = /Steps =\s*\[([\s\S]*?)\]/.exec(entry)?.[1] ?? "";
    const steps = [...stepsBlock.matchAll(/"((?:[^"\\]|\\.)*)"/g)].map(m => unescape(m[1]));
    items.push({ ...lib.get(head[1]), name: head[2], summary: unescape(summary[1]), steps });
  }
}

const muscleWords = { LowerBack: "lower back", Abs: "abdominals", Traps: "trapezius", Quads: "quadriceps" };
const muscles = x => [x.primary, ...(x.secondary ?? [])].map(m => muscleWords[m] ?? m.toLowerCase()).join(", ");
const equipmentWords = { Other: "", Bodyweight: "no equipment", EzBar: "an EZ curl bar", Band: "a resistance band" };

// The thumbnails' own style description (thumbs.mjs). The thumbnail itself isn't sent: given one, the model copies it
// into the first panel and turns the camera for the rest.
const prompt = (x, problems) => [
  `A storyboard of six keyframes of one repetition of the exercise "${x.name}", for a fitness app, laid out as a grid of three columns and two rows of equal square panels,`,
  `read left to right, top row first. Clean, modern flat vector illustrations in one consistent icon-set style: one athletic, gender-neutral figure drawn`,
  `in soft light grey with simple shading, the working muscles (${muscles(x)}) glowing electric blue #3F7DFF, equipment`,
  `(${equipmentWords[x.equipment] ?? x.equipment.toLowerCase()}) in mid grey, solid very dark navy background #151821.`,
  `What it is: ${x.summary}`,
  x.steps.length ? `How it's done: ${x.steps.join(" ")}` : "",
  x.hold
    ? `It's a hold, so the panels show getting into it, evenly spaced: panel 1 the set-up, panels 2 to 5 moving into the position, panel 6 the held position.`
    : `The six panels are evenly spaced moments of the movement going one way only, never back: panel 1 the starting position, panel 6 the opposite end of the movement where the repetition turns around, and panels 2 to 5 evenly in between, so each panel is a small step from the one before.`,
  `Think of them as six frames of a video filmed on a tripod: one fixed camera angle (the side or three-quarter view that shows this movement most clearly) for all six panels,`,
  `the figure never turns, never changes direction and never changes size, and it stands in exactly the same spot in every panel.`,
  `Only the moving body parts and the equipment they move change; whatever stays still (feet on the floor, a bench, a machine, a bar the figure hangs from) stays exactly in place.`,
  `Whole body and all equipment in frame in every panel, centered with some margin.`,
  `The navy background runs continuously across all six panels: no borders, gutters, frames, dividing lines, floor lines or scenery,`,
  `and no text, letters, numbers, arrows, logos or watermarks.`,
  problems ? `A previous attempt had these problems, avoid them: ${problems}` : "",
  hint,
].filter(Boolean).join(" ");

mkdirSync(out, { recursive: true });
mkdirSync(sheets, { recursive: true });
const todo = items.filter(x => (!only || only.includes(x.id)) && (force || !existsSync(`${out}/${x.id}.mp4`))
  && (!fromSheets || existsSync(`${sheets}/${x.id}.webp`)));
console.log(fromSheets
  ? `${todo.length} to rebuild from storyboards into ${out}`
  : `${todo.length} to make with ${model} (${quality}), checked by ${reviewer}, on ${keys.length} key(s) into ${out}`);

const differs = (data, i, bg, tolerance = 36) => Math.abs(data[i] - bg[0]) + Math.abs(data[i + 1] - bg[1]) + Math.abs(data[i + 2] - bg[2]) > tolerance;

// Where the storyboard divides near `at` (a fraction of the width or height): the column (row) crossing the least
// drawing, so a figure that reaches over the line, or a panel drawn a little wider, isn't cut through.
function split(data, width, height, vertical, at, bg) {
  const length = vertical ? width : height;
  const across = vertical ? height : width;
  const target = length * at;
  let best = Math.round(target), bestInk = Infinity;
  for (let p = Math.round(target - length * 0.06); p <= Math.round(target + length * 0.06); p++) {
    let ink = 0;
    for (let q = 0; q < across; q++)
      if (differs(data, (vertical ? q * width + p : p * width + q) * 3, bg))
        ink++;
    // Ties go to the line nearest the target.
    if (ink < bestInk || (ink === bestInk && Math.abs(p - target) < Math.abs(best - target)))
      [best, bestInk] = [p, ink];
  }
  return best;
}

// The storyboard's panels as square frames of `size` px, in reading order, as raw RGB, and its background colour
// (the model's navy is never quite #151821, and the frames are filled out with it).
async function frames(sheet) {
  const { data, info } = await sharp(sheet).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width, height } = info;
  const corners = [[4, 4], [width - 5, 4], [4, height - 5], [width - 5, height - 5]].map(([cx, cy]) => (cy * width + cx) * 3);
  const bg = [0, 1, 2].map(c => corners.map(i => data[i + c]).sort((p, q) => p - q)[1]);
  const xs = [0, ...Array.from({ length: columns - 1 }, (_, i) => split(data, width, height, true, (i + 1) / columns, bg)), width];
  const ys = [0, ...Array.from({ length: rows - 1 }, (_, i) => split(data, width, height, false, (i + 1) / rows, bg)), height];
  // A few px off each inner edge, in case the model drew a thin divider after all.
  const inset = Math.round(width / 250);
  const boxes = [];
  for (let r = 0; r < rows; r++)
    for (let c = 0; c < columns; c++) {
      const left = xs[c] + (c > 0 ? inset : 0), right = xs[c + 1] - (c < columns - 1 ? inset : 0);
      const top = ys[r] + (r > 0 ? inset : 0), bottom = ys[r + 1] - (r < rows - 1 ? inset : 0);
      boxes.push({ left, top, width: right - left, height: bottom - top });
    }
  const fill = { r: bg[0], g: bg[1], b: bg[2] };
  const raw = await Promise.all(boxes.map(box => sharp(sheet).extract(box)
    .resize(size, size, { fit: "contain", background: fill }).removeAlpha().raw().toBuffer()));
  // The model's background has faint noise and gradients: made one flat colour, it doesn't shimmer and compresses well.
  for (const frame of raw)
    for (let i = 0; i < frame.length; i += 3)
      if (!differs(frame, i, bg, 30))
        frame.set(bg, i);
  return { raw, bg };
}

// Which pixels are drawing rather than background.
function ink(frame, bg) {
  const mask = new Uint8Array(size * size);
  for (let i = 0; i < mask.length; i++)
    mask[i] = differs(frame, i * 3, bg) ? 1 : 0;
  return mask;
}

// How much of a's drawing lands on b's when a is moved by (dx, dy), sampling every `every` px.
function overlap(a, b, dx, dy, every) {
  let n = 0;
  for (let yy = 0; yy < size; yy += every)
    for (let xx = 0; xx < size; xx += every) {
      const sx = xx - dx, sy = yy - dy;
      if (sx >= 0 && sy >= 0 && sx < size && sy < size && a[sy * size + sx] && b[yy * size + xx])
        n++;
    }
  return n;
}

// Moves a frame so that what stays still (most of the drawing) sits on the previous frame's, or the animation
// would wobble.
function shiftTo(frame, reference, bg) {
  const a = ink(frame, bg), b = ink(reference, bg);
  const range = Math.round(size / 6);
  let best = [0, 0], bestScore = -1;
  for (let dy = -range; dy <= range; dy += 3)
    for (let dx = -range; dx <= range; dx += 3) {
      const score = overlap(a, b, dx, dy, 3);
      if (score > bestScore) [best, bestScore] = [[dx, dy], score];
    }
  const [cx, cy] = best;
  bestScore = -1;
  for (let dy = cy - 2; dy <= cy + 2; dy++)
    for (let dx = cx - 2; dx <= cx + 2; dx++) {
      const score = overlap(a, b, dx, dy, 1);
      if (score > bestScore) [best, bestScore] = [[dx, dy], score];
    }
  const [dx, dy] = best;
  const shifted = Buffer.alloc(frame.length);
  for (let i = 0; i < size * size; i++)
    shifted.set(bg, i * 3);
  for (let yy = 0; yy < size; yy++)
    for (let xx = 0; xx < size; xx++) {
      const sx = xx - dx, sy = yy - dy;
      if (sx >= 0 && sy >= 0 && sx < size && sy < size)
        frame.copy(shifted, (yy * size + xx) * 3, (sy * size + sx) * 3, (sy * size + sx) * 3 + 3);
    }
  return shifted;
}

// How different two frames are (share of pixels whose drawing differs).
function difference(a, b, bg) {
  const ma = ink(a, bg), mb = ink(b, bg);
  let n = 0;
  for (let i = 0; i < ma.length; i++)
    n += ma[i] !== mb[i] ? 1 : 0;
  return n / ma.length;
}

async function video(id, sheet) {
  const { raw, bg } = await frames(sheet);
  const keyframes = [raw[0]];
  for (const frame of raw.slice(1))
    keyframes.push(shiftTo(frame, keyframes.at(-1), bg));
  const last = keyframes.length - 1;
  // Asked for one way, but sometimes the model draws a whole rep, ending where it started: then it's played
  // forward on a loop rather than there and back.
  const distances = keyframes.map(k => difference(keyframes[0], k, bg));
  const roundTrip = distances[last] < 0.5 * Math.max(...distances);
  const holds = Math.max(0, Math.round(pause / step));
  const hold = i => Array(holds).fill(i);
  const order = roundTrip
    ? [0, ...hold(0), ...keyframes.slice(1, last).map((_, i) => i + 1), 0]
    : [0, ...hold(0), ...keyframes.slice(1).map((_, i) => i + 1), ...hold(last), ...keyframes.slice(1, last).map((_, i) => last - 1 - i), 0];

  const work = mkdtempSync(join(tmpdir(), `anim-${id}-`));
  try {
    for (const [n, i] of order.entries())
      await sharp(keyframes[i], { raw: { width: size, height: size, channels: 3 } }).png().toFile(join(work, `k${String(n).padStart(3, "0")}.png`));
    // Motion-compensated in-betweens at `fps`; the sequence ends on its first frame so the loop joins up, and that
    // repeated last frame is dropped.
    const perStep = Math.max(1, Math.round(fps * step / 1000));
    const ffmpeg = a => execFileSync("ffmpeg", ["-loglevel", "error", "-y", ...a], { stdio: ["ignore", "ignore", "pipe"] });
    ffmpeg(["-framerate", String(fps / perStep), "-i", join(work, "k%03d.png"),
      "-vf", `minterpolate=fps=${fps}:mi_mode=mci:mc_mode=aobmc:me_mode=bidir:vsbmc=1`, join(work, "f%04d.png")]);
    const count = readdirSync(work).filter(f => f.startsWith("f")).length;
    const used = (order.length - 1) * perStep;
    for (let n = used + 1; n <= count; n++)
      rmSync(join(work, `f${String(n).padStart(4, "0")}.png`));
    // H.264 Main profile, which every phone decodes in hardware; no sound.
    ffmpeg(["-framerate", String(fps), "-i", join(work, "f%04d.png"), "-c:v", "libx264", "-profile:v", "main",
      "-pix_fmt", "yuv420p", "-preset", "slow", "-crf", String(crf), "-movflags", "+faststart", "-an", join(out, `${id}.mp4`)]);
  } finally {
    rmSync(work, { recursive: true, force: true });
  }
  return roundTrip;
}

let done = 0, failed = 0, flagged = 0, inputTokens = 0, outputTokens = 0;
// Request slots per key, spaced evenly across the minute and shared by that key's workers.
const nextSlot = keys.map(() => 0);
async function slot(k) {
  const wait = Math.max(0, nextSlot[k] - performance.now());
  nextSlot[k] = Math.max(nextSlot[k], performance.now()) + 60000 / perMinute;
  await new Promise(r => setTimeout(r, wait));
}

class Fatal extends Error {}

// One OpenAI call with the key's pacing, retrying rate limits and passing errors; a used-up key or a bad request stops.
async function call(k, url, body, paced) {
  for (let attempt = 0; ; attempt++) {
    if (paced)
      await slot(k);
    let res, j;
    try {
      res = await fetch(url, {
        method: "POST",
        headers: { Authorization: `Bearer ${keys[k]}`, "Content-Type": "application/json" },
        body: JSON.stringify(body),
        signal: AbortSignal.timeout(240000),
      });
      j = await res.json();
    } catch (e) {
      if (attempt >= 5) throw e;
      await new Promise(r => setTimeout(r, 5000 * (attempt + 1)));
      continue;
    }
    const message = j.error?.message ?? `HTTP ${res.status}`;
    if (res.ok) {
      inputTokens += j.usage?.input_tokens ?? 0;
      outputTokens += j.usage?.output_tokens ?? 0;
      return j;
    }
    if (/quota|billing|incorrect api key|invalid.*key|deactivated/i.test(message))
      throw new Fatal(`key ${k + 1}: ${message}`);
    if (res.status === 429) {
      // "Please try again in 12s": wait that long (plus a little) and keep the pace after it.
      const s = +(/try again in ([\d.]+)s/.exec(message)?.[1] ?? 20);
      nextSlot[k] = Math.max(nextSlot[k], performance.now() + (s + 2) * 1000);
      if (!paced) await new Promise(r => setTimeout(r, (s + 2) * 1000));
      continue;
    }
    if (attempt >= 5 || (res.status >= 400 && res.status < 500)) throw new Error(message);
    await new Promise(r => setTimeout(r, 5000 * (attempt + 1)));
  }
}

const reviewSchema = {
  type: "object",
  additionalProperties: false,
  required: ["sameCameraAngle", "sameFigureAndPlace", "smoothOrder", "rightExercise", "cleanLayout", "problems"],
  properties: {
    sameCameraAngle: { type: "boolean", description: "All six panels are seen from the same viewpoint: the figure never rotates, turns its body or faces another way between panels (the first panel included)." },
    sameFigureAndPlace: { type: "boolean", description: "The same figure (same build, clothes, equipment) at roughly the same size in every panel. Small shifts of position are fine: they are corrected afterwards." },
    smoothOrder: { type: "boolean", description: "Read left to right, top row first, the poses follow on logically, each a small step from the last with no big jump: either one way from start to end, or one whole repetition that goes and comes back. False when poses are out of order, jump, or stay the same for several panels and then leap." },
    rightExercise: { type: "boolean", description: "The panels show the named exercise done correctly." },
    cleanLayout: { type: "boolean", description: "Exactly six panels in a 3×2 grid on a plain dark background, no text, numbers, borders or extra figures." },
    problems: { type: "string", description: "Short description of what's wrong, empty when nothing is." },
  },
};

// The vision model's verdict on a storyboard: passed, and what's wrong otherwise.
async function review(x, png, k) {
  const image = (await sharp(png).resize(960).jpeg({ quality: 85 }).toBuffer()).toString("base64");
  const j = await call(k, "https://api.openai.com/v1/responses", {
    model: reviewer,
    input: [{
      role: "user",
      content: [
        { type: "input_text", text: `This is a storyboard for a looping animation of the exercise "${x.name}" (${x.summary}). Its six panels (3 columns × 2 rows, read left to right, top row first) will be played in order as frames of a video, so it must look like a video filmed on a tripod. Judge whether it would play as a smooth, believable animation; ignore small position shifts and slight background differences, which are corrected afterwards.` },
        { type: "input_image", image_url: `data:image/jpeg;base64,${image}` },
      ],
    }],
    text: { format: { type: "json_schema", name: "storyboard_review", strict: true, schema: reviewSchema } },
  }, false);
  const text = j.output?.flatMap(o => o.content ?? []).find(c => c.type === "output_text")?.text ?? "{}";
  const v = JSON.parse(text);
  const passed = v.sameCameraAngle && v.sameFigureAndPlace && v.smoothOrder && v.rightExercise && v.cleanLayout;
  return { passed, problems: v.problems || Object.keys(v).filter(key => v[key] === false).join(", ") };
}

async function make(x, k) {
  try {
    if (fromSheets) {
      await video(x.id, readFileSync(`${sheets}/${x.id}.webp`));
      done++;
      return;
    }
    let png, verdict, problems = null;
    for (let attempt = 1; attempt <= attempts; attempt++) {
      const j = await call(k, "https://api.openai.com/v1/images/generations",
        { model, prompt: prompt(x, problems), size: "1536x1024", quality, n: 1 }, true);
      png = Buffer.from(j.data[0].b64_json, "base64");
      verdict = await review(x, png, k);
      if (verdict.passed)
        break;
      problems = verdict.problems;
      console.log(`  ${x.id} attempt ${attempt} rejected: ${problems}`);
    }
    await sharp(png).webp({ quality: 85 }).toFile(`${sheets}/${x.id}.webp`);
    await video(x.id, png);
    if (!verdict.passed) {
      flagged++;
      appendFileSync(reviewLog, `${x.id}: ${verdict.problems}\n`);
    }
    done++;
    if (done % 10 === 0) console.log(`${done}/${todo.length} (flagged ${flagged}, failed ${failed}; tokens in ${inputTokens}, out ${outputTokens})`);
  } catch (e) {
    failed++;
    console.log(`FAILED ${x.id}: ${e.message}`);
    if (e instanceof Fatal) {
      // This key is used up: its worker stops, the others carry on.
      console.log(`STOPPING key ${k + 1}`);
      throw e;
    }
  }
}

let next = 0;
const workers = (fromSheets ? [0] : keys.map((_, k) => k)).flatMap(k => Array.from({ length: concurrency }, () => k));
await Promise.all(workers.map(async k => {
  try {
    while (next < todo.length) await make(todo[next++], k);
  } catch {
    // Reported above.
  }
}));

// The app only offers the animation for exercises that have one in this build (ExerciseAnimationAssets).
const { syncManifests } = await import("./sync-thumbnail-manifest.mjs");
syncManifests();
console.log(`done ${done}, flagged ${flagged} (see anim-review.txt), failed ${failed}; tokens in ${inputTokens}, out ${outputTokens}`);
