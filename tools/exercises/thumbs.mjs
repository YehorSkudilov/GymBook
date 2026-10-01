// Exercise thumbnails: one illustration per exercise, made with OpenAI's image API in one consistent style, saved as
// small WebP files shipped inside the app (GymBook/Resources/Raw/exercises/<id>.webp, ~6 KB each at low quality).
//   OPENAI_API_KEY=... node thumbs.mjs [--model gpt-image-2] [--quality medium] [--only id,id] [--out dir] [--force]
// Needs `sharp` (npm install sharp) where it runs. Skips exercises that already have a file unless --force.
import { readFileSync, writeFileSync, existsSync, mkdirSync, readdirSync } from "node:fs";
import { createRequire } from "node:module";

const args = process.argv.slice(2);
const opt = (name, fallback) => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : fallback; };
const model = opt("model", "gpt-image-2");
const quality = opt("quality", "low");
const out = opt("out", new URL("../../GymBook/Resources/Raw/exercises/", import.meta.url).pathname.replace(/^\/(\w:)/, "$1"));
const only = opt("only", null)?.split(",");
const force = args.includes("--force");
const concurrency = +opt("concurrency", "1");
// OpenAI limits images per minute by account tier (5 at the lowest): requests start no faster than this.
const perMinute = +opt("per-minute", "5");
const require = createRequire(opt("sharp-from", process.cwd() + "/"));
const sharp = require("sharp");
const key = process.env.OPENAI_API_KEY;
if (!key) throw new Error("Set OPENAI_API_KEY.");

// Name, equipment, muscles and a one-line description of each exercise, from the region files.
const dir = new URL("../../GymBook/Src/Services/Exercises/", import.meta.url);
const lib = new Map(JSON.parse(readFileSync(new URL("./library.json", import.meta.url), "utf8")).map(x => [x.id, x]));
const items = [];
for (const f of readdirSync(dir).filter(f => /^ExerciseLibrary\.\w+\.cs$/.test(f) && !f.includes("Aliases"))) {
  const text = readFileSync(new URL(f, dir), "utf8");
  for (const m of text.matchAll(/new\("([a-z0-9_]+)", "([^"]+)"[\s\S]*?Summary = "((?:[^"\\]|\\.)*)"/g))
    items.push({ ...lib.get(m[1]), name: m[2], summary: m[3].replace(/\\"/g, '"') });
}

const muscleWords = { LowerBack: "lower back", Abs: "abdominals", Traps: "trapezius", Quads: "quadriceps" };
const muscles = x => [x.primary, ...(x.secondary ?? [])].map(m => muscleWords[m] ?? m.toLowerCase()).join(", ");
const equipmentWords = { Other: "", Bodyweight: "no equipment", EzBar: "an EZ curl bar", Band: "a resistance band" };

const prompt = x => [
  `A clean, modern flat vector illustration for a fitness app's exercise thumbnail: "${x.name}".`,
  `What it is: ${x.summary}`,
  `Show one athletic, gender-neutral figure mid-movement at the most recognizable point of the exercise, in a clear side or three-quarter view, whole body and all equipment (${equipmentWords[x.equipment] ?? x.equipment.toLowerCase()}) in frame, centered with some margin.`,
  `The figure is drawn in soft light grey with simple shading; the working muscles (${muscles(x)}) glow in electric blue #3F7DFF. Equipment in mid grey.`,
  `Solid very dark navy background #151821 filling the whole square, no floor line, no scenery.`,
  `No text, letters, numbers, logos or watermarks. Same minimal style as a consistent icon set.`,
].join(" ");

mkdirSync(out, { recursive: true });
const todo = items.filter(x => (!only || only.includes(x.id)) && (force || !existsSync(`${out}/${x.id}.webp`)));
console.log(`${todo.length} to make with ${model} (${quality}) into ${out}`);

let done = 0, failed = 0, inputTokens = 0, outputTokens = 0;
// Request slots spaced evenly across the minute, shared by all workers.
let nextSlot = 0;
async function slot() {
  const wait = Math.max(0, nextSlot - performance.now());
  nextSlot = Math.max(nextSlot, performance.now()) + 60000 / perMinute;
  await new Promise(r => setTimeout(r, wait));
}

async function make(x) {
  for (let attempt = 0; attempt < 6; attempt++) {
    try {
      await slot();
      const res = await fetch("https://api.openai.com/v1/images/generations", {
        method: "POST",
        headers: { Authorization: `Bearer ${key}`, "Content-Type": "application/json" },
        body: JSON.stringify({ model, prompt: prompt(x), size: "1024x1024", quality, n: 1 }),
        signal: AbortSignal.timeout(180000),
      });
      const j = await res.json();
      if (res.status === 429) {
        // "Please try again in 12s": wait that long (plus a little) and keep the pace after it.
        const s = +(/try again in ([\d.]+)s/.exec(j.error?.message ?? "")?.[1] ?? 20);
        nextSlot = Math.max(nextSlot, performance.now() + (s + 2) * 1000);
        throw new Error("rate limited");
      }
      if (!res.ok) throw new Error(j.error?.message ?? `HTTP ${res.status}`);
      inputTokens += j.usage?.input_tokens ?? 0;
      outputTokens += j.usage?.output_tokens ?? 0;
      const png = Buffer.from(j.data[0].b64_json, "base64");
      // 384 px is sharp on a phone at the sizes the app shows (up to ~120 pt); WebP keeps it ~6 KB at low quality.
      await sharp(png).resize(384, 384).webp({ quality: 82 }).toFile(`${out}/${x.id}.webp`);
      done++;
      if (done % 10 === 0) console.log(`${done}/${todo.length} (tokens in ${inputTokens}, out ${outputTokens})`);
      return;
    } catch (e) {
      if (attempt === 5) { failed++; console.log(`FAILED ${x.id}: ${e.message}`); return; }
      if (e.message !== "rate limited")
        await new Promise(r => setTimeout(r, 5000 * (attempt + 1)));
    }
  }
}
let next = 0;
await Promise.all(Array.from({ length: concurrency }, async () => { while (next < todo.length) await make(todo[next++]); }));
console.log(`done ${done}, failed ${failed}; tokens in ${inputTokens}, out ${outputTokens}`);
