"use client";

import { useState } from "react";
import { AdminRole, isForever, ProForever, SuperAdminRole, type ProInfo, type UserDetail } from "@/lib/api";
import * as f from "@/lib/format";
import { Badges, Bars, Info, Status, Tiles, useAct, useAdmin, useLoad } from "./ui";
import s from "./admin.module.css";

const storeNames = { GooglePlay: "Google Play", AppStore: "App Store", MicrosoftStore: "Microsoft Store" };

function proUntil(until: string) {
  return isForever(until) ? "for good" : `until ${f.date(until)}`;
}

function proText(pro: ProInfo) {
  const parts: string[] = [];
  if (pro.grantedUntil) parts.push(`Given ${proUntil(pro.grantedUntil)}`);
  if (pro.store && pro.storeExpiresAt)
    parts.push(
      `${pro.inTrial ? "Free trial" : storeNames[pro.store]} (${pro.productId === "gymbook_pro_yearly" ? "yearly" : "monthly"}), ` +
        `${pro.autoRenewing ? "renews" : "ends"} ${f.date(pro.storeExpiresAt)}`,
    );
  return parts.length ? parts.join(" · ") : "No";
}

const durationLabel = (start: string, end?: string) => {
  const minutes = end ? Math.round((Date.parse(end) - Date.parse(start)) / 60_000) : 0;
  return minutes > 0 ? `${minutes} min · ` : "";
};

/**
 * One account: who it is, how it trains, its AI use and Pro, and what an admin can do about it. The API has the final
 * say on every action (nobody acts on their own account but for Pro; only a SuperAdmin acts on another admin, changes
 * roles or deletes); the buttons follow the same rules so they don't offer what would be refused.
 */
export function UserDetailScreen({ id }: { id: string }) {
  const { api } = useAdmin();
  const { data: d, error, busy, reload } = useLoad(() => api.user(id), id);

  return (
    <div className={s.stack}>
      <a href="#/users" className={s.small}>
        ← Users
      </a>
      <Status busy={busy && !d} error={error} />
      {d && <Detail d={d} reload={reload} />}
    </div>
  );
}

