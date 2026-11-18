using System.Globalization;

namespace UOM.Application.Formatting;

public static class DisplayDateTimeExtensions
{
    private static readonly TimeZoneInfo Istanbul =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul");

    public static DateTime ToIstanbul(this DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            Istanbul);

    public static string ToLocalDisplay(this DateTime utc)
    {
        var local = utc.ToIstanbul();
        return local.ToString("d MMMM yyyy dddd, HH.mm", CultureInfo.GetCultureInfo("tr-TR"));
    }
}