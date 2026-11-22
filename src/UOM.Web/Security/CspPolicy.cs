namespace UOM.Web.Security;

public static class CspPolicy
{
    public static string Build(string nonce, bool upgradeInsecureRequests)
    {
        // i.ytimg.com: youtube-lite kapak görseli. İframe kaynağı spec'teki youtube-nocookie.com.
        var policy = string.Join(
            "; ",
            "default-src 'self'",
            $"script-src 'self' 'nonce-{nonce}' https://challenges.cloudflare.com",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data: blob: https://*.tile.openstreetmap.org https://i.ytimg.com",
            "font-src 'self'",
            "connect-src 'self' https://*.tile.openstreetmap.org https://challenges.cloudflare.com",
            "frame-src 'self' https://www.youtube-nocookie.com https://challenges.cloudflare.com",
            "media-src 'self'",
            "worker-src 'self' blob:",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");

        if (upgradeInsecureRequests)
        {
            policy += "; upgrade-insecure-requests";
        }

        return policy;
    }
}
