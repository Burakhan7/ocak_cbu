using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicCampusHubService
{
    Task<CampusHubDto> GetHubAsync(CancellationToken cancellationToken = default);
}