# Picking demonstration videos from YouTube search results

GymBook's exercise library is hand-written C#, one file per region, in
`C:\GitHub\GymBook\GymBook\Src\Services\Exercises\`. Each exercise is a `new("id", "Name", ...) { ... },` block.
Some already have a `Video = new(...)` line; you fill in the rest for ONE region file.

For every exercise without a video, `<scratchpad>\candidates\<Region>.md` lists YouTube's top search results:

```
## low_bar_squat | Low-Bar Back Squat | Barbell | Strength
- Po9CDtfcLJI | 2:54 | Squat University | How to Perform a Low Bar Back Squat
- BeM2oyJ0W0E | short |  | The BEST Low Bar Squat Tutorial
```

(`short` = a YouTube Short, under a minute; Shorts show no channel there.)

**Do NOT use WebSearch** (the session's budget is used up). Pick from the candidates only. You can never invent or
recall an id: every id you write must be one listed in the candidates file and must pass the check below.

## Choosing

Pick the candidate that most clearly is a clean demonstration of THIS exercise:
- The same movement, variant and equipment as the entry (read its Summary in the C# file when the name is
  ambiguous). Barbell entry → barbell video; reformer entry → reformer video; flexion ≠ extension.
- Prefer, in order: a Short or a video under ~1:30 that's just the demonstration ("how to", "form", "tutorial",
  "demo") → a reputable coaching or physio channel's short tutorial (≤ 4 min) → nothing.
- Reject: compilations ("top 5", "best exercises for"), "mistakes" or "stop doing" videos, workouts and
  follow-alongs, reactions, memes, clickbait, anything over ~6 minutes, and titles that name a different
  exercise or variant. If no candidate is a good match, give the exercise no video. A missing video is fine; a
  wrong one is a bug the user sees.

## Verifying (every id you write)

Run with Bash:
`curl -s "https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=ID&format=json"`
It must return JSON with `title` and `author_name` (anything else, e.g. "Unauthorized" or "Not Found", means it
can't be embedded: don't use it). Check the title again there. You can batch several ids in one Bash call.

## Start and end times (only with hard evidence)

The app plays just a section with `new("ID", Start, End)` (seconds). For a picked video longer than ~1:30 you may
look for chapters: `curl -s -A "Mozilla/5.0" "https://www.youtube.com/watch?v=ID&hl=en"` and search the
description in the page for timestamp lines (e.g. `0:00 Intro`, `0:35 Demonstration`, `1:10 Common mistakes`).
Only if a chapter clearly is the demonstration, set Start to its timestamp and End to the next chapter's.
Otherwise no times. Never estimate.

## Writing it into the file

Add `Video` as the LAST property in the block, after `Tips`, matching the indentation:

```csharp
            Tips =
            [
                "...",
            ],
            Video = new("Po9CDtfcLJI"),
        },
```

Edit only Video lines in your file; change nothing else. Don't build or run the app.
Append to `<scratchpad>\videos-<Region>.md` one line per exercise you handled:
`id | video id or NONE | title | channel | start-end or -`.

When done, reply with only: the file, how many got a video, how many got none, and how many have start/end times.
