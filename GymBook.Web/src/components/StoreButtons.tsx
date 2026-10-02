import { platforms, type Platform } from "@/lib/site";
import { AndroidLogo, AppleLogo, WatchLogo, WindowsLogo } from "./Icons";
import styles from "./StoreButtons.module.css";

const logos: Record<Platform["id"], React.ReactNode> = {
  ios: <AppleLogo />,
  mac: <AppleLogo />,
  android: <AndroidLogo />,
  wear: <WatchLogo />,
  windows: <WindowsLogo />,
};

// One button per platform, named with its store (Android and Wear OS share Google Play, iPhone and Mac the App Store);
// a platform that isn't out yet shows as "Coming soon" instead of a link.
export function StoreButtons({ large = false }: { large?: boolean }) {
  return (
    <div className={`${styles.row} ${large ? styles.large : ""}`}>
      {platforms.map((p) =>
        p.url ? (
          <a key={p.id} href={p.url} className={styles.button} target="_blank" rel="noopener noreferrer">
            {logos[p.id]}
            <span className={styles.text}>
              <small>{p.name} · {p.store}</small>
              <strong>Get it</strong>
            </span>
          </a>
        ) : (
          <span key={p.id} className={`${styles.button} ${styles.soon}`} aria-disabled="true">
            {logos[p.id]}
            <span className={styles.text}>
              <small>{p.name} · {p.store}</small>
              <strong>Coming soon</strong>
            </span>
          </span>
        ),
      )}
    </div>
  );
}
