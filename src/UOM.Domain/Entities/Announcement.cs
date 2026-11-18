using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class Announcement : BaseEntity, IPublishable
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public AnnouncementType Type { get; set; }

    public string? ExternalUrl { get; set; }

    public DateTime? DeadlineUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsPinned { get; set; }

    public PublishStatus Status { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
}