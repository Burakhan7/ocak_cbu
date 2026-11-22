using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace UOM.Application.Features.Public;

public static partial class ParentSiteText
{
    public const int SummaryMaxLength = 220;

    private static readonly string[] EventTokens =
    [
        "etkinlik",
        "şölen",
        "turnuva",
        "seminer",
        "tören",
        "şenlik",
        "konferans",
        "yürüyüş",
        "kampı",
        "buluşma",
        "programına",
        "programina",
        "finaline",
    ];

    public static string? Plain(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var withoutBlocks = ScriptOrStylePattern().Replace(raw, " ");
        var withoutTags = TagPattern().Replace(withoutBlocks, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var collapsed = WhitespacePattern().Replace(decoded, " ").Trim();
        return collapsed.Length == 0 ? null : collapsed;
    }

    public static string? Summarize(string? raw)
    {
        var collapsed = Plain(raw);
        if (collapsed is null)
        {
            return null;
        }

        if (collapsed.Length <= SummaryMaxLength)
        {
            return collapsed;
        }

        var cut = collapsed.LastIndexOf(' ', SummaryMaxLength);
        if (cut < 80)
        {
            cut = SummaryMaxLength;
        }

        return collapsed[..cut].TrimEnd() + "\u2026";
    }

    public static bool IsEvent(string? title, string? summary)
    {
        var haystack = ((title ?? string.Empty) + " " + (summary ?? string.Empty)).ToLower(CultureInfo.GetCultureInfo("tr-TR"));
        foreach (var token in EventTokens)
        {
            if (haystack.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsHttp(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    public static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !IsHttp(url))
        {
            return null;
        }

        var uri = new Uri(url.Trim());
        var path = uri.AbsolutePath.TrimEnd('/');
        var query = string.IsNullOrEmpty(uri.Query) ? string.Empty : uri.Query;
        return $"{uri.Scheme.ToLowerInvariant()}://{uri.IdnHost.ToLowerInvariant()}{path}{query}";
    }

    public static string? SafeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith('/'))
        {
            return trimmed.StartsWith("//", StringComparison.Ordinal) ? null : trimmed;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? trimmed
            : null;
    }

    public static DateOnly? DateFromImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var match = ImageDatePattern().Match(url);
        if (!match.Success)
        {
            return null;
        }

        var text = match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value;
        return DateOnly.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    [GeneratedRegex("<script\\b[\\s\\S]*?</script>|<style\\b[\\s\\S]*?</style>", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex ScriptOrStylePattern();

    [GeneratedRegex("<[^>]+>", RegexOptions.None, 1000)]
    private static partial Regex TagPattern();

    [GeneratedRegex("\\s+", RegexOptions.None, 1000)]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex("(?:^|/)(\\d{4})(\\d{2})(\\d{2})\\d{6}", RegexOptions.None, 1000)]
    private static partial Regex ImageDatePattern();
}
