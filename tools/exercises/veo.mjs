// Offline exercise animations: a short looping video per exercise, made by Google's Veo (Gemini API) from the
// exercise's thumbnail. The thumbnail is both the first and the last frame, so the same figure does one rep and the
// clip loops; it's then cropped to a square around the movement, made silent and saved as
// GymBook/Resources/Raw/exercise-animations/<id>.mp4 (the exercise page plays it offline instead of the YouTube video).
//   GEMINI_API_KEY=... node veo.mjs --max-clips 27 [--only id,id] [--force] [--model veo-3.1-lite-generate-preview]
//       [--concurrency 3] [--size 288] [--sharp-from dir]
// --max-clips is a hard limit on clips generated (Google charges per generated second: Lite is $0.05/s, so an 8 s clip
// is $0.40; refused clips aren't charged). Attempts stop at twice that. The raw 1280×720 clip is kept in
// veo-raw/<id>.mp4 so it can be re-cropped without paying again (--recrop). Needs sharp and ffmpeg; in a cloud
// session Node's fetch needs NODE_USE_ENV_PROXY=1 to use the proxy.
import { readFileSync, writeFileSync, existsSync, mkdirSync, readdirSync, mkdtempSync, rmSync } from "node:fs";
import { createRequire } from "node:module";
import { execFileSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const args = process.argv.slice(2);
const opt = (name, fallback) => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : fallback; };
const path = rel => new URL(rel, import.meta.url).pathname.replace(/^\/(\w:)/, "$1");
const sharp = createRequire(opt("sharp-from", process.cwd() + "/"))("sharp");
const model = opt("model", "veo-3.1-lite-generate-preview");
const pricePerSecond = { "veo-3.1-lite-generate-preview": 0.05, "veo-3.1-fast-generate-preview": 0.10, "veo-3.1-generate-preview": 0.40 }[model] ?? 0.40;
const seconds = 8; // Veo only takes the first and last frame together at 8 s
const maxClips = +opt("max-clips", "0");
const concurrency = +opt("concurrency", "3");
const size = opt("size", "288");
const only = opt("only", null)?.split(",");
const force = args.includes("--force");
const recrop = args.includes("--recrop");
const out = path("../../GymBook/Resources/Raw/exercise-animations/");
const raw = path("./veo-raw/");
const thumbs = path("../../GymBook/Resources/Raw/exercises/");
const key = process.env.GEMINI_API_KEY;
if (!recrop && !key) throw new Error("Set GEMINI_API_KEY.");
if (!recrop && !(maxClips > 0)) throw new Error("Give --max-clips: the most clips this run may pay for.");

// Name, summary, steps and muscles of every exercise, from the region files and library.json.
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
    const steps = [...(/Steps =\s*\[([\s\S]*?)\]/.exec(entry)?.[1] ?? "").matchAll(/"((?:[^"\\]|\\.)*)"/g)].map(m => unescape(m[1]));
    items.push({ ...lib.get(head[1]), name: head[2], summary: unescape(summary[1]), steps });
  }
}

const muscleWords = { LowerBack: "lower back", Abs: "abs", Traps: "traps", Quads: "quads" };
const muscles = x => {
  const list = [x.primary, ...(x.secondary ?? [])].map(m => muscleWords[m] ?? m.toLowerCase());
  return list.length > 1 ? `the ${list.slice(0, -1).join(", ")} and ${list.at(-1)}` : `the ${list[0]}`;
};

const prompt = x => [
  `Smooth 2D animation in exactly the style of this image: the same single figure, the same flat vector look, colours and simple shading, on the same plain dark navy background.`,
  `The figure performs one slow, controlled, complete repetition of the exercise "${x.name}" with perfect form, starting from the position shown and ending back in exactly the position shown.`,
  `How the exercise is done: ${x.steps.join(" ")}`,
  `Only ${muscles(x)} glow blue, exactly as in this image, with the same blue the whole time; every other part of the body stays light grey and never glows.`,
  `Locked-off static camera: no camera movement, no zoom, no cuts. The figure keeps exactly the same body, proportions and size the whole time and never turns around. The equipment stays the same.`,
  `Nothing else appears: no text, no other people, no scenery, no floor, no background change. Silent: no speech, no voice, no music, only quiet ambient room tone.`,
].join(" ");

mkdirSync(out, { recursive: true });
mkdirSync(raw, { recursive: true });

