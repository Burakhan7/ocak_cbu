using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IPublicQuoteService
{
    Task<IReadOnlyList<QuoteDto>> GetActiveQuotesAsync(CancellationToken cancellationToken = default);
}