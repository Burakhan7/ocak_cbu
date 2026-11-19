using System.Text.Json;

namespace UOM.Infrastructure.Features.Public;

public static class MediaUrlHelper
{
    /// <summary>
    /// URL path segment under wwwroot where <see cref="LocalFileStorage"/> writes files
    /// (see Storage:LocalRoot, default "wwwroot/uploads").
    /// </summary>
    public const string PublicUploadsPath = "uploads";

    public static string? ResolveUrl(string? storagePath, string? variantsJson, string variant = "medium")
    {
        if (!string.IsNullOrWhiteSpace(variantsJson))
        {
            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(variantsJson);
                if (map is not null && map.TryGetValue(variant, out var path) && !string.IsNullOrWhiteSpace(path))
                {
                    return ToPublicUrl(path);
                }
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        return ToPublicUrl(storagePath);
    }

    public static string? ToPublicUrl(string? relativeStoragePath)
    {
        if (string.IsNullOrWhiteSpace(relativeStoragePath))
        {
            return null;
        }

        var normalized = relativeStoragePath.TrimStart('/').Replace('\\', '/');
        if (normalized.StartsWith(PublicUploadsPath + "/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, PublicUploadsPath, StringComparison.OrdinalIgnoreCase))
        {
            return "/" + normalized;
        }

        return "/" + PublicUploadsPath + "/" + normalized;
    }
}