using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
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

        return await query
            .OrderBy(g => g.Category)
            .ThenBy(g => g.Order)
            .ThenBy(g => g.Title)
            .Select(g => new GuideListItemDto(g.Title, g.Slug, g.Summary, g.Category, g.LastReviewedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<GuideDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var article = await db.GuideArticles
            .AsNoTracking()
            .WherePubliclyVisible(clock)
            .FirstOrDefaultAsync(g => g.Slug == slug, cancellationToken);

        return article is null
            ? null
            : new GuideDetailDto(
                article.Title,
                article.Slug,
                article.Summary,
                sanitizer.Sanitize(article.BodyHtml),
                article.Category,
                article.LastReviewedAtUtc,
                article.MetaTitle,
                article.MetaDescription);
    }
}