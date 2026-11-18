using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class GuideArticle : BaseEntity, IPublishable, ISeoEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string BodyHtml { get; set; } = string.Empty;

    public GuideCategory Category { get; set; }

    public int Order { get; set; }

    public int? CoverImageId { get; set; }

    public MediaFile? CoverImage { get; set; }

    public PublishStatus Status { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? LastReviewedAtUtc { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public int? OgImageId { get; set; }
}