using Microsoft.EntityFrameworkCore;
using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicParentHubService(AppDbContext db) : IPublicParentHubService
{
    public async Task<ParentHubPageDto> GetHubPageAsync(int newsPage, int newsPageSize, CancellationToken cancellationToken = default)
    {
        newsPage = Math.Max(1, newsPage);
        newsPageSize = Math.Clamp(newsPageSize, 1, 48);

        var newsQuery = db.ParentNewsLinks.AsNoTracking().Where(n => n.IsActive);
        var total = await newsQuery.CountAsync(cancellationToken);
        var newsRows = await newsQuery
            .Include(n => n.Image)
            .OrderBy(n => n.Order)
            .ThenByDescending(n => n.PublishedDate)
            .Skip((newsPage - 1) * newsPageSize)
            .Take(newsPageSize)
            .ToListAsync(cancellationToken);

        var projects = await GetActiveProjectRowsAsync(cancellationToken);
        var magazines = await db.MagazineIssues
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Include(m => m.CoverImage)
            .OrderByDescending(m => m.IssueDate)
            .ToListAsync(cancellationToken);

        var videos = await db.Videos
            .AsNoTracking()
            .OrderByDescending(v => v.IsFeatured)
            .ThenBy(v => v.Order)
            .ThenByDescending(v => v.PublishedDate)
            .ToListAsync(cancellationToken);

        return new ParentHubPageDto(
            new PagedResult<ParentNewsCardDto>
            {
                Items = newsRows.Select(MapNews).ToList(),
                Page = newsPage,
                PageSize = newsPageSize,
                TotalCount = total,
            },
            projects.Select(MapProject).ToList(),
            magazines.Select(MapMagazine).ToList(),
            videos.Select(MapVideo).ToList());
    }

    public async Task<IReadOnlyList<ParentNewsCardDto>> GetNewsStripForHomeAsync(int count, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 12);
        var rows = await db.ParentNewsLinks
            .AsNoTracking()
            .Where(n => n.IsActive)
            .Include(n => n.Image)
            .OrderBy(n => n.Order)
            .ThenByDescending(n => n.PublishedDate)
            .Take(count)
            .ToListAsync(cancellationToken);

        return rows.Select(MapNews).ToList();
    }

    public async Task<IReadOnlyList<ParentProjectCardDto>> GetActiveProjectsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await GetActiveProjectRowsAsync(cancellationToken);
        return rows.Select(MapProject).ToList();
    }

    public async Task<MagazineVideoHomeDto> GetMagazineAndFeaturedVideoForHomeAsync(CancellationToken cancellationToken = default)
    {
        var magazine = await db.MagazineIssues
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Include(m => m.CoverImage)
            .OrderByDescending(m => m.IssueDate)
            .FirstOrDefaultAsync(cancellationToken);

        var video = await db.Videos
            .AsNoTracking()
            .OrderByDescending(v => v.IsFeatured)
            .ThenBy(v => v.Order)
            .ThenByDescending(v => v.PublishedDate)
            .FirstOrDefaultAsync(cancellationToken);

        return new MagazineVideoHomeDto(
            magazine is null ? null : MapMagazine(magazine),
            video is null ? null : MapVideo(video));
    }

    private Task<List<ParentProject>> GetActiveProjectRowsAsync(CancellationToken cancellationToken) =>
        db.ParentProjects
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.LogoImage)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

    private static ParentNewsCardDto MapNews(ParentNewsLink n) =>
        new(
            n.Id,
            n.Title,
            n.Summary,
            n.ExternalUrl,
            n.PublishedDate,
            ResolveNewsImage(n));

    private static string? ResolveNewsImage(ParentNewsLink n)
    {
        var fromMedia = MediaUrlHelper.ResolveUrl(n.Image?.StoragePath, n.Image?.Variants, "medium");
        if (!string.IsNullOrWhiteSpace(fromMedia))
        {
            return fromMedia;
        }

        return string.IsNullOrWhiteSpace(n.ImageUrl) ? null : n.ImageUrl.Trim();
    }

    private static ParentProjectCardDto MapProject(ParentProject p) =>
        new(
            p.Id,
            p.Name,
            p.ShortDescription,
            p.ExternalUrl,
            MediaUrlHelper.ResolveUrl(p.LogoImage?.StoragePath, p.LogoImage?.Variants, "thumb"),
            p.LogoImage?.AltText ?? p.Name);

    private static MagazineCardDto MapMagazine(MagazineIssue m) =>
        new(
            m.Id,
            m.Title,
            m.IssueDate,
            m.ExternalUrl,
            MediaUrlHelper.ResolveUrl(m.CoverImage?.StoragePath, m.CoverImage?.Variants, "medium"),
            m.CoverImage?.AltText ?? m.Title);

    private static VideoListItemDto MapVideo(Video v) =>
        new(v.Id, v.Title, v.YouTubeVideoId, v.Source, v.PublishedDate, v.IsFeatured);
}