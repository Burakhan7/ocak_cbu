namespace UOM.Infrastructure.Features.Public;

internal static class PublicCacheKeys
{
    public const string SiteSettings = "site-settings-public";
    public const string Redirects = "public-redirect-map";
    public const string MediaAssetPrefix = "media-asset:";
    public const string FullTextReady = "search-fts-ready";

    public static string MediaAsset(int id) => MediaAssetPrefix + id;
}
