using System.Net;
using GymBook.Contracts;

namespace GymBook.Services;

/// <summary>
/// Feature requests and bug reports as web pages: their text comes as HTML, as GitHub shows it (formatting, code,
/// tables, checklists, pictures), in the app's colours. Links to change a comment use <see cref="Scheme"/>, which the
/// page's WebView handles itself; other links open in the browser.
/// </summary>
public static class FeedbackHtml
{
    public const string Scheme = "gymbook-feedback";

    /// <summary>A ticket, then its comments, each of the user's own with Edit and Delete.</summary>
    public static string Thread(FeedbackThread thread)
    {
        var t = thread.Ticket;
        var html = new System.Text.StringBuilder();
        html.Append($"<h1>{E(t.Title)}</h1>");
        html.Append($"<p class=\"meta\"><span class=\"chip{(t.IsClosed ? " closed" : " open")}\">{E(t.Status)}</span>");
        html.Append($"{(t.Kind == FeedbackKind.Feature ? "Feature request" : "Bug report")} #{t.Number} · {By(t.Author, t.IsTeam, t.IsMine)} · {Date(t.CreatedAt)}</p>");
        html.Append($"<div class=\"markdown\">{(t.BodyHtml.Trim().Length > 0 ? t.BodyHtml : "<p class=\"meta\">No description.</p>")}</div>");

        html.Append($"<h2 class=\"section\">{(thread.Comments.Count == 0 ? "No comments yet" : thread.Comments.Count == 1 ? "1 comment" : $"{thread.Comments.Count} comments")}</h2>");
        foreach (var c in thread.Comments)
        {
            html.Append($"<div class=\"card{(c.IsTeam ? " team" : "")}\">");
            html.Append($"<p class=\"meta\">{By(c.Author, c.IsTeam, c.IsMine)} · {Date(c.CreatedAt)}{(c.Edited ? " · edited" : "")}</p>");
            html.Append($"<div class=\"markdown\">{c.BodyHtml}</div>");
            if (c.IsMine)
                html.Append($"<p class=\"actions\"><a href=\"{Scheme}://comment/{c.Id}/edit\">Edit</a><a href=\"{Scheme}://comment/{c.Id}/delete\">Delete</a></p>");
            html.Append("</div>");
        }
        return Page(html.ToString());
    }

    static string By(string author, bool team, bool mine) =>
        mine ? "you" : team ? $"<span class=\"teamname\">{E(author)}</span>" : author.Contains(' ') ? E(author) : "@" + E(author);

    static string Date(DateTimeOffset at) => E(at.ToLocalTime().ToString("d MMM yyyy"));

    static string E(string text) => WebUtility.HtmlEncode(text);

    static string Page(string content, string background = "#0B0D12") => $$"""
        <!DOCTYPE html>
        <html><head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
        :root { color-scheme: dark; }
        body { margin: 0; padding: 14px 16px 28px; background: {{background}}; color: #F4F6FB;
               font: 15px/1.55 -apple-system, "Segoe UI", Roboto, "Open Sans", sans-serif; overflow-wrap: anywhere; }
        a { color: #6E9BFF; }
        h1 { font-size: 22px; line-height: 1.3; margin: 0 0 6px; }
        h2.section { font-size: 16px; margin: 26px 0 6px; color: #9AA3B5; }
        .meta { color: #9AA3B5; font-size: 13px; margin: 0 0 10px; }
        .chip { display: inline-block; padding: 2px 10px; margin-right: 8px; border-radius: 999px; font-size: 12px; font-weight: 600; }
        .chip.open { background: rgba(63,125,255,.18); color: #7FA6FF; }
        .chip.closed { background: #1D212C; color: #9AA3B5; }
        .teamname { color: #7FA6FF; font-weight: 600; }
        .card { background: #151821; border-radius: 16px; padding: 12px 14px 4px; margin: 10px 0; }
        .card.team { border-left: 3px solid #3F7DFF; }
        .actions a { margin-right: 18px; font-size: 13px; font-weight: 600; text-decoration: none; }
        .markdown > :first-child { margin-top: 0; }
        .markdown h1, .markdown h2, .markdown h3 { line-height: 1.3; margin: 18px 0 8px; }
        .markdown h1 { font-size: 20px; } .markdown h2 { font-size: 18px; } .markdown h3 { font-size: 16px; }
        .markdown img, .markdown video { max-width: 100%; height: auto; border-radius: 10px; }
        .markdown code { background: #1D212C; padding: 1px 5px; border-radius: 5px; font: 13px/1.4 ui-monospace, Consolas, monospace; }
        .markdown pre { background: #1D212C; padding: 10px 12px; border-radius: 10px; overflow-x: auto; }
        .markdown pre code { padding: 0; background: none; }
        .markdown blockquote { margin: 0 0 10px; padding: 0 12px; border-left: 3px solid #2A3040; color: #9AA3B5; }
        .markdown table { border-collapse: collapse; display: block; overflow-x: auto; margin-bottom: 10px; }
        .markdown td, .markdown th { border: 1px solid #2A3040; padding: 5px 9px; }
        .markdown hr { border: 0; border-top: 1px solid #2A3040; }
        .markdown ul, .markdown ol { padding-left: 22px; }
        .markdown .task-list-item { list-style: none; }
        .markdown .task-list-item input { margin: 0 6px 0 -18px; }
        .markdown .anchor, .markdown .octicon { display: none; }
        .markdown details { background: #151821; border-radius: 10px; padding: 6px 10px; margin-bottom: 10px; }
        </style>
        </head><body>{{content}}</body></html>
        """;
}
