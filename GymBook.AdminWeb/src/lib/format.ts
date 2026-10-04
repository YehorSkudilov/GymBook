// How the web admin shows numbers and dates, like the admin app's Format (GymBook.Admin/Src/Services/Format.cs).

const locale = "en-GB";

export const number = (n: number) => n.toLocaleString(locale);

/** An instant (with a zone), shown in this browser's time zone. */
export const date = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString(locale, { day: "numeric", month: "short", year: "numeric" }) : "—";

export const dateTime = (value?: string | null) =>
  value
    ? new Date(value).toLocaleString(locale, { day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" })
    : "—";

/**
 * A workout time: the wall-clock time where it happened, stored without a zone, so shown as it is (not converted).
 */
export const wallDate = (value?: string | null) => (value ? date(value.slice(0, 10) + "T12:00:00Z") : "—");

/** A day (yyyy-MM-dd), e.g. "4 Oct". */
export const day = (value: string) =>
  new Date(value + "T12:00:00Z").toLocaleDateString(locale, { day: "numeric", month: "short", timeZone: "UTC" });

export function ago(value?: string | null) {
  if (!value) return "never";
  const minutes = (Date.now() - new Date(value).getTime()) / 60_000;
  if (minutes < 2) return "just now";
  if (minutes < 60) return `${Math.floor(minutes)} min ago`;
  const hours = minutes / 60;
  if (hours < 24) return `${Math.floor(hours)} h ago`;
  const days = hours / 24;
  if (days < 30) return `${Math.floor(days)} d ago`;
  return date(value);
}

export const weight = (kg: number) => `${kg.toLocaleString(locale, { maximumFractionDigits: 1 })} kg`;

export const volume = (kg: number) =>
  kg >= 1000 ? `${(kg / 1000).toLocaleString(locale, { maximumFractionDigits: 1 })} t` : `${Math.round(kg)} kg`;

export const hours = (h: number) => `${h.toLocaleString(locale, { maximumFractionDigits: 1 })} h`;

/** "BuildMuscle" → "Build muscle". */
export const words = (value: string) => {
  const spaced = value.replace(/([a-z])([A-Z])/g, "$1 $2").toLowerCase();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
};

/** A quota window, e.g. "day" → "per day". */
export const period = (value: string) => `per ${value.toLowerCase()}`;

export const quota = (used: number, limit: number, per: string) => `${used} of ${limit} ${period(per)}`;

export const initials = (name: string | undefined, email: string) => {
  const source = name?.trim() || email;
  const parts = source.split(/[\s@._-]+/).filter(Boolean);
  return ((parts[0]?.[0] ?? "?") + (name?.trim() && parts[1] ? parts[1][0] : "")).toUpperCase();
};
