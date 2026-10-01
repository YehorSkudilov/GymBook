// Offline exercise animations: a short looping GIF per exercise, shown on the exercise page instead of the YouTube
// video when there's no connection. Each one is drawn by OpenAI's image API from the exercise's thumbnail (so it has
// the same figure, colours and style): one request returns a 2×2 storyboard of four keyframes of the movement, which
// is cut into frames and played start → end → start, like one rep.
//   OPENAI_API_KEY=key[,key...] node anims.mjs [--model gpt-image-2] [--quality low] [--only id,id] [--force]
//       [--size 288] [--pause 700] [--step 240] [--per-minute 5] [--concurrency 1] [--fidelity high] [--hint "pose detail"] [--from-sheets]
// Several keys (comma separated, e.g. one per OpenAI project) are each paced on their own, so they add up.
// Output: GymBook/Resources/Raw/exercise-animations/<id>.gif, plus the storyboard itself in anim-sheets/<id>.webp so
// the GIFs can be rebuilt (other --size, --pause, --step) without paying again: --from-sheets does only that, no key needed.
// Needs `sharp` (npm install sharp) where it runs. Skips exercises that already have a GIF unless --force.
import { readFileSync, writeFileSync, existsSync, mkdirSync, readdirSync } from "node:fs";
import { createRequire } from "node:module";

const args = process.argv.slice(2);
const opt = (name, fallback) => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : fallback; };
const path = rel => new URL(rel, import.meta.url).pathname.replace(/^\/(\w:)/, "$1");
const model = opt("model", "gpt-image-2");
const quality = opt("quality", "low");
const out = opt("out", path("../../GymBook/Resources/Raw/exercise-animations/"));
const thumbs = path("../../GymBook/Resources/Raw/exercises/");
const sheets = opt("sheets", path("./anim-sheets/"));
const only = opt("only", null)?.split(",");
const force = args.includes("--force");
const fromSheets = args.includes("--from-sheets");
const hint = opt("hint", null);
// Input fidelity (how closely the reference is kept); only sent when given, as not every model takes it.
const fidelity = opt("fidelity", null);
// 288 px is sharp in the exercise page's ~360 pt box on a phone and keeps a GIF around 60-100 KB.
const size = +opt("size", "288");
const concurrency = +opt("concurrency", "1");
// OpenAI limits images per minute by account tier (5 at the lowest): each key starts requests no faster than this.
const perMinute = +opt("per-minute", "5");
// How long each frame shows (ms): a pause at both ends of the rep, quick in between.
const pause = +opt("pause", "700"), step = +opt("step", "240");
const require = createRequire(opt("sharp-from", process.cwd() + "/"));
const sharp = require("sharp");
const keys = (process.env.OPENAI_API_KEY ?? "").split(",").map(k => k.trim()).filter(Boolean);
if (!fromSheets && keys.length === 0) throw new Error("Set OPENAI_API_KEY (several keys may be comma separated).");

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

