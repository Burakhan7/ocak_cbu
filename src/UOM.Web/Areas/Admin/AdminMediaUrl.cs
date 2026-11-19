using UOM.Infrastructure.Features.Public;

namespace UOM.Web.Areas.Admin;

public static class AdminMediaUrl
{
    public static string? Resolve(string? storagePath, string? variantsJson, string variant = "medium") =>
        MediaUrlHelper.ResolveUrl(storagePath, variantsJson, variant);
}