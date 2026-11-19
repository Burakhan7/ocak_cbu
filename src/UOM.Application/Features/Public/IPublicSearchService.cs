using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicSearchService
{
    Task<SearchResultDto> SearchAsync(string? query, CancellationToken cancellationToken = default);
}
