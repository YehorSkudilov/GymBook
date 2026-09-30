"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { createContext, useContext, useEffect, useState } from "react";
import { request } from "@/lib/client";
import type { Unit } from "@/lib/format";
import type { Me } from "@/lib/types";
import { HomeIcon, LogoutIcon, SparkleIcon, UsersIcon } from "./Icons";
import styles from "./Shell.module.css";

type ShellState = { me: Me | null; unit: Unit; setUnit: (u: Unit) => void };

const ShellContext = createContext<ShellState>({ me: null, unit: "kg", setUnit: () => {} });

/** The signed-in admin and the chosen weight unit. */
export const useShell = () => useContext(ShellContext);

const nav = [
  { href: "/", label: "Dashboard", icon: HomeIcon },
  { href: "/users", label: "Users", icon: UsersIcon },
  { href: "/ai", label: "AI usage", icon: SparkleIcon },
];

function readUnit(): Unit {
  try {
    return localStorage.getItem("unit") === "lbs" ? "lbs" : "kg";
  } catch {
    return "kg";
  }
}

// A sidebar on desktop and the app's frosted bottom bar on phones; the sign-in page gets neither.
export function Shell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [me, setMe] = useState<Me | null>(null);
  const [unit, setUnitState] = useState<Unit>("kg");
  const isLogin = pathname === "/login";

  useEffect(() => {
    // Browser-only preference, so it can't be read during server rendering.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setUnitState(readUnit());
  }, []);

  useEffect(() => {
    if (isLogin) return;
    request<Me>("/api/session").then(setMe, () => {});
  }, [isLogin]);

  const setUnit = (u: Unit) => {
    setUnitState(u);
    try {
      localStorage.setItem("unit", u);
    } catch {}
  };

  async function signOut() {
    await fetch("/api/session", { method: "DELETE" });
    setMe(null);
    router.replace("/login");
  }

  if (isLogin) return <>{children}</>;

  const isActive = (href: string) => (href === "/" ? pathname === "/" : pathname.startsWith(href));

  return (
    <ShellContext.Provider value={{ me, unit, setUnit }}>
      <div className={styles.layout}>
        <aside className={styles.sidebar}>
          <Link href="/" className={styles.brand}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/logo.svg" alt="" width={26} height={32} />
            <span>
              Gym Book
              <small>Admin</small>
            </span>
          </Link>

          <nav className={styles.nav}>
            {nav.map(({ href, label, icon: Icon }) => (
              <Link key={href} href={href} className={`${styles.navItem} ${isActive(href) ? styles.navActive : ""}`}>
                <Icon size={20} />
                {label}
              </Link>
            ))}
          </nav>

          <div className={styles.unit}>
            <span className="eyebrow">Weight unit</span>
            <div className={styles.toggle}>
              {(["kg", "lbs"] as const).map((u) => (
                <button key={u} className={unit === u ? styles.toggleOn : ""} onClick={() => setUnit(u)}>
                  {u}
                </button>
              ))}
            </div>
          </div>

          <div className={styles.me}>
            <div className={styles.meText}>
              <strong>{me?.email ?? "…"}</strong>
              {me && <span className={`badge ${me.role === "SuperAdmin" ? "badge-purple" : "badge-blue"}`}>{me.role}</span>}
            </div>
            <button className={styles.iconBtn} onClick={signOut} title="Sign out" aria-label="Sign out">
              <LogoutIcon size={18} />
            </button>
          </div>
        </aside>

        <main className={styles.main}>
          <header className={styles.mobileTop}>
            <Link href="/" className={styles.brand}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src="/logo.svg" alt="" width={22} height={27} />
              <span>Admin</span>
            </Link>
            <div className={styles.toggle}>
              {(["kg", "lbs"] as const).map((u) => (
                <button key={u} className={unit === u ? styles.toggleOn : ""} onClick={() => setUnit(u)}>
                  {u}
                </button>
              ))}
            </div>
            <button className={styles.iconBtn} onClick={signOut} aria-label="Sign out">
              <LogoutIcon size={18} />
            </button>
          </header>
          {children}
        </main>

        <nav className={styles.bottomBar}>
          {nav.map(({ href, label, icon: Icon }) => (
            <Link key={href} href={href} className={isActive(href) ? styles.bottomActive : ""}>
              <Icon size={22} />
              <span>{label}</span>
            </Link>
          ))}
        </nav>
      </div>
    </ShellContext.Provider>
  );
}