function Detail({ d, reload }: { d: UserDetail; reload: () => void }) {
  const { api, toast, go } = useAdmin();
  const { act, working } = useAct(reload);
  const u = d.user;
  const isSelf = u.id === api.session?.userId;
  const canAct = !isSelf && (!u.role || api.isSuperAdmin);
  const note = isSelf ? "This is your account. Admins can only change their own Pro here." : !canAct ? "Only a SuperAdmin can change another admin's account." : null;
  const weeks = d.stats.workoutsByWeek;

  async function copy(text: string, what: string) {
    try {
      await navigator.clipboard.writeText(text);
      toast(`${what} copied`);
    } catch {
      window.prompt(`Copy the ${what.toLowerCase()}:`, text);
    }
  }

  async function changeRole() {
    const choice = window.prompt(`Role for ${u.email}: type Admin, SuperAdmin, or none for a regular user.`, u.role ?? "none");
    if (choice == null) return;
    const role = [AdminRole, SuperAdminRole].find((r) => r.toLowerCase() === choice.trim().toLowerCase()) ?? null;
    if (role == null && choice.trim().toLowerCase() !== "none") return window.alert("That's not a role: Admin, SuperAdmin or none.");
    if (role === (u.role ?? null)) return;
    const what = !role
      ? "a regular user, with no admin access"
      : role === SuperAdminRole
        ? "a SuperAdmin, who can also change roles and delete accounts"
        : "an Admin, who can see every user and disable accounts";
    await act(() => api.setRole(u.id, role), { confirm: `${u.email} becomes ${what}. It takes effect within a few minutes.` });
  }

  async function remove() {
    const typed = window.prompt(
      `This permanently deletes ${u.email} and all of its workouts, plans and settings. It can't be undone.\n\nType the email to confirm.`,
    );
    if (typed == null) return;
    if (typed.trim().toLowerCase() !== u.email.toLowerCase()) return window.alert("Not deleted: the email didn't match.");
    if (await act(() => api.deleteUser(u.id))) go("#/users");
  }

  const disabled = u.status === "Disabled";

  return (
    <>
      <div className={s.head}>
        <div style={{ display: "flex", gap: 14, alignItems: "center", minWidth: 0 }}>
          <span className={s.avatarLarge}>{f.initials(u.name, u.email)}</span>
          <div style={{ minWidth: 0 }}>
            <h1 className={s.title} style={{ overflowWrap: "anywhere" }}>
              {u.name ? `@${u.name}` : u.email}
            </h1>
            <p className={s.caption}>
              Joined {f.date(u.createdAt)} · active {f.ago(u.lastActiveAt)}
            </p>
          </div>
        </div>
        <div className={s.chips}>
          <a className={s.buttonQuiet} href={`mailto:${u.email}`}>
            Email
          </a>
          <button className={s.buttonQuiet} onClick={() => copy(u.email, "Email")}>
            Copy email
          </button>
          <button className={s.buttonQuiet} onClick={reload}>
            Refresh
          </button>
        </div>
      </div>
      <Badges user={u} />

      <h2 className={s.section}>Actions</h2>
      {note && (
        <div className={s.card}>
          <p className={s.caption}>{note}</p>
        </div>
      )}
      <div className={s.cardFlush}>
        <ProActions d={d} reload={reload} />
        {canAct && (
          <>
            <ActionRow
              title={disabled ? "Enable account" : "Disable account"}
              detail={disabled ? "Lets them sign in again" : "Signs them out and keeps them out"}
              danger={!disabled}
              disabled={working}
              onClick={() =>
                act(() => api.setDisabled(u.id, !disabled), {
                  confirm: disabled ? undefined : `${u.email} is signed out everywhere and can't sign in until it's enabled again.`,
                })
              }
            />
            <ActionRow
              title="Sign out everywhere"
              detail={`Ends all ${f.number(d.activeSessions)} sessions`}
              disabled={working}
              onClick={() =>
                act(() => api.signOutEverywhere(u.id), {
                  confirm: `Every device signed in as ${u.email} is signed out within a few minutes.`,
                  done: "Signed out everywhere",
                })
              }
            />
            <ActionRow
              title={u.emailVerified ? "Mark email unverified" : "Mark email verified"}
              detail={u.emailVerified ? "They'll have to enter a code again" : "Skips the emailed code"}
              disabled={working}
              onClick={() =>
                act(() => api.setEmailVerified(u.id, !u.emailVerified), {
                  confirm: u.emailVerified
                    ? `${u.email} has to verify the email again before syncing or using AI plans.`
                    : `${u.email} can sync and use AI plans without entering the emailed code.`,
                })
              }
            />
            <ActionRow
              title="Reset AI quota"
              detail="Gives back the AI plans and chats used"
              disabled={working}
              onClick={() =>
                act(() => api.resetAiQuota(u.id), { confirm: `${u.email} gets back the AI plans and chat messages used in the current windows.` })
              }
            />
            {d.hasPassword && (
              <ActionRow
                title="Send password reset"
                detail="Emails a code to choose a new password"
                disabled={working}
                onClick={() =>
                  act(() => api.sendPasswordReset(u.id), {
                    confirm: `${u.email} gets an email with a code to choose a new password in the app.`,
                    done: "Email sent",
                  })
                }
              />
            )}
            {api.isSuperAdmin && (
              <>
                <ActionRow title="Change role" detail={u.role ?? "Regular user"} disabled={working} onClick={changeRole} />
                <ActionRow title="Delete account" detail="Removes it and all its data for good" danger disabled={working} onClick={remove} />
              </>
            )}
          </>
        )}
      </div>

      <h2 className={s.section}>Account</h2>
      <div className={s.card}>
        <Info
          rows={[
            ["Joined", f.dateTime(u.createdAt)],
            ["Last active", f.ago(u.lastActiveAt)],
            ["Status", u.status],
            ["Email", `${u.email} (${u.emailVerified ? "verified" : "not verified"})`],
            [
              "Signs in with",
              [d.hasPassword && "password", u.hasGoogle && "Google"].filter(Boolean).join(" and ") || "nothing (no password or Google)",
            ],
            ["Signed-in devices", f.number(d.activeSessions)],
            ["Role", u.role ?? "Regular user"],
            [
              "User ID",
              <button key="id" className={s.chip} onClick={() => copy(u.id, "User ID")} style={{ fontSize: 12 }}>
                {u.id}
              </button>,
            ],
          ]}
        />
      </div>

      <h2 className={s.section}>AI and Pro</h2>
      <div className={s.card}>
        <Info
          rows={[
            ["Gym Book Pro", proText(d.pro)],
            ["AI plans", `${f.quota(d.ai.plan.used, d.ai.plan.limit, d.ai.plan.period)} · ${d.ai.plans30d} in 30 days`],
            ["Plan chat", `${f.quota(d.ai.chat.used, d.ai.chat.limit, d.ai.chat.period)} · ${d.ai.chats30d} in 30 days`],
          ]}
        />
      </div>

      <h2 className={s.section}>Training</h2>
      <Tiles
        tiles={[
          { label: "Workouts", value: f.number(d.stats.workouts), note: `${d.stats.workouts30d} in 30 days` },
          { label: "Working sets", value: f.number(d.stats.workingSets) },
          { label: "Volume", value: f.volume(d.stats.volumeKg) },
          { label: "Time trained", value: f.hours(d.stats.totalHours) },
          { label: "First workout", value: f.wallDate(d.stats.firstWorkoutAt) },
          { label: "Last workout", value: f.wallDate(d.stats.lastWorkoutAt) },
          { label: "Custom exercises", value: f.number(d.stats.customExercises) },
          {
            label: "Body weights",
            value: f.number(d.stats.bodyWeightEntries),
            note: d.stats.latestBodyWeightKg != null ? `latest ${f.weight(d.stats.latestBodyWeightKg)}` : undefined,
          },
        ]}
      />
      {weeks.length > 0 && (
        <div className={s.card}>
          <h3>Workouts per week</h3>
          <p className={s.small}>
            Weeks of {f.day(weeks[0].weekStart)} – {f.day(weeks[weeks.length - 1].weekStart)}
          </p>
          <Bars values={weeks.map((w) => w.count)} labels={weeks.map((w) => f.day(w.weekStart))} />
        </div>
      )}

      <h2 className={s.section}>Profile</h2>
      <div className={s.card}>
        {d.profile ? (
          <Info
            rows={[
              ["Goal", f.words(d.profile.goal)],
              ["Experience", f.words(d.profile.experience)],
              ["Schedule", `${d.profile.daysPerWeek} days a week, ${d.profile.sessionMinutes} min`],
              ["Equipment", f.words(d.profile.equipmentAccess)],
              ["Body weight", f.weight(d.profile.bodyWeightKg) + (d.profile.bodyFatPercent != null ? `, ${d.profile.bodyFatPercent.toFixed(1)}% fat` : "")],
              ["Born", d.profile.birthYear?.toString() ?? "—"],
              ["Training since", f.wallDate(d.profile.trainingSince)],
              ["Units", d.profile.unit],
              ["Onboarding", d.profile.onboardingDone ? "Done" : "Not finished"],
              ["Updated", f.dateTime(d.profile.updatedAt)],
            ]}
          />
        ) : (
          <p className={s.caption}>No profile synced yet.</p>
        )}
      </div>

      <h2 className={s.section}>Plans</h2>
      <div className={s.cardFlush}>
        {d.plans.length === 0 && <p className={`${s.caption} ${s.row}`}>No plans.</p>}
        {d.plans.map((p) => (
          <div key={p.id} className={s.row}>
            <span className={s.rowMain}>
              <span className={s.rowTitle}>{p.isActive ? `${p.name} (active)` : p.name}</span>
              <span className={s.small}>
                {f.words(p.goal)} · {p.daysPerWeek}×/week · {p.days} days, {p.exercises} exercises · made {f.wallDate(p.createdAt)}
              </span>
            </span>
          </div>
        ))}
      </div>

      <h2 className={s.section}>Recent workouts</h2>
      <div className={s.cardFlush}>
        {d.recentWorkouts.length === 0 && <p className={`${s.caption} ${s.row}`}>No workouts.</p>}
        {d.recentWorkouts.map((w) => (
          <div key={w.id} className={s.row}>
            <span className={s.rowMain}>
              <span className={s.rowTitle}>
                {w.name}
                {w.planWeek != null ? ` (week ${w.planWeek})` : ""}
              </span>
              <span className={s.small}>
                {f.wallDate(w.startedAt)} · {durationLabel(w.startedAt, w.endedAt)}
                {w.exercises} exercises, {w.workingSets} sets, {f.volume(w.volumeKg)}
              </span>
            </span>
          </div>
        ))}
      </div>
    </>
  );
}

