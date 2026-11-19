namespace UOM.Infrastructure.Features.Public;

internal static class RedirectPath
{
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var trimmed = path.Trim();
        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        var hash = trimmed.IndexOf('#');
        if (hash >= 0)
        {
            trimmed = trimmed[..hash];
        }

        var query = trimmed.IndexOf('?');
        var suffix = string.Empty;
        if (query >= 0)
        {
            suffix = trimmed[query..];
            trimmed = trimmed[..query];
        }

        if (trimmed.Length > 1)
        {
            trimmed = trimmed.TrimEnd('/');
        }

        return trimmed + suffix;
    }

    public static bool IsLocalPath(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.StartsWith('/'))
        {
            return false;
        }

        if (normalized.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        return !normalized.Contains("://", StringComparison.Ordinal);
    }
}
