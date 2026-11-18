namespace UOM.Application.Abstractions;

public interface IAuditUserProvider
{
    string? GetCurrentUserId();
}