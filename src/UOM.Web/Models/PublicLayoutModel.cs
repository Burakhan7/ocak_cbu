using UOM.Application.Features.Public.Models;

namespace UOM.Web.Models;

public sealed class PublicLayoutModel
{
    public SiteSettingsDto Site { get; init; } = new(
        "Ülkü Ocakları Manisa", null, null, null, null, null, null, null, null, false, null);

    public IReadOnlyList<StaticPageLinkDto> FooterPages { get; init; } = [];

    public bool SearchEnabled { get; init; } = true;
}