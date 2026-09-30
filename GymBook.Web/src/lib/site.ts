// Everything the site says about where to get the app. The store links are baked in at build time from
// NEXT_PUBLIC_* variables (the Publish Web workflow passes them); a platform without a link shows "Coming soon".

export const site = {
  name: "Gym Book",
  tagline: "Your training plan, logbook and coach in one.",
  description:
    "Gym Book builds your training plan, tracks every set, tells you which muscles are recovered and shows your progress. Works offline, syncs across your phone and PC.",
  url: process.env.NEXT_PUBLIC_SITE_URL || "https://gymbook.app",
  contactEmail: "yskudilov@gmail.com",
  privacyUpdated: "28 September 2026",
};

export type Platform = {
  id: "ios" | "android" | "windows";
  name: string;
  store: string;
  url?: string;
};

export const platforms: Platform[] = [
  {
    id: "ios",
    name: "iPhone & iPad",
    store: "App Store",
    url: process.env.NEXT_PUBLIC_APP_STORE_URL || undefined,
  },
  {
    id: "android",
    name: "Android",
    store: "Google Play",
    url:
      process.env.NEXT_PUBLIC_PLAY_STORE_URL ||
      "https://play.google.com/store/apps/details?id=com.yehorskudilov.gymbook",
  },
  {
    id: "windows",
    name: "Windows",
    store: "Microsoft Store",
    url: process.env.NEXT_PUBLIC_MS_STORE_URL || undefined,
  },
];
