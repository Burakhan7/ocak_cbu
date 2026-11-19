using System.Text.RegularExpressions;

namespace UOM.Infrastructure.Features.Public;

internal static partial class SearchText
{
    public static string? FullTextCondition(string query)
    {
        var words = Word().Matches(query)
            .Select(match => match.Value)
            .Where(word => word.Length > 1)
            .Take(8)
            .ToArray();

        if (words.Length == 0)
        {
            return null;
        }

        return string.Join(" AND ", words.Select(word => $"\"{word}*\""));
    }

    public static string LikePattern(string query)
    {
        var trimmed = query.Trim();
        if (trimmed.Length > 200)
        {
            trimmed = trimmed[..200];
        }

        var escaped = trimmed
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
        return "%" + escaped + "%";
    }

    public static string? Excerpt(string? summary, string? body, int max = 180)
    {
        var source = !string.IsNullOrWhiteSpace(summary) ? summary : body;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var text = Tags().Replace(source, " ");
        text = Whitespace().Replace(text, " ").Trim();
        if (text.Length <= max)
        {
            return text;
        }

        return text[..max].TrimEnd() + "…";
    }

    [GeneratedRegex(@"[\p{L}\p{Nd}]+", RegexOptions.CultureInvariant)]
    private static partial Regex Word();

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
