namespace UOM.Application.Features.Public;

public interface IFeedService
{
    Task<string> BuildRssAsync(string baseUrl, CancellationToken cancellationToken = default);
}
