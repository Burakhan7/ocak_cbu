using Microsoft.EntityFrameworkCore;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicVideoService(AppDbContext db) : IPublicVideoService
{
    public async Task<IReadOnlyList<VideoListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Videos
            .AsNoTracking()
            .OrderByDescending(v => v.IsFeatured)
            .ThenBy(v => v.Order)
            .ThenByDescending(v => v.PublishedDate)
            .Select(v => new VideoListItemDto(v.Id, v.Title, v.YouTubeVideoId, v.Source, v.PublishedDate, v.IsFeatured))
            .ToListAsync(cancellationToken);
    }
}