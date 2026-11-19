using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicSearchService(AppDbContext db, IClock clock, IConfiguration configuration, IMemoryCache cache)
    : IPublicSearchService
{
    public async Task<SearchResultDto> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue("Features:Search", true);
        var text = (query ?? string.Empty).Trim();
        if (text.Length > 200)
        {
            text = text[..200];
        }

        if (!enabled || text.Length == 0)
        {
            return new SearchResultDto(text, enabled, false, []);
        }

        if (await ShouldUseFullTextAsync(cancellationToken))
        {
            var condition = SearchText.FullTextCondition(text);
            if (condition is not null)
            {
                try
                {
                    var hits = await CollectAsync(fullText: true, condition, cancellationToken);
                    return new SearchResultDto(text, true, true, hits);
                }
                catch (Exception)
                {
                    cache.Set(PublicCacheKeys.FullTextReady, false, TimeSpan.FromMinutes(10));
                }
            }
        }

        var likeHits = await CollectAsync(fullText: false, SearchText.LikePattern(text), cancellationToken);
        return new SearchResultDto(text, true, false, likeHits);
    }

    private async Task<bool> ShouldUseFullTextAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(configuration["Search:Provider"], "FullText", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!db.Database.IsSqlServer())
        {
            return false;
        }

        if (cache.TryGetValue(PublicCacheKeys.FullTextReady, out bool ready))
        {
            return ready;
        }

        try
        {
            var installed = await db.Database
                .SqlQueryRaw<int>("SELECT CAST(FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS int) AS [Value]")
                .FirstAsync(cancellationToken);
            var indexed = installed == 1 && await db.Database
                .SqlQueryRaw<int>("SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'NewsPosts')) THEN 1 ELSE 0 END AS [Value]")
                .FirstAsync(cancellationToken) == 1;
            cache.Set(PublicCacheKeys.FullTextReady, indexed, TimeSpan.FromMinutes(10));
            return indexed;
        }
        catch (Exception)
        {
            cache.Set(PublicCacheKeys.FullTextReady, false, TimeSpan.FromMinutes(10));
            return false;
        }
    }

    private async Task<IReadOnlyList<SearchHitDto>> CollectAsync(
        bool fullText,
        string conditionOrPattern,
        CancellationToken cancellationToken)
    {
        var hits = new List<SearchHitDto>();
        hits.AddRange(await SearchNewsAsync(fullText, conditionOrPattern, cancellationToken));
        hits.AddRange(await SearchEventsAsync(fullText, conditionOrPattern, cancellationToken));
        hits.AddRange(await SearchGuidesAsync(fullText, conditionOrPattern, cancellationToken));
        hits.AddRange(await SearchAnnouncementsAsync(fullText, conditionOrPattern, cancellationToken));
        hits.AddRange(await SearchResourcesAsync(fullText, conditionOrPattern, cancellationToken));
        return hits
            .OrderByDescending(hit => hit.WhenUtc ?? DateTime.MinValue)
            .Take(40)
            .ToList();
    }

    private async Task<List<SearchHitDto>> SearchNewsAsync(bool fullText, string term, CancellationToken cancellationToken)
    {
        var query = db.NewsPosts.AsNoTracking().WherePubliclyVisible(clock);
        query = fullText
            ? query.Where(n =>
                EF.Functions.Contains(n.Title, term)
                || EF.Functions.Contains(n.Summary!, term)
                || EF.Functions.Contains(n.BodyHtml, term))
            : query.Where(n =>
                EF.Functions.Like(n.Title, term)
                || (n.Summary != null && EF.Functions.Like(n.Summary, term))
                || EF.Functions.Like(n.BodyHtml, term));

        var rows = await query
            .OrderByDescending(n => n.PublishedAtUtc)
            .Take(10)
            .Select(n => new { n.Title, n.Summary, n.BodyHtml, n.Slug, n.PublishedAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(n => new SearchHitDto(
                "Haber",
                n.Title,
                SearchText.Excerpt(n.Summary, n.BodyHtml),
                PublicContentPaths.News(n.Slug),
                n.PublishedAtUtc))
            .ToList();
    }

    private async Task<List<SearchHitDto>> SearchEventsAsync(bool fullText, string term, CancellationToken cancellationToken)
    {
        var query = db.Events.AsNoTracking().WherePubliclyVisible(clock);
        query = fullText
            ? query.Where(e =>
                EF.Functions.Contains(e.Title, term)
                || EF.Functions.Contains(e.Summary!, term)
                || EF.Functions.Contains(e.BodyHtml, term))
            : query.Where(e =>
                EF.Functions.Like(e.Title, term)
                || (e.Summary != null && EF.Functions.Like(e.Summary, term))
                || EF.Functions.Like(e.BodyHtml, term));

        var rows = await query
            .OrderByDescending(e => e.StartsAtUtc)
            .Take(10)
            .Select(e => new { e.Title, e.Summary, e.BodyHtml, e.Slug, e.StartsAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(e => new SearchHitDto(
                "Etkinlik",
                e.Title,
                SearchText.Excerpt(e.Summary, e.BodyHtml),
                PublicContentPaths.Event(e.Slug),
                e.StartsAtUtc))
            .ToList();
    }

    private async Task<List<SearchHitDto>> SearchGuidesAsync(bool fullText, string term, CancellationToken cancellationToken)
    {
        var query = db.GuideArticles.AsNoTracking().WherePubliclyVisible(clock);
        query = fullText
            ? query.Where(g =>
                EF.Functions.Contains(g.Title, term)
                || EF.Functions.Contains(g.Summary!, term)
                || EF.Functions.Contains(g.BodyHtml, term))
            : query.Where(g =>
                EF.Functions.Like(g.Title, term)
                || (g.Summary != null && EF.Functions.Like(g.Summary, term))
                || EF.Functions.Like(g.BodyHtml, term));

        var rows = await query
            .OrderByDescending(g => g.PublishedAtUtc)
            .Take(10)
            .Select(g => new { g.Title, g.Summary, g.BodyHtml, g.Slug, g.PublishedAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(g => new SearchHitDto(
                "Rehber",
                g.Title,
                SearchText.Excerpt(g.Summary, g.BodyHtml),
                PublicContentPaths.Guide(g.Slug),
                g.PublishedAtUtc))
            .ToList();
    }

    private async Task<List<SearchHitDto>> SearchAnnouncementsAsync(bool fullText, string term, CancellationToken cancellationToken)
    {
        var query = db.Announcements.AsNoTracking().WhereListableAnnouncements(clock);
        query = fullText
            ? query.Where(a => EF.Functions.Contains(a.Title, term) || EF.Functions.Contains(a.Body, term))
            : query.Where(a => EF.Functions.Like(a.Title, term) || EF.Functions.Like(a.Body, term));

        var rows = await query
            .OrderByDescending(a => a.PublishedAtUtc)
            .Take(10)
            .Select(a => new { a.Title, a.Body, a.Slug, a.PublishedAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(a => new SearchHitDto(
                "Duyuru",
                a.Title,
                SearchText.Excerpt(null, a.Body),
                PublicContentPaths.Announcement(a.Slug),
                a.PublishedAtUtc))
            .ToList();
    }

    private async Task<List<SearchHitDto>> SearchResourcesAsync(
        bool fullText,
        string term,
        CancellationToken cancellationToken)
    {
        var query = db.Resources.AsNoTracking().WherePubliclyVisible(clock);
        query = fullText
            ? query.Where(r =>
                EF.Functions.Contains(r.Title, term)
                || EF.Functions.Contains(r.Description!, term)
                || EF.Functions.Contains(r.CourseCode!, term))
            : query.Where(r =>
                EF.Functions.Like(r.Title, term)
                || (r.Description != null && EF.Functions.Like(r.Description, term))
                || (r.CourseCode != null && EF.Functions.Like(r.CourseCode, term)));

        var rows = await query
            .OrderByDescending(r => r.PublishedAtUtc)
            .Take(10)
            .Select(r => new { r.Id, r.Title, r.Description, r.Type, r.ExternalUrl, r.PublishedAtUtc })
            .ToListAsync(cancellationToken);

        return rows.Select(r =>
        {
            var url = r.Type == ResourceType.Link && !string.IsNullOrWhiteSpace(r.ExternalUrl)
                ? r.ExternalUrl!
                : "/cbu/kaynaklar/" + r.Id + "/indir";
            return new SearchHitDto("Kaynak", r.Title, SearchText.Excerpt(r.Description, null), url, r.PublishedAtUtc);
        }).ToList();
    }
}
