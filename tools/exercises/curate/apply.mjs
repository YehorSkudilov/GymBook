// Writes verified video picks (curate/results/<chunk>.json, never *.unverified.json) into the exercise library files:
// each exercise's `Video = new(...)` line becomes the pick's id with its Start/End. Logs every change to
// logs/videos-curated.md. Run `node check.mjs` afterwards.   node curate/apply.mjs [chunk...]
import { readFileSync, writeFileSync, readdirSync, appendFileSync } from "node:fs";

const results = new URL("./results/", import.meta.url);
const lib = new URL("../../../GymBook/Src/Services/Exercises/", import.meta.url);
const only = process.argv.slice(2);
const files = readdirSync(results).filter(f => /^[\w-]+\.json$/.test(f) && !f.includes(".unverified") && (!only.length || only.includes(f.replace(".json", ""))));

const picks = new Map();
for (const f of files)
  for (const p of JSON.parse(readFileSync(new URL(f, results), "utf8")))
    if (p.videoId && /^[\w-]{11}$/.test(p.videoId)) picks.set(p.id, p);

let changed = 0, same = 0, missing = new Set(picks.keys());
const log = [];
for (const f of readdirSync(lib).filter(f => /^ExerciseLibrary\.\w+\.cs$/.test(f))) {
  const raw = readFileSync(new URL(f, lib), "utf8");
  const crlf = raw.includes("\r\n");
  const lines = raw.split(/\r?\n/);
  let id = null, dirty = false;
  for (let i = 0; i < lines.length; i++) {
    const m = /new\("([a-z0-9_]+)", "/.exec(lines[i]);
    if (m) id = m[1];
    const v = /^(\s*)Video = new\((.*)\),$/.exec(lines[i]);
    if (!v || !id || !picks.has(id)) continue;
    missing.delete(id);
    const p = picks.get(id);
    const args = [`"${p.videoId}"`, ...(p.start != null || p.end != null ? [p.start ?? 0] : []), ...(p.end != null ? [p.end] : [])];
    const line = `${v[1]}Video = new(${args.join(", ")}),`;
    if (line === lines[i]) { same++; continue; }
    log.push(`${id} | ${v[2]} -> ${args.join(", ")} | ${p.channel} | ${p.title} | ${p.chapter || "-"} | ${p.verdict ?? ""}`);
    lines[i] = line;
    dirty = true;
    changed++;
  }
  if (dirty) writeFileSync(new URL(f, lib), lines.join(crlf ? "\r\n" : "\n"));
}
if (log.length) appendFileSync(new URL("../logs/videos-curated.md", import.meta.url), log.join("\n") + "\n");
console.log(`${files.length} chunk files: ${changed} videos changed, ${same} already the same, ${missing.size} ids not found${missing.size ? ": " + [...missing].join(" ") : ""}`);
