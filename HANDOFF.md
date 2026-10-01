# Handoff: open work on GymBook (updated 2026-10-01 by Claude Code)

## LATEST (2026-10-01, later session): read this section first; it supersedes the "State" section below

### 1. Adding ~110 missing exercises (in progress, nothing committed)
- The brief the writers followed is `tools/exercises/ADD_BRIEF.md`. It builds on `BRIEF.md` (entries) and
  `CURATE_BRIEF.md` (videos). Every new entry gets a full Summary, Steps and Tips, plus a video checked with `yt.mjs info` + `check`.
- `tools/exercises/check.mjs`: the hardcoded `C:/GitHub/...` path is now relative (the repo moved to D:).
- **Done and passing `check.mjs`:** Pilates (+8), Power (+13), Core (+9), Cardio (+5), Chest (+8), Back (+8), Legs (+10),
  PosteriorChain (+8), Shoulders (+7), Arms (+7), Mobility/Yoga (+22).
  **All writing is done: 841 exercises, all with a video, check.mjs reports no problems.**
  - Pancake Stretch skipped: it's the existing stretch_seated_straddle.
  - High Cable Curl skipped: it duplicates the existing high_cable_curl ("Overhead Cable Curl").
  - Frog Squat skipped: the name is ambiguous and has no good source.
  - Skipped as duplicates: V-Sit Hold (= Boat Pose) and Jackknife Sit-Up (= V-Up).
  - Weaker video picks, worth a look in the app: sandbag_over_shoulder (Conquer Athlete), kb_thruster (OriGym),
    devil_press (PureGym), human_flag (Andry Strong), interval_run (an explainer, not a demo), hiking (SIKANA).
  - power_jerk was kept next to push_jerk on purpose: feet move vs. stay planted, per Catalyst Athletics.
- The planned lists below are for reference only; every region is finished:
  Planned lists for every region (Chest/Back/Legs/PosteriorChain are done):
  - Chest: Paused Bench, Larsen Press, Spoto Press, Reverse-Grip Bench, Guillotine Press, Hindu Push-Up, Spiderman
    Push-Up, Pseudo Planche Push-Up.
  - Back: Renegade Row, Helms Row, Smith Machine Row, Rope Climb, One-Arm Pull-Up, Kelso Shrug, Chest-Supported T-Bar Row, Skin the Cat.
  - Shoulders: Lu Raise, Dumbbell Y-Raise, Scaption, Rear Delt Row, Plate Bus Driver, Behind-the-Back Cable Lateral
    Raise, Freestanding Handstand.
  - Arms: 21s, Cheat Curl, Cable Preacher Curl, High Cable Curl, Lying Cable Curl, Waiter Curl, California Press, Close-Grip Push-Up.
  - Legs: Bodyweight Lunge, Skater Squat, Landmine Squat, Hatfield Squat, Pin Squat, FFE Split Squat, Smith Split Squat,
    Poliquin Step-Up, Peterson Step-Up, ATG Split Squat, Frog Squat.
  - PosteriorChain: KB RDL, Single-Leg KB RDL, Smith RDL, Suitcase Deadlift, Zercher Deadlift, Hamstring Walkout,
    Single-Leg Back Extension, Kas Glute Bridge.
  - Mobility: yoga (Sphinx, Fish, Plow, Supported Shoulder Stand, Supported Headstand, Lizard, Goddess, Reverse Warrior,
    Pyramid, Revolved Triangle, Dancer, Sun Salutation B, Seated Spinal Twist, Cow Face, Wide-Legged Forward Fold,
    Reclined Bound Angle, Half Split, Front Split); stretches (Pancake, Middle Split); mobility (Shin Box Get-Up, Scapular CARs,
    Foam Roll Adductors).
  - **How to resume:** run `node tools/exercises/check.mjs` and compare each file against these lists. Finish anything missing
    or half-written by following ADD_BRIEF.md, then make sure `check.mjs` says `no problems` and every entry has a video.
- **Pictures: 766 of 841 done when this session stopped.** `ExerciseThumbnailAssets.cs` was synced to the files on disk.
  `thumbs.mjs` needs `OPENAI_API_KEY`. No key is stored anywhere in the repo, so the owner has to supply a new one.
  - Done: the 8 Pilates pictures and some of Power/Core/Cardio (look in `GymBook/Resources/Raw/exercises/`).
  - When all entries are in, run `node thumbs.mjs` with no `--only`. It skips existing files and makes the rest, then rewrites
    `ExerciseThumbnailAssets.cs`.
  - Open and check every new picture. Known bad one: `pilates_reformer_short_box_tree.webp` shows a standing stretch, but the
    exercise is seated on the box. Regenerate it with `--only pilates_reformer_short_box_tree --force --hint "seated on the short box, one leg raised and held, hands walking up the leg"`.
- Then update the counts in this file and in `tools/exercises/HANDOFF.md`.

### 2. Signature Programs replace the "standard plan" (done, not compiled)
The owner's request: built-in plans based on famous lifters' and coaches' programs, each showing who it's from, ranked for
the quiz answers on a **Recommended** tab plus an **All** tab, and "Standard plan" renamed. What was built:
- `Services/SignaturePrograms.cs`: 22 programs (Golden Six, Arnold Split, Reg Park 5x5, StrongLifts-style 5x5,
  Rippetoe novice LP, Wendler 5/3/1 BBB, PHUL, PHAT, Reddit PPL, GZCLP, Heavy Duty, Blood & Guts, Texas Method,
  Bill Starr 5x5, Westside for Skinny Bastards, a Tyson-inspired bodyweight circuit, r/bodyweightfitness RR,
  Simple & Sinister, Dan John's 10,000 swings, a Bret Contreras-style glute program, Steve Reeves, McGuff's Big Five).
  Each one has an author, a bio, a summary, its goal, levels, days, minutes and equipment. Each exercise slot lists
  stand-ins in order (barbell, then dumbbell, then band or bodyweight); `Build` picks the first one the user's equipment allows.
  - `Rank(answers)` scores goal (40 primary / 25 secondary), experience, days, equipment coverage and session length,
    and gives reasons like "Built for strength · 3 days, as you asked".
  - Programs set their own sets and reps. Effort and rest come from `TrainingGoals.Prescription` for the program's goal
    unless a slot overrides them (Heavy Duty and Yates use RIR 0). Neck exercises keep the app's neck prescription.
    They're dropped when the user turned neck work off, and added (via the now-public `PlanGenerator.AddNeck`) when they want it.
  - A/B programs with 3 days (StrongLifts, Rippetoe, Reeves) are stored as A, B, A, since a plan "week" is one pass
    through its workouts. Their summaries say the original alternates.
  - Plan descriptions end with "Inspired by X's program. Not affiliated with or endorsed by X."
  - Every exercise id was checked against `tools/exercises/library.json` (143 ids, none missing).
- Wizard (`PlanWizardViewModel`, `PlanWizardPage.xaml`): BuildWith now offers "Build with AI" / "Signature Programs".
  A new `Programs` step (skipped only when the AI builds the plan, so users who aren't signed in or are out of quota always see it)
  shows Recommended (top 5, ranked #1–#5) and All (A–Z) as chip tabs. Tapping a card selects it and shows its summary and the author's bio.
  The result shows an "Inspired by" card with the bio. `PlanGenerator.Generate` is now only the fallback when the AI call fails.
- **For the owner to test:** the wizard with and without sign-in; both tabs; picking from All; a home-dumbbell and a
  bodyweight profile (stand-ins); neck on and off; Regenerate a saved plan via programs; the scores feel right.

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
