using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicGuideService(AppDbContext db, IClock clock, IHtmlSanitizer sanitizer) : IPublicGuideService
{
    public async Task<IReadOnlyList<GuideListItemDto>> ListPublishedAsync(
        GuideCategory? category,
        CancellationToken cancellationToken = default)
    {
        var query = db.GuideArticles.AsNoTracking().WherePubliclyVisible(clock);
        if (category is { } cat)
        {
            query = query.Where(g => g.Category == cat);
        }

        var articles = await query
            .Include(g => g.CoverImage)
            .OrderBy(g => g.Category)
            .ThenBy(g => g.Order)
            .ThenBy(g => g.Title)
            .ToListAsync(cancellationToken);

        return articles.Select(MapList).ToList();
    }

    public async Task<GuideDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var article = await db.GuideArticles
            .AsNoTracking()
            .Include(g => g.CoverImage)
            .WherePubliclyVisible(clock)
            .FirstOrDefaultAsync(g => g.Slug == slug, cancellationToken);

        return article is null ? null : MapDetail(article);
    }

    private GuideListItemDto MapList(GuideArticle g) =>
        new(
            g.Title,
            g.Slug,
            g.Summary,
            g.Category,
            g.LastReviewedAtUtc,
            MediaUrlHelper.ResolveUrl(g.CoverImage?.StoragePath, g.CoverImage?.Variants),
            g.CoverImage?.AltText);

    private GuideDetailDto MapDetail(GuideArticle article) =>
        new(
            article.Title,
            article.Slug,
            article.Summary,
            sanitizer.Sanitize(article.BodyHtml),
            article.Category,
            article.LastReviewedAtUtc,
            MediaUrlHelper.ResolveUrl(article.CoverImage?.StoragePath, article.CoverImage?.Variants, "large"),
            article.CoverImage?.AltText,
            article.MetaTitle,
            article.MetaDescription);
}