"use client";

import { useCallback, useEffect, useState } from "react";
import { initials } from "@/lib/format";
import styles from "./ui.module.css";

export type Tone = "blue" | "purple" | "green" | "orange" | "yellow" | "red";

/** A number with its icon in a soft circle, like the stats on the app's Home tab. */
export function StatTile({
  icon,
  tone,
  value,
  label,
  hint,
}: {
  icon: React.ReactNode;
  tone: Tone;
  value: React.ReactNode;
  label: string;
  hint?: React.ReactNode;
}) {
  return (
    <div className={`card ${styles.stat}`}>
      <span className={`${styles.statIcon} ${styles[tone]}`}>{icon}</span>
      <div className={styles.statText}>
        <strong>{value}</strong>
        <span>{label}</span>
        {hint && <small>{hint}</small>}
      </div>
    </div>
  );
}

/** Bars over time; the last bar (today, this week) is highlighted like the website's volume chart. */
export function BarChart({
  data,
  height = 140,
  tone = "blue",
  label,
}: {
  data: { key: string; value: number; label: string; stack?: number }[];
  height?: number;
  tone?: "blue" | "purple" | "green";
  label?: (value: number) => string;
}) {
  const max = Math.max(1, ...data.map((d) => d.value + (d.stack ?? 0)));
  return (
    <div className={styles.chart} style={{ height }}>
      {data.map((d, i) => {
        const total = d.value + (d.stack ?? 0);
        const title = `${d.label}: ${label ? label(d.value) : d.value}${d.stack != null ? ` + ${d.stack}` : ""}`;
        return (
          <div key={d.key} className={styles.barSlot} title={title}>
            <div className={styles.barStack} style={{ height: `${(total / max) * 100}%` }}>
              {d.stack != null && d.stack > 0 && (
                <i className={styles.barStackTop} style={{ height: `${(d.stack / total) * 100}%` }} />
              )}
              <i
                className={`${styles.bar} ${styles[`bar_${tone}`]} ${i === data.length - 1 ? styles.barNow : ""}`}
                style={{ flex: 1 }}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
}

/** Initials on the app's accent gradient. */
export function Avatar({ name, size = 36 }: { name: string; size?: number }) {
  return (
    <span className={styles.avatar} style={{ width: size, height: size, fontSize: size * 0.38 }}>
      {initials(name)}
    </span>
  );
}

/** A quota or recovery-style meter: green, then yellow past 70%, red when full. */
export function Meter({ value, max }: { value: number; max: number }) {
  const pct = max > 0 ? Math.min(100, (value / max) * 100) : 0;
  const tone = pct >= 100 ? styles.meterRed : pct >= 70 ? styles.meterYellow : styles.meterGreen;
  return (
    <div className={styles.meter}>
      <i className={tone} style={{ width: `${pct}%` }} />
    </div>
  );
}

export function Skeleton({ height = 20, width = "100%" }: { height?: number; width?: number | string }) {
  return <div className="skeleton" style={{ height, width }} />;
}

/**
 * The app's bottom sheet for confirmations (DialogSheet): title, what will happen, and the action. Destructive
 * actions are red, like the app's.
 */
export function ConfirmSheet({
  open,
  title,
  message,
  confirmLabel,
  danger,
  busy,
  disabled,
  onConfirm,
  onCancel,
  children,
}: {
  open: boolean;
  title: string;
  message: React.ReactNode;
  confirmLabel: string;
  danger?: boolean;
  busy?: boolean;
  /** Keeps the button off without the "Working" label, e.g. until a typed confirmation matches. */
  disabled?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  children?: React.ReactNode;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onCancel();
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onCancel]);

  if (!open) return null;
  return (
    <div className={styles.backdrop} onClick={onCancel}>
      <div className={styles.sheet} role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <span className={styles.grabber} />
        <h3>{title}</h3>
        <div className={styles.sheetMessage}>{message}</div>
        {children}
        <div className={styles.sheetActions}>
          <button className={`btn ${danger ? "btn-danger" : "btn-primary"} btn-block`} onClick={onConfirm} disabled={busy || disabled}>
            {busy ? "Working…" : confirmLabel}
          </button>
          <button className="btn btn-block" onClick={onCancel} disabled={busy}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}

/** A short message at the bottom, like the app's toasts. */
export function useToast() {
  const [toast, setToast] = useState<{ text: string; error?: boolean } | null>(null);

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 3200);
    return () => clearTimeout(t);
  }, [toast]);

  const show = useCallback((text: string, error = false) => setToast({ text, error }), []);
  const view = toast ? <div className={`toast ${toast.error ? "toast-error" : ""}`}>{toast.text}</div> : null;
  return { show, view };
}
