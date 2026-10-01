# Handoff: open work on GymBook (written 2026-10-01 by Claude Code, for the next session, e.g. a cloud session)

## Current continuation state (2026-10-01, updated by Codex)

This section supersedes the older progress snapshots below. Read it first.

- **Pictures:** 608/734 WebP assets are present in `GymBook/Resources/Raw/exercises/`; 126 remain. All local image
  workers were stopped for this handoff. `ExerciseThumbnailAssets.cs` lists the packaged pictures; `thumbs.mjs`
  refreshes it at the end of a run, and `node tools/exercises/sync-thumbnail-manifest.mjs` refreshes it manually.
- **Videos:** 545 picks across 26 chunks have been verified and applied. `node tools/exercises/check.mjs` reports
  `total 734 with video 710` and `no problems`; 24 exercises still have no video. Seven `.unverified.json` files
  contain 154 candidate picks: Mobility-1, Pilates-1, Pilates-2, Power-1, Power-2, Power-3, and Shoulders-1.
  Shoulders-2 and Shoulders-3 have no result file yet. The other 26 chunks have verified `.json` files.
- **YouTube:** recent requests from this PC returned HTTP 429. Continue promptly, but honor a returned cooldown before
  retrying; verify with `info` and `check` before promoting a chunk.
- **Compilation report:** a fractional chapter time in `ExerciseLibrary.Back.cs` exposed that `ExerciseVideo.Start`
  and `.End` were typed as integers. `ExerciseVideo` now stores doubles and rounds to invariant integer seconds when
  constructing YouTube URLs. The app has not been built or run.
- **Exercise images in UI:** `Aspect="AspectFill"` plus `Margin="-1"` is applied across exercise image views. The
  active workout strip keeps unselected images fully opaque; its initials/color fallback is shown only when that
  exercise has no packaged image.

### API keys for the next session

The owner explicitly requested these project keys be included here for the Claude handoff and says they expire
2026-10-02. Use each only through `OPENAI_API_KEY`; do not add them to commands that print output. Each project was
configured for 5 images/minute. Split image IDs into disjoint `--only` lists if using more than one key at once.

- Project 1: `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`
- Project 2: `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`
- Project 3: `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`
- Project 4: `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`
- Project 5: `sk-proj-dzhckPNvmMY4jYfvUgERPcJd_j6ZgASsSwMQl4s7hpyBsYeIV4NzYQ9n3eiXXGpcn2x5xu1mblT3BlFJ-UHTZzJne05yx46n4-UhDFfPYHGYSl5hqOH77mMh0ZBzxDnPQtz1l9XqZgUCYGXmpPPzBz_qgA`

Resume the pictures with `cd tools/exercises`, `npm install --no-save sharp` if needed, then run `node thumbs.mjs`
with one of the keys set in the process environment. The script skips existing images. Don't start parallel default
runs; assign disjoint `--only id,id,...` sets. After image generation, sync the manifest, apply all verified video
chunks with `node tools/exercises/curate/apply.mjs <keys...>`, then run `node tools/exercises/check.mjs`.
Do not build, run, or test the app.

Read this first, then `tools/exercises/HANDOFF.md` for the exercise library's background. Everything below was
committed to main in one commit at the end of the session (2026-10-01). **The owner builds and tests the app themselves: don't build, run tests or launch
the app unless asked.** Implement by reading the code carefully instead. Exercise ids are stable forever (plans and
history reference them): rename freely, never change or remove an id.

## What was done in this session (none of it compiled yet)

1. **Exercise library** (`GymBook/Src/Services/Exercises/`, 734 hand-written exercises, each with a YouTube video):
   C# changes reviewed by reading only. 5 videos have Start/End from chapters.
2. **Sign-in required** (`Src/Views/SignInGate.cs`, `AccountViewModel` "required" mode, `App.xaml.cs`): without a
   session a sign-in sheet that can't be closed covers the app (launch, resume, sign-out, expired session). Sessions
   live on the device, so signed-in users work offline as before. Users with local data and no connection get
   "Continue offline for now" until they're back online; their data moves into the account on sign-in.
3. **Search engine** (`Src/Services/ExerciseSearch.cs`): ranked, typo-tolerant, gym shorthand (db, rdl, ohp, pecs...),
   prefix matching; plus `Similarity()` for replacements.
4. **Filters** (`Src/ViewModels/ExerciseFilter.cs`, `Src/Views/ExerciseFilterPage.xaml(.cs)`): muscle, kind,
   equipment, level, compound/isolation, fits my equipment, done before, my exercises, secondary muscles; order (best
   match, A–Z, most done, recently done). Filter button with badge on the library tab and in the picker.
5. **Focused replace**: replacing an exercise in a workout or plan opens the picker on "Similar" (same muscle and
   movement, closest first) with an "All exercises" switch (`ExercisePickerViewModel`, `ExercisePickerService.PickOneAsync(title, similarTo)`).
