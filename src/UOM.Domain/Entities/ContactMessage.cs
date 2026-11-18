using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class ContactMessage : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public ContactSubject Subject { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsStudentAtCbu { get; set; }

    public int? FacultyId { get; set; }

    public Faculty? Faculty { get; set; }

    public DateTime KvkkConsentAtUtc { get; set; }

    public string? IpHash { get; set; }

    public bool IsRead { get; set; }

    public string? ReadByUserId { get; set; }

    public DateTime? DeleteAfterUtc { get; set; }
}