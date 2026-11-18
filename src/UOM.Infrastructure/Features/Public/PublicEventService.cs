using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicEventService : IPublicEventService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IConfiguration _configuration;
    private readonly ISiteSettingsProvider _siteSettings;

    public PublicEventService(
        AppDbContext db,
        IClock clock,
        IHtmlSanitizer htmlSanitizer,
        IConfiguration configuration,
        ISiteSettingsProvider siteSettings)
    {
        _db = db;
        _clock = clock;
        _htmlSanitizer = htmlSanitizer;
        _configuration = configuration;
        _siteSettings = siteSettings;
    }

    public async Task<PagedResult<EventListItemDto>> ListAsync(EventListQuery query, CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.CoverImage)
            .WherePubliclyVisible(_clock);

        var now = _clock.UtcNow;
        if (query.UpcomingOnly)
        {
            baseQuery = baseQuery.Where(e => e.StartsAtUtc >= now);
        }

        var ordered = query.UpcomingOnly
            ? baseQuery.OrderBy(e => e.StartsAtUtc)
            : baseQuery.OrderByDescending(e => e.StartsAtUtc);

        var total = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip(Math.Max(0, query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventListItemDto>
        {
            Items = items.Select(MapList).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
        };
    }

    public async Task<EventDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var evt = await _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.CoverImage)
            .WherePubliclyVisible(_clock)
            .FirstOrDefaultAsync(e => e.Slug == slug, cancellationToken);

        if (evt is null)
        {
            return null;
        }

        var settings = await _siteSettings.GetAsync(cancellationToken);
        return MapDetail(evt, settings.WhatsAppChannelUrl);
    }

    public async Task<EventDetailDto?> GetNextUpcomingAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var evt = await _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.CoverImage)
            .WherePubliclyVisible(_clock)
            .Where(e => e.StartsAtUtc >= now)
            .OrderBy(e => e.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (evt is null)
        {
            return null;
        }

        var settings = await _siteSettings.GetAsync(cancellationToken);
        return MapDetail(evt, settings.WhatsAppChannelUrl);
    }

    public async Task<IReadOnlyList<EventListItemDto>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var items = await _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.CoverImage)
            .WherePubliclyVisible(_clock)
            .Where(e => e.StartsAtUtc >= now)
            .OrderBy(e => e.StartsAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

        return items.Select(MapList).ToList();
    }

    public async Task<byte[]> BuildCalendarFeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var items = await _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.CoverImage)
            .WherePubliclyVisible(_clock)
            .Where(e => e.StartsAtUtc >= now)
            .OrderBy(e => e.StartsAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return IcalCalendarBuilder.BuildFeed(items.Select(MapList));
    }

    public async Task<byte[]> BuildSingleEventIcsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var detail = await GetBySlugAsync(slug, cancellationToken);
        if (detail is null)
        {
            return [];
        }

        return IcalCalendarBuilder.BuildSingle(detail);
    }

    private EventDetailDto MapDetail(Event evt, string? whatsAppUrl)
    {
        var now = _clock.UtcNow;
        var isPast = evt.StartsAtUtc < now;
        var internalInterest = _configuration.GetValue("Features:EventInterestForm", false);
        var cta = EventCtaResolver.Resolve(evt.CallToActionType, evt.CallToActionUrl, whatsAppUrl, internalInterest, isPast);

        VenueDto? venue = evt.Venue is null
            ? null
            : new(evt.Venue.Name, evt.Venue.Address, evt.Venue.Latitude, evt.Venue.Longitude);

        return new EventDetailDto(
            evt.Id,
            evt.Title,
            evt.Slug,
            evt.Summary,
            _htmlSanitizer.Sanitize(evt.BodyHtml),
            evt.StartsAtUtc,
            evt.EndsAtUtc,
            evt.Audience,
            evt.Type,
            evt.CallToActionType,
            evt.CallToActionUrl,
            cta,
            venue,
            MediaUrlHelper.ResolveUrl(evt.CoverImage?.StoragePath, evt.CoverImage?.Variants, "large"),
            evt.CoverImage?.AltText,
            isPast,
            evt.MetaTitle,
            evt.MetaDescription);
    }

    private EventListItemDto MapList(Event evt) =>
        new(
            evt.Id,
            evt.Title,
            evt.Slug,
            evt.Summary,
            evt.StartsAtUtc,
            evt.EndsAtUtc,
            evt.Venue?.Name,
            MediaUrlHelper.ResolveUrl(evt.CoverImage?.StoragePath, evt.CoverImage?.Variants));
}