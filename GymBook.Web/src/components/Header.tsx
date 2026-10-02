import Link from "next/link";
import styles from "./Header.module.css";

// A frosted bar like the app's nav bar, stuck to the top. The links to sections are plain anchors: Next's Link does
// nothing when the address already ends in that #section (tapped before, then scrolled away), a plain one scrolls again.
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
          {/* eslint-disable-next-line @next/next/no-html-link-for-pages -- see above */}
          <a href="/#features">Features</a>
          {/* eslint-disable-next-line @next/next/no-html-link-for-pages -- see above */}
          <a href="/#faq">FAQ</a>
          <Link href="/support/">Support</Link>
        </nav>
        {/* eslint-disable-next-line @next/next/no-html-link-for-pages -- see above */}
        <a href="/#download" className={`btn btn-primary ${styles.cta}`}>
          Get the app
        </a>
      </div>
    </header>
  );
}
