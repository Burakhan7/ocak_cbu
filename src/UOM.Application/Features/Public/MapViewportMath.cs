namespace UOM.Application.Features.Public;

/// <summary>
/// Web Mercator used by the map picker. Screen Y grows south and is scaled by
/// 256 × 2^zoom. Skipping that scale, or flipping Y, saturates latitude at the
/// pole so a drag only changes longitude.
/// </summary>
public static class MapViewportMath
{
    public const double TileSize = 256d;

    /// <summary>Google Maps cuts the Mercator plane off here. ±90 is not a usable map latitude.</summary>
    public const double WebMercatorLimit = 85.0511287798066;

    public const double ManisaLatitude = 38.6191;

    public const double ManisaLongitude = 27.4289;

    public const int DefaultZoom = 8;

    public readonly record struct LatLng(double Latitude, double Longitude);

    public static bool IsUsable(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude)
            || double.IsNaN(longitude) || double.IsInfinity(longitude))
        {
            return false;
        }

        if (Math.Abs(latitude) >= WebMercatorLimit)
        {
            return false;
        }

        var lng = NormalizeLongitude(longitude);
        return lng is >= -180d and <= 180d;
    }

    public static double NormalizeLongitude(double longitude)
    {
        if (double.IsNaN(longitude) || double.IsInfinity(longitude))
        {
            return 0d;
        }

        var wrapped = (longitude + 180d) % 360d;
        if (wrapped < 0d)
        {
            wrapped += 360d;
        }

        return wrapped - 180d;
    }

    public static LatLng PointToLatLng(
        double centerLatitude,
        double centerLongitude,
        double zoom,
        double viewportWidth,
        double viewportHeight,
        double pixelX,
        double pixelY)
    {
        if (!IsUsable(centerLatitude, centerLongitude)
            || viewportWidth <= 1d
            || viewportHeight <= 1d
            || !double.IsFinite(pixelX)
            || !double.IsFinite(pixelY))
        {
            return IsUsable(centerLatitude, centerLongitude)
                ? new LatLng(centerLatitude, NormalizeLongitude(centerLongitude))
                : new LatLng(ManisaLatitude, ManisaLongitude);
        }

        var scale = Math.Pow(2d, zoom);
        if (!double.IsFinite(scale) || scale <= 0d)
        {
            scale = 1d;
        }

        var center = Project(centerLatitude, centerLongitude);
        var x = (center.X * scale + (pixelX - viewportWidth / 2d)) / scale;
        var y = (center.Y * scale + (pixelY - viewportHeight / 2d)) / scale;
        var lng = NormalizeLongitude(x / TileSize * 360d - 180d);
        var n = Math.PI - 2d * Math.PI * y / TileSize;
        var lat = Math.Atan(Math.Sinh(n)) * 180d / Math.PI;
        if (!double.IsFinite(lat))
        {
            lat = centerLatitude;
        }

        var limit = WebMercatorLimit - 1e-6;
        lat = Math.Clamp(lat, -limit, limit);
        return new LatLng(lat, lng);
    }

    /// <summary>
    /// Prefer the Maps API reading when it agrees with the viewport projection.
    /// A native latitude stuck near ±90 (the reported 89.999997 case) is discarded.
    /// </summary>
    public static LatLng ResolvePicker(double? nativeLatitude, double? nativeLongitude, LatLng projected)
    {
        if (nativeLatitude is double lat
            && nativeLongitude is double lng
            && IsUsable(lat, lng)
            && IsUsable(projected.Latitude, projected.Longitude))
        {
            var dLng = NormalizeLongitude(lng - projected.Longitude);
            if (Math.Abs(lat - projected.Latitude) <= 1d && Math.Abs(dLng) <= 1d)
            {
                return new LatLng(lat, NormalizeLongitude(lng));
            }
        }

        if (IsUsable(projected.Latitude, projected.Longitude))
        {
            return projected;
        }

        if (nativeLatitude is double fallbackLat
            && nativeLongitude is double fallbackLng
            && IsUsable(fallbackLat, fallbackLng))
        {
            return new LatLng(fallbackLat, NormalizeLongitude(fallbackLng));
        }

        return new LatLng(ManisaLatitude, ManisaLongitude);
    }

    private static (double X, double Y) Project(double latitude, double longitude)
    {
        var siny = Math.Sin(latitude * Math.PI / 180d);
        siny = Math.Clamp(siny, -0.9999, 0.9999);
        var x = TileSize * (0.5 + longitude / 360d);
        var y = TileSize * (0.5 - Math.Log((1 + siny) / (1 - siny)) / (4 * Math.PI));
        return (x, y);
    }
}
