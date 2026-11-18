using System.Text.Json;
using UOM.Application.Abstractions;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence;

public sealed class AuditLogger(
    AppDbContext db,
    IAuditUserProvider auditUserProvider,
    IClock clock) : IAuditLogger
{
    public async Task LogAsync(
        string action,
        string entityName,
        int? entityId,
        object? changes = null,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = auditUserProvider.GetCurrentUserId(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            ChangesJson = changes is null ? null : JsonSerializer.Serialize(changes),
            AtUtc = clock.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}