// With the thumbnail as a reference the model tends to copy it into the first panel and turn the camera for the
// rest, so by default the storyboard is drawn from the thumbnails' own style description instead (--reference to
// send the thumbnail anyway).
const useReference = args.includes("--reference");
const prompt = x => [
  useReference
    ? `The attached image is the thumbnail of the exercise "${x.name}" in a fitness app. Turn it into a 2×2 storyboard of four keyframes of one repetition, drawn exactly in the attached image's style:`
    : `A 2×2 storyboard of four keyframes of one repetition of the exercise "${x.name}", for a fitness app, as clean, modern flat vector illustrations in one consistent icon-set style:`,
  `one athletic, gender-neutral figure drawn in soft light grey with simple shading, the working muscles (${muscles(x)}) glowing electric blue #3F7DFF,`,
  `equipment (${equipmentWords[x.equipment] ?? x.equipment.toLowerCase()}) in mid grey, solid very dark navy background #151821.`,
  `What it is: ${x.summary}`,
  x.steps.length ? `How it's done: ${x.steps.join(" ")}` : "",
  x.hold
    ? `It's a hold, so the panels show getting into it: top-left the set-up, top-right and bottom-left moving into the position, bottom-right the held position.`
    : `The four panels go one way only, from one end of the movement to the other, never back: top-left the starting position, top-right a third of the way, bottom-left two thirds of the way, bottom-right the opposite end of the movement, where the repetition turns around (so the bottom-right pose is the one most different from the top-left).`,
  useReference
    ? `Keep the attached image's camera angle exactly: all four panels are seen from the same viewpoint as the attached image, never turning the figure or the camera between panels.`
    : `Pick the one camera angle that shows this movement most clearly (a side or three-quarter view) and draw all four panels from exactly that viewpoint, never turning the figure or the camera between panels.`,
  `Every panel uses that same camera angle, the same scale and the same position in its panel, so that played one after another they animate smoothly:`,
  `only the moving body parts and the equipment they move change; whatever stays still (feet on the floor, a bench, a machine) stays exactly in place.`,
  `Whole body and all equipment in frame in every panel, centered with some margin.`,
  `Each panel is exactly one quarter of the square image. The navy background runs continuously across all four panels:`,
  `no borders, gutters, frames, dividing lines, floor lines or scenery, and no text, letters, numbers, arrows, logos or watermarks.`,
  hint,
].filter(Boolean).join(" ");

mkdirSync(out, { recursive: true });
mkdirSync(sheets, { recursive: true });
const todo = items.filter(x => (!only || only.includes(x.id)) && (force || !existsSync(`${out}/${x.id}.gif`))
  && (fromSheets ? existsSync(`${sheets}/${x.id}.webp`) : existsSync(`${thumbs}/${x.id}.webp`)));
console.log(fromSheets
  ? `${todo.length} to rebuild from storyboards into ${out}`
  : `${todo.length} to make with ${model} (${quality}) on ${keys.length} key(s) into ${out}`);

// Where the storyboard divides: the column (row) near the middle crossing the least drawing, so a figure that
// reaches over the middle line, or a panel the model drew a little wider, isn't cut through.
const background = [0x15, 0x18, 0x21];
function split(data, width, height, vertical) {
  const length = vertical ? width : height;
  const across = vertical ? height : width;
  let best = length / 2, bestInk = Infinity;
  for (let p = Math.round(length * 0.42); p <= Math.round(length * 0.58); p++) {
    let ink = 0;
    for (let q = 0; q < across; q++) {
      const i = ((vertical ? q * width + p : p * width + q)) * 3;
      if (Math.abs(data[i] - background[0]) + Math.abs(data[i + 1] - background[1]) + Math.abs(data[i + 2] - background[2]) > 36)
        ink++;
    }
    // Ties go to the line nearest the middle.
    if (ink < bestInk || (ink === bestInk && Math.abs(p - length / 2) < Math.abs(best - length / 2)))
      [best, bestInk] = [p, ink];
  }
  return best;
}

// The storyboard as four equal square frames of `size` px, in panel order, as raw RGB, and its background colour
// (the model's navy is never quite #151821, and the frames are filled out with it).
async function frames(sheet) {
  const { data, info } = await sharp(sheet).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width, height } = info;
  const corners = [[4, 4], [width - 5, 4], [4, height - 5], [width - 5, height - 5]].map(([cx, cy]) => (cy * width + cx) * 3);
  const bg = [0, 1, 2].map(c => corners.map(i => data[i + c]).sort((p, q) => p - q)[1]);
  const fill = { r: bg[0], g: bg[1], b: bg[2] };
  const x = split(data, width, height, true), y = split(data, width, height, false);
  // A few px off each inner edge, in case the model drew a thin divider after all.
  const inset = Math.round(width / 170);
  const boxes = [
    { left: 0, top: 0, width: x - inset, height: y - inset },
    { left: x + inset, top: 0, width: width - x - inset, height: y - inset },
    { left: 0, top: y + inset, width: x - inset, height: height - y - inset },
    { left: x + inset, top: y + inset, width: width - x - inset, height: height - y - inset },
  ];
  const raw = await Promise.all(boxes.map(box => sharp(sheet).extract(box)
    .resize(size, size, { fit: "contain", background: fill }).removeAlpha().raw().toBuffer()));
  return { raw, bg };
}

