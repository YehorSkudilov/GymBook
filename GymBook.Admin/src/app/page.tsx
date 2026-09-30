"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { BoltIcon, DumbbellIcon, SparkleIcon, UsersIcon } from "@/components/Icons";
import { useShell } from "@/components/Shell";
import { UserStatusBadges } from "@/components/UserBadges";
import { Avatar, BarChart, Skeleton, StatTile } from "@/components/ui";
import { useAdmin } from "@/lib/client";
import { ago, number } from "@/lib/format";
import type { Dashboard } from "@/lib/types";

function greeting() {
  const h = new Date().getHours();
  return h < 5 ? "Up late" : h < 12 ? "Good morning" : h < 18 ? "Good afternoon" : "Good evening";
}

const shortDay = (iso: string) => new Date(iso + "T00:00:00").toLocaleDateString(undefined, { day: "numeric", month: "short" });

// Home: who's using Gym Book, how much they train, and how much AI they use - laid out like the app's Home tab.
export default function DashboardPage() {
  const { me } = useShell();
  const router = useRouter();
  const { data, error, loading } = useAdmin<Dashboard>("dashboard");

  const today = new Date().toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long" });

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <p className="subtle">{today}</p>
          <h1>
            {greeting()}
            {me ? `, ${me.email.split("@")[0]}` : ""}
          </h1>
        </div>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <div className="stats-row">
        {loading || !data ? (
          [0, 1, 2, 3].map((i) => (
            <div key={i} className="card">
              <Skeleton height={46} />
            </div>
          ))
        ) : (
          <>
            <StatTile
              icon={<UsersIcon />}
              tone="blue"
              value={number(data.totalUsers)}
              label="Users"
              hint={`+${data.newUsers7d} this week · ${data.verifiedUsers} verified`}
            />
            <StatTile
              icon={<BoltIcon />}
              tone="green"
              value={number(data.activeUsers7d)}
              label="Active this week"
              hint={data.totalUsers ? `${Math.round((data.activeUsers7d / data.totalUsers) * 100)}% of users` : undefined}
            />
            <StatTile
              icon={<DumbbellIcon />}
              tone="orange"
              value={number(data.workouts7d)}
              label="Workouts this week"
              hint={`${number(data.workoutsTotal)} all time · ${number(data.plansTotal)} plans`}
            />
            <StatTile
              icon={<SparkleIcon />}
              tone="purple"
              value={number(data.aiPlans7d + data.aiChats7d)}
              label="AI requests this week"
              hint={`${data.aiPlans7d} plans · ${data.aiChats7d} chat messages`}
            />
          </>
        )}
      </div>

      <div className="grid-2">
        <section className="card">
          <div className="card-head">
            <h2>Workouts</h2>
            <span className="subtle">Last 30 days</span>
          </div>
          {data ? (
            <BarChart
              tone="green"
              data={data.workoutsByDay.map((d) => ({ key: d.date, value: d.count, label: shortDay(d.date) }))}
              label={(v) => `${v} workouts`}
            />
          ) : (
            <Skeleton height={140} />
          )}
        </section>
        <section className="card">
          <div className="card-head">
            <h2>Sign-ups</h2>
            <span className="subtle">{data ? `${data.newUsers30d} in 30 days` : "Last 30 days"}</span>
          </div>
          {data ? (
            <BarChart
              data={data.signupsByDay.map((d) => ({ key: d.date, value: d.count, label: shortDay(d.date) }))}
              label={(v) => `${v} sign-ups`}
            />
          ) : (
            <Skeleton height={140} />
          )}
        </section>
      </div>

      <div className="grid-3">
        <section className="card">
          <div className="card-head">
            <h2>Newest users</h2>
            <Link href="/users" className="btn btn-small">
              All users
            </Link>
          </div>
          <div className="table-wrap">
            <table>
              <tbody>
                {data?.recentUsers.map((u) => (
                  <tr key={u.id} className="clickable" onClick={() => router.push(`/users/${u.id}`)}>
                    <td>
                      <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                        <Avatar name={u.name || u.email} />
                        <div style={{ display: "grid" }}>
                          <strong>{u.name || u.email.split("@")[0]}</strong>
                          <span className="faint" style={{ fontSize: 13 }}>
                            {u.email}
                          </span>
                        </div>
                      </div>
                    </td>
                    <td>
                      <UserStatusBadges user={u} />
                    </td>
                    <td className="num subtle">{ago(u.createdAt)}</td>
                  </tr>
                ))}
                {data && data.recentUsers.length === 0 && (
                  <tr>
                    <td className="empty">No users yet.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          {!data && <Skeleton height={200} />}
        </section>

        <section className="card">
          <div className="card-head">
            <h2>Most active</h2>
            <span className="subtle">Workouts, 7 days</span>
          </div>
          <div style={{ display: "grid", gap: 10 }}>
            {data?.mostActive7d.map((u, i) => (
              <Link
                key={u.id}
                href={`/users/${u.id}`}
                style={{ display: "flex", alignItems: "center", gap: 12, padding: "6px 0" }}
              >
                <span className="faint" style={{ width: 16, fontWeight: 600 }}>
                  {i + 1}
                </span>
                <Avatar name={u.email} size={32} />
                <span style={{ flex: 1, minWidth: 0, overflow: "hidden", textOverflow: "ellipsis" }}>{u.email}</span>
                <span className="badge badge-green">{u.count}</span>
              </Link>
            ))}
            {data && data.mostActive7d.length === 0 && <p className="empty">No workouts this week.</p>}
            {!data && <Skeleton height={160} />}
          </div>
          {data && (data.disabledUsers > 0 || data.admins > 0) && (
            <p className="faint" style={{ marginTop: 16, fontSize: 13 }}>
              {data.admins} admin{data.admins === 1 ? "" : "s"} · {data.disabledUsers} disabled account
              {data.disabledUsers === 1 ? "" : "s"}
            </p>
          )}
        </section>
      </div>
    </div>
  );
}
