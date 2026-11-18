using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicAnnouncementService(AppDbContext db, IClock clock) : IPublicAnnouncementService
{
    public async Task<IReadOnlyList<AnnouncementListItemDto>> ListListableAsync(
        AnnouncementType? type,
        CancellationToken cancellationToken = default)
    {
        var query = db.Announcements.AsNoTracking().WhereListableAnnouncements(clock);
        if (type is { } t)
        {
            query = query.Where(a => a.Type == t);
        }

        var now = clock.UtcNow;
        var items = await query
            .OrderByDescending(a => a.IsPinned)
            .ThenBy(a => a.DeadlineUtc ?? a.ExpiresAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(a => MapListItem(a, now)).ToList();
    }

    public async Task<AnnouncementListItemDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var item = await db.Announcements
            .AsNoTracking()
            .WhereListableAnnouncements(clock)
            .FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);

        return item is null ? null : MapListItem(item, clock.UtcNow);
    }

    internal static AnnouncementListItemDto MapListItem(Announcement a, DateTime utcNow)
    {
        var urgent = a.DeadlineUtc is { } deadline && deadline > utcNow && (deadline - utcNow).TotalDays < 3;
        return new AnnouncementListItemDto(
            a.Id,
            a.Title,
            a.Slug,
            a.Body,
            a.Type,
            a.DeadlineUtc,
            a.ExpiresAtUtc,
            a.ExternalUrl,
            a.IsPinned,
            urgent);
    }
}