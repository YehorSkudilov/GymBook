// Display helpers. Weights arrive in kg; the admin picks kg or lbs in the top bar, like the app's Weight unit setting.

export type Unit = "kg" | "lbs";

const LBS_PER_KG = 2.2046226218;

export function weight(kg: number, unit: Unit) {
  const value = unit === "lbs" ? kg * LBS_PER_KG : kg;
  return `${Math.round(value).toLocaleString()} ${unit}`;
}

/** Volume like the app's stats: 21.4k lbs. */
export function volume(kg: number, unit: Unit) {
  const value = unit === "lbs" ? kg * LBS_PER_KG : kg;
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M ${unit}`;
  if (value >= 10_000) return `${(value / 1000).toFixed(1)}k ${unit}`;
  return `${Math.round(value).toLocaleString()} ${unit}`;
}

export function number(n: number) {
  return n.toLocaleString();
}

export function date(iso?: string | null) {
  if (!iso) return "-";
  return new Date(iso).toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
}

export function dateTime(iso?: string | null) {
  if (!iso) return "-";
  return new Date(iso).toLocaleString(undefined, { day: "numeric", month: "short", hour: "numeric", minute: "2-digit" });
}

/** "just now", "5 min ago", "3 days ago", or the date once it's old. */
export function ago(iso?: string | null) {
  if (!iso) return "Never";
  const seconds = (Date.now() - new Date(iso).getTime()) / 1000;
  if (seconds < 60) return "Just now";
  if (seconds < 3600) return `${Math.floor(seconds / 60)} min ago`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)} h ago`;
  if (seconds < 86400 * 30) return `${Math.floor(seconds / 86400)} days ago`;
  return date(iso);
}

export function minutesBetween(start: string, end?: string) {
  if (!end) return "-";
  const minutes = Math.round((new Date(end).getTime() - new Date(start).getTime()) / 60000);
  return minutes >= 60 ? `${Math.floor(minutes / 60)} h ${minutes % 60} min` : `${minutes} min`;
}

/** Enum names as the app shows them: BuildMuscle -> Build muscle. */
export function words(value: string) {
  const spaced = value.replace(/([a-z])([A-Z])/g, "$1 $2").toLowerCase();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}

export function initials(nameOrEmail: string) {
  const parts = nameOrEmail.split(/[\s@._-]+/).filter(Boolean);
  return ((parts[0]?.[0] ?? "?") + (parts[1]?.[0] ?? "")).toUpperCase();
}
