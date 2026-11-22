using FluentAssertions;
using UOM.Application.Features.Public;

namespace UOM.Application.Tests.Features.Public;

public class MapViewportMathTests
{
    // Default picker view: Manisa, zoom 8, 800×400. Center pixel is (400, 200).
    private const double CenterLat = MapViewportMath.ManisaLatitude;
    private const double CenterLng = MapViewportMath.ManisaLongitude;

    [Fact]
    public void Center_click_on_the_default_view_is_manisa()
    {
        var point = MapViewportMath.PointToLatLng(CenterLat, CenterLng, MapViewportMath.DefaultZoom, 800, 400, 400, 200);

        point.Latitude.Should().BeApproximately(38.6191, 1e-6);
        point.Longitude.Should().BeApproximately(27.4289, 1e-6);
        point.Latitude.Should().BeInRange(38, 39);
        point.Longitude.Should().BeInRange(27, 28);
    }

    [Fact]
    public void Dragging_changes_latitude_and_longitude_and_does_not_clamp_to_the_pole()
    {
        var center = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 800, 400, 400, 200);
        var north = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 800, 400, 400, 120);
        var south = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 800, 400, 400, 280);
        var east = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 800, 400, 480, 200);
        var west = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 800, 400, 320, 200);

        north.Latitude.Should().BeGreaterThan(center.Latitude + 0.2);
        south.Latitude.Should().BeLessThan(center.Latitude - 0.2);
        east.Longitude.Should().BeGreaterThan(center.Longitude + 0.2);
        west.Longitude.Should().BeLessThan(center.Longitude - 0.2);

        north.Longitude.Should().BeApproximately(center.Longitude, 1e-6);
        east.Latitude.Should().BeApproximately(center.Latitude, 1e-6);

        foreach (var point in new[] { center, north, south, east, west })
        {
            point.Latitude.Should().BeInRange(38, 40);
            point.Longitude.Should().BeInRange(26, 29);
            Math.Abs(point.Latitude).Should().BeLessThan(85);
        }
    }

    [Fact]
    public void Top_edge_of_the_default_view_is_not_the_north_pole()
    {
        // Regression: a zoom-0 mercator fed with raw screen Y (or a flipped Y)
        // returns ~89.999997, so the pin sticks to the pole and only slides east-west.
        var brokenY = -200d;
        var brokenN = Math.PI - 2d * Math.PI * brokenY / MapViewportMath.TileSize;
        var brokenLat = Math.Atan(Math.Sinh(brokenN)) * 180d / Math.PI;
        brokenLat.Should().BeGreaterThan(89);

        var top = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 960, 400, 480, 0);
        top.Latitude.Should().BeApproximately(39.47232789, 1e-4);
        top.Longitude.Should().BeApproximately(27.4289, 1e-4);
        top.Latitude.Should().BeLessThan(45);
        MapViewportMath.IsUsable(89.999997, -123.798169).Should().BeFalse();
        MapViewportMath.IsUsable(top.Latitude, top.Longitude).Should().BeTrue();
    }

    [Fact]
    public void Pole_clamped_native_reading_is_replaced_by_the_viewport_point()
    {
        var projected = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 800, 400, 400, 200);
        var chosen = MapViewportMath.ResolvePicker(89.999997, -123.798169, projected);

        chosen.Latitude.Should().BeInRange(38, 39);
        chosen.Longitude.Should().BeInRange(27, 28);
        chosen.Latitude.Should().BeApproximately(projected.Latitude, 1e-9);
        chosen.Longitude.Should().BeApproximately(projected.Longitude, 1e-9);
    }

    [Fact]
    public void Native_reading_that_matches_the_viewport_is_kept()
    {
        var projected = new MapViewportMath.LatLng(38.62, 27.43);
        var chosen = MapViewportMath.ResolvePicker(38.6204, 27.4302, projected);

        chosen.Latitude.Should().BeApproximately(38.6204, 1e-9);
        chosen.Longitude.Should().BeApproximately(27.4302, 1e-9);
    }

    [Fact]
    public void Zero_size_viewport_does_not_invent_a_pole()
    {
        var point = MapViewportMath.PointToLatLng(CenterLat, CenterLng, 8, 0, 0, 10, 10);
        point.Latitude.Should().BeApproximately(CenterLat, 1e-9);
        point.Longitude.Should().BeApproximately(CenterLng, 1e-9);
    }
}
