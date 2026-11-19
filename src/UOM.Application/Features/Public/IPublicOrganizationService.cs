using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicOrganizationService
{
    Task<OrganizationPageDto> GetOrganizationPageAsync(CancellationToken cancellationToken = default);

    Task<DistrictDetailDto?> GetDistrictBySlugAsync(string slug, CancellationToken cancellationToken = default);
}