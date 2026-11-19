using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class PublicQuoteService(AppDbContext db, IMemoryCache cache) : IPublicQuoteService
{
    public const string CacheKey = "public-active-quotes";

    public async Task<IReadOnlyList<QuoteDto>> GetActiveQuotesAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<QuoteDto>? cached) && cached is not null)
        {
            return cached;
        }

        var quotes = await db.Quotes
            .AsNoTracking()
            .Where(q => q.IsActive)
            .OrderBy(q => q.Order)
            .ThenBy(q => q.Id)
            .Select(q => new QuoteDto(
                q.Id,
                q.Text,
                q.Author,
                QuoteAuthorLabels.ToTurkish(q.Author),
                q.SourceNote))
            .ToListAsync(cancellationToken);

        cache.Set(CacheKey, quotes, TimeSpan.FromMinutes(5));
        return quotes;
    }
}