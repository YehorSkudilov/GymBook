"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import {
  BackIcon,
  BoltIcon,
  ChartIcon,
  CheckIcon,
  DumbbellIcon,
  LogoutIcon,
  MailIcon,
  ShieldIcon,
  SparkleIcon,
  TimerIcon,
  TrashIcon,
} from "@/components/Icons";
import { useShell } from "@/components/Shell";
import { UserStatusBadges } from "@/components/UserBadges";
import { Avatar, BarChart, ConfirmSheet, Meter, Skeleton, StatTile, useToast } from "@/components/ui";
import { adminDelete, adminPost, useAdmin } from "@/lib/client";
import { ago, date, dateTime, minutesBetween, number, volume, weight, words } from "@/lib/format";
import type { Role, UserDetail } from "@/lib/types";
import styles from "./user.module.css";

type Pending = {
  title: string;
  message: React.ReactNode;
  confirmLabel: string;
  danger?: boolean;
  run: () => Promise<void>;
  done: string;
  /** Typed confirmation required before the button works (deleting). */
  confirmText?: string;
};

const shortDate = (iso: string) => new Date(iso + "T00:00:00").toLocaleDateString(undefined, { day: "numeric", month: "short" });

export default function UserPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { me, unit } = useShell();
  const { data, error, loading, reload } = useAdmin<UserDetail>(`users/${encodeURIComponent(id)}`);
  const toast = useToast();
  const [pending, setPending] = useState<Pending | null>(null);
  const [typed, setTyped] = useState("");
  const [busy, setBusy] = useState(false);

  if (error && !data)
    return (
      <div className="page">
        <BackLink />
        <div className="error-banner">{error}</div>
      </div>
    );
  if (loading && !data)
    return (
      <div className="page">
        <BackLink />
        <Skeleton height={120} />
        <div className="stats-row">
          {[0, 1, 2, 3].map((i) => (
            <Skeleton key={i} height={86} />
          ))}
        </div>
        <Skeleton height={260} />
      </div>
    );
  if (!data) return null;

  const u = data.user;
  const isSelf = me?.userId === u.id;
  const isSuper = me?.role === "SuperAdmin";
  // Mirrors the API's rules: never your own account, and only a SuperAdmin touches another admin.
  const canAct = !!me && !isSelf && (!u.role || isSuper);
  const profile = data.profile;
  const age = profile?.birthYear ? new Date().getFullYear() - profile.birthYear : null;

  function ask(p: Pending) {
    setTyped("");
    setPending(p);
  }

  async function confirm() {
    if (!pending) return;
    setBusy(true);
    try {
      await pending.run();
      toast.show(pending.done);
      setPending(null);
    } catch (e) {
      toast.show(e instanceof Error ? e.message : "Something went wrong.", true);
    } finally {
      setBusy(false);
    }
  }

  const act = (path: string, body?: unknown) => async () => {
    await adminPost(`users/${encodeURIComponent(u.id)}/${path}`, body);
    await reload();
  };

  function changeRole(role: Role | null) {
    ask({
      title: role ? `Make ${role}?` : "Remove admin access?",
      message: role
        ? role === "SuperAdmin"
          ? `${u.email} will be able to do everything you can, including changing roles and deleting accounts.`
          : `${u.email} will be able to see every user and the stats, and disable accounts, but not change roles or delete accounts.`
        : `${u.email} will no longer be able to sign in to the admin site.`,
      confirmLabel: role ? `Make ${role}` : "Remove access",
      danger: !role,
      run: act("role", { role }),
      done: role ? `${u.email} is now ${role === "Admin" ? "an" : "a"} ${role}.` : "Admin access removed.",
    });
  }

  return (
    <div className="page">
      <BackLink />

      {/* ---------- Who ---------- */}
      <section className={`card ${styles.hero}`}>
        <Avatar name={u.name || u.email} size={64} />
        <div className={styles.heroText}>
          <h1>{u.name || u.email.split("@")[0]}</h1>
          <p className="subtle">{u.email}</p>
          <UserStatusBadges user={u} />
        </div>
        <dl className={styles.heroMeta}>
          <div>
            <dt>Joined</dt>
            <dd>{date(u.createdAt)}</dd>
          </div>
          <div>
            <dt>Last active</dt>
            <dd>{ago(u.lastActiveAt)}</dd>
          </div>
          <div>
            <dt>Signed in on</dt>
            <dd>
              {data.activeSessions} device{data.activeSessions === 1 ? "" : "s"}
            </dd>
          </div>
          <div>
            <dt>Sign-in</dt>
            <dd>{[data.hasPassword && "Password", u.hasGoogle && "Google"].filter(Boolean).join(" + ") || "-"}</dd>
          </div>
        </dl>
      </section>

      {/* ---------- Training ---------- */}
      <div className="stats-row">
        <StatTile
          icon={<DumbbellIcon />}
          tone="blue"
          value={number(data.stats.workouts)}
          label="Workouts"
          hint={`${data.stats.workouts30d} in the last 30 days`}
        />
        <StatTile
          icon={<ChartIcon />}
          tone="purple"
          value={number(data.stats.workingSets)}
          label="Working sets"
        />
        <StatTile
          icon={<BoltIcon />}
          tone="orange"
          value={volume(data.stats.volumeKg, unit)}
          label="Volume lifted"
        />
        <StatTile
          icon={<TimerIcon />}
          tone="green"
          value={`${data.stats.totalHours} h`}
          label="Time training"
          hint={data.stats.lastWorkoutAt ? `Last ${ago(data.stats.lastWorkoutAt)}` : "No workouts yet"}
        />
      </div>

      <div className="grid-3">
        <section className="card">
          <div className="card-head">
            <h2>Workouts per week</h2>
            <span className="subtle">Last 12 weeks</span>
          </div>
          <BarChart
            height={160}
            data={data.stats.workoutsByWeek.map((w) => ({
              key: w.weekStart,
              value: w.count,
              label: `Week of ${shortDate(w.weekStart)}`,
            }))}
            label={(v) => `${v} workouts`}
          />
          {profile && (
            <p className="faint" style={{ marginTop: 12, fontSize: 13 }}>
              Aims for {profile.daysPerWeek} days a week
            </p>
          )}
        </section>

        <section className="card">
          <div className="card-head">
            <h2>Profile</h2>
            {profile && <span className="subtle">Updated {ago(profile.updatedAt)}</span>}
          </div>
          {profile ? (
            <dl className={styles.facts}>
              <Fact label="Goal" value={words(profile.goal)} />
              <Fact label="Experience" value={words(profile.experience)} />
              <Fact label="Schedule" value={`${profile.daysPerWeek} days · ${profile.sessionMinutes} min`} />
              <Fact label="Equipment" value={words(profile.equipmentAccess)} />
              <Fact label="Body weight" value={weight(data.stats.latestBodyWeightKg ?? profile.bodyWeightKg, unit)} />
              {age != null && <Fact label="Age" value={`${age}`} />}
              {profile.bodyFatPercent != null && <Fact label="Body fat" value={`${profile.bodyFatPercent}%`} />}
              {profile.trainingSince && <Fact label="Training since" value={date(profile.trainingSince)} />}
              <Fact label="Uses" value={profile.unit === "Lbs" ? "lbs" : "kg"} />
              <Fact label="Onboarding" value={profile.onboardingDone ? "Done" : "Not finished"} />
            </dl>
          ) : (
            <p className="empty">Hasn&apos;t synced a profile yet.</p>
          )}
        </section>
      </div>

      <div className="grid-3">
        <section className="card">
          <div className="card-head">
            <h2>Recent workouts</h2>
            <span className="subtle">
              {data.stats.firstWorkoutAt ? `Training since ${date(data.stats.firstWorkoutAt)}` : ""}
            </span>
          </div>
          {data.recentWorkouts.length ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Workout</th>
                    <th>When</th>
                    <th>Duration</th>
                    <th className="num">Exercises</th>
                    <th className="num">Sets</th>
                    <th className="num">Volume</th>
                  </tr>
                </thead>
                <tbody>
                  {data.recentWorkouts.map((w) => (
                    <tr key={w.id}>
                      <td>
                        <strong>{w.name || "Workout"}</strong>
                        {w.planWeek != null && <span className="faint"> · week {w.planWeek}</span>}
                      </td>
                      <td className="subtle">{dateTime(w.startedAt)}</td>
                      <td className="subtle">{minutesBetween(w.startedAt, w.endedAt)}</td>
                      <td className="num">{w.exercises}</td>
                      <td className="num">{w.workingSets}</td>
                      <td className="num">{volume(w.volumeKg, unit)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="empty">No workouts logged yet.</p>
          )}
        </section>

        <section className="card">
          <div className="card-head">
            <h2>Plans</h2>
            <span className="subtle">
              {data.stats.customExercises} custom exercise{data.stats.customExercises === 1 ? "" : "s"}
            </span>
          </div>
          <div className={styles.plans}>
            {data.plans.map((p) => (
              <div key={p.id} className={`${styles.plan} ${p.isActive ? styles.planActive : ""}`}>
                <div className={styles.planTop}>
                  <strong>{p.name || "Untitled plan"}</strong>
                  {p.isActive && <span className="badge badge-green">Active</span>}
                </div>
                <span className="subtle">
                  {words(p.goal)} · {p.days} day{p.days === 1 ? "" : "s"} · {p.exercises} exercises
                </span>
                <span className="faint">Created {date(p.createdAt)}</span>
              </div>
            ))}
            {data.plans.length === 0 && <p className="empty">No plans yet.</p>}
          </div>
        </section>
      </div>

      <div className="grid-2">
        {/* ---------- AI ---------- */}
        <section className="card">
          <div className="card-head">
            <h2>
              <SparkleIcon size={18} /> AI usage
            </h2>
            <span className="subtle">
              {data.ai.plans30d} plans · {data.ai.chats30d} messages in 30 days
            </span>
          </div>
          <div className={styles.quotas}>
            <Quota title="AI plans" use={data.ai.plan} />
            <Quota title="Coach messages" use={data.ai.chat} />
          </div>
          {canAct && (
            <button
              className="btn btn-small"
              style={{ marginTop: 16 }}
              disabled={data.ai.plan.used === 0 && data.ai.chat.used === 0}
              onClick={() =>
                ask({
                  title: "Reset AI quota?",
                  message: "Gives back the AI plans and coach messages used in the current limits, so they can use them again now.",
                  confirmLabel: "Reset quota",
                  run: act("ai-quota/reset"),
                  done: "AI quota reset.",
                })
              }
            >
              Reset quota
            </button>
          )}
        </section>

        {/* ---------- Actions ---------- */}
        <section className="card">
          <div className="card-head">
            <h2>
              <ShieldIcon size={18} /> Account
            </h2>
          </div>
          {!canAct ? (
            <p className="subtle">
              {isSelf
                ? "This is your account. Manage it in the app; another SuperAdmin can change its role."
                : "Only a SuperAdmin can change another admin's account."}
            </p>
          ) : (
            <div className={styles.actions}>
              <ActionRow
                title="Email"
                text={u.emailVerified ? "Verified" : "Not verified yet: syncing and AI are blocked until it is."}
              >
                <button
                  className="btn btn-small"
                  onClick={() =>
                    ask({
                      title: u.emailVerified ? "Mark email unverified?" : "Mark email verified?",
                      message: u.emailVerified
                        ? "They'll be asked for a code from their inbox again before they can sync or use AI."
                        : "Skips the emailed code. Only do this if you're sure the address is theirs.",
                      confirmLabel: u.emailVerified ? "Mark unverified" : "Mark verified",
                      run: act("email-verified", { verified: !u.emailVerified }),
                      done: u.emailVerified ? "Marked unverified." : "Marked verified.",
                    })
                  }
                >
                  <CheckIcon size={16} /> {u.emailVerified ? "Unverify" : "Verify"}
                </button>
              </ActionRow>

              <ActionRow title="Password reset" text="Email them a code to set a new password.">
                <button
                  className="btn btn-small"
                  onClick={() =>
                    ask({
                      title: "Send password reset email?",
                      message: `${u.email} gets a code to enter in the app under "Forgot password?".`,
                      confirmLabel: "Send email",
                      run: act("password-reset-email"),
                      done: "Password reset email sent.",
                    })
                  }
                >
                  <MailIcon size={16} /> Send
                </button>
              </ActionRow>

              <ActionRow
                title="Sessions"
                text={`Signed in on ${data.activeSessions} device${data.activeSessions === 1 ? "" : "s"}.`}
              >
                <button
                  className="btn btn-small"
                  disabled={data.activeSessions === 0}
                  onClick={() =>
                    ask({
                      title: "Sign out everywhere?",
                      message: "Every device is signed out within 15 minutes. Data already on their devices stays there.",
                      confirmLabel: "Sign out everywhere",
                      run: act("sign-out"),
                      done: "Signed out everywhere.",
                    })
                  }
                >
                  <LogoutIcon size={16} /> Sign out
                </button>
              </ActionRow>

              <ActionRow
                title={u.status === "Disabled" ? "Disabled" : "Access"}
                text={
                  u.status === "Disabled"
                    ? "Can't sign in or sync."
                    : u.status === "Locked"
                      ? "Locked for a few minutes after too many wrong passwords."
                      : "Can sign in and sync."
                }
              >
                {u.status === "Disabled" ? (
                  <button
                    className="btn btn-small btn-success"
                    onClick={() =>
                      ask({
                        title: "Enable account?",
                        message: "They can sign in and sync again.",
                        confirmLabel: "Enable",
                        run: act("disabled", { disabled: false }),
                        done: "Account enabled.",
                      })
                    }
                  >
                    Enable
                  </button>
                ) : (
                  <button
                    className="btn btn-small btn-danger"
                    onClick={() =>
                      ask({
                        title: "Disable account?",
                        message:
                          "Signs them out everywhere and stops them signing in or syncing until you enable it again. Nothing is deleted.",
                        confirmLabel: "Disable",
                        danger: true,
                        run: act("disabled", { disabled: true }),
                        done: "Account disabled.",
                      })
                    }
                  >
                    Disable
                  </button>
                )}
              </ActionRow>

              {isSuper && (
                <>
                  <ActionRow title="Role" text="Admins can use this site; SuperAdmins can also change roles and delete accounts.">
                    <select
                      className="select"
                      style={{ width: 150, height: 34 }}
                      value={u.role ?? ""}
                      onChange={(e) => changeRole((e.target.value || null) as Role | null)}
                    >
                      <option value="">User</option>
                      <option value="Admin">Admin</option>
                      <option value="SuperAdmin">SuperAdmin</option>
                    </select>
                  </ActionRow>

                  <ActionRow title="Delete account" text="Deletes the account and everything synced to it. Can't be undone.">
                    <button
                      className="btn btn-small btn-danger"
                      onClick={() =>
                        ask({
                          title: "Delete this account?",
                          message: (
                            <>
                              Permanently deletes <strong>{u.email}</strong> with its {data.stats.workouts} workouts,{" "}
                              {data.plans.length} plans and profile, the same as &quot;Delete account&quot; in the app. Type the
                              email to confirm.
                            </>
                          ),
                          confirmLabel: "Delete forever",
                          danger: true,
                          confirmText: u.email,
                          run: async () => {
                            await adminDelete(`users/${encodeURIComponent(u.id)}`);
                            router.replace("/users");
                          },
                          done: "Account deleted.",
                        })
                      }
                    >
                      <TrashIcon size={16} /> Delete
                    </button>
                  </ActionRow>
                </>
              )}
            </div>
          )}
        </section>
      </div>

      <ConfirmSheet
        open={!!pending}
        title={pending?.title ?? ""}
        message={pending?.message}
        confirmLabel={pending?.confirmLabel ?? ""}
        danger={pending?.danger}
        busy={busy}
        disabled={!!pending?.confirmText && typed.trim().toLowerCase() !== pending.confirmText.toLowerCase()}
        onConfirm={confirm}
        onCancel={() => !busy && setPending(null)}
      >
        {pending?.confirmText && (
          <input className="input" placeholder={pending.confirmText} value={typed} onChange={(e) => setTyped(e.target.value)} />
        )}
      </ConfirmSheet>
      {toast.view}
    </div>
  );
}

function BackLink() {
  return (
    <Link href="/users" className={styles.back}>
      <BackIcon size={18} /> Users
    </Link>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function Quota({ title, use }: { title: string; use: { used: number; limit: number; period: string } }) {
  return (
    <div className={styles.quota}>
      <div className={styles.quotaTop}>
        <strong>{title}</strong>
        <span className="subtle">
          {use.used} of {use.limit} per {use.period}
        </span>
      </div>
      <Meter value={use.used} max={use.limit} />
    </div>
  );
}

function ActionRow({ title, text, children }: { title: string; text: string; children: React.ReactNode }) {
  return (
    <div className={styles.actionRow}>
      <div>
        <strong>{title}</strong>
        <span>{text}</span>
      </div>
      {children}
    </div>
  );
}
