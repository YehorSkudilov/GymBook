namespace GymBook.Api.Privacy;

/// <summary>
/// The public account-deletion page (GET /delete-account) that Google Play's data safety section links to: how to
/// delete a GymBook account in the app, or request it by email without the app, and what gets deleted.
/// </summary>
public static class DeleteAccountPage
{
    public static IResult Page() =>
        Results.Content(Html.Replace("{{EMAIL}}", PrivacyPolicy.ContactEmail), "text/html; charset=utf-8");

    const string Html = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Delete your GymBook account</title>
        <style>
          body { font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; line-height: 1.6; color: #1d2330; background: #fff; margin: 0; }
          main { max-width: 720px; margin: 0 auto; padding: 32px 20px 64px; }
          h1 { font-size: 1.9rem; }
          h2 { font-size: 1.2rem; margin-top: 2rem; }
          li { margin: 0.3rem 0; }
          a { color: #2a62d6; }
        </style>
        </head>
        <body>
        <main>
        <h1>Delete your GymBook account</h1>
        <p>You can delete your GymBook account and all data synced to it at any time, in the app or by email.</p>

        <h2>In the app</h2>
        <ol>
          <li>Open GymBook and go to the <strong>Profile</strong> tab.</li>
          <li>Tap <strong>Delete account</strong>.</li>
          <li>Enter your password to confirm, or confirm with Google if you signed up with Google.</li>
        </ol>
        <p>Your account and its data are deleted from our server straight away.</p>

        <h2>Without the app</h2>
        <p>Email <a href="mailto:{{EMAIL}}?subject=Delete%20my%20GymBook%20account">{{EMAIL}}</a> from the address you signed up with,
        with the subject "Delete my GymBook account". We'll delete the account and all its data within 30 days and confirm by email.</p>

        <h2>What gets deleted</h2>
        <p>Everything stored with your account: your email address and password, your profile (name, body weight, age, body fat and
        training details), training plans, workouts and sets (including a workout in progress), custom exercises and body weight history,
        and your sign-in sessions. Nothing is kept afterwards, apart from server logs, which are kept only briefly for security.</p>
        <p>Data saved only on your own device stays there until you sign out or uninstall GymBook.</p>

        <p>See also the <a href="/privacy">privacy policy</a>.</p>
        </main>
        </body>
        </html>
        """;
}
