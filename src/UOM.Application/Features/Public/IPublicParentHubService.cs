using UOM.Application.Common;
using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicParentHubService
{
    Task<ParentHubPageDto> GetHubPageAsync(int newsPage, int newsPageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParentNewsCardDto>> GetNewsStripForHomeAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParentProjectCardDto>> GetActiveProjectsAsync(CancellationToken cancellationToken = default);

    Task<MagazineVideoHomeDto> GetMagazineAndFeaturedVideoForHomeAsync(CancellationToken cancellationToken = default);
}