using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicPageService
{
    Task<StaticPageDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaticPageLinkDto>> GetFooterPagesAsync(CancellationToken cancellationToken = default);

    Task<RedirectDto?> FindRedirectAsync(string path, CancellationToken cancellationToken = default);
}