/** Give Pro (for good, a year, a month, or until a date) or take it back. Allowed on your own account too. */
function ProActions({ d, reload }: { d: UserDetail; reload: () => void }) {
  const { api } = useAdmin();
  const { act, working } = useAct(reload);
  const [open, setOpen] = useState(false);
  const [until, setUntil] = useState("");
  const u = d.user;
  const isSelf = u.id === api.session?.userId;
  if (!isSelf && u.role && !api.isSuperAdmin) return null;

  const grant = async (value: string) => {
    if (await act(() => api.setProGrant(u.id, value), { done: "Gym Book Pro given" })) setOpen(false);
  };
  const inMonths = (months: number) => {
    const date = new Date();
    date.setMonth(date.getMonth() + months);
    return date.toISOString();
  };

  if (d.pro.grantedUntil)
    return (
      <ActionRow
        title="Take back Gym Book Pro"
        detail={`Given ${proUntil(d.pro.grantedUntil)}`}
        danger
        disabled={working}
        onClick={() =>
          act(() => api.setProGrant(u.id, null), {
            confirm: `${u.email} loses the Pro you gave them. A subscription they bought in a store keeps running.`,
            done: "Pro taken back",
          })
        }
      />
    );

  return (
    <>
      <ActionRow title="Give Gym Book Pro" detail="Free, for good or until a date" disabled={working} onClick={() => setOpen(!open)} />
      {open && (
        <div className={s.grantPanel}>
          <button className={s.button} disabled={working} onClick={() => grant(ProForever)}>
            For good
          </button>
          <button className={s.buttonQuiet} disabled={working} onClick={() => grant(inMonths(12))}>
            A year
          </button>
          <button className={s.buttonQuiet} disabled={working} onClick={() => grant(inMonths(1))}>
            A month
          </button>
          <input className={s.input} type="date" value={until} min={new Date().toISOString().slice(0, 10)} onChange={(e) => setUntil(e.target.value)} />
          <button className={s.buttonQuiet} disabled={working || !until} onClick={() => grant(new Date(until + "T23:59:59").toISOString())}>
            Until then
          </button>
        </div>
      )}
    </>
  );
}

function ActionRow({ title, detail, danger, disabled, onClick }: { title: string; detail: string; danger?: boolean; disabled?: boolean; onClick: () => void }) {
  return (
    <button className={s.row} onClick={onClick} disabled={disabled}>
      <span className={s.rowMain}>
        <span className={`${s.rowTitle} ${danger ? s.rowDanger : ""}`}>{title}</span>
        <span className={s.small}>{detail}</span>
      </span>
      <span className={s.small}>›</span>
    </button>
  );
}
