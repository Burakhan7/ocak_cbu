using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Identity;

public sealed class NullAuditUserProvider : IAuditUserProvider
{
    public string? GetCurrentUserId() => null;
}