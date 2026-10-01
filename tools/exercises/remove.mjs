// node remove.mjs <file> <id>...  removes those entries (the whole new(...) { ... }, block)
import { readFileSync, writeFileSync } from "node:fs";
const [file, ...ids] = process.argv.slice(2);
let src = readFileSync(file, "utf8");
for (const id of ids) {
  const start = src.search(new RegExp(String.raw`\r?\n        new\("` + id + String.raw`", `));
  if (start < 0) { console.log("not found", id); continue; }
  const endMarker = src.indexOf("\n        },", start + 2);
  src = src.slice(0, start) + src.slice(endMarker + "\n        },".length);
  console.log("removed", id);
}
writeFileSync(file, src);
