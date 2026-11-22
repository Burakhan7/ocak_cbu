namespace UOM.Web.Security;

public static class UploadContentPolicy
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".webp",
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".pdf",
        ".docx",
        ".pptx",
        ".xlsx",
        ".zip",
    };

    public static bool IsServable(string? requestPath)
    {
        if (string.IsNullOrEmpty(requestPath))
        {
            return false;
        }

        var path = requestPath.Replace('\\', '/');
        if (!path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return AllowedExtensions.Contains(Path.GetExtension(path));
    }
}
