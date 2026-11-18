using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class Resource : BaseEntity, IPublishable
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ResourceType Type { get; set; }

    public int? FileId { get; set; }

    public MediaFile? File { get; set; }

    public string? ExternalUrl { get; set; }

    public int? FacultyId { get; set; }

    public Faculty? Faculty { get; set; }

    public string? CourseCode { get; set; }

    public string? Semester { get; set; }

    public int DownloadCount { get; set; }

    public PublishStatus Status { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
}