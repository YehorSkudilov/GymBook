# Handoff: open work on GymBook (updated 2026-10-01 by Claude Code)

Read this first, then `tools/exercises/HANDOFF.md` for the exercise library's structure and tools. **The owner builds
and tests the app themselves: don't build, run tests or launch the app unless asked.** Implement by reading the code
carefully instead. Exercise ids are stable forever (plans and history reference them): rename freely, never change or
remove an id.

## State

- **Pictures: done.** All 736 exercises have a picture in `GymBook/Resources/Raw/exercises/<id>.webp` (384×384 WebP,
  dark background; every file opens and the set was spot-checked for style). `ExerciseThumbnailAssets.cs` lists all
  736 (`node tools/exercises/sync-thumbnail-manifest.mjs` refreshes it). The website mockup's four pictures are in
  `GymBook.Web/public/exercises/` and `PhoneMockup.tsx` uses `/exercises/<id>.webp`. The OpenAI keys that were here
  are no longer needed and were removed (they expire 2026-10-02 anyway).
- **Offline animations (in progress).** The exercise page has an "Animation | Video" toggle above the
  demonstration. It opens on the animation every time. Offline, the video half is greyed out and can't be picked, and
  the page goes back to the animation when the connection drops. When the connection comes back, the video loads again
  from scratch. The web view stays invisible until the player page has loaded, and a failed load is cleared without
  being shown (`YouTubePlayer.LoadFailed`), so no connection error page is ever visible. The animations are short
  silent looping MP4s (`Resources/Raw/exercise-animations/<id>.mp4`, 288 px, 24 fps, ~40–70 KB each; a GIF of the same
  frames was ~10× bigger) played by CommunityToolkit.Maui.MediaElement 10.0.0 (registered in MauiProgram with no
  Android foreground service, TextureView). Until an exercise has one, its picture shows in its place.
  Made by `tools/exercises/anims.mjs`: gpt-image-2 draws a 3×2 storyboard of six poses from one fixed camera, a vision
  model (gpt-5.4-mini) checks it (same angle, same figure, poses in order) and it's redrawn up to 3 times; the panels
  are lined up, played there and back, and ffmpeg's motion interpolation adds the in-between frames. Storyboards are
  kept in `tools/exercises/anim-sheets/` (`--from-sheets` rebuilds the videos without the API); ones that never passed
  the check are listed in `tools/exercises/anim-review.txt` for a look (`--only id --force --hint "..."` redoes one).
- **Search, filters, replace.** `Src/Services/ExercisePatterns.cs` sorts every exercise (custom ones by name) into a
  movement pattern (chest press, rear delt, hinge, row...). Search shows a "Similar exercises" section after the
  matches: the rest of a pattern the query names ("reverse flys" → Reverse Pec Deck, Face Pull), or the closest
  exercises when nothing matches every word ("military press", "french press"). Filters are a pyramid (type of
  training → muscle → movement → compound/isolation → equipment → level): each level only offers what the levels above
  leave, with counts; muscle chips above the list hide muscles with nothing left. Removed: kind chips in the chip bar
  (now the filters' top level) and "Count secondary muscles". Replace ranks by pattern + muscle (Similarity) and
  prefers equipment the user has. Search and patterns were compiled and run against the library in a scratch console
  app; the MAUI side (filters, sheet, rows) is uncompiled.
- **Videos: done.** All 35 curation chunks in `tools/exercises/curate/results/` are verified `.json` files and have
  been applied to `ExerciseLibrary.*.cs`; every change is logged in `tools/exercises/logs/videos-curated.md`.
  `node tools/exercises/check.mjs` reports `total 736 with video 736` and `no problems`. (736 since `cable_lean_away_lateral_raise` and `barbell_reverse_wrist_curl` were added; they aren't in a curation chunk).
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

A review pass replaced 13 weak picks (small channels, 2012 Howcast) with better sources: Pilatesology for four
Pilates mat exercises, Hinge Health (clam), Live Lean TV (sled pull), Boxing Science (landmine punch), Phil Daru
(band punch), Brian Alsruhe (sledgehammer, trimmed before its closing promo), RP (standing DB press), DeltaBolic
(chest-supported reverse fly), Astros Strength (incline Y-T-W), Simonster Strength (planche lean chapter). Worth a
glance in the app: band_resisted_punch and sledgehammer_tire_strike were matched on title and description only
(the sledgehammer video may not show a tire). Still from small channels after a deep search, because nothing better
exists: cable_internal_rotation (Athletes' Potential). z_press stays Buff Dudes (barbell, per its description).
Some agents truncated fractional
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
- Exercise page media toggle: opens on the animation; the MP4 loops silently on Android, iOS and Windows (and stops
  when leaving the page); airplane mode on the video greys the toggle and switches to the animation; back online the
  video loads; no web view error page ever. NuGet restore with MediaElement's AndroidX Media3 packages next to the
  Play services ones.
- Exercises tab: "reverse flys", "military press", "pec dec"; the "Similar exercises" heading; filter sheet levels
  shrinking as upper ones are picked, counts; muscle chips hiding; replace (Similar) for a bench press, a reverse fly.

## Background: what these sessions built (none of it compiled yet)

1. **Exercise library** (`GymBook/Src/Services/Exercises/`, 736 hand-written exercises, each with a curated YouTube
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
then `node check.mjs` must end with `with video 736` and `no problems`.
