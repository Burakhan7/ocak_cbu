using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicCampusHubService(
    AppDbContext db,
    IClock clock,
    ISiteSettingsProvider siteSettings) : IPublicCampusHubService
{
    public async Task<CampusHubDto> GetHubAsync(CancellationToken cancellationToken = default)
    {
        var settings = await siteSettings.GetAsync(cancellationToken);
        var now = clock.UtcNow;
        var weekAhead = now.AddDays(7);

        var newStudentGuides = await db.GuideArticles
            .AsNoTracking()
            .WherePubliclyVisible(clock)
            .Where(g => g.Category == GuideCategory.YeniOgrenci)
            .OrderBy(g => g.Order)
            .ThenBy(g => g.Title)
            .Select(g => new GuideListItemDto(g.Title, g.Slug, g.Summary, g.Category, g.LastReviewedAtUtc))
            .ToListAsync(cancellationToken);

        var campusEvents = await db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.CoverImage)
            .WherePubliclyVisible(clock)
            .Where(e => e.Audience == EventAudience.CbuStudents && e.StartsAtUtc >= now && e.StartsAtUtc <= weekAhead)
            .OrderBy(e => e.StartsAtUtc)
            .Take(6)
            .ToListAsync(cancellationToken);

        var eventDtos = campusEvents.Select(e => new EventListItemDto(
            e.Id,
            e.Title,
            e.Slug,
            e.Summary,
            e.StartsAtUtc,
            e.EndsAtUtc,
            e.Venue?.Name,
            MediaUrlHelper.ResolveUrl(e.CoverImage?.StoragePath, e.CoverImage?.Variants))).ToList();

        var announcements = await db.Announcements
            .AsNoTracking()
            .WhereListableAnnouncements(clock)
            .OrderByDescending(a => a.IsPinned)
            .ThenBy(a => a.DeadlineUtc ?? a.ExpiresAtUtc)
            .Take(6)
            .ToListAsync(cancellationToken);

        var announcementDtos = announcements.Select(a => PublicAnnouncementService.MapListItem(a, now)).ToList();

        var resourceCount = await db.Resources.WherePubliclyVisible(clock).CountAsync(cancellationToken);

        var campuses = await db.Campuses
            .AsNoTracking()
            .Where(c => c.Latitude != null && c.Longitude != null)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var campusPins = campuses
            .Select(c => new CampusMapPinDto(
                c.Name,
                c.ShortName,
                c.Latitude!.Value,
                c.Longitude!.Value,
                GeoDistance.KilometersBetween(settings.MapLat, settings.MapLng, c.Latitude!.Value, c.Longitude!.Value)))
            .ToList();

        return new CampusHubDto(
            settings.Tagline,
            newStudentGuides,
            eventDtos,
            announcementDtos,
            resourceCount,
            campusPins,
            settings.MapLat,
            settings.MapLng,
            settings.WhatsAppChannelUrl);
    }
}