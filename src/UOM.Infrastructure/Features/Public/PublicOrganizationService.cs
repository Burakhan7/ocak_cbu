using Microsoft.EntityFrameworkCore;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicOrganizationService(AppDbContext db) : IPublicOrganizationService
{
    public async Task<OrganizationPageDto> GetOrganizationPageAsync(CancellationToken cancellationToken = default)
    {
        var board = await db.BoardMembers
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Include(b => b.PhotoImage)
            .OrderBy(b => b.Order)
            .ThenBy(b => b.FullName)
            .ToListAsync(cancellationToken);

        var districts = await db.Districts
            .AsNoTracking()
            .OrderByDescending(d => d.IsActive)
            .ThenBy(d => d.Order)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return new OrganizationPageDto(
            board.Select(MapBoardMember).ToList(),
            districts
                .Where(d => d.IsActive && d.Latitude is not null && d.Longitude is not null)
                .Select(d => MapDistrictPin(d, requireCoordinates: true))
                .ToList(),
            districts.Select(d => MapDistrictPin(d, requireCoordinates: false)).ToList());
    }

    public async Task<DistrictDetailDto?> GetDistrictBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var district = await db.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.Slug == slug, cancellationToken);
        return district is null
            ? null
            : new DistrictDetailDto(
                district.Id,
                district.Name,
                district.Slug,
                district.PresidentName,
                district.Address,
                district.Phone,
                district.Latitude,
                district.Longitude,
                district.IsActive);
    }

    private static BoardMemberPublicDto MapBoardMember(BoardMember b)
    {
        var showPhoto = b.ConsentOnFile && b.PhotoImageId is not null;
        return new BoardMemberPublicDto(
            b.Id,
            b.FullName,
            b.Title,
            showPhoto ? MediaUrlHelper.ResolveUrl(b.PhotoImage?.StoragePath, b.PhotoImage?.Variants, "medium") : null,
            showPhoto ? b.PhotoImage?.AltText ?? b.FullName : null,
            showPhoto);
    }

    private static DistrictMapPinDto MapDistrictPin(District d, bool requireCoordinates)
    {
        var lat = d.Latitude ?? 0;
        var lng = d.Longitude ?? 0;
        if (requireCoordinates && (d.Latitude is null || d.Longitude is null))
        {
            lat = 0;
            lng = 0;
        }

        return new DistrictMapPinDto(
            d.Id,
            d.Name,
            d.Slug,
            lat,
            lng,
            d.PresidentName,
            d.Address,
            d.Phone,
            d.IsActive);
    }
}