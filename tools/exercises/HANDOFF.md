# Exercise library rewrite — handoff (2026-10-01)

The scraped free-exercise-db catalogue (ExerciseCatalog.json, tools/import-exercises.mjs, photos) was deleted and
replaced by a hand-written library in `GymBook/Src/Services/Exercises/`:

- `ExerciseLibrary.cs` — core: `Def` type, `Find`, `Canonical`/`IsAlias`, `Details`, `Thumbnail` (video thumbnail).
- `ExerciseLibrary.<Region>.cs` — 12 region files (Chest, Back, Shoulders, Arms, Legs, PosteriorChain, Core, Neck,
  Power, Cardio, Pilates, Mobility), 734 exercises with Summary, Steps, Tips, Category, Level, Hold, Video.
- `ExerciseLibrary.Aliases.cs` — 660 old dataset ids → new ids. Never change or remove an id; rename freely.
- `GymBook/Src/Models/ExerciseDetails.cs` — `ExerciseCategory`, `ExerciseLevel`, `ExerciseVideo(YouTubeId, Start, End)`.

Also changed: TrainingGoals (timed holds/stretches, slow-control Pilates/mobility), ExerciseDetailPage (embedded
YouTube player + form tips), exercise list kind chips (Pilates/Yoga/Stretching/Mobility/Cardio/Power),
DataStore.MigrateExerciseIds (called from HomeViewModel), AiPlanService candidates skip Stretch/Yoga/Mobility,
PlanGenerator ids, Wear csproj links `Exercises\*.cs`.

**Not built or tested yet.** One compile error (raw string `$$"""` in ExerciseViewModels.VideoPage) was fixed.

## Left to do

Done 2026-10-01 (second session): all 734 exercises have a verified video. Mobility picks are in
`logs/videos-Mobility.md`, the 11 former gaps in `logs/videos-extra.md`, Chest log now has real titles. The six
"weak" picks were re-checked against their Summaries and kept.

Third session: the C# changes were reviewed by reading (models, csproj links, ids, enums, detail page, player);
nothing found. Every video's watch page was checked for description chapters; Start/End set on the 5 that have a
clear demonstration chapter (hill_sprints, band_pallof_press, front_squat, lying_chin_nod, power_clean).
PhoneMockup.tsx and both privacy policies (web + API) now describe the YouTube videos. Loose picks settled:
upright_row replaced (the old video used a narrow grip); harness_band_neck_extension, machine_neck_extension and
outdoor_cycling kept (no closer match exists on YouTube).

1. **Build** (phone + Wear) and fix any compile errors. The user builds and tests; don't build unless asked.

## Tools here

- `node yt.mjs search|info|check`: YouTube search, video info with chapters (start seconds) and description, and the
  embed check. No transcripts: YouTube bot-checks every way of reading them, so timecodes come from chapters only.
- `CURATE_BRIEF.md` + `curate/chunks.json`: how the reputable-source video curation (fourth session, multi-agent) picked
  videos and timecodes; 35 chunks of ~22 exercises.
- `thumbs.mjs`: the AI exercise pictures (OpenAI gpt-image-2, low quality, one consistent style) into
  `GymBook/Resources/Raw/exercises/<id>.webp`; needs OPENAI_API_KEY and sharp. Paced to 5 images a minute (the
  account limit); re-running skips existing files, so a new exercise only needs `--only <id>`.

- `node check.mjs` — validates every region file (ids, names, enums, required ids), writes `library.json`, prints
  per-file counts and how many have a video. Run after any edit.
- `node candidates.mjs .<Region>.cs` / `node fill.mjs <Region>` — YouTube search results per exercise without a
  video into `candidates/<Region>.md` (fill.mjs is the reliable one: slow, retries, timeout).
- Verify a video: `curl -s "https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=ID&format=json"`.
- `node remove.mjs <file> <id>...` — delete entries.
- `BRIEF.md` (how entries are written), `PICK_BRIEF.md` (picking videos), `old-ids.txt` (old dataset ids).

WebSearch has a 200-per-session budget shared by all agents; it ran out. Use fill.mjs + curl instead.
