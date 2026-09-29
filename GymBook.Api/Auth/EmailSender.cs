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
    public async Task<bool> SendCodeAsync(string to, string purpose, string subject, string text, CancellationToken ct)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Smtp is not configured.");
        var key = $"email-code:{purpose}:{to.ToUpperInvariant()}";
        if (sent.TryGetValue(key, out _))
            return false;
        sent.Set(key, true, Cooldown);

        if (!options.IsConfigured)
        {
            log.LogWarning("SMTP isn't configured; email to {To} not sent: {Subject}\n{Text}", to, subject, text);
            return true;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = text };

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
