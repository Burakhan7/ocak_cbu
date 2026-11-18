using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class ParentProject : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public int? LogoImageId { get; set; }

    public MediaFile? LogoImage { get; set; }

    public string ExternalUrl { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsActive { get; set; }
}