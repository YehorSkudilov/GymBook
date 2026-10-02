import { BoltIcon, ChartIcon, DumbbellIcon, ListIcon, OfflineIcon, SparkleIcon, SyncIcon, TimerIcon } from "@/components/Icons";
import { PhoneMockup } from "@/components/PhoneMockup";
import { StoreButtons } from "@/components/StoreButtons";
import { site } from "@/lib/site";
import styles from "./page.module.css";

const features = [
  {
    icon: <SparkleIcon />,
    tone: "purple",
    title: "A plan built for you",
    text: "Answer a few questions about your goal, experience, days and equipment, and get a full program. Tweak it by chatting with the AI coach, or import one you already follow.",
  },
  {
    icon: <DumbbellIcon />,
    tone: "blue",
    title: "Log sets in seconds",
    text: "Your last weights are already filled in. Tap to finish a set, the rest timer can start on its own, and warm-up sets are worked out for you.",
  },
  {
    icon: <BoltIcon />,
    tone: "green",
    title: "Know what's recovered",
    text: "A muscle map shows how rested each muscle group is, so you know which day is ready to go - and get a heads-up before you train something still sore.",
  },
  {
    icon: <ChartIcon />,
    tone: "orange",
    title: "See yourself progress",
    text: "Volume, sets, estimated 1RM and personal records per exercise, a training calendar and full history, with weights that go up when you're ready for it.",
  },
  {
    icon: <ListIcon />,
    tone: "blue",
    title: "739 exercises",
    text: "Every exercise has photos, step-by-step instructions and the muscles it works. Filter by muscle or equipment, or add your own.",
  },
  {
    icon: <SyncIcon />,
    tone: "purple",
    title: "Phone, watch and computer, in sync",
    text: "Sign in to back up your training and pick up where you left off on your phone, Mac or PC - even mid-workout. On a Wear OS watch, tick sets and see your rest and heart rate.",
  },
];

const faq = [
  {
    q: "Is Gym Book free?",
    a: "Yes. Logging, plans, recovery and progress are all free, with no ads.",
  },
  {
    q: "Do I need an account?",
    a: "No. Gym Book works fully offline and keeps everything on your device. An account is only needed if you want to back up your data and sync it between devices.",
  },
  {
    q: "Which devices does it run on?",
    a: "Android phones, Wear OS watches, iPhone and iPad, Macs and Windows PCs. Your data syncs between all of them when you're signed in, and the watch app follows the workout on your phone.",
  },
  {
    q: "How do the AI features work?",
    a: "When you ask for a plan, chat with the coach or import a plan, the details it needs (like your goal, experience and the plan) are sent to OpenAI to write the answer. Your name and email are never sent. See the privacy policy for the details.",
  },
  {
    q: "Can I use kilograms?",
    a: "Yes - switch between kg and lbs any time in your profile.",
  },
  {
    q: "How do I delete my data?",
    a: "Delete your account from the Profile tab and everything synced to it is removed from our server straight away. You can also request it by email without the app.",
  },
];

