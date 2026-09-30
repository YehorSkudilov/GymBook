import Link from "next/link";
import styles from "./Header.module.css";

// A frosted bar like the app's nav bar, stuck to the top.
export function Header() {
  return (
    <header className={styles.header}>
      <div className={`container ${styles.inner}`}>
        <Link href="/" className={styles.brand}>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src="/logo.svg" alt="" width={22} height={27} />
          <span>Gym Book</span>
        </Link>
        <nav className={styles.nav}>
          <Link href="/#features">Features</Link>
          <Link href="/#faq">FAQ</Link>
          <Link href="/support/">Support</Link>
        </nav>
        <Link href="/#download" className={`btn btn-primary ${styles.cta}`}>
          Get the app
        </Link>
      </div>
    </header>
  );
}
