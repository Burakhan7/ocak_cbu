using UOM.Application.Features.Public.Models;

namespace UOM.Web.Models;

public sealed class PublicLayoutModel
{
    public SiteSettingsDto Site { get; init; } = new(
        "Ülkü Ocakları Manisa", null, null, null, null, null, null, null, null, false);

    public IReadOnlyList<StaticPageLinkDto> FooterPages { get; init; } = [];
}