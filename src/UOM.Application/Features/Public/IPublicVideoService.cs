using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicVideoService
{
    Task<IReadOnlyList<VideoListItemDto>> ListAsync(CancellationToken cancellationToken = default);
}