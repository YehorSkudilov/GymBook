"use client";

import * as f from "@/lib/admin/format";
import { UserList } from "./Users";
import { Bars, Head, Status, Tiles, useAdmin, useLoad } from "./ui";
import s from "./admin.module.css";

/** The numbers at a glance; each user tile opens the matching users. */
export function Dashboard() {
  const { api, go } = useAdmin();
  const { data: d, error, busy, reload } = useLoad(() => api.dashboard(), "dashboard");
  const users = (filter: string | null) => () => go(filter ? `#/users?filter=${filter}` : "#/users");

  return (
    <div className={s.stack}>
      <Head title="Dashboard" caption="Everyone using Gym Book, at a glance." onRefresh={reload} />
      <Status busy={busy && !d} error={error} />
      {d && (
        <>
          <Tiles
            tiles={[
              { label: "Users", value: f.number(d.totalUsers), note: `${f.number(d.verifiedUsers)} verified`, onClick: users(null) },
              { label: "New", value: f.number(d.newUsers7d), note: `7 days · ${f.number(d.newUsers30d)} in 30`, onClick: users("new") },
              { label: "Active", value: f.number(d.activeUsers7d), note: "used the app in 7 days", onClick: users("active") },
              { label: "Unverified", value: f.number(d.totalUsers - d.verifiedUsers), note: "email not confirmed", onClick: users("unverified") },
              { label: "Disabled", value: f.number(d.disabledUsers), note: "can't sign in", onClick: users("disabled") },
              { label: "Admins", value: f.number(d.admins), note: "Admin or SuperAdmin", onClick: users("admins") },
              { label: "Workouts", value: f.number(d.workouts7d), note: `7 days · ${f.number(d.workoutsTotal)} total` },
              { label: "Plans", value: f.number(d.plansTotal), note: "total" },
              { label: "AI", value: f.number(d.aiPlans7d + d.aiChats7d), note: `7 days · ${d.aiPlans7d} plans, ${d.aiChats7d} chats`, onClick: () => go("#/ai") },
            ]}
          />

          <div className={s.card}>
            <h3>Sign-ups</h3>
            <p className={s.small}>Last {d.signupsByDay.length} days</p>
            <Bars values={d.signupsByDay.map((x) => x.count)} labels={d.signupsByDay.map((x) => f.day(x.date))} />
          </div>
          <div className={s.card}>
            <h3>Workouts</h3>
            <p className={s.small}>Last {d.workoutsByDay.length} days</p>
            <Bars values={d.workoutsByDay.map((x) => x.count)} labels={d.workoutsByDay.map((x) => f.day(x.date))} />
          </div>

          <h2 className={s.section}>Newest users</h2>
          <UserList users={d.recentUsers} />

          <h2 className={s.section}>Most active this week</h2>
          <div className={s.cardFlush}>
            {d.mostActive7d.length === 0 && <p className={`${s.caption} ${s.row}`}>Nobody worked out this week.</p>}
            {d.mostActive7d.map((u, i) => (
              <a key={u.id} className={s.row} href={`#/user/${encodeURIComponent(u.id)}`}>
                <span className={s.avatar}>{i + 1}</span>
                <span className={s.rowMain}>
                  <span className={s.rowTitle}>{u.email}</span>
                </span>
                <span className={s.caption}>{u.count} workouts</span>
              </a>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
