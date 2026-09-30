import type { UserRow } from "@/lib/types";

/** Role, status and email state as pills. Regular, active, verified users show nothing but a quiet "Verified". */
export function UserStatusBadges({ user }: { user: UserRow }) {
  return (
    <span style={{ display: "inline-flex", flexWrap: "wrap", gap: 6 }}>
      {user.role && <span className={`badge ${user.role === "SuperAdmin" ? "badge-purple" : "badge-blue"}`}>{user.role}</span>}
      {user.status === "Disabled" && <span className="badge badge-red">Disabled</span>}
      {user.status === "Locked" && <span className="badge badge-yellow">Locked</span>}
      {user.emailVerified ? <span className="badge">Verified</span> : <span className="badge badge-yellow">Unverified</span>}
      {user.hasGoogle && <span className="badge">Google</span>}
    </span>
  );
}
