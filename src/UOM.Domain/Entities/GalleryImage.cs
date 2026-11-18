using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class GalleryImage : BaseEntity
{
    public int AlbumId { get; set; }

    public GalleryAlbum Album { get; set; } = null!;

    public int MediaFileId { get; set; }

    public MediaFile MediaFile { get; set; } = null!;

    public string? Caption { get; set; }

    public int Order { get; set; }
}