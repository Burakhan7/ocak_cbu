using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class ParentNewsLink : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string? ImageUrl { get; set; }

    public int? ImageId { get; set; }

    public MediaFile? Image { get; set; }

    public string ExternalUrl { get; set; } = string.Empty;

    public DateOnly PublishedDate { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }
}