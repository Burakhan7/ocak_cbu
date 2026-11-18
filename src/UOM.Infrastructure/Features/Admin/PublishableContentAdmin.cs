using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Common;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

/// <summary>Shared onay akışı yardımcıları (GuideArticle, Resource, Announcement).</summary>
internal static class PublishableContentAdmin
{
    public static async Task<Result> SubmitForApprovalAsync<T>(
        AppDbContext db,
        IAuditLogger audit,
        int id,
        CancellationToken cancellationToken)
        where T : class, IPublishable
    {
        var entity = await db.Set<T>().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail("Kayıt bulunamadı.");
        }

        if (entity.Status is not (PublishStatus.Draft or PublishStatus.PendingApproval))
        {
            return Result.Fail("Onaya gönderilemez.");
        }

        entity.Status = PublishStatus.PendingApproval;
        await db.SaveChangesAsync(cancellationToken);
        await audit.LogAsync("SubmitForApproval", typeof(T).Name, id, null, cancellationToken);
        return Result.Ok();
    }

    public static async Task<Result> PublishAsync<T>(
        AppDbContext db,
        IAuditLogger audit,
        IClock clock,
        int id,
        CancellationToken cancellationToken)
        where T : class, IPublishable
    {
        var entity = await db.Set<T>().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail("Kayıt bulunamadı.");
        }

        if (entity.Status != PublishStatus.PendingApproval)
        {
            return Result.Fail("Yalnızca onay bekleyen içerik yayınlanabilir.");
        }

        entity.Status = PublishStatus.Published;
        entity.PublishedAtUtc ??= clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.LogAsync("Publish", typeof(T).Name, id, null, cancellationToken);
        return Result.Ok();
    }
}