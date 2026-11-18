namespace UOM.Application.Features.Public;

public static class ReservedPublicSlugs
{
    public static readonly HashSet<string> Routes = new(StringComparer.OrdinalIgnoreCase)
    {
        "cbu", "duyurular", "haberler", "etkinlikler", "genel-merkez", "teskilat",
        "galeri", "videolar", "iletisim", "ara", "sitemap.xml", "robots.txt", "feed.xml",
        "health", "yonetim", "onizleme", "home", "contact", "news", "event",
    };

    public static bool IsReserved(string slug) => Routes.Contains(slug);
}