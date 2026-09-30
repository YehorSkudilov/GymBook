"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { SearchIcon } from "@/components/Icons";
import { UserStatusBadges } from "@/components/UserBadges";
import { Avatar, Skeleton } from "@/components/ui";
import { useAdmin } from "@/lib/client";
import { ago, date, number } from "@/lib/format";
import type { UserRow } from "@/lib/types";
import styles from "./users.module.css";

const filters = [
  { id: "", label: "All" },
  { id: "admins", label: "Admins" },
  { id: "unverified", label: "Unverified" },
  { id: "disabled", label: "Disabled" },
];

// Every account, searchable by email or profile name, with the app's filter chips.
export default function UsersPage() {
  const router = useRouter();
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState("");

  // Search once typing pauses.
  useEffect(() => {
    const t = setTimeout(() => setQuery(search.trim()), 300);
    return () => clearTimeout(t);
  }, [search]);

  const params = new URLSearchParams();
  if (query) params.set("q", query);
  if (filter) params.set("filter", filter);
  const { data, error, loading } = useAdmin<UserRow[]>(`users${params.size ? `?${params}` : ""}`);

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h1>Users</h1>
          <p className="subtle">{data ? `${number(data.length)}${data.length >= 300 ? "+" : ""} accounts` : " "}</p>
        </div>
      </div>

      <div className={styles.toolbar}>
        <div className={styles.search}>
          <SearchIcon size={18} />
          <input
            className="input"
            placeholder="Search by email or name"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="chips">
          {filters.map((f) => (
            <button key={f.id} className={`chip ${filter === f.id ? "chip-active" : ""}`} onClick={() => setFilter(f.id)}>
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <section className="card">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>User</th>
                <th>Status</th>
                <th className="num">Workouts</th>
                <th className="num">Plans</th>
                <th>Last active</th>
                <th>Joined</th>
              </tr>
            </thead>
            <tbody>
              {data?.map((u) => (
                <tr key={u.id} className="clickable" onClick={() => router.push(`/users/${u.id}`)}>
                  <td>
                    <div className={styles.who}>
                      <Avatar name={u.name || u.email} />
                      <div>
                        <strong>{u.name || u.email.split("@")[0]}</strong>
                        <span>{u.email}</span>
                      </div>
                    </div>
                  </td>
                  <td>
                    <UserStatusBadges user={u} />
                  </td>
                  <td className="num">{number(u.workouts)}</td>
                  <td className="num">{number(u.plans)}</td>
                  <td className="subtle">{ago(u.lastActiveAt)}</td>
                  <td className="subtle">{date(u.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {loading && !data && (
            <div style={{ display: "grid", gap: 10, padding: 20 }}>
              {[0, 1, 2, 3, 4].map((i) => (
                <Skeleton key={i} height={40} />
              ))}
            </div>
          )}
          {data?.length === 0 && <p className="empty">No users match.</p>}
        </div>
      </section>
    </div>
  );
}
