"use client";

import Link from "next/link";
import { useState } from "react";
import { SparkleIcon, UsersIcon } from "@/components/Icons";
import { Avatar, BarChart, Skeleton, StatTile } from "@/components/ui";
import { useAdmin } from "@/lib/client";
import { number } from "@/lib/format";
import type { AiUsage } from "@/lib/types";

const ranges = [7, 30, 90];

const shortDay = (iso: string) => new Date(iso + "T00:00:00").toLocaleDateString(undefined, { day: "numeric", month: "short" });

// AI plans and coach messages are the app's only paid calls (OpenAI): how many, who, and the limits in force.
export default function AiPage() {
  const [days, setDays] = useState(30);
  const { data, error } = useAdmin<AiUsage>(`ai-usage?days=${days}`);

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>AI usage</h1>
          <p className="subtle">AI plans and coach messages - each one is an OpenAI request.</p>
        </div>
        <div className="chips">
          {ranges.map((r) => (
            <button key={r} className={`chip ${days === r ? "chip-active" : ""}`} onClick={() => setDays(r)}>
              {r} days
            </button>
          ))}
        </div>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <div className="stats-row">
        {data ? (
          <>
            <StatTile icon={<SparkleIcon />} tone="purple" value={number(data.plans + data.chats)} label="Requests" />
            <StatTile
              icon={<SparkleIcon />}
              tone="blue"
              value={number(data.plans)}
              label="AI plans"
              hint={`Limit ${data.planQuota.limit} per ${data.planQuota.period}`}
            />
            <StatTile
              icon={<SparkleIcon />}
              tone="green"
              value={number(data.chats)}
              label="Coach messages"
              hint={`Limit ${data.chatQuota.limit} per ${data.chatQuota.period}`}
            />
            <StatTile icon={<UsersIcon />} tone="orange" value={number(data.users)} label="People using AI" />
          </>
        ) : (
          [0, 1, 2, 3].map((i) => (
            <div key={i} className="card">
              <Skeleton height={46} />
            </div>
          ))
        )}
      </div>

      <div className="grid-3">
        <section className="card">
          <div className="card-head">
            <h2>Per day</h2>
            <span className="subtle">
              <span style={{ color: "var(--accent)" }}>■</span> plans&nbsp;&nbsp;
              <span style={{ color: "var(--accent-2)" }}>■</span> coach messages
            </span>
          </div>
          {data ? (
            <BarChart
              height={200}
              data={data.byDay.map((d) => ({ key: d.date, value: d.plans, stack: d.chats, label: shortDay(d.date) }))}
              label={(v) => `${v} plans`}
            />
          ) : (
            <Skeleton height={200} />
          )}
        </section>

        <section className="card">
          <div className="card-head">
            <h2>Top users</h2>
          </div>
          <div style={{ display: "grid", gap: 10 }}>
            {data?.topUsers.map((u) => (
              <Link key={u.id} href={`/users/${u.id}`} style={{ display: "flex", alignItems: "center", gap: 12 }}>
                <Avatar name={u.email} size={32} />
                <span style={{ flex: 1, minWidth: 0, overflow: "hidden", textOverflow: "ellipsis" }}>{u.email}</span>
                <span className="badge badge-blue" title="AI plans">
                  {u.plans}
                </span>
                <span className="badge badge-purple" title="Coach messages">
                  {u.chats}
                </span>
              </Link>
            ))}
            {data?.topUsers.length === 0 && <p className="empty">No AI requests in this period.</p>}
            {!data && <Skeleton height={200} />}
          </div>
          <p className="faint" style={{ marginTop: 16, fontSize: 13 }}>
            Limits are set on the API (RateLimiting__PlanGenerations* and RateLimiting__PlanChat*). Reset one person&apos;s
            quota from their page.
          </p>
        </section>
      </div>
    </div>
  );
}
