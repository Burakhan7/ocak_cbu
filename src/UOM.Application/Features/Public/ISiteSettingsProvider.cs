using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface ISiteSettingsProvider
{
    Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetHomeSectionOrderAsync(CancellationToken cancellationToken = default);
}