// Keep the app's compile-time thumbnail list aligned with the generated WebP assets.
import { readdirSync, writeFileSync } from "node:fs";

const assets = new URL("../../GymBook/Resources/Raw/exercises/", import.meta.url);
const output = new URL("../../GymBook/Src/Services/Exercises/ExerciseThumbnailAssets.cs", import.meta.url);
const ids = readdirSync(assets).filter(name => name.endsWith(".webp")).map(name => name.slice(0, -5)).sort();
const source = [
  "// Generated from Resources/Raw/exercises by tools/exercises/sync-thumbnail-manifest.mjs.",
  "namespace GymBook.Services;",
  "",
  "public static class ExerciseThumbnailAssets",
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

writeFileSync(output, source);
console.log(`Indexed ${ids.length} exercise thumbnail assets.`);
