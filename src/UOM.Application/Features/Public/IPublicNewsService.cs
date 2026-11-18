using UOM.Application.Common;
using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicNewsService
{
    Task<PagedResult<NewsListItemDto>> ListAsync(NewsListQuery query, CancellationToken cancellationToken = default);

    Task<NewsDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsListItemDto>> GetFeaturedForHomeAsync(int featuredCount, int regularCount, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsListItemDto>> GetRelatedAsync(int categoryId, int excludeId, int count, CancellationToken cancellationToken = default);
}