// Which pixels are drawing rather than background.
const ink = (frame, bg) => {
  const mask = new Uint8Array(size * size);
  for (let i = 0; i < mask.length; i++)
    mask[i] = Math.abs(frame[i * 3] - bg[0]) + Math.abs(frame[i * 3 + 1] - bg[1]) + Math.abs(frame[i * 3 + 2] - bg[2]) > 36 ? 1 : 0;
  return mask;
};

// How much of a's drawing lands on b's when a is moved by (dx, dy), sampling every `step` px.
function overlap(a, b, dx, dy, step) {
  let n = 0;
  for (let yy = 0; yy < size; yy += step)
    for (let xx = 0; xx < size; xx += step) {
      const sx = xx - dx, sy = yy - dy;
      if (sx >= 0 && sy >= 0 && sx < size && sy < size && a[sy * size + sx] && b[yy * size + xx])
        n++;
    }
  return n;
}

// The model doesn't place the figure in exactly the same spot in every panel: each frame is moved so that what
// stays still (most of the drawing) sits on the first frame's, or the animation would wobble.
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
  for (let dy = cy - 2; dy <= cy + 2; dy++)
    for (let dx = cx - 2; dx <= cx + 2; dx++) {
      const score = overlap(a, b, dx, dy, 1);
      if (score > bestScore || (dx === cx && dy === cy && score >= bestScore)) [best, bestScore] = [[dx, dy], score];
    }
  const [dx, dy] = best;
  const out = Buffer.alloc(frame.length);
  for (let i = 0; i < size * size; i++)
    out.set(bg, i * 3);
  for (let yy = 0; yy < size; yy++)
    for (let xx = 0; xx < size; xx++) {
      const sx = xx - dx, sy = yy - dy;
      if (sx >= 0 && sy >= 0 && sx < size && sy < size)
        frame.copy(out, (yy * size + xx) * 3, (sy * size + sx) * 3, (sy * size + sx) * 3 + 3);
    }
  return out;
}

// How different two frames are (share of pixels whose drawing differs).
function difference(a, b, bg) {
  const ma = ink(a, bg), mb = ink(b, bg);
  let n = 0;
  for (let i = 0; i < ma.length; i++)
    n += ma[i] !== mb[i] ? 1 : 0;
  return n / ma.length;
}

async function gif(id, sheet) {
  const { raw, bg } = await frames(sheet);
  const f = [raw[0], ...raw.slice(1).map(frame => shiftTo(frame, raw[0], bg))];
  // Asked for start → end, but sometimes the model draws a whole rep, ending where it started: then the third panel
  // is the turning point, and the rep is played 1 → 2 → 3 → 2.
  const roundTrip = difference(f[0], f[3], bg) < 0.6 * difference(f[0], f[2], bg);
  const [order, delays] = roundTrip
    ? [[0, 1, 2, 1], [pause, step, pause, step]]
    : [[0, 1, 2, 3, 2, 1], [pause, step, step, pause, step, step]];
  const pages = order.map(i => sharp(f[i], { raw: { width: size, height: size, channels: 3 } }).png().toBuffer());
  await sharp(await Promise.all(pages), { join: { animated: true } })
    .gif({ loop: 0, delay: delays, colours: 128, dither: 0, effort: 10, interFrameMaxError: 6, interPaletteMaxError: 3 })
    .toFile(`${out}/${id}.gif`);
  return roundTrip;
}

