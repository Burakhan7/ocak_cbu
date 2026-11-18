using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;

namespace UOM.Application.Features.Public;

public interface IPublicGuideService
{
    Task<IReadOnlyList<GuideListItemDto>> ListPublishedAsync(GuideCategory? category, CancellationToken cancellationToken = default);

    Task<GuideDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}