6. **AI exercise pictures**: `ExerciseLibrary.Thumbnail(id)` now returns `exercises/<id>.webp`, bundled in the app at
   `GymBook/Resources/Raw/exercises/` and loaded through `Src/Converters/ThumbnailConverter.cs` (key `Thumb` in App.xaml).
7. Privacy policies (web `GymBook.Web/src/app/privacy/page.tsx`, API `GymBook.Api/Privacy/PrivacyPolicy.cs`) updated
   for YouTube videos and bundled pictures. `PhoneMockup.tsx` uses video thumbnails for now.

## Open tasks

### A. Finish the exercise pictures (needs an OpenAI key)

State at handoff: 80 of 734 made. Check: `ls GymBook/Resources/Raw/exercises | wc -l`. Generate the rest; it skips existing
files and is paced to the account's 5 images/minute (~12 s each):

    cd tools/exercises
    npm install --no-save sharp  # once; don't commit node_modules
    OPENAI_API_KEY=... node thumbs.mjs          # defaults: gpt-image-2, low quality, into the app folder

Look at a few results for consistency (light grey figure, blue working muscles, dark #151821 background, no text).
Regenerate a bad one with `node thumbs.mjs --only <id> --force`.

Then the website mockup: copy `bench_press.webp`, `db_shoulder_press.webp`, `incline_bench_press.webp`,
`triceps_pushdown.webp` into `GymBook.Web/public/exercises/` and change `GymBook.Web/src/components/PhoneMockup.tsx`
to use `/exercises/<id>.webp` (it currently builds YouTube thumbnail URLs from video ids).

### B. Finish the video curation (reputable channels, exact timecodes)

Goal: every exercise's video from a reputable fitness source (Jeff Nippard, RP, Squat University, ATHLEAN-X,
Catalyst Athletics, Hinge Health, Yoga With Adriene, Pilatesology... see the list), trimmed with Start/End when the
video is longer than the demonstration. **The rules are in `tools/exercises/CURATE_BRIEF.md`; follow them exactly.**
Key rule: timecodes come only from a video's chapters (`node yt.mjs info <id>` prints them with start seconds); YouTube
blocks transcripts, so never estimate times, and don't use a long video without a matching chapter.

The work is split into 35 chunks (`tools/exercises/curate/chunks.json`, keys like `Arms-1`). Results live in
`tools/exercises/curate/results/`:
- `<key>.json` = verified picks, ready to apply.
- `<key>.unverified.json` = a picker's results not yet verified: verify them (below) and save as `<key>.json`.
- no file = not done: curate it per the brief, then verify.

Picks are a JSON array, one object per exercise in the chunk:
`{"id","videoId","start","end","channel","title","timeSource":"chapter"|"whole","chapter","note","verdict":"ok"|"fixed"|"reverted"}`
(`start`/`end` integer seconds or null).

Verifying a pick means running `node yt.mjs info <videoId>` and `node yt.mjs check <videoId>` and confirming: it embeds;
the channel is reputable per the brief; title/chapter is this exact exercise and variant (compare with the summary in
chunks.json); start/end are exactly a chapter start and the next chapter's start. Fix it, or revert to the chunk's
`current` video if it can't be fixed. Never write an id you haven't checked.

State at handoff: `Arms-1`, `Arms-2`, `Back-1`, `Mobility-2` have `.unverified.json` picks (verify them); the other 31
chunks aren't started. Chunks can be done in parallel by subagents, one chunk each (curate, then an independent
verify), as long as each one writes only its own `results/<key>.json`.

Apply all verified chunks and check the library:

    node curate/apply.mjs        # or: node curate/apply.mjs Arms-1 Arms-2
    node check.mjs               # must end with "no problems" and "with video 734"

`apply.mjs` logs every change to `logs/videos-curated.md`.

### C. For the owner to build and test (don't do this yourself)

- Phone + Wear build; nothing from this session has been compiled.
- Sign-in: fresh install online and offline; existing install without an account (offline → "Continue offline for
  now"; online → sign in, history kept); sign out; airplane mode while signed in.
- Exercises tab: search ("db bench", "rdl", typos), chips, filter sheet, badge, clear; picker; replace in a workout and
  a plan (Similar / All exercises).
- Pictures in exercise rows, plan/workout cards, home and calendar strips; custom exercises keep their initials.
- Exercise page: embedded video starts/ends at the set times.

## Working from a cloud session

- The OpenAI key is not in the repo; ask the owner for one (or an env var) before task A.
- YouTube may treat a cloud server as a bot. If `node tools/exercises/yt.mjs search "bench press"` returns nothing or
  errors after its retries, stop task B and tell the owner (it worked from their PC); never pick videos without the
  checks.
- Commit the results (pictures, `curate/results/`, the updated `ExerciseLibrary.*.cs`, `logs/`) when done; never
  `node_modules`.
