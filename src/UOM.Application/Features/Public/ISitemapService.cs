namespace UOM.Application.Features.Public;

public interface ISitemapService
{
    Task<string> BuildXmlAsync(string baseUrl, CancellationToken cancellationToken = default);
}
