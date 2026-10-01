# Linking demonstration videos to GymBook's exercises

GymBook's exercise library is hand-written C#, one file per region, in
`C:\GitHub\GymBook\GymBook\Src\Services\Exercises\`. Each exercise is a `new("id", "Name", ...) { ... },` block.
You give the exercises of ONE region file a YouTube demonstration video each, where a good one exists.

## The one rule that matters

**Never invent, guess or "remember" a video id.** Every id you write must (1) come from a real search result or page
you fetched in this task, and (2) pass the oEmbed check below. A missing video is fine; a wrong one is a bug the
user will see. If in doubt, leave the exercise without a video.

## What a good video is

- It shows THIS exercise (same movement, same equipment, same variant). A "top 5 chest exercises" compilation is
  wrong; a video of the dumbbell version for the barbell entry is wrong.
- Short and to the point: prefer YouTube Shorts and short (< 2 min) form demos from reputable coaching channels
  (e.g. Renaissance Periodization, Jeff Nippard, Squat University, ATHLEAN-X, Mind Pump, Bowflex, Gymshark,
  Men's Health / Women's Health, Well+Good, Howcast, Livestrong, physio channels; for Pilates: Pilates Anytime,
  Lottie Murphy, Move With Nicole, Jessica Valant, Balanced Body, Merrithew; for yoga: Yoga With Adriene,
  Yoga Journal; for neck: Iron Neck, Neck Flex, physio channels; for Olympic lifting: Catalyst Athletics, USA
  Weightlifting, Torokhtiy). Other channels are fine if the video is clearly a clean demonstration.
- No clickbait, no reaction videos, no live streams, no long workouts where the exercise is one minute of forty.

## Start and end times (seconds)

The app embeds the video and can play just a section: `new ExerciseVideo("ID", Start, End)`. You cannot watch
videos, so only set Start/End when you have hard evidence: chapter timestamps in the video's description or a
transcript that you actually fetched and that clearly marks where the demonstration starts and ends. Otherwise
give only the id (and prefer a short video that is all demonstration, so no trimming is needed).

## How to search and verify

1. Use WebSearch, e.g. `"<exercise name>" how to youtube shorts` or `<exercise name> proper form youtube`.
   Results with youtube.com/watch?v=ID, youtube.com/shorts/ID or youtu.be/ID give you the id (11 characters).
2. Verify EVERY id with WebFetch on
   `https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=ID&format=json`
   It must return JSON with a `title` and `author_name`. A 401/403/404 means the video is missing or can't be
   embedded: don't use it. Check that the title really is this exercise.
3. One good video per exercise is enough. Spend your effort widely: try every exercise in the file, at most two or
   three searches each, then move on.

## Writing it into the file

Add a `Video` line as the LAST property inside the exercise's block, after `Tips`:

```csharp
            Tips =
            [
                "...",
            ],
            Video = new("dQw4w9WgXcQ"),
        },
```

With trimming: `Video = new("dQw4w9WgXcQ", 12, 41),`. Edit only your file and only add Video lines; don't change
anything else. Don't build or run anything.

Also write a log to `<scratchpad>\videos-<Region>.md`: one line per exercise —
`id | video id or NONE | video title | channel | start-end or -`.

When done, reply with only: the file, how many exercises got a video, how many got none, and how many have
start/end times.
