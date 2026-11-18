using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class GalleryAlbum : BaseEntity, IPublishable, ISeoEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CoverImageId { get; set; }

    public MediaFile? CoverImage { get; set; }

    public int? EventId { get; set; }

    public Event? Event { get; set; }

    public DateOnly Date { get; set; }

    public PublishStatus Status { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public int? OgImageId { get; set; }

    public ICollection<GalleryImage> Images { get; set; } = [];
}