using System.Text.Json;

namespace UOM.Infrastructure.Features.Public;

internal static class MediaUrlHelper
{
    public static string? ResolveUrl(string? storagePath, string? variantsJson, string variant = "medium")
    {
        if (!string.IsNullOrWhiteSpace(variantsJson))
        {
            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(variantsJson);
                if (map is not null && map.TryGetValue(variant, out var path) && !string.IsNullOrWhiteSpace(path))
                {
                    return "/" + path.TrimStart('/');
                }
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        return string.IsNullOrWhiteSpace(storagePath) ? null : "/" + storagePath.TrimStart('/');
    }
}