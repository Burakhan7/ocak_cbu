using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using UOM.Application.Features.Public;
using UOM.Application.Formatting;

namespace UOM.Infrastructure.Features.Public;

public static partial class ParentSiteListingParser
{
    public const int MaxItems = 24;

    public static IReadOnlyList<ParentSiteItem> ParseListingHtml(string? html, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(html) || !html.Contains("blogs-item", StringComparison.Ordinal))
        {
            return [];
        }

        try
        {
            var origin = OriginOf(pageUrl);
            var parts = html.Split("blogs-item", StringSplitOptions.None);
            var items = new List<ParentSiteItem>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 1; i < parts.Length && items.Count < MaxItems; i++)
            {
                var chunk = parts[i].Length > 12000 ? parts[i][..12000] : parts[i];
                var href = FirstBlogHref(chunk) ?? FirstHref(chunk);
                var title = InnerText(H5Pattern().Match(chunk));
                var summary = InnerText(DescPattern().Match(chunk));
                var image = FirstContentImage(chunk);
                var item = Build(title, href, summary, image, ParentSiteText.DateFromImageUrl(image), origin);
                if (item is null)
                {
                    continue;
                }

                var key = ParentSiteText.NormalizeUrl(item.ExternalUrl);
                if (key is null || !seen.Add(key))
                {
                    continue;
                }

                items.Add(item);
            }

