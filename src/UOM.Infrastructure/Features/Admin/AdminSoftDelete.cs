using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Common;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

internal static class AdminSoftDelete
{
    public static async Task<Result> DeleteAsync<TEntity>(
        AppDbContext db,
        IAuditLogger auditLogger,
        int id,
        string notFoundMessage,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity
    {
        var entity = await db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(notFoundMessage);
        }

        entity.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Delete", typeof(TEntity).Name, id, null, cancellationToken);
        return Result.Ok();
    }
}