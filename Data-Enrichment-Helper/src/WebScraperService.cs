using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Data_Enrichment_Helper;

public class WebScraperService
{
    private readonly HttpClient _http;

    private static readonly string[] CommonContactPaths =
    {
        "/contact", "/contact-us", "/contactus", "/contact_us",
        "/about", "/about-us", "/aboutus",
        "/kontakt", "/impressum",
        "/support", "/help", "/get-in-touch"
    };

    public WebScraperService(HttpClient http) => _http = http;

    public async Task<SiteResult> FindPhoneFastAsync(string seedUrl, int maxPages)
    {
        if (!seedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) seedUrl = "https://" + seedUrl;
        if (!Uri.TryCreate(seedUrl, UriKind.Absolute, out var seedUri)) throw new ArgumentException("Invalid URL");

        var baseHost = seedUri.Host.ToLowerInvariant();
        var baseRoot = $"{seedUri.Scheme}://{seedUri.Host}";
        var probeUrls = new List<string> { seedUrl };

        foreach (var p in CommonContactPaths)
        {
            if (seedUrl.TrimEnd('/').EndsWith(p, StringComparison.OrdinalIgnoreCase)) continue;
            probeUrls.Add(baseRoot + p);
        }

        int pagesTried = 0;
        (string Url, string Phones, string Emails)? firstHit = null;
        string? seedHtml = null;
        bool seedDead = false;
        var lockObj = new object();
        using var cts = new CancellationTokenSource();
        using var sem = new SemaphoreSlim(6);

        var probeTasks = probeUrls.Select(async url =>
        {
            bool isSeed = url == seedUrl;
            if (cts.IsCancellationRequested) return;
            try { await sem.WaitAsync(cts.Token).ConfigureAwait(false); } catch { return; }
            try
            {
                if (cts.IsCancellationRequested) return;
                Interlocked.Increment(ref pagesTried);
                var html = await _http.GetStringAsync(url, cts.Token).ConfigureAwait(false);
                if (isSeed) seedHtml = html;

                var phones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ExtractPhonesFromHtml(html, phones);
                ExtractEmailsFromHtml(html, emails);
                if (phones.Count > 0 || emails.Count > 0)
                {
                    lock (lockObj) { if (firstHit == null) firstHit = (url, string.Join("; ", phones), string.Join("; ", emails)); }
                    cts.Cancel();
                }
            }
            catch (OperationCanceledException) { if (isSeed && !cts.IsCancellationRequested) { seedDead = true; cts.Cancel(); } }
            catch (HttpRequestException ex) { if (isSeed && ex.StatusCode == null) { seedDead = true; cts.Cancel(); } }
            catch { }
            finally { sem.Release(); }
        }).ToList();

        try { await Task.WhenAll(probeTasks).ConfigureAwait(false); } catch { }

        if (firstHit != null) return new SiteResult(firstHit.Value.Phones, firstHit.Value.Emails, pagesTried, firstHit.Value.Url);
        if (seedDead || seedHtml == null) return new SiteResult("", "", pagesTried, "");

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var priorityQ = new Queue<string>();
        var normalQ = new Queue<string>();

        void EnqueueLinks(string html, string fromUrl)
        {
            foreach (var link in ExtractInternalLinks(html, fromUrl, baseHost))
            {
                if (!Uri.TryCreate(link, UriKind.Absolute, out var linkUri)) continue;
                if (!visited.Add(NormalizeUri(linkUri))) continue;
                if (IsContactOrAboutUrl(link)) priorityQ.Enqueue(link); else normalQ.Enqueue(link);
            }
        }

        visited.Add(NormalizeUri(seedUri));
        foreach (var p in probeUrls) if (Uri.TryCreate(p, UriKind.Absolute, out var pu)) visited.Add(NormalizeUri(pu));
        EnqueueLinks(seedHtml, seedUrl);

        while (pagesTried < maxPages && (priorityQ.Count > 0 || normalQ.Count > 0))
        {
            var batch = new List<string>();
            while (batch.Count < 6 && pagesTried + batch.Count < maxPages && (priorityQ.Count > 0 || normalQ.Count > 0))
                batch.Add(priorityQ.Count > 0 ? priorityQ.Dequeue() : normalQ.Dequeue());
            pagesTried += batch.Count;

            var fetched = await Task.WhenAll(batch.Select(async u =>
            {
                try { return (Url: u, Html: (string?)await _http.GetStringAsync(u).ConfigureAwait(false)); }
                catch { return (Url: u, Html: (string?)null); }
            })).ConfigureAwait(false);

            foreach (var (u, h) in fetched)
            {
                if (h == null) continue;
                var phones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ExtractPhonesFromHtml(h, phones);
                ExtractEmailsFromHtml(h, emails);
                if (phones.Count > 0 || emails.Count > 0) return new SiteResult(string.Join("; ", phones), string.Join("; ", emails), pagesTried, u);
                EnqueueLinks(h, u);
            }
        }
        return new SiteResult("", "", pagesTried, "");
    }

