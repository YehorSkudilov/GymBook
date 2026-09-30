using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;

namespace GymBook.Api.Auth;

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Gym Book";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>
/// Emails the one-time codes for password reset and email change. Without SMTP settings, Development writes the
/// mail to the log instead so the flows can be tried locally; anywhere else email features report unavailable.
/// </summary>
public class EmailSender(SmtpOptions options, IHostEnvironment env, IMemoryCache sent, ILogger<EmailSender> log)
{
    // One code per address and purpose a minute, and a few a day, so nobody can flood someone's inbox through us, and
    // an address that doesn't exist (a typo) isn't mailed over and over, each one bouncing.
    static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
    static readonly TimeSpan DailyWindow = TimeSpan.FromDays(1);
    const int DailyLimit = 5;

    public bool IsAvailable => options.IsConfigured || env.IsDevelopment();

    static readonly byte[] Logo = ReadLogo();

    static byte[] ReadLogo()
    {
        using var stream = typeof(EmailSender).Assembly.GetManifestResourceStream("EmailLogo.png")
            ?? throw new InvalidOperationException("The EmailLogo.png resource is missing.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>
    /// Sends a code. False (and nothing sent) when one went to this address for this purpose within the last minute,
    /// or <see cref="DailyLimit"/> already went today.
    /// </summary>
    public async Task<bool> SendCodeAsync(string to, string purpose, CodeEmail mail, CancellationToken ct)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Smtp is not configured.");
        var key = $"email-code:{purpose}:{to.ToUpperInvariant()}";
        var dailyKey = "daily-" + key;
        if (sent.TryGetValue(key, out _))
            return false;
        // The day starts with the first code, and the count lives as long as it.
        var today = sent.GetOrCreate(dailyKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DailyWindow;
            return new DailyCount();
        })!;
        if (today.Count >= DailyLimit)
        {
            log.LogWarning("Daily limit of {Limit} {Purpose} emails reached for an address; not sending", DailyLimit, purpose);
            return false;
        }
        sent.Set(key, true, Cooldown);
        Interlocked.Increment(ref today.Count);

        if (!options.IsConfigured)
        {
            log.LogWarning("SMTP isn't configured; email to {To} not sent: {Subject}\n{Text}", to, mail.Subject, mail.ToText());
            return true;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = mail.Subject;
        // The styled version, with the plain one for mail apps that don't show HTML. The logo travels inside the email
        // (a cid: image): mail apps don't show SVG, and Gmail drops data: URLs.
        var body = new BodyBuilder { TextBody = mail.ToText() };
        var logo = body.LinkedResources.Add("gymbook.png", Logo, new ContentType("image", "png"));
        logo.ContentId = MimeKit.Utils.MimeUtils.GenerateMessageId();
        body.HtmlBody = mail.ToHtml($"cid:{logo.ContentId}");
        message.Body = body.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(options.Host, options.Port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrEmpty(options.Username))
                await client.AuthenticateAsync(options.Username, options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch
        {
            // Didn't go out: let the user ask again straight away, and it doesn't count towards today's.
            sent.Remove(key);
            Interlocked.Decrement(ref today.Count);
            throw;
        }
        return true;
    }
}

/// <summary>How many codes went to one address for one purpose today (see <see cref="EmailSender"/>).</summary>
sealed class DailyCount
{
    public int Count;
}

/// <summary>
/// A one-time code email, in the app's look: dark background, a card, the code large in the accent colour. The
/// styles are inline and the layout is tables, since that's what mail apps reliably render.
/// </summary>
public record CodeEmail(string Subject, string Heading, string Intro, string Code, string Footer)
{
    // The app's colours (GymBook/Resources/Styles/Colors.xaml).
    const string Bg = "#0B0D12", Surface = "#151821", Surface2 = "#1D212C", Stroke = "#2C3240",
        TextPrimary = "#F4F6FB", TextSecondary = "#9AA3B5", TextTertiary = "#626B7E", Accent = "#3F7DFF";
    const string Font = "'Open Sans','Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    public string ToText() => $"{Heading}\n\n{Intro}\n\n{Code}\n\nIt's valid for 5 minutes.\n\n{Footer}\n\n- Gym Book";

    /// <summary>The styled email; <paramref name="logoSrc"/> is where the logo image is, e.g. a cid: reference to the attached one.</summary>
    public string ToHtml(string logoSrc)
    {
        static string E(string s) => System.Net.WebUtility.HtmlEncode(s);
        return $"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="color-scheme" content="dark">
            <meta name="supported-color-schemes" content="dark">
            <title>{E(Subject)}</title>
            </head>
            <body style="margin:0;padding:0;background:{Bg};">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{Bg};">
              <tr><td align="center" style="padding:32px 16px;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:480px;">
                  <tr><td style="padding:0 4px 20px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>
                      <td style="vertical-align:middle;padding-right:10px;">
                        <img src="{E(logoSrc)}" width="32" height="32" alt="" style="display:block;border:0;outline:none;width:32px;height:32px;">
                      </td>
                      <td style="vertical-align:middle;font-family:{Font};font-size:20px;font-weight:700;color:{TextPrimary};">Gym Book</td>
                    </tr></table>
                  </td></tr>
                  <tr><td style="background:{Surface};border:1px solid {Stroke};border-radius:20px;padding:28px 24px;">
                    <div style="font-family:{Font};font-size:24px;font-weight:700;color:{TextPrimary};margin:0 0 10px;">{E(Heading)}</div>
                    <div style="font-family:{Font};font-size:15px;line-height:22px;color:{TextSecondary};margin:0 0 22px;">{E(Intro)}</div>
                    <div style="background:{Surface2};border-radius:14px;padding:18px 12px;text-align:center;">
                      <div style="font-family:{Font};font-size:12px;font-weight:600;letter-spacing:1.2px;color:{TextTertiary};margin:0 0 6px;">YOUR CODE</div>
                      <div style="font-family:'SFMono-Regular',Consolas,'Courier New',monospace;font-size:34px;font-weight:700;letter-spacing:8px;color:{Accent};">{E(Code)}</div>
                    </div>
                    <div style="font-family:{Font};font-size:13px;color:{TextTertiary};margin:14px 0 0;text-align:center;">It's valid for 5 minutes.</div>
                  </td></tr>
                  <tr><td style="padding:18px 8px 0;font-family:{Font};font-size:12px;line-height:18px;color:{TextTertiary};text-align:center;">
                    {E(Footer)}
                  </td></tr>
                </table>
              </td></tr>
            </table>
            </body>
            </html>
            """;
    }
}
