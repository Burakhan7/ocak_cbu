using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Domain.Entities;

public class Video : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string YouTubeVideoId { get; set; } = string.Empty;

    public VideoSource Source { get; set; }

    public DateOnly PublishedDate { get; set; }

    public bool IsFeatured { get; set; }

    public int Order { get; set; }
}