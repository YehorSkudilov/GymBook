import type { Metadata } from "next";
import Link from "next/link";
import { site } from "@/lib/site";

export const metadata: Metadata = {
  title: "Terms of use",
  description: "The terms for using the Gym Book app and website.",
};

export default function TermsPage() {
  const email = site.contactEmail;
  return (
    <article className="prose">
      <h1>Terms of use</h1>
      <p className="updated">Last updated: {site.privacyUpdated}</p>

      <p>
        These terms apply to the Gym Book app and this website. By using Gym Book you agree to them. If you don&apos;t, please don&apos;t use
        the app.
      </p>

      <h2>Not medical advice</h2>
      <p>
        Gym Book gives general training information: plans, suggested weights, recovery estimates and AI-generated suggestions. It is not
        medical advice and doesn&apos;t replace a doctor, physiotherapist or qualified coach. Check with a doctor before starting a new
        exercise program, especially if you have an injury or health condition. Stop exercising if you feel pain, dizziness or shortness of
        breath. You train at your own risk.
      </p>

      <h2>AI-generated content</h2>
      <p>
        Plans, coach replies and suggestions written by AI can be wrong or unsuitable for you. Use your own judgement before following them.
      </p>

      <h2>Your account</h2>
      <p>
        An account is optional. If you create one, keep your password safe and make sure the email address is yours. You&apos;re responsible
        for what happens under your account. You can delete it at any time (see <Link href="/delete-account/">how</Link>).
      </p>

      <h2>Fair use</h2>
      <p>
        Don&apos;t misuse the service: no attempts to break into it, overload it, access other people&apos;s data, or use the AI features to
        generate content unrelated to training. We may limit, suspend or close accounts that do.
      </p>

      <h2>Your data</h2>
      <p>
        Your training data is yours. How we handle it is described in the <Link href="/privacy/">privacy policy</Link>.
      </p>

      <h2>Availability</h2>
      <p>
        Gym Book is provided &quot;as is&quot;. We work to keep sync and the AI features running, but can&apos;t promise they will always be
        available or error-free, and features may change over time. The app keeps working offline on your device whether or not the server
        is reachable. Export your data from the app if you want your own copy.
      </p>

      <h2>Liability</h2>
      <p>
        To the extent the law allows, we aren&apos;t liable for injuries, lost data or other damage arising from using Gym Book. Nothing in
        these terms limits rights you have under consumer protection law.
      </p>

      <h2>Changes</h2>
      <p>If these terms change, the new version will be posted here with a new &quot;last updated&quot; date.</p>

      <h2>Contact</h2>
      <p>
        Questions: <a href={`mailto:${email}`}>{email}</a>.
      </p>
    </article>
  );
}