    private static void ExtractPhonesFromHtml(string html, HashSet<string> phones)
    {
        var matches = Regex.Matches(html, "href\\s*=\\s*[\"']tel:([^\"']+)[\"']", RegexOptions.IgnoreCase);
        foreach (Match m in matches)
        {
            var phone = Uri.UnescapeDataString(m.Groups[1].Value).Trim().TrimEnd('/', '.', ',', ';', ' ');
            if (!string.IsNullOrWhiteSpace(phone)) phones.Add(Regex.Replace(phone, "\\s+", " ").Trim());
        }
    }

    private static void ExtractEmailsFromHtml(string html, HashSet<string> emails)
    {
        var matches = Regex.Matches(html, "href\\s*=\\s*[\"']mailto:([^\"']+)[\"']", RegexOptions.IgnoreCase);
        foreach (Match m in matches)
        {
            var raw = System.Net.WebUtility.HtmlDecode(Uri.UnescapeDataString(m.Groups[1].Value)).Split('?')[0];
            foreach (var part in raw.Split(',', ';'))
            {
                var email = part.Trim().TrimEnd('/', '.', ' ');
                if (email.Length > 3 && email.Contains('@')) emails.Add(email.ToLowerInvariant());
            }
        }
    }

    private static IEnumerable<string> ExtractInternalLinks(string html, string currentUrl, string baseHost)
    {
        var links = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri)) return links;

        var matches = Regex.Matches(html, "href\\s*=\\s*[\"']([^\"'#]+)[\"']", RegexOptions.IgnoreCase);
        foreach (Match m in matches)
        {
            var href = m.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(href)) continue;

            var lh = href.ToLowerInvariant();
            if (lh.StartsWith("tel:") || lh.StartsWith("mailto:") || lh.StartsWith("javascript:") || lh.StartsWith("data:")) continue;
            if (href.StartsWith("#")) continue;

            if (!Uri.TryCreate(currentUri, href, out var resolved)) continue;
            if (resolved.Scheme != "http" && resolved.Scheme != "https") continue;
            if (!resolved.Host.Equals(baseHost, StringComparison.OrdinalIgnoreCase)) continue;

            var path = resolved.AbsolutePath.ToLowerInvariant();
            if (IsNonHtmlPath(path)) continue;

            links.Add(resolved.ToString());
        }
        return links;
    }

    private static bool IsNonHtmlPath(string path)
    {
        string[] exts = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".pdf", ".zip", ".rar", ".7z", ".tar", ".gz", ".mp3", ".mp4", ".avi", ".mov", ".wav", ".webm", ".css", ".js", ".xml", ".json", ".ico", ".woff", ".woff2", ".ttf", ".eot", ".otf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
        return exts.Any(e => path.EndsWith(e));
    }

    private static bool IsContactOrAboutUrl(string url)
    {
        var lower = url.ToLowerInvariant();
        return lower.Contains("contact") || lower.Contains("about") || lower.Contains("reach-us") || lower.Contains("get-in-touch") || lower.Contains("support") || lower.Contains("help") || lower.Contains("kontakt") || lower.Contains("impressum");
    }

    private static string NormalizeUri(Uri uri)
    {
        var path = uri.AbsolutePath.ToLowerInvariant();
        if (path.Length > 1 && path.EndsWith("/")) path = path.TrimEnd('/');
        return uri.Host.ToLowerInvariant() + path;
    }
}