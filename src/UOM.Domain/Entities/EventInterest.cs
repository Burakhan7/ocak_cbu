using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class EventInterest : BaseEntity
{
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int? FacultyId { get; set; }

    public Faculty? Faculty { get; set; }

    public DateTime KvkkConsentAtUtc { get; set; }

    public string? IpHash { get; set; }

    public DateTime? DeleteAfterUtc { get; set; }
}