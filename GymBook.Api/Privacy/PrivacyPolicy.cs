
namespace GymBook.Api.Privacy;

/// <summary>
/// The public privacy policy page (GET /privacy) that the Play Store listing and the app link to: a plain,
/// login-free HTML page. Keep it in step with what the app and API actually store and share.
/// </summary>
public static class PrivacyPolicy
{
    public const string LastUpdated = "28 September 2026";
    public const string ContactEmail = "yskudilov@gmail.com";

    public static IResult Page() =>
        Results.Content(Html.Replace("{{EMAIL}}", ContactEmail).Replace("{{UPDATED}}", LastUpdated), "text/html; charset=utf-8");

    const string Html = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>GymBook privacy policy</title>
        <style>
          body { font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; line-height: 1.6; color: #1d2330; background: #fff; margin: 0; }
          main { max-width: 720px; margin: 0 auto; padding: 32px 20px 64px; }
          h1 { font-size: 1.9rem; margin-bottom: 0.2rem; }
          h2 { font-size: 1.2rem; margin-top: 2rem; }
          .updated { color: #626b7e; margin-top: 0; }
          li { margin: 0.3rem 0; }
          a { color: #2a62d6; }
        </style>
        </head>
        <body>
        <main>
        <h1>GymBook privacy policy</h1>
        <p class="updated">Last updated: {{UPDATED}}</p>

        <p>This policy explains what the GymBook workout app ("GymBook", "we") collects, how it's used and who it's shared with.
        GymBook works fully offline without an account; an account is optional and only used to back up and sync your data between devices.</p>

        <h2>Data stored on your device</h2>
        <p>Everything you enter is saved on your device: your profile (name, goal, experience, training days, units, body weight,
        and optionally your age, body fat percentage and how long you've trained), your training plans, workouts and sets,
        custom exercises and body weight history. Without an account, this data never leaves your device.</p>

        <h2>Data we collect when you create an account</h2>
        <ul>
          <li><strong>Account details:</strong> your email address and password. Passwords are stored only as a secure hash, never in readable form. If you sign in with Google, we receive your Google account's email address and account ID from Google, and nothing else.</li>
          <li><strong>Your training data:</strong> the data listed above, including a workout in progress, is uploaded so it can be restored and synced to your other devices.</li>
          <li><strong>Sign-in sessions:</strong> sign-in tokens that keep you signed in on each device, stored only as hashes.</li>
          <li><strong>Server logs:</strong> the server may record IP addresses and request times in its logs, used only to keep the service secure and to fix problems.</li>
        </ul>

        <h2>How we use your data</h2>
        <p>Only to provide GymBook's features: signing you in, syncing your data between your devices, and working out training
        suggestions such as starting weights, progression and muscle recovery. Those suggestions are calculated from your own data.
        We don't use your data for advertising, don't build marketing profiles, and don't sell it.</p>

        <h2>Who we share data with</h2>
        <p>We don't sell or share your personal data with third parties. Your account data is stored on a server we operate,
        run by our hosting provider, which processes it only on our behalf. We disclose data only if required by law.</p>
        <p>GymBook contains no advertising, analytics or tracking software. A few features use other services:</p>
        <ul>
          <li><strong>Exercise photos</strong> are loaded from GitHub (raw.githubusercontent.com), which receives your device's IP address when it serves them.
          No account information is sent.</li>
          <li><strong>Email:</strong> password reset and email change codes are sent through our email provider, which receives the address and the message.</li>
          <li><strong>"Watch video"</strong> on an exercise opens a YouTube search in your browser. What YouTube collects is covered by Google's privacy policy.</li>
        </ul>

        <h2>Security</h2>
        <p>All communication between the app and our server is encrypted with HTTPS. Access to your data requires your signed-in account,
        and each account can only ever read its own data.</p>

        <h2>Keeping and deleting your data</h2>
        <p>Your account data is kept while your account exists. You can delete your account at any time in the app
        (Profile tab &rarr; Delete account); this permanently deletes your account and all data synced to it from our server.
        Signing out removes your data from that device. Data kept only on your device is deleted when you uninstall the app.</p>
        <p>You can also <a href="/delete-account">request deletion without the app</a>, and export your data from the app at any time.</p>

        <h2>Children</h2>
        <p>GymBook is not directed at children under 13, and we don't knowingly collect data from them.</p>

        <h2>Changes to this policy</h2>
        <p>If this policy changes, the new version will be posted on this page with a new "last updated" date.</p>

        <h2>Contact</h2>
        <p>Questions about this policy or your data can be sent by email to <a href="mailto:{{EMAIL}}">{{EMAIL}}</a>.</p>
        </main>
        </body>
        </html>
        """;
}
