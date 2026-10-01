# Curating exercise videos: reputable sources, exact timecodes

GymBook shows one YouTube demonstration per exercise, embedded in the app and trimmed with Start/End (seconds).
Every exercise already has a video (`current`), verified to be the right movement but often from a random channel
or a Short with no channel. Your job: for each exercise in your chunk, find the best video from a **reputable
fitness source**, and when the video is longer than the demonstration, the **exact timecodes** of the part that
demonstrates this exercise.

Your chunk: `node -e "const c=require('./curate/chunks.json').find(c=>c.key==='KEY');console.log(JSON.stringify(c.items,null,1))"`
(run in `C:\GitHub\GymBook\tools\exercises`). Each item: id, name, equipment, category, summary, current (video id).

## Tools (Bash, in `C:\GitHub\GymBook\tools\exercises`)

- `node yt.mjs search "<query>"`: top results as `id | length | channel | title | views | age` (`short` = a Short;
  Shorts show channel `?` in search, `info` shows it).
- `node yt.mjs info <id>`: title, channel, length, views, upload date, **chapters with their start seconds**, and
  the description (chapters are sometimes only written in the description as `0:00 Intro` lines).
- `node yt.mjs check <id>...`: whether it can be embedded (it must say OK).

Do NOT use WebSearch or WebFetch. Searches are cheap; run several per exercise (e.g. `"<name> jeff nippard"`,
`"<name> squat university"`, `"how to <name>"`, `"<name> form"`). YouTube throttles: the script retries; if a
search keeps failing, move on and come back.

## Reputable sources

Prefer, roughly in this order of trust (any of their videos or Shorts):
- Strength/hypertrophy: Jeff Nippard, Renaissance Periodization (Dr. Mike Israetel), Squat University,
  ATHLEAN-X, Jeremy Ethier, Alan Thrall, Mark Rippetoe / Starting Strength, Barbell Medicine, Juggernaut Training
  Systems, Calgary Barbell, Mind Pump, Scott Herman, Buff Dudes, Bodybuilding.com, Garage Strength.
- Olympic lifting / power: Catalyst Athletics, Oleksiy Torokhtiy, Clarence Kennedy, Hookgrip, USA Weightlifting,
  Garage Strength. Strongman: Brian Alsruhe, Mitchell Hooper, Kalle Beck, Strongman Corporation.
- Calisthenics / gymnastics: FitnessFAQs, Calisthenicmovement, Hybrid Calisthenics, Tom Merrick, GMB Fitness.
- Rehab / mobility / neck: E3 Rehab, [P]rehab, Hinge Health, Bob & Brad, AskDoctorJo, Tom Morrison, Squat
  University, Kinstretch/FRC coaches, Knees Over Toes Guy (Ben Patrick), Neck Flex, Iron Neck.
- Yoga: Yoga With Adriene, Yoga with Kassandra, Yoga With Tim, Man Flow Yoga, Yoga Journal, SarahBethYoga.
- Pilates: Pilatesology, Move With Nicole, Jessica Valant Pilates, Pilates Anytime, Balanced Body, Kathryn Ross-Nash.
- Cardio / conditioning: Concept2 (rowing, ski, bike), GCN (cycling), The Run Experience, Global Triathlon
  Network, Precision Run, Onnit Academy.
- Also fine: physiotherapy clinics and hospitals (Mayo Clinic, Cleveland Clinic...), certified coaches with a
  clear professional channel, official equipment makers (Hammer Strength, Rogue, TRX, Life Fitness), established
  media (Men's Health, Women's Health, Well+Good, Howcast, PureGym, Nuffield Health, Live Lean TV, Muscle & Motion).

Avoid: channels with no evident expertise, reposts/compilations, reaction or meme videos, clickbait ("you're doing
it WRONG"), workouts and follow-alongs, very old low-resolution videos (before ~2013), and anything whose title or
chapters name a different variant (dumbbell vs barbell, incline vs flat, flexion vs extension, reformer vs mat).

## Timecodes: only from chapters, never estimated

There is no way to read transcripts (YouTube blocks it). So:
- A long video (e.g. a 12-minute Jeff Nippard video) is usable **only if** it has a chapter that is clearly the
  demonstration of THIS exercise. Then Start = that chapter's start second, End = the next chapter's start
  second (or no End if it's the last chapter). Copy the seconds from `info`; never round or guess.
- A video that is itself just the demonstration (a Short, or a focused tutorial of up to ~3 minutes, or a longer
  one whose chapters are all about this exercise) needs no Start/End; you may set Start to skip a chapter named
  "Intro" when the video has chapters.
- A long video without such a chapter: don't use it.

## Deciding

1. Look at `current` with `info` first. If it is already from a reputable source, clearly this exercise, and
   either short or properly timecodable, keep it (add timecodes if chapters allow).
2. Otherwise search. Pick the most reputable source that clearly demonstrates this exact exercise. A Short from
   Jeff Nippard beats a 2-minute video from an unknown channel.
3. If nothing reputable fits, keep `current` (it is already verified as the right movement). Never return a video
   you haven't checked with `info` and `check` in this run, and never invent or recall an id.

Don't edit any files. Return your results only.
