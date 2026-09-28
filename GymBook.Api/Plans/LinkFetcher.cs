using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace GymBook.Api.Plans;

/// <summary>What a link to import pointed at: text (a page or text file), or the bytes of an image or PDF.</summary>
public record FetchedLink(string ContentType, string? Text, byte[]? Bytes);

/// <summary>
/// Downloads a link the user wants to import a plan from. The link comes from the user, so this guards against it
/// being used to reach the server's own network: only http(s), only public addresses (checked on every connection,
/// redirects included), a size cap and a short timeout.
/// </summary>
public partial class LinkFetcher(HttpClient http, ILogger<LinkFetcher> logger)
{
    public const int MaxBytes = 5 * 1024 * 1024;

    /// <summary>The handler for this client: refuses connections to private, loopback and link-local addresses.</summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        // A proxy would make the address check apply to the proxy instead of the site.
        UseProxy = false,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 3,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        ConnectCallback = async (context, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            var address = addresses.FirstOrDefault(IsPublic)
                ?? throw new HttpRequestException($"{context.DnsEndPoint.Host} isn't a public address.");
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };

    static bool IsPublic(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
            return false;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal || ip.IsIPv6Multicast);
        var b = ip.GetAddressBytes();
        return !(b[0] == 10
            || b[0] == 127
            || b[0] == 0
            || b[0] >= 224                                // multicast and reserved
            || b[0] == 169 && b[1] == 254                 // link-local, incl. cloud metadata (169.254.169.254)
            || b[0] == 172 && b[1] >= 16 && b[1] <= 31
            || b[0] == 192 && b[1] == 168
            || b[0] == 100 && b[1] >= 64 && b[1] <= 127); // carrier-grade NAT
    }

    public async Task<FetchedLink> FetchAsync(string link, CancellationToken ct)
    {
        if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
            throw new PlanGenerationException("The link must be a web address (https://…).");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("GymBook", "1.0"));
        request.Headers.Accept.ParseAdd("text/html, text/plain, image/*, application/pdf;q=0.9, */*;q=0.5");
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogInformation(e, "Couldn't fetch the import link {Link}", uri);
            throw new PlanGenerationException("Couldn't open that link. Check it works in a browser, or paste the plan as text.", e);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new PlanGenerationException($"That link didn't open (the site answered {(int)response.StatusCode}). Try pasting the plan as text.");
            if (response.Content.Headers.ContentLength > MaxBytes)
                throw new PlanGenerationException("That link is too big to import. Try pasting the plan as text.");

            var bytes = await ReadCappedAsync(response.Content, ct);
            var type = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "application/octet-stream";
            if (type.StartsWith("image/") || type == "application/pdf")
                return new FetchedLink(type, null, bytes);

            var charset = response.Content.Headers.ContentType?.CharSet;
            var encoding = charset != null ? SafeEncoding(charset) : Encoding.UTF8;
            var text = encoding.GetString(bytes);
            if (type.Contains("html") || text.TrimStart().StartsWith('<'))
                text = HtmlToText(text);
            return new FetchedLink("text/plain", text, null);
        }
    }

    static async Task<byte[]> ReadCappedAsync(HttpContent content, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxBytes)
                throw new PlanGenerationException("That link is too big to import. Try pasting the plan as text.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    static Encoding SafeEncoding(string charset)
    {
        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>The readable text of a page: scripts, styles and tags dropped, block elements as line breaks.</summary>
    static string HtmlToText(string html)
    {
        var text = NonContent().Replace(html, " ");
        text = BlockTag().Replace(text, "\n");
        text = AnyTag().Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        text = Spaces().Replace(text, " ");
        text = BlankLines().Replace(text, "\n");
        return text.Trim();
    }

    [GeneratedRegex(@"<(script|style|noscript|svg|head)\b.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NonContent();
    [GeneratedRegex(@"<\s*(br|/p|/div|/li|/tr|/h[1-6]|/table|/section|/article)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockTag();
    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTag();
    [GeneratedRegex(@"[ \t\f\v ]+")]
    private static partial Regex Spaces();
    [GeneratedRegex(@"\s*\n\s*")]
    private static partial Regex BlankLines();
}