// The raw clip → a square around everything the figure covers during the clip (plus a margin), silent, small.
async function crop(id) {
  const frames = mkdtempSync(join(tmpdir(), "veo-"));
  try {
    execFileSync("ffmpeg", ["-loglevel", "error", "-i", `${raw}/${id}.mp4`, "-vf", "fps=4,scale=640:360", join(frames, "f%03d.png")]);
    let minX = 1e9, minY = 1e9, maxX = 0, maxY = 0;
    for (const f of readdirSync(frames)) {
      const { data, info } = await sharp(join(frames, f)).removeAlpha().raw().toBuffer({ resolveWithObject: true });
      const bg = [data[0], data[1], data[2]];
      for (let y = 0; y < info.height; y++)
        for (let x = 0; x < info.width; x++) {
          const i = (y * info.width + x) * 3;
          if (Math.abs(data[i] - bg[0]) + Math.abs(data[i + 1] - bg[1]) + Math.abs(data[i + 2] - bg[2]) > 45) {
            minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y);
          }
        }
    }
    const side = Math.min(720, Math.round(Math.max(maxX - minX, maxY - minY) * 2 * 1.12));
    const cx = (minX + maxX), cy = (minY + maxY);
    const x = Math.round(Math.min(1280 - side, Math.max(0, cx - side / 2))), y = Math.round(Math.min(720 - side, Math.max(0, cy - side / 2)));
    execFileSync("ffmpeg", ["-loglevel", "error", "-y", "-i", `${raw}/${id}.mp4`, "-vf", `crop=${side}:${side}:${x}:${y},scale=${size}:${size}:flags=lanczos`,
      "-an", "-c:v", "libx264", "-profile:v", "main", "-pix_fmt", "yuv420p", "-crf", "22", "-preset", "slow", "-movflags", "+faststart", `${out}/${id}.mp4`]);
  } finally {
    rmSync(frames, { recursive: true, force: true });
  }
}

const api = "https://generativelanguage.googleapis.com/v1beta";
let clips = 0, attempts = 0, refused = 0;
const maxAttempts = maxClips * 2;

// One clip: the thumbnail centred on its own background as a 16:9 first and last frame; retried while Google refuses
// it (its safety and audio filters refuse some at random; those aren't charged).
async function make(x) {
  const thumb = `${thumbs}/${x.id}.webp`;
  const { dominant } = await sharp(thumb).stats();
  const ref = await sharp(thumb).resize(720, 720, { kernel: "lanczos3" }).extend({ left: 280, right: 280, background: dominant }).png().toBuffer();
  const image = { bytesBase64Encoded: ref.toString("base64"), mimeType: "image/png" };
  while (clips < maxClips && attempts < maxAttempts) {
    attempts++;
    const res = await fetch(`${api}/models/${model}:predictLongRunning`, {
      method: "POST",
      headers: { "x-goog-api-key": key, "Content-Type": "application/json" },
      body: JSON.stringify({ instances: [{ prompt: prompt(x), image, lastFrame: image }], parameters: { aspectRatio: "16:9", durationSeconds: seconds } }),
    });
    let op = await res.json();
    if (!res.ok) {
      if (res.status === 429 && !/billing|plan/i.test(op.error?.message ?? "")) {
        attempts--;
        await new Promise(r => setTimeout(r, 30000));
        continue;
      }
      throw new Error(op.error?.message ?? `HTTP ${res.status}`);
    }
    while (!op.done) {
      await new Promise(r => setTimeout(r, 8000));
      op = await (await fetch(`${api}/${op.name}`, { headers: { "x-goog-api-key": key } })).json();
    }
    const uri = op.response?.generateVideoResponse?.generatedSamples?.[0]?.video?.uri;
    if (!uri) {
      refused++;
      console.log(`  ${x.id} refused: ${(op.response?.generateVideoResponse?.raiMediaFilteredReasons?.[0] ?? op.error?.message ?? "no video").slice(0, 120)}`);
      continue;
    }
    clips++;
    const video = await fetch(uri, { headers: { "x-goog-api-key": key } });
    writeFileSync(`${raw}/${x.id}.mp4`, Buffer.from(await video.arrayBuffer()));
    await crop(x.id);
    console.log(`${x.id} ✓ (${clips}/${maxClips} clips, ~$${(clips * seconds * pricePerSecond).toFixed(2)})`);
    return;
  }
}

if (recrop) {
  for (const x of items.filter(x => (!only || only.includes(x.id)) && existsSync(`${raw}/${x.id}.mp4`)))
    await crop(x.id);
} else {
  const todo = items.filter(x => (!only || only.includes(x.id)) && (force || !existsSync(`${out}/${x.id}.mp4`)) && existsSync(`${thumbs}/${x.id}.webp`));
  console.log(`${todo.length} to make with ${model}, at most ${maxClips} clips (~$${(maxClips * seconds * pricePerSecond).toFixed(2)})`);
  let next = 0;
  await Promise.all(Array.from({ length: concurrency }, async () => {
    while (next < todo.length && clips < maxClips && attempts < maxAttempts) {
      const x = todo[next++];
      try {
        await make(x);
      } catch (e) {
        console.log(`FAILED ${x.id}: ${e.message}`);
        if (/billing|plan|quota|key/i.test(e.message))
          next = todo.length;
      }
    }
  }));
  console.log(`made ${clips} clips (~$${(clips * seconds * pricePerSecond).toFixed(2)}), ${refused} refused (not charged), ${attempts} attempts`);
}
const { syncManifests } = await import("./sync-thumbnail-manifest.mjs");
syncManifests();
