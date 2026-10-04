"use client";

import { useState } from "react";
import type { RankReportRow } from "@/lib/api";
import * as f from "@/lib/format";
import { Chips, Head, Status, useAct, useAdmin, useLoad } from "./ui";
import s from "./admin.module.css";

/** Reports about ranked lifts and public profiles: open ones, or all of them. */
export function Reports() {
  const { api } = useAdmin();
  const [all, setAll] = useState(false);
  const { data, error, busy, reload } = useLoad(() => api.rankReports(all), String(all));

  return (
    <div className={s.stack}>
      <Head title="Reports" caption="Ranked lifts and public profiles people reported." onRefresh={reload} />
      <Chips
        options={[
          ["Open", false],
          ["All", true],
        ]}
        value={all}
        onChange={setAll}
      />
      <Status busy={busy && !data} error={error} />
      {data &&
        (data.length === 0 ? (
          <p className={s.caption}>{all ? "No reports yet." : "No open reports."}</p>
        ) : (
          <div className={s.stack}>
            {data.map((r) => (
              <Report key={r.id} r={r} reload={reload} />
            ))}
          </div>
        ))}
    </div>
  );
}

function Report({ r, reload }: { r: RankReportRow; reload: () => void }) {
  const { api } = useAdmin();
  const { act, working } = useAct(reload);
  const name = `@${r.targetUsername}`;

  return (
    <div className={s.card}>
      <div className={s.rowTitle}>
        {name} · {r.lift ?? "profile"}
        {r.targetHidden && <span className={s.badgeWarning} style={{ marginLeft: 8 }}>Hidden</span>}
      </div>
      <p className={s.small}>
        {r.targetEmail} · reported by @{r.reporter} · {f.ago(r.createdAt)}
        {r.openAboutTarget > 1 ? ` · ${r.openAboutTarget} open reports` : ""}
      </p>
      <p style={{ margin: "10px 0 14px" }}>{r.reason ? `“${r.reason}”` : <span className={s.caption}>No reason given</span>}</p>
      <div className={s.chips}>
        <button
          className={s.buttonQuiet}
          disabled={working}
          onClick={() =>
            act(() => api.setRankHidden(r.targetUserId, !r.targetHidden), {
              confirm: r.targetHidden
                ? `${name} is back on the boards.`
                : `${name} disappears from every board. They still see their own ranks.`,
            })
          }
        >
          {r.targetHidden ? "Show on leaderboards" : "Hide from leaderboards"}
        </button>
        <button
          className={s.buttonQuiet}
          disabled={working}
          onClick={() =>
            act(() => api.clearPublicProfile(r.targetUserId), {
              confirm: `${name}'s bio and picture are removed. Their username stays.`,
              done: "Profile cleared",
            })
          }
        >
          Clear public profile
        </button>
        <button className={s.buttonQuiet} disabled={working} onClick={() => act(() => api.resolveReport(r.id), { done: "Resolved" })}>
          Resolve
        </button>
        <a className={s.buttonQuiet} href={`#/user/${encodeURIComponent(r.targetUserId)}`}>
          Open account
        </a>
      </div>
    </div>
  );
}
