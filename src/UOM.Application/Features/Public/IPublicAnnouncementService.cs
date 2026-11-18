using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;

namespace UOM.Application.Features.Public;

public interface IPublicAnnouncementService
{
    Task<IReadOnlyList<AnnouncementListItemDto>> ListListableAsync(AnnouncementType? type, CancellationToken cancellationToken = default);

    Task<AnnouncementListItemDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}