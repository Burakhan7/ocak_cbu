using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class Venue : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int? CampusId { get; set; }

    public Campus? Campus { get; set; }

    public int? CoverImageId { get; set; }

    public MediaFile? CoverImage { get; set; }

    public ICollection<Event> Events { get; set; } = [];
}