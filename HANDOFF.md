# Handoff: open work on GymBook (updated 2026-10-01 by Claude Code)

Read this first, then `tools/exercises/HANDOFF.md` for the exercise library's structure and tools. **The owner builds
and tests the app themselves: don't build, run tests or launch the app unless asked.** Implement by reading the code
carefully instead. Exercise ids are stable forever (plans and history reference them): rename freely, never change or
remove an id.

## State

- **Pictures: done.** All 734 exercises have a picture in `GymBook/Resources/Raw/exercises/<id>.webp` (384×384 WebP,
  dark background; every file opens and the set was spot-checked for style). `ExerciseThumbnailAssets.cs` lists all
  734 (`node tools/exercises/sync-thumbnail-manifest.mjs` refreshes it). The website mockup's four pictures are in
  `GymBook.Web/public/exercises/` and `PhoneMockup.tsx` uses `/exercises/<id>.webp`. The OpenAI keys that were here
  are no longer needed and were removed (they expire 2026-10-02 anyway).
- **Videos: done.** All 35 curation chunks in `tools/exercises/curate/results/` are verified `.json` files and have
  been applied to `ExerciseLibrary.*.cs`; every change is logged in `tools/exercises/logs/videos-curated.md`.
  `node tools/exercises/check.mjs` reports `total 734 with video 734` and `no problems`.
- **Nothing from these sessions has been compiled.**

## Fixed in this session

- `check.mjs` didn't recognise fractional Start/End (e.g. `new("YLPQsdRDmB0", 30.66, 73.799)`), so it reported those
  exercises as having no video (the earlier "24 without video" was this). The pattern now accepts decimals.
- `yt.mjs info` was getting HTTP 429 from the watch page while the owner's browser worked fine. It now uses YouTube's
  internal API (`youtubei/v1/player` + `/next`), which returns the same details, description and chapters in ~1 s,
  and falls back to the watch page. `info` prints `embeddable: ?` now; `check` (oEmbed) is the embed test.
- Parallel curation agents shared one scratchpad and overwrote each other's helper scripts, which misfiled a few
  picks into other chunks' files. All were cleaned up; every applied chunk was checked to contain only its own ids,
  each once. If you run agents in parallel again, give each its own scratchpad subfolder.

## Judgement calls the owner may want to review

Picks kept from smaller or older channels because no reputable alternative fitted the exact variant (all embed and
show the right movement): Pilates side_kick_circles, clam; Howcast 2012 videos for hip_circles, swimming, seal;
Power sled_pull, landmine_rotational_punch, band_resisted_punch, sledgehammer_tire_strike; Shoulders
standing_db_press, chest_supported_reverse_fly (alternative: DeltaBolic sIQNCJ6Xwsk), z_press (Buff Dudes; title
doesn't say barbell), cable_internal_rotation, ytw_raise, planche_lean. Some agents truncated fractional
chapter times to whole seconds, others kept them exact; both work (`ExerciseVideo` takes doubles and the YouTube URL
rounds).

## For the owner to build and test (don't do this yourself)

- Phone + Wear build.
- Sign-in: fresh install online and offline; existing install without an account (offline → "Continue offline for
  now"; online → sign in, history kept); sign out; airplane mode while signed in.
- Exercises tab: search ("db bench", "rdl", typos), chips, filter sheet, badge, clear; picker; replace in a workout and
  a plan (Similar / All exercises).
- Pictures in exercise rows, plan/workout cards, home and calendar strips (`AspectFill`, `Margin="-1"`); custom
  exercises keep their initials.
- Exercise page: embedded video starts/ends at the set times, including fractional ones.

## Background: what these sessions built (none of it compiled yet)

1. **Exercise library** (`GymBook/Src/Services/Exercises/`, 734 hand-written exercises, each with a curated YouTube
   video, Start/End from chapters where the video is longer than the demonstration).
2. **Sign-in required** (`Src/Views/SignInGate.cs`, `AccountViewModel` "required" mode, `App.xaml.cs`): without a
   session a sign-in sheet that can't be closed covers the app. Users with local data and no connection get
   "Continue offline for now"; their data moves into the account on sign-in.
3. **Search engine** (`Src/Services/ExerciseSearch.cs`): ranked, typo-tolerant, gym shorthand, prefix matching; plus
   `Similarity()` for replacements.
4. **Filters** (`Src/ViewModels/ExerciseFilter.cs`, `Src/Views/ExerciseFilterPage.xaml(.cs)`).
5. **Focused replace**: the picker opens on "Similar" with an "All exercises" switch
   (`ExercisePickerViewModel`, `ExercisePickerService.PickOneAsync(title, similarTo)`).
6. **AI exercise pictures**: `ExerciseLibrary.Thumbnail(id)` returns `exercises/<id>.webp`, loaded through
   `Src/Converters/ThumbnailConverter.cs` (key `Thumb` in App.xaml).
7. `ExerciseVideo.Start`/`.End` are doubles; YouTube URLs round them to invariant integer seconds.
8. Privacy policies (web `GymBook.Web/src/app/privacy/page.tsx`, API `GymBook.Api/Privacy/PrivacyPolicy.cs`) cover
   YouTube videos and bundled pictures.

## Re-curating a video later

Rules: `tools/exercises/CURATE_BRIEF.md`. Picks format and verify procedure: one JSON array per chunk in
`curate/results/<key>.json`, `{"id","videoId","start","end","channel","title","timeSource","chapter","note","verdict"}`;
every id must pass `node yt.mjs info <id>` and `node yt.mjs check <id>`. Apply with `node curate/apply.mjs <key>`,
then `node check.mjs` must end with `with video 734` and `no problems`.
