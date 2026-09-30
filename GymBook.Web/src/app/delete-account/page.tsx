import type { Metadata } from "next";
import Link from "next/link";
import { site } from "@/lib/site";

export const metadata: Metadata = {
  title: "Delete your account",
  description: "How to delete your Gym Book account and all data synced to it.",
};

// Keep in step with GymBook.Api/Privacy/DeleteAccountPage.cs, which Google Play's data safety section still links to.
export default function DeleteAccountPage() {
  const email = site.contactEmail;
  return (
    <article className="prose">
      <h1>Delete your account</h1>
      <p className="updated">You can delete your Gym Book account and all data synced to it at any time, in the app or by email.</p>

      <h2>In the app</h2>
      <ol>
        <li>
          Open Gym Book and go to the <strong>Profile</strong> tab.
        </li>
        <li>
          Tap <strong>Delete account</strong>.
        </li>
        <li>Enter your password to confirm, or confirm with Google if you signed up with Google.</li>
      </ol>
      <p>Your account and its data are deleted from our server straight away.</p>

      <h2>Without the app</h2>
      <p>
        Email <a href={`mailto:${email}?subject=Delete%20my%20Gym%20Book%20account`}>{email}</a> from the address you signed up with, with
        the subject &quot;Delete my Gym Book account&quot;. We&apos;ll delete the account and all its data within 30 days and confirm by
        email.
      </p>

      <h2>What gets deleted</h2>
      <p>
        Everything stored with your account: your email address and password, your profile (name, body weight, age, body fat and training
        details), training plans, workouts and sets (including a workout in progress), custom exercises and body weight history, and your
        sign-in sessions. Nothing is kept afterwards, apart from server logs, which are kept only briefly for security.
      </p>
      <p>Data saved only on your own device stays there until you sign out or uninstall Gym Book.</p>
      <p className="callout">
        See also the <Link href="/privacy/">privacy policy</Link>.
      </p>
    </article>
  );
}
