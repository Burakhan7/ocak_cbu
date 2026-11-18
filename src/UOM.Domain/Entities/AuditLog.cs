using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public string? ChangesJson { get; set; }

    public DateTime AtUtc { get; set; }

    public string? IpHash { get; set; }
}