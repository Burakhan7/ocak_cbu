namespace UOM.Web.Security;

public static class CspPolicy
{
    public static string Build(string nonce, bool upgradeInsecureRequests)
    {
        // i.ytimg.com: youtube-lite kapak görseli. İframe kaynağı spec'teki youtube-nocookie.com.
        // Google Maps JavaScript API (v=weekly, loading=async) is loaded from maps.googleapis.com
        // and maps.gstatic.com. That loader does not use eval, so script-src stays nonce-only for
        // our scripts and does not add 'unsafe-eval'. style-src already allows 'unsafe-inline'
        // because Razor style attributes and Maps' injected element styles need it.
        // Tiles are not scripts. Raster imagery (renderingType RASTER, no unsafe-eval) is fetched
        // from *.google.com (mt0.google.com, maps.google.com), *.googleapis.com, *.gstatic.com,
        // *.googleusercontent.com and *.ggpht.com. Blocking those hosts leaves the canvas blank.
        // ulkuocaklari.org.tr / .gov.tr: parent listing card images only.
        var policy = string.Join(
            "; ",
            "default-src 'self'",
            $"script-src 'self' 'nonce-{nonce}' https://challenges.cloudflare.com https://maps.googleapis.com https://maps.gstatic.com",
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
            "img-src 'self' data: blob: https://maps.googleapis.com https://maps.gstatic.com https://*.googleapis.com https://*.gstatic.com https://*.google.com https://*.googleusercontent.com https://*.ggpht.com https://i.ytimg.com https://ulkuocaklari.org.tr https://ulkuocaklari.gov.tr",
            "font-src 'self' https://fonts.gstatic.com",
            "connect-src 'self' https://maps.googleapis.com https://maps.gstatic.com https://*.googleapis.com https://*.gstatic.com https://*.google.com https://*.googleusercontent.com https://*.ggpht.com data: blob: https://challenges.cloudflare.com",
            "frame-src 'self' https://*.google.com https://www.youtube-nocookie.com https://challenges.cloudflare.com",
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
