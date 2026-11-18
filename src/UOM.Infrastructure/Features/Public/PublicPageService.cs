using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicPageService : IPublicPageService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public PublicPageService(AppDbContext db, IClock clock, IHtmlSanitizer htmlSanitizer)
    {
        _db = db;
        _clock = clock;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<StaticPageDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (ReservedPublicSlugs.IsReserved(slug))
        {
            return null;
        }

        var page = await _db.StaticPages
            .AsNoTracking()
            .WherePubliclyVisible(_clock)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        return page is null
            ? null
            : new StaticPageDto(
                page.Title,
                page.Slug,
                _htmlSanitizer.Sanitize(page.BodyHtml),
                page.MetaTitle,
                page.MetaDescription);
    }

    public async Task<IReadOnlyList<StaticPageLinkDto>> GetFooterPagesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.StaticPages
            .AsNoTracking()
            .WherePubliclyVisible(_clock)
            .Where(p => p.ShowInFooter)
            .OrderBy(p => p.Order)
            .Select(p => new StaticPageLinkDto(p.Title, p.Slug))
            .ToListAsync(cancellationToken);
    }

    public async Task<RedirectDto?> FindRedirectAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = path.Trim('/');
        var redirect = await _db.Redirects
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.FromPath == normalized || r.FromPath == path, cancellationToken);

        return redirect is null
            ? null
            : new RedirectDto(redirect.ToPath, redirect.StatusCode);
    }
}