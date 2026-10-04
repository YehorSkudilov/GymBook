"use client";

import { useState } from "react";
import * as f from "@/lib/format";
import { Bars, Chips, Head, Status, Tiles, useAct, useAdmin, useLoad } from "./ui";
import s from "./admin.module.css";

/** Whether the AI features need Gym Book Pro, then AI plans and chat messages (each a paid OpenAI call) and the heaviest users. */
export function AiUsage() {
  const { api } = useAdmin();
  const [days, setDays] = useState(30);
  const { data: a, error, busy, reload } = useLoad(() => api.aiUsage(days), String(days));

  return (
    <div className={s.stack}>
      <Head title="AI & Pro" caption="AI plans and plan chat messages, each a paid OpenAI call." onRefresh={reload} />
      <ProSetting />
      <Chips
        options={[
          ["7 days", 7],
          ["30 days", 30],
          ["90 days", 90],
        ]}
        value={days}
        onChange={setDays}
      />
      <Status busy={busy && !a} error={error} />
      {a && (
        <>
          <Tiles
            tiles={[
              { label: "AI plans", value: f.number(a.plans), note: `in ${a.days} days` },
              { label: "Chat messages", value: f.number(a.chats), note: `in ${a.days} days` },
              { label: "Users", value: f.number(a.users), note: "used AI" },
              { label: "Per user", value: (a.users ? (a.plans + a.chats) / a.users : 0).toFixed(1), note: "calls on average" },
            ]}
          />
          <div className={s.card}>
            <h3>Per day</h3>
            <Bars values={a.byDay.map((x) => x.plans)} stacked={a.byDay.map((x) => x.chats)} labels={a.byDay.map((x) => f.day(x.date))} />
            <div className={s.legend} style={{ marginTop: 8 }}>
              <span>
                <i className={s.dot} style={{ background: "var(--accent)" }} />
                AI plans
              </span>
              <span>
                <i className={s.dot} style={{ background: "var(--accent-2)" }} />
                Chat messages
              </span>
              {a.byDay.length > 0 && (
                <span>
                  {f.day(a.byDay[0].date)} – {f.day(a.byDay[a.byDay.length - 1].date)}
                </span>
              )}
            </div>
          </div>

          <h2 className={s.section}>Quotas per user</h2>
          <Tiles
            tiles={[
              { label: "Plan quota", value: f.number(a.planQuota.limit), note: f.period(a.planQuota.period) },
              { label: "Chat quota", value: f.number(a.chatQuota.limit), note: f.period(a.chatQuota.period) },
            ]}
          />

          <h2 className={s.section}>Top users</h2>
          <div className={s.cardFlush}>
            {a.topUsers.length === 0 && <p className={`${s.caption} ${s.row}`}>Nobody used AI in this period.</p>}
            {a.topUsers.map((u, i) => (
              <a key={u.id} className={s.row} href={`#/user/${encodeURIComponent(u.id)}`}>
                <span className={s.avatar}>{i + 1}</span>
                <span className={s.rowMain}>
                  <span className={s.rowTitle}>{u.email}</span>
                </span>
                <span className={s.caption}>
                  {u.plans} plans · {u.chats} chats
                </span>
              </a>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

/** The switch for everyone: AI needs Pro, or is free. Every admin sees it; only a SuperAdmin changes it. */
function ProSetting() {
  const { api } = useAdmin();
  const { data, error, reload } = useLoad(() => api.settings(), "settings");
  const { act, working } = useAct(reload);
  if (error) return <p className={s.error}>{error}</p>;
  if (!data) return null;
  const required = data.proRequired;

  return (
    <div className={s.card}>
      <div className={s.head}>
        <div>
          <h3>Require Gym Book Pro for AI</h3>
          <p className={s.caption}>
            {required
              ? "On: the AI features need Gym Book Pro (bought, or given by an admin)."
              : "Off: the AI features are free for everyone, within the quotas."}
          </p>
        </div>
        <span className={required ? s.badgeSuccess : s.badgeWarning}>{required ? "On" : "Off"}</span>
      </div>
      {api.isSuperAdmin ? (
        <button
          className={required ? s.buttonQuiet : s.button}
          style={{ marginTop: 12 }}
          disabled={working}
          onClick={() =>
            act(() => api.setSettings({ proRequired: !required }), {
              confirm: required
                ? "Make AI free for everyone? Every signed-in user can use the AI features without Gym Book Pro, within the quotas. Each use is a paid OpenAI call."
                : "Require Gym Book Pro? Users without Pro (bought, or given by an admin) can't use the AI features any more.",
              done: required ? "AI is free for everyone" : "AI needs Pro again",
            })
          }
        >
          {required ? "Make AI free for everyone" : "Require Pro"}
        </button>
      ) : (
        <p className={s.small} style={{ marginTop: 8 }}>
          Only a SuperAdmin can change this.
        </p>
      )}
    </div>
  );
}
