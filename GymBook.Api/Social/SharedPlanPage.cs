using System.Globalization;
using System.Net;
using System.Text;
using GymBook.Api.Controllers;
using GymBook.Api.Data;
using GymBook.Contracts;
using GymBook.Models;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Social;

/// <summary>
/// A publicly shared plan as a web page (GET /p/{id}), for anyone, without the app or an account: the link a user
/// sends from the plan's Share sheet. Rendered on the server, so it reads (and previews in chats) without JavaScript.
/// </summary>
public static class SharedPlanPage
{
    const string SiteUrl = "https://gymbook.app";

    public static async Task<IResult> Page(string id, ApiDbContext db, PlanShares shares, HttpContext http, CancellationToken ct)
    {
        var share = await db.PlanShares.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.IsPublic, ct);
        var view = share == null ? null : await shares.ViewAsync(share, ct);
        if (share == null || view == null)
            return Results.Content(Layout("Plan not found", "", """
                <h1>This plan isn't shared any more</h1>
                <p class="muted">Its owner stopped sharing it, or the link is mistyped.</p>
                """), "text/html; charset=utf-8", statusCode: 404);

        var owner = await db.SocialProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == share.OwnerId, ct);
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}/";
        var body = new StringBuilder();

        body.Append("<header>");
        if (owner != null)
        {
            var avatar = SocialController.AvatarPath(owner.Username, owner.AvatarVersion);
            body.Append("<div class=\"owner\">");
            body.Append(avatar != null
                ? $"<img class=\"avatar\" src=\"{E(baseUrl + avatar)}\" alt=\"\">"
                : $"<div class=\"avatar initial\">{E(owner.Username[..1].ToUpperInvariant())}</div>");
            body.Append($"<div><div class=\"by\">Shared by <strong>@{E(owner.Username)}</strong></div>");
            if (owner.Bio.Length > 0)
                body.Append($"<div class=\"muted small\">{E(owner.Bio)}</div>");
            body.Append("</div></div>");
        }
        body.Append($"<h1>{E(view.Name)}</h1>");
        var facts = new List<string> { GoalName(view.Goal), $"{view.Workouts.Count} workout{(view.Workouts.Count == 1 ? "" : "s")}" };
        if (view.DaysPerWeek > 0)
            facts.Add($"{view.DaysPerWeek} days a week");
        body.Append($"<p class=\"muted\">{E(string.Join(" · ", facts))}</p>");
        if (view.Description.Length > 0)
            body.Append($"<p>{E(view.Description)}</p>");
        body.Append("</header>");

        foreach (var (workout, i) in view.Workouts.Select((w, i) => (w, i)))
        {
            body.Append("<section class=\"card\">");
            body.Append($"<h2><span class=\"day\">Day {i + 1}</span>{E(workout.Name)}</h2>");
            body.Append("<table><thead><tr><th>Exercise</th><th>Sets × reps</th><th class=\"hide-narrow\">Rest</th></tr></thead><tbody>");
            foreach (var e in workout.Exercises)
            {
                var reps = e.RepMin == e.RepMax ? $"{e.RepMin}" : $"{e.RepMin}–{e.RepMax}";
                var rir = e.TargetRir is { } r ? $"<span class=\"muted small\"> · {r} RIR</span>" : "";
                body.Append($"<tr><td>{E(e.Name)}</td><td class=\"nowrap\">{e.Sets} × {E(reps)}{rir}</td><td class=\"hide-narrow nowrap\">{E(Rest(e.RestSeconds))}</td></tr>");
            }
            body.Append("</tbody></table></section>");
        }

        body.Append($"""
            <section class="cta">
              <h2>Train with this plan</h2>
              <p>Get Gym Book to follow it with your sets, weights and progress tracked for you.
              {(share.AllowCopy ? "In the app, open <strong>Plans → Open a shared link</strong> and paste this page's link to save your own copy." : "")}</p>
              <a class="button" href="{SiteUrl}">Get Gym Book</a>
            </section>
            """);

        var description = $"A {GoalName(view.Goal).ToLowerInvariant()} plan with {view.Workouts.Count} workouts" + (owner != null ? $", shared by @{owner.Username}" : "") + " on Gym Book.";
        return Results.Content(Layout(view.Name, description, body.ToString()), "text/html; charset=utf-8");
    }

    static string E(string s) => WebUtility.HtmlEncode(s);

    static string Rest(int seconds) => seconds < 60 ? $"{seconds} s"
        : seconds % 60 == 0 ? $"{seconds / 60} min"
        : $"{seconds / 60}:{(seconds % 60).ToString("00", CultureInfo.InvariantCulture)} min";

    static string GoalName(Goal goal) => goal switch
    {
        Goal.BuildMuscle => "Muscle building",
        Goal.Strength => "Strength",
        Goal.LoseFat => "Fat loss",
        Goal.Power => "Power",
        _ => "General fitness",
    };

    static string Layout(string title, string description, string body) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{{E(title)}} · Gym Book</title>
        <meta name="description" content="{{E(description)}}">
        <meta property="og:title" content="{{E(title)}}">
        <meta property="og:description" content="{{E(description)}}">
        <meta property="og:site_name" content="Gym Book">
        <meta name="robots" content="noindex">
        <style>
          :root { --bg: #f4f6fa; --card: #fff; --text: #1d2330; --muted: #6b7486; --line: #e3e7ef; --accent: #2a62d6; }
          @media (prefers-color-scheme: dark) { :root { --bg: #0f131a; --card: #181d27; --text: #e8ecf3; --muted: #9aa3b5; --line: #262d3a; --accent: #6f9bff; } }
          * { box-sizing: border-box; }
          body { font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; line-height: 1.5; color: var(--text); background: var(--bg); margin: 0; }
          main { max-width: 760px; margin: 0 auto; padding: 28px 16px 64px; }
          h1 { font-size: 1.9rem; margin: 0.4rem 0 0.2rem; line-height: 1.2; }
          h2 { font-size: 1.1rem; margin: 0 0 0.6rem; }
          .muted { color: var(--muted); }
          .small { font-size: 0.9rem; }
          .owner { display: flex; gap: 12px; align-items: center; }
          .avatar { width: 44px; height: 44px; border-radius: 50%; object-fit: cover; flex: none; }
          .initial { background: var(--accent); color: #fff; display: grid; place-items: center; font-weight: 600; }
          .card { background: var(--card); border: 1px solid var(--line); border-radius: 14px; padding: 16px; margin-top: 16px; }
          .day { color: var(--muted); font-weight: 400; margin-right: 8px; font-size: 0.9rem; }
          table { width: 100%; border-collapse: collapse; }
          th { text-align: left; color: var(--muted); font-weight: 500; font-size: 0.85rem; padding: 4px 0; }
          td { padding: 8px 8px 8px 0; border-top: 1px solid var(--line); vertical-align: top; }
          .nowrap { white-space: nowrap; }
          .cta { margin-top: 28px; text-align: center; }
          .button { display: inline-block; background: var(--accent); color: #fff; text-decoration: none; padding: 10px 22px; border-radius: 10px; font-weight: 600; }
          @media (max-width: 480px) { .hide-narrow { display: none; } h1 { font-size: 1.5rem; } }
        </style>
        </head>
        <body>
        <main>
        {{body}}
        </main>
        </body>
        </html>
        """;
}
