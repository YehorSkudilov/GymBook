# Adding new exercises to GymBook's library

The repo is at `D:\GitHub\GymBook` (older docs say `C:\GitHub\GymBook`; read that as D:). The library has 736
hand-written exercises in `D:\GitHub\GymBook\GymBook\Src\Services\Exercises\ExerciseLibrary.<Region>.cs`. You add
the NEW exercises listed in your task to your region file(s), each one complete with a curated YouTube video.

Read first, and follow exactly:
1. `D:\GitHub\GymBook\tools\exercises\BRIEF.md`: entry format, enums, naming, Summary/Steps/Tips rules, accuracy.
2. `D:\GitHub\GymBook\tools\exercises\CURATE_BRIEF.md`: how to pick the video and timecodes (reputable sources,
   chapters only, `node yt.mjs search|info|check`, run in `D:\GitHub\GymBook\tools\exercises`). Ignore its "don't edit
   files" line: you DO edit your region file(s). Do NOT use WebSearch or WebFetch.
3. Your region file(s): match their style, comment sections and level of detail. Video syntax is
   `Video = new("YouTubeId")` or `Video = new("YouTubeId", startSeconds, endSeconds)` (or with only a start), the
   last property of the entry, as existing entries do.

Rules:
- Only ADD entries. Never change or remove existing entries or ids. Put each new entry in the most fitting
  existing `// Section` (or a new section if none fits), not all at the end.
- Ids and names must be unique across the WHOLE library: check `D:\GitHub\GymBook\tools\exercises\library.json`
  (all current entries) before choosing. Other agents are adding to other regions at the same time; the only
  overlap risk is a name; make yours specific.
- If a listed exercise turns out to duplicate an existing entry (same movement under another name), or isn't a
  real, commonly done exercise, skip it and say so. You may change the name to what people really call it.
- Every new entry gets a video that passed `node yt.mjs info <id>` and `node yt.mjs check <id>` (OK) in this run.
  Never recall or invent a YouTube id. If no reputable source exists, use the best clearly-correct
  demonstration you verified, and note it.
- Use your own scratchpad subfolder for any temp files: `<scratchpad>\<your-key>\` (given in your task). Other agents
  run in parallel; don't touch their files or the shared tools.
- Edit with the Edit tool; keep the file's encoding (UTF-8) and CRLF line endings consistent with the file.
- When done, run `node check.mjs` in `D:\GitHub\GymBook\tools\exercises` and fix any problem it reports in YOUR
  file(s) (problems in other files are other agents' work in progress; ignore them).
- Don't build, run tests, generate pictures, or touch any other file.

Reply with only: per file, the ids added with `id | name | video id | channel | start-end`, plus any exercise you
skipped or renamed and why, and any weak video pick.