export default function Home() {
  return (
    <>
      {/* ---------- Hero ---------- */}
      <section className={styles.hero}>
        <div className={styles.glow} aria-hidden="true" />
        <div className={`container ${styles.heroInner}`}>
          <div className={styles.heroText}>
            <span className={styles.badge}>
              <i /> Free · No ads · Works offline
            </span>
            <h1>
              Train smarter.
              <br />
              <span className="gradient-text">Track everything.</span>
            </h1>
            <p className={styles.lead}>{site.description}</p>
            <div className={styles.heroActions}>
              <a href="#download" className="btn btn-primary">
                Download free
              </a>
              <a href="#features" className="btn btn-secondary">
                See features
              </a>
            </div>
          </div>
          <div className={styles.heroPhone}>
            <PhoneMockup />
          </div>
        </div>
      </section>

      {/* ---------- Highlights strip ---------- */}
      <section className={styles.strip}>
        <div className={`container ${styles.stripInner}`}>
          <div>
            <b>739</b>
            <span>exercises with photos</span>
          </div>
          <div>
            <OfflineIcon size={22} />
            <span>works without internet</span>
          </div>
          <div>
            <TimerIcon size={22} />
            <span>automatic rest timer</span>
          </div>
          <div>
            <SyncIcon size={22} />
            <span>syncs phone, watch &amp; PC</span>
          </div>
        </div>
      </section>

      {/* ---------- Features ---------- */}
      <section id="features" className="section">
        <div className="container">
          <div className="section-head">
            <span className="eyebrow">Features</span>
            <h2>Everything a training log should be</h2>
            <p>From the first plan to your hundredth workout, Gym Book does the bookkeeping so you can focus on lifting.</p>
          </div>
          <div className={styles.features}>
            {features.map((f) => (
              <article key={f.title} className={`card ${styles.feature}`}>
                <span className={`${styles.featureIcon} ${styles[f.tone]}`}>{f.icon}</span>
                <h3>{f.title}</h3>
                <p>{f.text}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      {/* ---------- Spotlights ---------- */}
      <section className={`section ${styles.spotlights}`}>
        <div className="container">
          <div className={styles.spotlight}>
            <div className={styles.spotText}>
              <span className="eyebrow">Recovery</span>
              <h2>Train what&apos;s ready. Rest what isn&apos;t.</h2>
              <p>
                Every set you log feeds a recovery estimate for each muscle group. Home shows whether your next day is good to go,
                and Gym Book warns you before you hit a muscle that still needs time.
              </p>
            </div>
            <div className={`card ${styles.spotCard}`}>
              <div className={styles.cardTitle}>
                <strong>Muscle recovery</strong>
                <span>Today</span>
              </div>
              {[
                { m: "Chest", v: 100 },
                { m: "Shoulders", v: 92 },
                { m: "Triceps", v: 85 },
                { m: "Back", v: 58 },
                { m: "Quads", v: 34 },
              ].map((r) => (
                <div key={r.m} className={styles.recoveryRow}>
                  <span>{r.m}</span>
                  <div className={styles.bar}>
                    <i
                      style={{ width: `${r.v}%` }}
                      className={r.v >= 80 ? styles.barGood : r.v >= 50 ? styles.barMid : styles.barLow}
                    />
                  </div>
                  <b>{r.v}%</b>
                </div>
              ))}
            </div>
          </div>

          <div className={`${styles.spotlight} ${styles.reverse}`}>
            <div className={styles.spotText}>
              <span className="eyebrow">AI coach</span>
              <h2>Your plan, adjusted in a sentence.</h2>
              <p>
                Short on time, missing equipment or want more arms? Tell the coach and it reworks the plan. Every week it takes a look at
                how training went and suggests what to change.
              </p>
            </div>
            <div className={`card ${styles.spotCard} ${styles.chat}`}>
              <div className={styles.msgMe}>Can you make Push A fit in 40 minutes?</div>
              <div className={styles.msgAi}>
                <SparkleIcon size={16} />
                <span>
                  Done - I dropped the second chest isolation and paired lateral raises with triceps pushdowns. Push A is now 4 exercises,
                  14 sets, about 38 minutes.
                </span>
              </div>
              <div className={styles.chatActions}>
                <span className="btn btn-primary">Apply changes</span>
                <span className="btn btn-secondary">Keep chatting</span>
              </div>
            </div>
          </div>

          <div className={styles.spotlight}>
            <div className={styles.spotText}>
              <span className="eyebrow">Progress</span>
              <h2>Watch the numbers climb.</h2>
              <p>
                Weekly volume, sets per muscle, estimated one-rep max and personal records for every exercise. When you hit all your reps,
                Gym Book bumps the weight next time.
              </p>
            </div>
            <div className={`card ${styles.spotCard}`}>
              <div className={styles.cardTitle}>
                <strong>Weekly volume</strong>
                <span className={styles.up}>▲ 18%</span>
              </div>
              <div className={styles.chart}>
                {[38, 44, 41, 52, 49, 60, 58, 71].map((h, i) => (
                  <i key={i} style={{ height: `${h}%` }} className={i === 7 ? styles.chartNow : undefined} />
                ))}
              </div>
              <div className={styles.prRow}>
                <span className={styles.prBadge}>PR</span>
                <span>Bench press · 205 lbs × 5</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ---------- Download ---------- */}
      <section id="download" className="section">
        <div className="container">
          <div className={`card ${styles.download}`}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/logo.svg" alt="" width={56} height={68} />
            <h2>Get Gym Book</h2>
            <p>Free on Android, Wear OS, iPhone, iPad, Mac and Windows.</p>
            <StoreButtons large />
          </div>
        </div>
      </section>

      {/* ---------- FAQ ---------- */}
      <section id="faq" className="section">
        <div className="container">
          <div className="section-head">
            <span className="eyebrow">FAQ</span>
            <h2>Questions, answered</h2>
          </div>
          <div className={styles.faq}>
            {faq.map((f) => (
              <details key={f.q} className="card">
                <summary>{f.q}</summary>
                <p>{f.a}</p>
              </details>
            ))}
          </div>
        </div>
      </section>
    </>
  );
}
