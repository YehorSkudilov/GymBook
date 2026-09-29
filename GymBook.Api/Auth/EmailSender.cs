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
    public string FromName { get; set; } = "GymBook";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>
/// Emails the one-time codes for password reset and email change. Without SMTP settings, Development writes the
/// mail to the log instead so the flows can be tried locally; anywhere else email features report unavailable.
/// </summary>
public class EmailSender(SmtpOptions options, IHostEnvironment env, IMemoryCache sent, ILogger<EmailSender> log)
{
    // One code per address and purpose a minute, so nobody can flood someone's inbox through us.
    static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);

    public bool IsAvailable => options.IsConfigured || env.IsDevelopment();

    /// <summary>Sends a code. False (and nothing sent) when one went to this address for this purpose within the last minute.</summary>
    public async Task<bool> SendCodeAsync(string to, string purpose, CodeEmail mail, CancellationToken ct)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Smtp is not configured.");
        var key = $"email-code:{purpose}:{to.ToUpperInvariant()}";
        if (sent.TryGetValue(key, out _))
            return false;
        sent.Set(key, true, Cooldown);

        if (!options.IsConfigured)
        {
            log.LogWarning("SMTP isn't configured; email to {To} not sent: {Subject}\n{Text}", to, mail.Subject, mail.ToText());
            return true;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = mail.Subject;
        // The styled version, with the plain one for mail apps that don't show HTML.
        message.Body = new BodyBuilder { TextBody = mail.ToText(), HtmlBody = mail.ToHtml() }.ToMessageBody();

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
            // Didn't go out: let the user ask again straight away.
            sent.Remove(key);
            throw;
        }
        return true;
    }
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

    public string ToText() => $"{Heading}\n\n{Intro}\n\n{Code}\n\nIt's valid for 5 minutes.\n\n{Footer}\n\n- GymBook";

    public string ToHtml()
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
                  <tr><td style="padding:0 4px 20px;font-family:{Font};font-size:20px;font-weight:700;color:{TextPrimary};">
                    <span style="color:{Accent};">&#9632;</span>&nbsp;GymBook
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
