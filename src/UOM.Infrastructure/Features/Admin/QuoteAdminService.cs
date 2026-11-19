using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class QuoteAdminService(AppDbContext db, IAuditLogger auditLogger, IMemoryCache cache)
{
    public Task<List<Quote>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Quotes.OrderBy(q => q.Order).ThenBy(q => q.Id).ToListAsync(cancellationToken);

    public Task<Quote?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Quotes.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<Result<Quote>> SaveAsync(Quote model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Text))
        {
            return Result<Quote>.Fail("Alıntı metni zorunludur.");
        }

        if (model.Id == 0)
        {
            db.Quotes.Add(model);
        }
        else
        {
            var existing = await db.Quotes.FirstOrDefaultAsync(q => q.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Quote>.Fail("Alıntı bulunamadı.");
            }

            existing.Text = model.Text.Trim();
            existing.Author = model.Author;
            existing.SourceNote = model.SourceNote;
            existing.IsActive = model.IsActive;
            existing.Order = model.Order;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(PublicQuoteService.CacheKey);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(Quote), model.Id, new { model.Author }, cancellationToken);
        return Result<Quote>.Ok(model);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quote is null)
        {
            return Result.Fail("Alıntı bulunamadı.");
        }

        quote.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(PublicQuoteService.CacheKey);
        await auditLogger.LogAsync("Delete", nameof(Quote), id, null, cancellationToken);
        return Result.Ok();
    }
}