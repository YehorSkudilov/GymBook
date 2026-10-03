import type { Metadata } from "next";
import Link from "next/link";
import { site } from "@/lib/site";

export const metadata: Metadata = {
  title: "Privacy policy",
  description: "What Gym Book collects, how it's used and who it's shared with.",
};

// Keep in step with GymBook.Api/Privacy/PrivacyPolicy.cs, which the store listings still link to.
export default function PrivacyPage() {
  const email = site.contactEmail;
  return (
    <article className="prose">
      <h1>Privacy policy</h1>
      <p className="updated">Last updated: {site.privacyUpdated}</p>

      <p>
        This policy explains what the Gym Book workout app (&quot;Gym Book&quot;, &quot;we&quot;) collects, how it&apos;s used and
        who it&apos;s shared with. Gym Book works fully offline without an account; an account is optional and only used to back up and
        sync your data between devices.
      </p>

      <h2>Data stored on your device</h2>
      <p>
        Everything you enter is saved on your device: your profile (name, goal, experience, training days, units, body weight, and
        optionally your age, body fat percentage and how long you&apos;ve trained), your training plans, workouts and sets, custom
        exercises, body weight history, the food you log with its calories and macros, foods you save to My foods, creatine doses and
        your nutrition goals. Without an account, this data never leaves your device.
      </p>

      <h2>Health data from Samsung Health and Health Connect</h2>
      <p>
        On Android, you can choose to let Gym Book read health data, either from Samsung Health directly or through Health Connect
        (from every app, or only the apps you pick). Only with your permission, given in Samsung Health or Health Connect and
        changeable there at any time, Gym Book reads: calories burned (total, active and resting), steps, food logged in other apps
        (each food&apos;s name, meal, time, calories, protein, carbohydrates and fat), weight, body fat, lean body mass, bone mass, body
        water mass, basal metabolic rate, height and sleep (when each sleep started and ended, and how long you were asleep). When you
        first connect, Gym Book reads all of this that&apos;s available, including
        data recorded before you connected (in Health Connect, older than 30 days only if you allow access to past data); after that it
        keeps the last 30 days up to date.
      </p>
      <p>
        Only if you turn on sending (it&apos;s off by default), Gym Book also writes to Samsung Health or Health Connect what you log in
        Gym Book: finished workouts (as strength-training sessions with their time and active calories), the food you log and the weight
        and body fat you enter. It only changes or deletes the records it wrote itself. Turning sending off stops it.
      </p>
      <p>
        This data is used only to show your nutrition, energy balance, activity and body composition in the app, to suggest nutrition
        goals, to estimate how quickly your muscles recover (sleep), and to keep your health app up to date with your training and food. It&apos;s saved in the app like the rest of your data
        and, if you have an account, synced to it. Health data is never used for advertising, never sold, and never shared with anyone,
        including the AI features.
      </p>

      <h2>Heart rate on Wear OS</h2>
      <p>
        With your permission, the Gym Book app on a Wear OS watch reads your heart rate from the watch&apos;s sensor while a workout is on
        screen, to show it on the watch and, live, on your phone. It isn&apos;t stored, synced to your account or shared.
      </p>

      <h2>Data we collect when you create an account</h2>
      <ul>
        <li>
          <strong>Account details:</strong> your email address and password. Passwords are stored only as a secure hash, never in
          readable form. If you sign in with Google, we receive your Google account&apos;s email address and account ID from Google, and
          nothing else.
        </li>
        <li>
          <strong>Your training data:</strong> the data listed above, including a workout in progress, is uploaded so it can be restored
          and synced to your other devices.
        </li>
        <li>
          <strong>Sign-in sessions:</strong> sign-in tokens that keep you signed in on each device, stored only as hashes.
        </li>
        <li>
          <strong>Server logs:</strong> the server may record IP addresses and request times in its logs, used only to keep the service
          secure and to fix problems.
        </li>
      </ul>

      <h2>How we use your data</h2>
      <p>
        Only to provide Gym Book&apos;s features: signing you in, syncing your data between your devices, and working out training
        suggestions such as starting weights, progression and muscle recovery. Those suggestions are calculated from your own data. We
        don&apos;t use your data for advertising, don&apos;t build marketing profiles, and don&apos;t sell it.
      </p>

      <h2>Who we share data with</h2>
      <p>
        We don&apos;t sell or share your personal data with third parties. Your account data is stored on a server we operate, run by our
        hosting provider, which processes it only on our behalf. We disclose data only if required by law.
      </p>
      <p>Gym Book contains no advertising, analytics or tracking software. A few features use other services:</p>
      <ul>
        <li>
          <strong>Exercise videos</strong> are YouTube videos: an exercise&apos;s page plays its demonstration in YouTube&apos;s
          embedded player, so YouTube receives your device&apos;s IP address and which video is shown. (The exercise pictures are part
          of the app and load nothing.) No account information is sent. What YouTube collects is covered by Google&apos;s privacy policy.
        </li>
        <li>
          <strong>AI features</strong> (AI plans, importing a plan, the AI coach and AI suggestions) send what they need to OpenAI to
          generate an answer: your training profile (goal, experience, days, session length, equipment, and age, body weight and training
          years if set), the plan, what you type or attach, and for AI suggestions a summary of your recent sets on the plan. Your name and
          email are not sent. They are used only when you use these features.
        </li>
        <li>
          <strong>Food search:</strong> the words you search for, or a barcode you scan, are sent to Open Food Facts (the free, open food
          database) and to our server, which looks them up in the USDA&apos;s FoodData Central and in FatSecret without keeping them.
          Nothing else is sent with them. Barcodes are read on your device by Google&apos;s code scanner (Google Play services), which gives
          Gym Book only the number, not your camera.
        </li>
        <li>
          <strong>Email:</strong> password reset and email change codes are sent through our email provider, which receives the address
          and the message.
        </li>
        <li>
          <strong>&quot;Open in YouTube&quot;</strong> on an exercise opens its video (for your own exercises, a YouTube search) in your
          browser or the YouTube app.
        </li>
      </ul>

      <h2>This website</h2>
      <p>
        This website uses no cookies, analytics or tracking. Our web server may log IP addresses and request times, used only to keep it
        running and secure. Its screenshots load exercise photos from GitHub, as the app does.
      </p>

      <h2>Security</h2>
      <p>
        All communication between the app and our server is encrypted with HTTPS. Access to your data requires your signed-in account, and
        each account can only ever read its own data.
      </p>

      <h2>Keeping and deleting your data</h2>
      <p>
        Your account data is kept while your account exists. You can delete your account at any time in the app (Profile tab → Delete
        account); this permanently deletes your account and all data synced to it from our server. Signing out removes your data from that
        device. Data kept only on your device is deleted when you uninstall the app.
      </p>
      <p>
        You can also <Link href="/delete-account/">request deletion without the app</Link>, and export your data from the app at any time.
      </p>

      <h2>Children</h2>
      <p>Gym Book is not directed at children under 13, and we don&apos;t knowingly collect data from them.</p>

      <h2>Changes to this policy</h2>
      <p>If this policy changes, the new version will be posted on this page with a new &quot;last updated&quot; date.</p>

      <h2>Contact</h2>
      <p>
        Questions about this policy or your data can be sent by email to <a href={`mailto:${email}`}>{email}</a>.
      </p>
    </article>
  );
}
