using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IHomeFeedService
{
    Task<IReadOnlyList<HomeFeedCardDto>> GetNewsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HomeFeedCardDto>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HomeFeedCardDto>> GetShowcaseAsync(CancellationToken cancellationToken = default);

    Task<HomeFeedCardDto?> GetHeroAsync(CancellationToken cancellationToken = default);
}
