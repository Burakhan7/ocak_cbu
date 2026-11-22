namespace UOM.Web.Models;

public sealed class GoogleMapRequest
{
    public bool Picker { get; init; }

    public string LatFieldId { get; init; } = "Latitude";

    public string LngFieldId { get; init; } = "Longitude";

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public string Size { get; init; } = "default";

    public bool AnnounceEmpty { get; init; }

    public IReadOnlyList<GoogleMapPin> Pins { get; init; } = [];
}

public sealed record GoogleMapPin(
    string Title,
    double Latitude,
    double Longitude,
    string? Line1,
    string? Line2,
    string? Href,
    string? LinkLabel);
