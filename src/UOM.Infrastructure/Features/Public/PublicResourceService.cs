using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicResourceService(AppDbContext db, IClock clock) : IPublicResourceService
{
    public async Task<PagedResult<ResourceListItemDto>> ListAsync(
        ResourceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = db.Resources
            .AsNoTracking()
            .Include(r => r.Faculty)
            .WherePubliclyVisible(clock);

        if (query.FacultyId is { } facultyId)
        {
            baseQuery = baseQuery.Where(r => r.FacultyId == facultyId);
        }

        if (!string.IsNullOrWhiteSpace(query.CourseCode))
        {
            var code = query.CourseCode.Trim();
            baseQuery = baseQuery.Where(r => r.CourseCode != null && r.CourseCode.Contains(code));
        }

        var ordered = baseQuery.OrderByDescending(r => r.PublishedAtUtc);
        var total = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip(Math.Max(0, query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ResourceListItemDto(
                r.Id,
                r.Title,
                r.Description,
                r.Type,
                r.Faculty != null ? r.Faculty.Name : null,
                r.CourseCode,
                r.Semester,
                r.ExternalUrl))
            .ToListAsync(cancellationToken);

        return new PagedResult<ResourceListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<ResourceDownloadDto?> GetForDownloadAsync(int id, CancellationToken cancellationToken = default)
    {
        var resource = await db.Resources
            .AsNoTracking()
            .Include(r => r.File)
            .WherePubliclyVisible(clock)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (resource is null)
        {
            return null;
        }

        return new ResourceDownloadDto(
            resource.Id,
            resource.Type,
            resource.Title,
            resource.ExternalUrl,
            resource.File?.StoragePath,
            resource.File?.ContentType,
            resource.File?.OriginalFileName);
    }

    public async Task<bool> TryIncrementDownloadCountAsync(int id, CancellationToken cancellationToken = default)
    {
        var resource = await db.Resources
            .WherePubliclyVisible(clock)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (resource is null)
        {
            return false;
        }

        resource.DownloadCount++;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}