using UOM.Application.Common;
using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicResourceService
{
    Task<PagedResult<ResourceListItemDto>> ListAsync(ResourceListQuery query, CancellationToken cancellationToken = default);

    Task<ResourceDownloadDto?> GetForDownloadAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> TryIncrementDownloadCountAsync(int id, CancellationToken cancellationToken = default);
}