            return items;
        }
        catch (RegexMatchTimeoutException)
        {
            return [];
        }
    }

    public static IReadOnlyList<ParentSiteItem> ParseFeedXml(string? xml, string feedUrl)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return [];
        }

        var head = xml.Length > 800 ? xml[..800] : xml;
        if (!head.Contains("<rss", StringComparison.OrdinalIgnoreCase)
            && !head.Contains("<feed", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        try
        {
            var origin = OriginOf(feedUrl);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                MaxCharactersFromEntities = 4096,
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            var items = new List<ParentSiteItem>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string? title = null;
            string? link = null;
            string? summary = null;
            string? image = null;
            string? dateText = null;
            var inItem = false;

            void Flush()
            {
                if (!inItem || items.Count >= MaxItems)
                {
                    return;
                }

                var item = Build(title, link, summary, image, ParseFeedDate(dateText), origin);
                if (item is not null)
                {
                    var key = ParentSiteText.NormalizeUrl(item.ExternalUrl);
                    if (key is not null && seen.Add(key))
                    {
                        items.Add(item);
                    }
                }
            }

            while (reader.Read())
            {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName is "item" or "entry")
            {
                if (inItem)
                {
                    Flush();
                }

                inItem = true;
                title = link = summary = image = dateText = null;
                continue;
            }

                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName is "item" or "entry")
                {
                    Flush();
                    inItem = false;
                    continue;
                }

                if (!inItem || reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                var name = reader.LocalName;
                if (name is "enclosure" or "thumbnail")
                {
                    image ??= reader.GetAttribute("url")?.Trim();
                    if (!reader.IsEmptyElement)
                    {
                        SkipRestOfElement(reader);
                    }

                    continue;
                }

                if (name == "content")
                {
                    var mediaUrl = reader.GetAttribute("url");
                    if (!string.IsNullOrWhiteSpace(mediaUrl))
                    {
                        image ??= mediaUrl.Trim();
                        if (!reader.IsEmptyElement)
                        {
                            SkipRestOfElement(reader);
                        }

                        continue;
                    }

                    if (!reader.IsEmptyElement)
                    {
                        summary ??= ReadInnerText(reader);
                    }

                    continue;
                }

                if (name == "link")
                {
                    var href = reader.GetAttribute("href");
                    var rel = reader.GetAttribute("rel");
                    if (!string.IsNullOrWhiteSpace(href) && rel is null or "" or "alternate")
                    {
                        link ??= href.Trim();
                    }

                    if (!reader.IsEmptyElement)
                    {
                        var text = ReadInnerText(reader).Trim();
                        if (text.Length > 0)
                        {
                            link ??= text;
                        }
                    }

                    continue;
                }

                if (reader.IsEmptyElement)
                {
                    continue;
                }

                if (name == "title")
                {
                    title ??= ReadInnerText(reader);
                }
                else if (name is "description" or "summary")
                {
                    summary ??= ReadInnerText(reader);
                }
                else if (name is "pubDate" or "published" or "updated" or "date")
                {
                    dateText ??= ReadInnerText(reader);
                }
            }

            Flush();
            return items;
        }
        catch (XmlException)
        {
            return [];
        }
        catch (RegexMatchTimeoutException)
        {
            return [];
        }
    }

    public static string? ParseDetailImage(string? html, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        try
        {
            var origin = OriginOf(pageUrl);
            var raw = Attribute(OgImagePattern().Match(html)) ?? Attribute(OgImageContentFirstPattern().Match(html));
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var decoded = WebUtility.HtmlDecode(raw.Trim());
            if (IsDecorative(decoded))
            {
                return null;
            }

            var absolute = Absolute(decoded, origin) ?? decoded;
            return ParentSiteText.SafeImageUrl(absolute);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static ParentSiteItem? Build(
        string? title,
        string? href,
        string? summaryRaw,
        string? imageRaw,
        DateOnly? publishedDate,
        string origin)
    {
        var cleanTitle = ParentSiteText.Plain(title);
        if (string.IsNullOrWhiteSpace(cleanTitle))
        {
            return null;
        }

        if (cleanTitle.Length > 300)
        {
            cleanTitle = cleanTitle[..300].TrimEnd() + "\u2026";
        }

        var url = Absolute(href, origin);
        if (url is null)
        {
            return null;
        }

        var summary = ParentSiteText.Summarize(summaryRaw);
        var image = Absolute(imageRaw, origin) ?? imageRaw?.Trim();
        if (image is not null && IsDecorative(image))
        {
            image = null;
        }

        var kind = ParentSiteText.IsEvent(cleanTitle, summary)
            ? ParentSiteItemKind.Event
            : ParentSiteItemKind.News;

        return new ParentSiteItem(cleanTitle, summary, ParentSiteText.SafeImageUrl(image), url, publishedDate, kind);
    }

    private static string? Absolute(string? href, string origin)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        var trimmed = href.Trim();
        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            trimmed = "https:" + trimmed;
        }
        else if (trimmed.StartsWith('/'))
        {
            trimmed = origin.TrimEnd('/') + trimmed;
        }

        return ParentSiteText.IsHttp(trimmed) ? trimmed : null;
    }

    private static string OriginOf(string pageUrl)
    {
        if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
        {
            return uri.GetLeftPart(UriPartial.Authority);
        }

        return pageUrl.TrimEnd('/');
    }

    private static DateOnly? ParseFeedDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            && !DateTimeOffset.TryParse(text.Trim(), CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out parsed))
        {
            return null;
        }

        return DateOnly.FromDateTime(parsed.UtcDateTime.ToIstanbul());
    }

    private static string ReadInnerText(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return string.Empty;
        }

        var depth = reader.Depth;
        var text = new System.Text.StringBuilder();
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
            {
                text.Append(reader.Value);
            }
        }

        return text.ToString();
    }

    private static void SkipRestOfElement(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return;
        }

        var depth = reader.Depth;
        while (reader.Read() && reader.Depth > depth)
        {
        }
    }

    private static string? FirstContentImage(string chunk)
    {
        var background = Attribute(ImagePattern().Match(chunk));
        if (!string.IsNullOrWhiteSpace(background) && !IsDecorative(background))
        {
            return background;
        }

        foreach (Match match in ImgSrcPattern().Matches(chunk))
        {
            var src = match.Groups[1].Value.Trim();
            if (!IsDecorative(src))
            {
                return src;
            }
        }

        return null;
    }

    private static bool IsDecorative(string url)
    {
        var lower = url.ToLowerInvariant();
        return lower.Contains("/theme/", StringComparison.Ordinal)
            || lower.Contains("preloader", StringComparison.Ordinal)
            || lower.Contains("logo", StringComparison.Ordinal)
            || lower.Contains("data:image", StringComparison.Ordinal)
            || lower.EndsWith(".svg", StringComparison.Ordinal);
    }

    private static string? InnerText(Match match) =>
        match.Success ? match.Groups[1].Value : null;

    private static string? Attribute(Match match) =>
        match.Success ? match.Groups[1].Value : null;

    private static string? FirstBlogHref(string chunk)
    {
        foreach (Match match in HrefPattern().Matches(chunk))
        {
            var href = match.Groups[1].Value;
            if (href.Contains("/blog/i/", StringComparison.OrdinalIgnoreCase))
            {
                return href;
            }
        }

        return null;
    }

    private static string? FirstHref(string chunk)
    {
        var match = HrefPattern().Match(chunk);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex("<h5\\b[^>]*>([\\s\\S]*?)</h5>", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex H5Pattern();

    [GeneratedRegex("<div\\s+class=\"blogs-desc\"[^>]*>([\\s\\S]*?)</div>", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex DescPattern();

    [GeneratedRegex("data-background-image\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex ImagePattern();

    [GeneratedRegex("\\b(?:data-src|src)\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex ImgSrcPattern();

    [GeneratedRegex("<meta\\b[^>]*\\bproperty\\s*=\\s*[\"']og:image[\"'][^>]*\\bcontent\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex OgImagePattern();

    [GeneratedRegex("<meta\\b[^>]*\\bcontent\\s*=\\s*[\"']([^\"']+)[\"'][^>]*\\bproperty\\s*=\\s*[\"']og:image[\"']", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex OgImageContentFirstPattern();

    [GeneratedRegex("href\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex HrefPattern();
}
