// YouTube lookups for picking exercise videos, without the YouTube API or WebSearch.
//   node yt.mjs search "<query>"      top results: id | length | channel | title
//   node yt.mjs info <id>             title, channel, length, views, upload date, chapters, description
//   node yt.mjs check <id>...         oEmbed check: embeddable or not, with title and channel
// Retries with backoff when YouTube throttles. (No transcripts: YouTube now bot-checks every way of getting them.)

const UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
const sleep = ms => new Promise(r => setTimeout(r, ms));

async function get(url, init = {}) {
  for (let attempt = 0; ; attempt++) {
    try {
      const res = await fetch(url, { ...init, signal: AbortSignal.timeout(20000), headers: { "User-Agent": UA, "Accept-Language": "en-US,en", ...init.headers } });
      if (res.status === 429 || res.status >= 500) throw new Error(`HTTP ${res.status}`);
      return res;
    } catch (e) {
      if (attempt >= 4) throw e;
      await sleep(2000 * 2 ** attempt);
    }
  }
}

function initialJson(html, name) {
  const start = html.indexOf(`${name} = `) >= 0 ? html.indexOf(`${name} = `) + name.length + 3 : html.indexOf(`"${name}":`) + name.length + 3;
  if (start < name.length + 3) return null;
  // Walk to the matching brace (the JSON is followed by ";</script>" or more script).
  let depth = 0, inStr = false;
  for (let i = start; i < html.length; i++) {
    const c = html[i];
    if (inStr) { if (c === "\\") i++; else if (c === '"') inStr = false; continue; }
    if (c === '"') inStr = true;
    else if (c === "{") depth++;
    else if (c === "}" && --depth === 0) return JSON.parse(html.slice(start, i + 1));
  }
  return null;
}

function* walk(node) {
  if (!node || typeof node !== "object") return;
  yield node;
  for (const k in node) yield* walk(node[k]);
}

const fmt = s => `${Math.floor(s / 60)}:${String(Math.floor(s % 60)).padStart(2, "0")}`;

async function search(q) {
  const html = await (await get(`https://www.youtube.com/results?search_query=${encodeURIComponent(q)}&hl=en&gl=US`)).text();
  const data = initialJson(html, "var ytInitialData");
  const out = [], seen = new Set();
  for (const n of walk(data)) {
    if (n.videoRenderer) {
      const v = n.videoRenderer;
      if (!seen.has(v.videoId)) out.push(`${v.videoId} | ${v.lengthText?.simpleText ?? "?"} | ${v.ownerText?.runs?.[0]?.text ?? ""} | ${v.title?.runs?.map(r => r.text).join("") ?? ""} | ${v.viewCountText?.simpleText ?? ""} | ${v.publishedTimeText?.simpleText ?? ""}`);
      seen.add(v.videoId);
    }
    if (n.shortsLockupViewModel) {
      const id = n.shortsLockupViewModel.onTap?.innertubeCommand?.reelWatchEndpoint?.videoId;
      if (id && !seen.has(id)) out.push(`${id} | short | ? | ${n.shortsLockupViewModel.overlayMetadata?.primaryText?.content ?? ""}`);
      if (id) seen.add(id);
    }
  }
  return out.slice(0, 15);
}

// The internal API ("player" for details, "next" for chapters) is throttled far less than the watch page, so try it
// first and fall back to the page.
async function api(endpoint, id) {
  const body = JSON.stringify({ videoId: id, context: { client: { clientName: "WEB", clientVersion: "2.20240101.00.00", hl: "en", gl: "US" } } });
  return (await get(`https://www.youtube.com/youtubei/v1/${endpoint}?prettyPrint=false`, { method: "POST", headers: { "Content-Type": "application/json" }, body })).json();
}

async function page(id) {
  try {
    const [player, data] = await Promise.all([api("player", id), api("next", id)]);
    if (player?.videoDetails) return { player, data };
  } catch { }
  const html = await (await get(`https://www.youtube.com/watch?v=${id}&hl=en`)).text();
  return { html, player: initialJson(html, "var ytInitialPlayerResponse"), data: initialJson(html, "var ytInitialData") };
}

async function info(id) {
  const { player, data } = await page(id);
  const d = player?.videoDetails;
  if (!d) return `${id}: unavailable (${player?.playabilityStatus?.status ?? "no player"})`;
  const chapters = [];
  for (const n of walk(data)) {
    const m = n.macroMarkersListItemRenderer;
    if (m && m.onTap?.watchEndpoint?.startTimeSeconds != null)
      chapters.push(`${fmt(m.onTap.watchEndpoint.startTimeSeconds)} (${m.onTap.watchEndpoint.startTimeSeconds}s) ${m.title?.simpleText ?? ""}`);
  }
  const micro = player.microformat?.playerMicroformatRenderer;
  return [
    `${id} | ${d.title}`,
    `channel: ${d.author} | length: ${fmt(+d.lengthSeconds)} (${d.lengthSeconds}s) | views: ${d.viewCount} | uploaded: ${micro?.uploadDate ?? "?"} | embeddable: ${micro?.isFamilySafe != null ? player.playabilityStatus?.playableInEmbed ?? "?" : "?"}`,
    `chapters: ${chapters.length ? "\n  " + [...new Set(chapters)].join("\n  ") : "none"}`,
    `description:\n${d.shortDescription}`,
  ].join("\n");
}

async function check(ids) {
  const out = [];
  for (const id of ids) {
    const res = await get(`https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=${id}&format=json`);
    out.push(res.ok ? (j => `${id} | OK | ${j.author_name} | ${j.title}`)(await res.json()) : `${id} | NOT EMBEDDABLE (${res.status})`);
  }
  return out.join("\n");
}

const [cmd, ...rest] = process.argv.slice(2);
const result = cmd === "search" ? (await search(rest.join(" "))).join("\n")
  : cmd === "info" ? await info(rest[0])
  : cmd === "check" ? await check(rest)
  : "usage: node yt.mjs search <query> | info <id> | check <id>...";
console.log(result);