let done = 0, failed = 0, inputTokens = 0, outputTokens = 0;
// Request slots per key, spaced evenly across the minute and shared by that key's workers.
const nextSlot = keys.map(() => 0);
async function slot(k) {
  const wait = Math.max(0, nextSlot[k] - performance.now());
  nextSlot[k] = Math.max(nextSlot[k], performance.now()) + 60000 / perMinute;
  await new Promise(r => setTimeout(r, wait));
}

async function make(x, k) {
  if (fromSheets) {
    try {
      await gif(x.id, readFileSync(`${sheets}/${x.id}.webp`));
      done++;
    } catch (e) {
      failed++;
      console.log(`FAILED ${x.id}: ${e.message}`);
    }
    return;
  }
  const reference = useReference ? await sharp(`${thumbs}/${x.id}.webp`).resize(1024, 1024).png().toBuffer() : null;
  for (let attempt = 0; attempt < 6; attempt++) {
    try {
      await slot(k);
      let res;
      if (useReference) {
        const form = new FormData();
        form.append("model", model);
        form.append("prompt", prompt(x));
        form.append("image[]", new Blob([reference], { type: "image/png" }), `${x.id}.png`);
        form.append("size", "1024x1024");
        form.append("quality", quality);
        form.append("n", "1");
        if (fidelity)
          form.append("input_fidelity", fidelity);
        res = await fetch("https://api.openai.com/v1/images/edits", {
          method: "POST",
          headers: { Authorization: `Bearer ${keys[k]}` },
          body: form,
          signal: AbortSignal.timeout(240000),
        });
      } else {
        res = await fetch("https://api.openai.com/v1/images/generations", {
          method: "POST",
          headers: { Authorization: `Bearer ${keys[k]}`, "Content-Type": "application/json" },
          body: JSON.stringify({ model, prompt: prompt(x), size: "1024x1024", quality, n: 1 }),
          signal: AbortSignal.timeout(240000),
        });
      }
      const j = await res.json();
      if (res.status === 429 && !/quota|billing/i.test(j.error?.message ?? "")) {
        // "Please try again in 12s": wait that long (plus a little) and keep the pace after it.
        const s = +(/try again in ([\d.]+)s/.exec(j.error?.message ?? "")?.[1] ?? 20);
        nextSlot[k] = Math.max(nextSlot[k], performance.now() + (s + 2) * 1000);
        throw new Error("rate limited");
      }
      if (!res.ok) throw new Error(j.error?.message ?? `HTTP ${res.status}`);
      inputTokens += j.usage?.input_tokens ?? 0;
      outputTokens += j.usage?.output_tokens ?? 0;
      const png = Buffer.from(j.data[0].b64_json, "base64");
      await sharp(png).webp({ quality: 85 }).toFile(`${sheets}/${x.id}.webp`);
      await gif(x.id, png);
      done++;
      if (done % 10 === 0) console.log(`${done}/${todo.length} (tokens in ${inputTokens}, out ${outputTokens})`);
      return;
    } catch (e) {
      if (attempt === 5 || /quota|billing|invalid.*key|incorrect api key/i.test(e.message)) {
        failed++;
        console.log(`FAILED ${x.id}: ${e.message}`);
        return;
      }
      if (e.message !== "rate limited")
        await new Promise(r => setTimeout(r, 5000 * (attempt + 1)));
    }
  }
}

let next = 0;
const workers = (fromSheets ? [0] : keys.map((_, k) => k)).flatMap(k => Array.from({ length: concurrency }, () => k));
await Promise.all(workers.map(async k => { while (next < todo.length) await make(todo[next++], k); }));

// The app only offers the animation for exercises that have one in this build (ExerciseAnimationAssets).
const { syncManifests } = await import("./sync-thumbnail-manifest.mjs");
syncManifests();
console.log(`done ${done}, failed ${failed}; tokens in ${inputTokens}, out ${outputTokens}`);
