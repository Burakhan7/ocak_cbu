using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicRedirectService
{
    Task<RedirectMatch?> FindAsync(string requestPath, CancellationToken cancellationToken = default);

    Task RegisterHitAsync(string requestPath, CancellationToken cancellationToken = default);
}
