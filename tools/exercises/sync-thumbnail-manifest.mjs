// Keep the app's compile-time list of bundled exercise pictures (WebP) aligned with Resources/Raw/exercises: the app
// shows a muscle's initials for exercises without one.
//   node sync-thumbnail-manifest.mjs
import { existsSync, readdirSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const raw = new URL("../../GymBook/Resources/Raw/", import.meta.url);
const exercises = new URL("../../GymBook/Src/Services/Exercises/", import.meta.url);

function write(folder, extension, className) {
  const assets = new URL(`${folder}/`, raw);
  const ids = existsSync(assets)
    ? readdirSync(assets).filter(name => name.endsWith(extension)).map(name => name.slice(0, -extension.length)).sort()
    : [];
  const source = [
    `// Generated from Resources/Raw/${folder} by tools/exercises/sync-thumbnail-manifest.mjs.`,
    "namespace GymBook.Services;",
    "",
    `public static class ${className}`,
    "{",
    "    static readonly HashSet<string> Ids = new(StringComparer.Ordinal)",
    "    {",
    ...ids.map(id => `        "${id}",`),
    "    };",
    "",
    "    public static bool Exists(string id) => Ids.Contains(id);",
    "}",
    "",
  ].join("\n");
  writeFileSync(new URL(`${className}.cs`, exercises), source);
  console.log(`Indexed ${ids.length} ${folder} assets.`);
}

export function syncManifests() {
  write("exercises", ".webp", "ExerciseThumbnailAssets");
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1])
  syncManifests();
