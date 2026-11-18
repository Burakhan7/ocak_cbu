using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class NewsPost : BaseEntity, IPublishable, ISeoEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string BodyHtml { get; set; } = string.Empty;

    public int? CoverImageId { get; set; }

    public MediaFile? CoverImage { get; set; }

    public NewsScope Scope { get; set; }

    public int? CategoryId { get; set; }

    public NewsCategory? Category { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsPinned { get; set; }

    public PublishStatus Status { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public int ViewCount { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public int? OgImageId { get; set; }

    public string? LastRejectionNote { get; set; }
}