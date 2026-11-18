using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class Campus : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? ShortName { get; set; }

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Description { get; set; }

    public int Order { get; set; }

    public ICollection<Faculty> Faculties { get; set; } = [];

    public ICollection<Venue> Venues { get; set; } = [];
}