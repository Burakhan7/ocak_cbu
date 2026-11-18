using UOM.Application.Common;
using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicEventService
{
    Task<PagedResult<EventListItemDto>> ListAsync(EventListQuery query, CancellationToken cancellationToken = default);

    Task<EventDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<EventDetailDto?> GetNextUpcomingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventListItemDto>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default);

    Task<byte[]> BuildCalendarFeedAsync(CancellationToken cancellationToken = default);

    Task<byte[]> BuildSingleEventIcsAsync(string slug, CancellationToken cancellationToken = default);
}