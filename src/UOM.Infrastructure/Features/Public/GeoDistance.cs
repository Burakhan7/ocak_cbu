namespace UOM.Infrastructure.Features.Public;

internal static class GeoDistance
{
    public static double? KilometersBetween(double? lat1, double? lon1, double lat2, double lon2)
    {
        if (lat1 is not { } a || lon1 is not { } o)
        {
            return null;
        }

        const double R = 6371.0;
        var dLat = DegreesToRadians(lat2 - a);
        var dLon = DegreesToRadians(lon2 - o);
        var x = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(a)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x));
        return Math.Round(R * c, 1);
    }

    private static double DegreesToRadians(double d) => d * Math.PI / 180.0;
}