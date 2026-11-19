using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IMediaAssetReader
{
    Task<ResponsiveImageDto?> GetAsync(int id, CancellationToken cancellationToken = default);
}
