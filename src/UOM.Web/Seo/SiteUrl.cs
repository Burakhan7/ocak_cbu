using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace UOM.Web.Seo;

public static class SiteUrl
{
    public static string Base(HttpRequest request, IConfiguration configuration)
    {
        var configured = configuration["Site:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        // TODO(spec): alan adı henüz belli değil (ARCHITECTURE §15 madde 2). İstek host'u kullanılır.
        return $"{request.Scheme}://{request.Host}";
    }

    public static string Absolute(HttpRequest request, IConfiguration configuration, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Base(request, configuration);
        }

        if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var normalized = path.StartsWith('/') ? path : "/" + path;
        return Base(request, configuration) + normalized;
    }
}
