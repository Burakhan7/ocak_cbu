using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class MagazineIssue : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public int? CoverImageId { get; set; }

    public MediaFile? CoverImage { get; set; }

    public string ExternalUrl { get; set; } = string.Empty;

    public DateOnly IssueDate { get; set; }

    public bool IsActive { get; set; }
}