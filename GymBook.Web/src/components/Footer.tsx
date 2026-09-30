import Link from "next/link";
import { site } from "@/lib/site";
import styles from "./Footer.module.css";

export function Footer() {
  return (
    <footer className={styles.footer}>
      <div className={`container ${styles.inner}`}>
        <div className={styles.brand}>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src="/logo.svg" alt="" width={18} height={22} />
          <span>Gym Book</span>
        </div>
        <nav className={styles.links}>
          <Link href="/privacy/">Privacy</Link>
          <Link href="/terms/">Terms</Link>
          <Link href="/delete-account/">Delete account</Link>
          <Link href="/support/">Support</Link>
          <a href={`mailto:${site.contactEmail}`}>Contact</a>
        </nav>
        <p className={styles.copy}>© {new Date().getFullYear()} Yehor Skudilov</p>
      </div>
    </footer>
  );
}
