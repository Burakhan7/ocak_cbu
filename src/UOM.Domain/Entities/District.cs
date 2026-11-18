using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class District : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? PresidentName { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool IsActive { get; set; }

    public int Order { get; set; }
}