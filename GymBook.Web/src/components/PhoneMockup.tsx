import { BoltIcon, ChartIcon, DumbbellIcon, ListIcon, PlayIcon } from "./Icons";
import styles from "./PhoneMockup.module.css";

// The same exercise illustrations the app shows in workout cards.
const thumbnail = (exerciseId: string) => `/exercises/${exerciseId}.webp`;
const pushDay = [
  "bench_press",
  "db_shoulder_press",
  "incline_bench_press",
  "triceps_pushdown",
];

const week = [
  { d: "M", n: 28, done: true },
  { d: "T", n: 29 },
  { d: "W", n: 30, done: true },
  { d: "T", n: 1, today: true },
  { d: "F", n: 2 },
  { d: "S", n: 3 },
  { d: "S", n: 4 },
];

// A drawn copy of the app's Home tab: the week strip, the stats and the "Up next" card, over the glass nav bar.
export function PhoneMockup() {
  return (
    <div className={styles.phone} role="img" aria-label="The Gym Book home screen">
      <div className={styles.screen}>
        <div className={styles.status}>
          <span>9:41</span>
          <span className={styles.island} />
          <span className={styles.statusIcons}>
            <i />
            <i />
            <i />
          </span>
        </div>

        <div className={styles.content}>
          <div className={styles.topRow}>
            <span className={styles.date}>Thursday, 1 October</span>
            <span className={styles.online}>
              <i />
              Online
            </span>
          </div>
          <h3 className={styles.greeting}>Good morning, Alex</h3>

          <div className={styles.card}>
            <div className={styles.cardHead}>
              <strong>This week</strong>
              <span className={styles.streak}>🔥 3 week streak</span>
            </div>
            <div className={styles.week}>
              {week.map((w, i) => (
                <div key={i} className={styles.day}>
                  <span>{w.d}</span>
                  <b className={`${w.done ? styles.dayDone : ""} ${w.today ? styles.dayToday : ""}`}>{w.n}</b>
                </div>
              ))}
            </div>
            <div className={styles.stats}>
              <div>
                <DumbbellIcon size={18} className={styles.statBlue} />
                <b>2/4</b>
                <span>Workouts</span>
              </div>
              <div>
                <ChartIcon size={18} className={styles.statPurple} />
                <b>38</b>
                <span>Sets</span>
              </div>
              <div>
                <BoltIcon size={18} className={styles.statOrange} />
                <b>21.4k</b>
                <span>lbs</span>
              </div>
            </div>
          </div>

          <div className={styles.card}>
            <div className={styles.upNext}>
              <div>
                <span className={styles.upLabel}>Up next</span>
                <h4>Push A</h4>
                <span className={styles.muscles}>Chest · Shoulders · Triceps</span>
                <span className={styles.ready}>100% recovered · ready to go</span>
              </div>
              <div className={styles.ring}>
                <BoltIcon size={16} />
              </div>
            </div>
            <div className={styles.thumbs}>
              {pushDay.map((id) => (
                // eslint-disable-next-line @next/next/no-img-element
                <img key={id} src={thumbnail(id)} alt="" loading="lazy" />
              ))}
            </div>
            <div className={styles.startRow}>
              <span className={styles.meta}>
                4 exercises · 15 sets
                <br />
                ~45 min
              </span>
              <span className={styles.start}>
                <PlayIcon size={14} /> Start
              </span>
            </div>
          </div>
        </div>

        <div className={styles.navBar}>
          <span>
            <ListIcon size={18} />
            Exercises
          </span>
          <span>
            <ListIcon size={18} />
            Plans
          </span>
          <span className={styles.navMain}>
            <DumbbellIcon size={22} />
          </span>
          <span>
            <ChartIcon size={18} />
            Progress
          </span>
          <span>
            <span className={styles.avatar} />
            Profile
          </span>
        </div>
      </div>
    </div>
  );
}
