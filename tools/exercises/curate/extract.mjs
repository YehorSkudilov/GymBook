// Saves the curation workflow's results (from a Claude Code workflow journal) as curate/results/<chunk>.json, so they
// survive the session: verified picks as <chunk>.json, a curator's picks still waiting for verification as
// <chunk>.unverified.json.   node curate/extract.mjs <path to journal.jsonl>
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";

const journal = process.argv[2];
if (!journal) throw new Error("usage: node curate/extract.mjs <journal.jsonl>");
const lines = readFileSync(journal, "utf8").trim().split("\n").map(l => JSON.parse(l));
const labels = new Map(lines.filter(l => l.type === "started").map(l => [l.agentId, l.label]));
const dir = new URL("./results/", import.meta.url);
mkdirSync(dir, { recursive: true });

let verified = 0, unverified = 0;
for (const l of lines.filter(l => l.type === "result" && l.result?.picks)) {
  const [stage, key] = (labels.get(l.agentId) ?? "").split(":");
  if (!key) continue;
  if (stage === "verify") {
    writeFileSync(new URL(`${key}.json`, dir), JSON.stringify(l.result.picks, null, 1));
    verified++;
  } else if (stage === "curate" && !existsSync(new URL(`${key}.json`, dir))) {
    writeFileSync(new URL(`${key}.unverified.json`, dir), JSON.stringify(l.result.picks, null, 1));
    unverified++;
  }
}
console.log(`saved ${verified} verified chunks and ${unverified} curated-but-unverified chunks to curate/results/`);
