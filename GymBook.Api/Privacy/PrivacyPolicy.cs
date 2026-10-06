
namespace GymBook.Api.Privacy;

/// <summary>
/// The public privacy policy page (GET /privacy) that the Play Store listing and the app link to: a plain,
/// login-free HTML page. Keep it in step with what the app and API actually store and share.
/// </summary>
public static class PrivacyPolicy
{
    public const string LastUpdated = "6 October 2026";
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
        GymBook needs an account, which backs up your data and syncs it between your devices. Your data is also kept on each device,
        so the app works without a connection.</p>

        <h2>Your account</h2>
        <ul>
          <li><strong>Account details:</strong> your email address and password. Passwords are stored only as a secure hash, never in readable form. If you sign in with Google, we receive your Google account's email address and account ID from Google, and nothing else. We also keep whether your email address has been verified.</li>
          <li><strong>Sign-in sessions:</strong> sign-in tokens that keep you signed in on each device, stored only as hashes.</li>
          <li><strong>Server logs:</strong> the server may record IP addresses and request times in its logs, used only to keep the service secure and to fix problems.</li>
        </ul>

        <h2>What you put in GymBook</h2>
        <p>What you enter is saved on your device and synced to your account so it can be restored and appear on your other devices:
        your profile (goal, experience, training days, session length, equipment, units, gender, body weight, and optionally your age,
        body fat percentage, height, activity level and how long you've trained), your training plans, workouts and their sets and notes
        (including a workout in progress), custom exercises, body weight history, the food you log with its calories and macros, foods
        you save to My foods, creatine doses, and your nutrition goals.</p>
        <p>A workout you delete is kept in your account as deleted, so you can bring it back from Recently deleted workouts and so the
        deletion reaches your other devices. It's removed for good when your account is deleted.</p>

        <h2>Health data from Samsung Health and Health Connect</h2>
        <p>On Android, you can choose to let GymBook read health data, either from Samsung Health directly or through Health Connect
        (from every app, or only the apps you pick). Only with your permission, given in Samsung Health or Health Connect and
        changeable there at any time, GymBook reads: calories burned (total, active and resting), steps, food logged in other apps
        (each food's name, meal, time, calories, protein, carbohydrates and fat), weight, body fat, lean body mass, bone mass, body
        water mass, basal metabolic rate, height and sleep (when each sleep started and ended, and how long you were asleep). When you
        first connect, GymBook reads all of this that's available, including data recorded before you connected (in Health Connect,
        older than 30 days only if you allow access to past data); after that it keeps the last 30 days up to date.</p>
        <p>Only if you turn on sending (it's off by default), GymBook also writes to Samsung Health or Health Connect what you log in
        GymBook: finished workouts (as strength-training sessions with their time and active calories), the food you log and the
        weight and body fat you enter. It only changes or deletes the records it wrote itself. Turning sending off stops it.</p>
        <p>This data is used to show your nutrition, energy balance (calories eaten against burned), activity and body composition in
        the app, to estimate and suggest your nutrition goals, to estimate how quickly your muscles recover (sleep), and to keep your
        health app up to date with your training and food. It's saved and synced like the rest of your data. It's never used for
        advertising, never sold, and never sent to the AI features. The one way any of it can become visible to others is your
        choice to join the strength leaderboards: your latest weigh-in, including one read from a health app, is what your lifts are
        compared to, as described below. Disconnecting stops further reading; deleting your account deletes what was synced.</p>

        <h2>Heart rate on Wear OS</h2>
        <p>With your permission, the GymBook app on a Wear OS watch reads your heart rate from the watch's sensor while a workout is
        on screen, to show it on the watch and, live, on your phone. It isn't stored, synced to your account or shared.</p>

        <h2>Food search and barcodes</h2>
        <p>When you search for a food to log, the words you type are sent to Open Food Facts (openfoodfacts.org), the free, open food
        database, and to GymBook's server, which looks them up in the USDA's FoodData Central and in FatSecret and keeps neither
        the search nor who made it. A barcode you scan is looked up the same way. Nothing else is sent with them: no account, health
        data or other personal information. Everyday foods built into the app and the foods you save to My foods are searched on
        your device.</p>
        <p>Barcodes are scanned with Google's code scanner, part of Google Play services, which reads the barcode on your device and
        hands GymBook only its number; GymBook doesn't get access to your camera or its pictures.</p>

        <h2>Your public profile</h2>
        <p>Every account has a public profile so friends, leaderboards and shared plans can show who's who:</p>
        <ul>
          <li><strong>Username, picture and bio:</strong> your username (the one you pick when you sign up, or one made up for you, which you can change), and the picture and bio you choose to add. Other GymBook users can see them, for example when they look you up by username or friend code, on a leaderboard or on a plan you share. Your picture can be loaded by anyone who knows your username. Your email address and the rest of your profile are never shown.</li>
          <li><strong>Friend code:</strong> a code you can give people so they can add you as a friend.</li>
          <li><strong>Gym Book Pro:</strong> if you have Gym Book Pro, a ring around your picture on the leaderboards shows it.</li>
        </ul>

        <h2>Leaderboards</h2>
        <p>The leaderboards are optional: you're only on them after you choose to join, and leaving removes you from all of them.
        While you're on them, everyone using GymBook can see, next to your username and picture:</p>
        <ul>
          <li><strong>Strength and consistency:</strong> your tier overall and in each category (chest, back, shoulders, legs, posterior chain, arms and consistency). For each strength category, the exercise it comes from (one you created yourself shows only as "Custom exercise"), its estimated one-rep max and that lift's ratio to your body weight, which together show your body weight; for consistency, how many workouts a week you've done over the last 8 weeks. They're worked out on our server from your synced workouts of the last year and your latest weigh-in (or the body weight in your profile), against the standards for your gender.</li>

          <li><strong>Nutrition:</strong> how many days in a row you've logged food, your longest run, and how many of the last 30 days your calories and protein were on target for your own goals. Only these numbers are shared, never what you ate. The app sends them to our server whenever you open Nutrition.</li>
        </ul>
        <p>Someone who thinks your numbers or profile are wrong or offensive can report them; our team reviews reports and can hide
        you from the leaderboards or clear your picture and bio.</p>

        <h2>Friends and shared plans</h2>
        <ul>
          <li><strong>Friends:</strong> who you add as a friend, and who adds you. Friends see each other on the friends leaderboards.</li>
          <li><strong>Reports:</strong> any report you send about another user, with the reason you give, for our team to review.</li>
          <li><strong>Shared plans:</strong> a plan you share with people in the app is visible to them, with your username and picture, and those you let edit it can change it. A plan you share by public link can be seen by anyone with the link, without an account, with your username, picture and bio. Stopping sharing ends both. A copy someone saved of a plan you shared is theirs and stays in their account.</li>
        </ul>

        <h2>Gym Book Pro and purchases</h2>
        <p>Gym Book Pro is bought through Google Play, the App Store or the Microsoft Store, which handle the payment: we never see
        your payment details. So that a purchase can only be used by your account, the app gives the store an identifier for your
        account that doesn't reveal who you are. Our server then checks the purchase with the store and keeps what it needs to know
        whether you have Gym Book Pro: which plan, when it renews or ends, whether it's a free trial or was refunded, and the store's
        identifiers for the purchase. Pro can also be given to you by our team. Cancelling is done in the store; deleting your
        account doesn't cancel a subscription.</p>

        <h2>AI features</h2>
        <p>The AI features (AI plans, importing a plan, the AI coach, AI suggestions and weekly plan reviews) send what they need to
        OpenAI to generate an answer: your training profile (goal, experience, days, session length, equipment, and your age, gender,
        body weight and how long you've trained, where set), the plan, what you type or attach, and for suggestions and reviews a
        summary of your recent workouts on the plan. To import a plan from a link, our server downloads the page or file at that link
        and sends its contents. When you import workouts from another app, exercise names the app can't match itself are sent so the
        AI can match them. Your name, email address and health data are not sent.</p>
        <p>The AI features run when you use them, with one exception: with Gym Book Pro, the app reviews your active plan about once a
        week after you've logged workouts on it, and shows any suggestions in the app. We record when each AI request was made and
        what kind it was, to apply usage limits.</p>

        <h2>How we use your data</h2>
        <p>Only to provide GymBook: signing you in, backing up and syncing your data, training suggestions such as starting weights,
        progression and muscle recovery, nutrition goals, the AI features you use, your public profile, friends, shared plans and the
        leaderboards, Gym Book Pro, support, reviewing reports, and keeping the service secure. Suggestions are calculated from your
        own data. We don't use your data for advertising, don't build marketing profiles, and don't sell it.</p>

        <h2>Who can access it</h2>
        <p>We don't sell your personal data or share it with third parties beyond what's described here. Your account data is stored
        on a server we operate, run by our hosting provider, which processes it only on our behalf. Our team can see account details
        in our admin tools (your email address, profile, a summary of your training, your plans and recent workouts, AI usage and
        subscription) and uses them only to run the service, to help when you contact us and to review reports. We disclose data
        only if required by law.</p>
        <p>GymBook contains no advertising, analytics or tracking software. Besides OpenAI and the stores, described above, a few
        features use other services:</p>
        <ul>
          <li><strong>Exercise videos</strong> are YouTube videos: an exercise's page plays its demonstration in YouTube's embedded player, so YouTube
          receives your device's IP address and which video is shown. (The exercise pictures are part of the app and load nothing.) No account information is sent.
          What YouTube collects is covered by Google's privacy policy. "Open in YouTube" opens the video (for your own exercises, a YouTube search) in your browser or the YouTube app.</li>
          <li><strong>Email:</strong> codes to verify your email address, reset your password or change your email are sent through our email provider, which receives the address and the message.</li>
          <li><strong>Sign in with Google:</strong> Google receives your sign-in to GymBook, as with any app you sign in to with Google.</li>
          <li><strong>Feature requests and bug reports</strong> you send from the app are kept in a private GitHub repository that only our team can see, with your username, and for a bug report your device's platform, system version, model and app version. Your email address isn't sent. Feature requests are shown to everyone in the app with your username, as are comments on them; a bug report and its comments are only shown to you. We keep which of them are yours and which requests you voted for. When you delete your account, your bug reports and comments are deleted and your feature requests stay without your username.</li>
        </ul>

        <h2>Security</h2>
        <p>All communication between the app and our server is encrypted with HTTPS. Access to your data requires your signed-in account,
        and each account can only ever read its own data, apart from what others share with it as described above.</p>

        <h2>Keeping and deleting your data</h2>
        <p>Your account data is kept while your account exists. You can delete your account at any time in the app
        (Profile tab &rarr; Delete account); this permanently deletes your account and everything kept for it on our server: your synced
        data, public profile and picture, leaderboard entries, friends, reports, shared plans and purchase records. Signing out removes
        your data from that device. Data on a device is also deleted when you uninstall the app.</p>
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
