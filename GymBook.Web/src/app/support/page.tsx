import type { Metadata } from "next";
import Link from "next/link";
import { site } from "@/lib/site";

export const metadata: Metadata = {
  title: "Support",
  description: "Help with Gym Book: syncing, accounts, units and contacting us.",
};

const topics = [
  {
    q: "My workouts aren't showing on my other device",
    a: "Make sure you're signed in with the same account on both devices and that both are online. The connection pill on Home shows the sync state; tap it to see details and sync now.",
  },
  {
    q: "I forgot my password",
    a: "On the sign-in screen tap \"Forgot password\" and we'll email you a code to set a new one. If you signed up with Google, use \"Continue with Google\" instead.",
  },
  {
    q: "How do I switch between kg and lbs?",
    a: "Go to the Profile tab and change the unit. Everything you've logged is converted, nothing is lost.",
  },
  {
    q: "Can I change or regenerate my plan?",
    a: "Yes. Open the plan on the Plans tab to edit days and exercises, chat with the AI coach to adjust it, or build a new one.",
  },
  {
    q: "How do I get my data out?",
    a: "Use the export option on the Profile tab to save a copy of everything you've logged.",
  },
];

export default function SupportPage() {
  const email = site.contactEmail;
  return (
    <article className="prose">
      <h1>Support</h1>
      <p className="updated">Stuck on something? Start here, or email us - we read every message.</p>

      {topics.map((t) => (
        <section key={t.q}>
          <h2>{t.q}</h2>
          <p>{t.a}</p>
        </section>
      ))}

      <h2>Contact us</h2>
      <p>
        Email <a href={`mailto:${email}?subject=Gym%20Book%20support`}>{email}</a>. It helps to include your device (e.g. Pixel 8, iPhone
        15, Windows 11), the app version from the Profile tab, and what you were doing when the problem happened.
      </p>

      <p className="callout">
        Looking for your data rights? See the <Link href="/privacy/">privacy policy</Link> or{" "}
        <Link href="/delete-account/">delete your account</Link>.
      </p>
    </article>
  );
}
