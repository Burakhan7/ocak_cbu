using System.Globalization;

namespace UOM.Application.Formatting;

public static class DisplayDateTimeExtensions
{
    private static readonly Lazy<TimeZoneInfo?> Istanbul = new(TryResolveIstanbul);

    public static DateTime ToIstanbul(this DateTime utc)
    {
        var istanbul = Istanbul.Value;
        var normalized = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        if (istanbul is null)
        {
            return normalized;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(normalized, istanbul);
    }

    private static TimeZoneInfo? TryResolveIstanbul()
    {
        try
        {
            var id = OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul";
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    public static string ToLocalDisplay(this DateTime utc)
    {
        var local = utc.ToIstanbul();
        return local.ToString("d MMMM yyyy dddd, HH.mm", CultureInfo.GetCultureInfo("tr-TR"));
    }
}