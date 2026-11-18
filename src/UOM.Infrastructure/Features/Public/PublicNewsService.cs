using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicNewsService : IPublicNewsService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public PublicNewsService(AppDbContext db, IClock clock, IHtmlSanitizer htmlSanitizer)
    {
        _db = db;
        _clock = clock;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<PagedResult<NewsListItemDto>> ListAsync(NewsListQuery query, CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.NewsPosts
            .AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CoverImage)
            .WherePubliclyVisible(_clock);

        if (query.Scope is { } scope)
        {
            baseQuery = baseQuery.Where(n => n.Scope == scope);
        }

        if (query.CategoryId is { } categoryId)
        {
            baseQuery = baseQuery.Where(n => n.CategoryId == categoryId);
        }

        var total = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .OrderByDescending(n => n.PublishedAtUtc)
            .Skip(Math.Max(0, query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => MapList(n))
            .ToListAsync(cancellationToken);

        return new PagedResult<NewsListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
        };
    }

    public async Task<NewsDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var post = await _db.NewsPosts
            .AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CoverImage)
            .WherePubliclyVisible(_clock)
            .FirstOrDefaultAsync(n => n.Slug == slug, cancellationToken);

        return post is null ? null : MapDetail(post);
    }

    public async Task<IReadOnlyList<NewsListItemDto>> GetFeaturedForHomeAsync(
        int featuredCount,
        int regularCount,
        CancellationToken cancellationToken = default)
    {
        var featured = await _db.NewsPosts
            .AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CoverImage)
            .WherePubliclyVisible(_clock)
            .Where(n => n.IsFeatured)
            .OrderByDescending(n => n.PublishedAtUtc)
            .Take(featuredCount)
            .ToListAsync(cancellationToken);

        var featuredIds = featured.Select(f => f.Id).ToHashSet();
        var regular = await _db.NewsPosts
            .AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CoverImage)
            .WherePubliclyVisible(_clock)
            .Where(n => !featuredIds.Contains(n.Id))
            .OrderByDescending(n => n.PublishedAtUtc)
            .Take(regularCount)
            .ToListAsync(cancellationToken);

        return featured.Concat(regular).Select(MapList).ToList();
    }

    public async Task<IReadOnlyList<NewsListItemDto>> GetRelatedAsync(
        int categoryId,
        int excludeId,
        int count,
        CancellationToken cancellationToken = default)
    {
        var items = await _db.NewsPosts
            .AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CoverImage)
            .WherePubliclyVisible(_clock)
            .Where(n => n.CategoryId == categoryId && n.Id != excludeId)
            .OrderByDescending(n => n.PublishedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

        return items.Select(MapList).ToList();
    }

    private NewsListItemDto MapList(NewsPost n) =>
        new(
            n.Id,
            n.Title,
            n.Slug,
            n.Summary,
            n.Scope,
            n.Category?.Name,
            n.PublishedAtUtc,
            MediaUrlHelper.ResolveUrl(n.CoverImage?.StoragePath, n.CoverImage?.Variants),
            n.CoverImage?.AltText);

    private NewsDetailDto MapDetail(NewsPost n) =>
        new(
            n.Id,
            n.Title,
            n.Slug,
            n.Summary,
            _htmlSanitizer.Sanitize(n.BodyHtml),
            n.Scope,
            n.Category?.Name,
            n.CategoryId,
            n.PublishedAtUtc,
            MediaUrlHelper.ResolveUrl(n.CoverImage?.StoragePath, n.CoverImage?.Variants, "large"),
            n.CoverImage?.AltText,
            n.MetaTitle,
            n.MetaDescription);
}