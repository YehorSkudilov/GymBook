// Re-searches the exercises whose candidate list came back empty, slowly, with backoff. node fill.mjs <Region>...
import { readFileSync, writeFileSync } from "node:fs";

const lib = new Map(JSON.parse(readFileSync(new URL("./library.json", import.meta.url), "utf8")).map(x => [x.id, x]));
const sleep = ms => new Promise(r => setTimeout(r, ms));

function query(x, alt) {
  const n = x.name;
  if (x.category === "Pilates") return alt ? `${n} pilates` : `${/pilates/i.test(n) ? n : "pilates " + n} exercise tutorial`;
  if (x.category === "Yoga") return alt ? `${n} yoga` : `${n} yoga pose how to`;
  if (x.category === "Stretch") return alt ? `${n}` : `${n} how to`;
  return alt ? `how to ${n}` : `${n} exercise how to proper form`;
}

function walk(node, out) {
  if (!node || typeof node !== "object") return;
  if (node.videoRenderer) {
    const v = node.videoRenderer;
    out.push({ id: v.videoId, title: v.title?.runs?.map(r => r.text).join("") ?? "", channel: v.ownerText?.runs?.[0]?.text ?? "", length: v.lengthText?.simpleText ?? "?" });
  }
  if (node.reelItemRenderer) out.push({ id: node.reelItemRenderer.videoId, title: node.reelItemRenderer.headline?.simpleText ?? "", channel: "", length: "short" });
  if (node.shortsLockupViewModel) {
    const v = node.shortsLockupViewModel;
    const id = v.onTap?.innertubeCommand?.reelWatchEndpoint?.videoId;
    if (id) out.push({ id, title: v.overlayMetadata?.primaryText?.content ?? v.accessibilityText ?? "", channel: "", length: "short" });
  }
  for (const k in node) walk(node[k], out);
}

async function search(q) {
  const res = await fetch(`https://www.youtube.com/results?search_query=${encodeURIComponent(q)}&hl=en&gl=US`, {
    signal: AbortSignal.timeout(15000), headers: { "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36", "Accept-Language": "en-US,en" },
  });
  const html = await res.text();
  const m = /var ytInitialData = (\{.*?\});<\/script>/s.exec(html);
  if (!m) return [];
  const out = [];
  walk(JSON.parse(m[1]), out);
  const seen = new Set();
  return out.filter(v => v.id && !seen.has(v.id) && seen.add(v.id)).slice(0, 10);
}

for (const region of process.argv.slice(2)) {
  const path = new URL(`./candidates/${region}.md`, import.meta.url);
  const sections = readFileSync(path, "utf8").split(/\n(?=## )/);
  let filled = 0, still = 0;
  for (let i = 0; i < sections.length; i++) {
    if (/\n- /.test(sections[i])) continue;
    const id = /^## (\S+)/.exec(sections[i])?.[1];
    const x = lib.get(id);
    if (!x) continue;
    let results = [];
    for (let attempt = 0; attempt < 6 && results.length === 0; attempt++) {
      await sleep(1500 + attempt * 3000);
      try { results = await search(query(x, attempt % 2 === 1)); } catch { }
    }
    const head = sections[i].split("\n")[0];
    sections[i] = head + "\n" + results.map(v => `- ${v.id} | ${v.length} | ${v.channel} | ${v.title}`).join("\n") + "\n";
    results.length ? filled++ : still++;
  }
  writeFileSync(path, sections.join("\n"));
  console.log(region, "filled", filled, "still empty", still);
}
