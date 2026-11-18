using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class MediaFile : BaseEntity
{
    public string StoragePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string AltText { get; set; } = string.Empty;

    /// <summary>JSON: thumb/medium/large WebP paths.</summary>
    public string? Variants { get; set; }
}