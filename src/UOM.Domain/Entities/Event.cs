using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class Event : BaseEntity, IPublishable, ISeoEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string BodyHtml { get; set; } = string.Empty;

    public int? CoverImageId { get; set; }

    public MediaFile? CoverImage { get; set; }

    public DateTime StartsAtUtc { get; set; }

    public DateTime? EndsAtUtc { get; set; }

    public int? VenueId { get; set; }

    public Venue? Venue { get; set; }

    public EventAudience Audience { get; set; }

    public EventType Type { get; set; }

    public EventCtaType CallToActionType { get; set; }

    public string? CallToActionUrl { get; set; }

    public int? Capacity { get; set; }

    public PublishStatus Status { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public int? OgImageId { get; set; }

    public ICollection<EventInterest> Interests { get; set; } = [];

    public ICollection<GalleryAlbum> GalleryAlbums { get; set; } = [];

    public string? LastRejectionNote { get; set; }
}