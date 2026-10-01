// For every exercise without a video: YouTube's top search results (id, title, channel, length), straight from
// youtube.com search, written to candidates/<File>.md for the video pickers.
// node candidates.mjs [FileFilter]
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";

const lib = JSON.parse(readFileSync(new URL("./library.json", import.meta.url), "utf8"));
const only = process.argv[2];
mkdirSync(new URL("./candidates/", import.meta.url), { recursive: true });

function query(x) {
  const n = x.name;
  if (x.category === "Pilates") return `${/pilates/i.test(n) ? n : "pilates " + n} exercise tutorial`;
  if (x.category === "Yoga") return `${n} yoga pose how to`;
  if (x.category === "Stretch") return `${n} how to`;
  return `${n} exercise how to proper form`;
}

function walk(node, out) {
  if (!node || typeof node !== "object") return;
  if (node.videoRenderer) {
    const v = node.videoRenderer;
    out.push({
      id: v.videoId,
      title: v.title?.runs?.map(r => r.text).join("") ?? "",
      channel: v.ownerText?.runs?.[0]?.text ?? "",
      length: v.lengthText?.simpleText ?? "?",
    });
  }
  if (node.reelItemRenderer) {
    const v = node.reelItemRenderer;
    out.push({ id: v.videoId, title: v.headline?.simpleText ?? "", channel: "", length: "short" });
  }
  if (node.shortsLockupViewModel) {
    const v = node.shortsLockupViewModel;
    const id = v.onTap?.innertubeCommand?.reelWatchEndpoint?.videoId ?? /reel\/?([\w-]{11})/.exec(JSON.stringify(v))?.[1];
    if (id) out.push({ id, title: v.overlayMetadata?.primaryText?.content ?? v.accessibilityText ?? "", channel: "", length: "short" });
  }
  for (const k in node) walk(node[k], out);
}

async function search(q) {
  const res = await fetch(`https://www.youtube.com/results?search_query=${encodeURIComponent(q)}&hl=en&gl=US`, {
    headers: { "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36", "Accept-Language": "en-US,en" },
  });
  const html = await res.text();
  const m = /var ytInitialData = (\{.*?\});<\/script>/s.exec(html);
  if (!m) return [];
  const out = [];
  walk(JSON.parse(m[1]), out);
  const seen = new Set();
  return out.filter(v => v.id && !seen.has(v.id) && seen.add(v.id)).slice(0, 10);
}

const todo = lib.filter(x => !x.video && (!only || x.file.includes(only)));
const byFile = {};
for (const x of todo) (byFile[x.file] ??= []).push(x);
for (const [file, list] of Object.entries(byFile)) {
  const path = new URL(`./candidates/${file.replace("ExerciseLibrary.", "").replace(".cs", "")}.md`, import.meta.url);
  let text = "";
  for (const x of list) {
    let results = [];
    for (let attempt = 0; attempt < 3 && results.length === 0; attempt++) {
      try { results = await search(query(x)); } catch { await new Promise(r => setTimeout(r, 2000)); }
    }
    text += `## ${x.id} | ${x.name} | ${x.equipment} | ${x.category}\n`;
    text += results.map(v => `- ${v.id} | ${v.length} | ${v.channel} | ${v.title}`).join("\n") + "\n\n";
    await new Promise(r => setTimeout(r, 400));
  }
  writeFileSync(path, text);
  console.log(file, list.length);
}
