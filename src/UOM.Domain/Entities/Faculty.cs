using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class Faculty : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public int CampusId { get; set; }

    public Campus Campus { get; set; } = null!;

    public int Order { get; set; }

    public int? CoverImageId { get; set; }

    public MediaFile? CoverImage { get; set; }

    public ICollection<Resource> Resources { get; set; } = [];
}