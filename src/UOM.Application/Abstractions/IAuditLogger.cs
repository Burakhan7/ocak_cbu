namespace UOM.Application.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityName,
        int? entityId,
        object? changes = null,
        CancellationToken cancellationToken = default);
}