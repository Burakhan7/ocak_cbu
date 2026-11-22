using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UOM.Application.Abstractions;
using UOM.Application.Privacy;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Privacy;

public sealed record KvkkPurgeResult(int ContactMessages, int EventInterests)
{
    public int Total => ContactMessages + EventInterests;
}

public sealed class KvkkRetentionService(
    AppDbContext db,
    IClock clock,
    IConfiguration configuration,
    ILogger<KvkkRetentionService> logger)
{
    public async Task<KvkkPurgeResult> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        await StampMissingEventInterestDatesAsync(cancellationToken);

        var expiredContacts = await db.ContactMessages
            .IgnoreQueryFilters()
            .Where(m => m.DeleteAfterUtc != null && m.DeleteAfterUtc <= now)
            .ToListAsync(cancellationToken);
        var expiredInterests = await db.EventInterests
            .IgnoreQueryFilters()
            .Where(m => m.DeleteAfterUtc != null && m.DeleteAfterUtc <= now)
            .ToListAsync(cancellationToken);

        if (expiredContacts.Count == 0 && expiredInterests.Count == 0)
        {
            return new KvkkPurgeResult(0, 0);
        }

        db.ContactMessages.RemoveRange(expiredContacts);
        db.EventInterests.RemoveRange(expiredInterests);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "KVKK retention purge removed {ContactCount} contact messages and {InterestCount} event interests.",
            expiredContacts.Count,
            expiredInterests.Count);

        return new KvkkPurgeResult(expiredContacts.Count, expiredInterests.Count);
    }

    private async Task StampMissingEventInterestDatesAsync(CancellationToken cancellationToken)
    {
        var days = configuration.GetValue("Kvkk:EventInterestRetentionDaysAfterEvent", 30);
        var pending = await db.EventInterests
            .IgnoreQueryFilters()
            .Include(i => i.Event)
            .Where(i => i.DeleteAfterUtc == null)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var row in pending)
        {
            var end = row.Event is null
                ? row.CreatedAtUtc
                : KvkkRetentionCalendar.EventEndUtc(row.Event.StartsAtUtc, row.Event.EndsAtUtc);
            row.DeleteAfterUtc = KvkkRetentionCalendar.EventInterestDeleteAfterUtc(end, days);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
