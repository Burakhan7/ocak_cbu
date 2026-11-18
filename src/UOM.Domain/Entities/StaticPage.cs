using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class StaticPage : BaseEntity, IPublishable, ISeoEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public bool ShowInFooter { get; set; }

    public int Order { get; set; }

    public PublishStatus Status { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public int? OgImageId { get; set; }
}