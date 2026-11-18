using System.Globalization;
using System.Text;
using UOM.Application.Features.Public.Models;

namespace UOM.Infrastructure.Features.Public;

internal static class IcalCalendarBuilder
{
    public static byte[] BuildFeed(IEnumerable<EventListItemDto> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//UOM//Manisa Etkinlikler//TR");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");

        foreach (var evt in events)
        {
            AppendEvent(sb, evt);
        }

        sb.AppendLine("END:VCALENDAR");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] BuildSingle(EventDetailDto evt)
    {
        var list = new EventListItemDto(evt.Id, evt.Title, evt.Slug, evt.Summary, evt.StartsAtUtc, evt.EndsAtUtc, evt.Venue?.Name, evt.CoverImageUrl);
        return BuildFeed([list]);
    }

    private static void AppendEvent(StringBuilder sb, EventListItemDto evt)
    {
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{evt.Slug}@ulkuocaklari-manisa");
        sb.AppendLine("SUMMARY:" + Escape(evt.Title));
        sb.AppendLine("DTSTART:" + FormatUtc(evt.StartsAtUtc));
        sb.AppendLine("DTEND:" + FormatUtc(evt.EndsAtUtc ?? evt.StartsAtUtc.AddHours(2)));
        if (!string.IsNullOrWhiteSpace(evt.VenueName))
        {
            sb.AppendLine("LOCATION:" + Escape(evt.VenueName));
        }

        sb.AppendLine("URL;VALUE=URI:https://ulkuocaklari.local/etkinlikler/" + evt.Slug);
        sb.AppendLine("END:VEVENT");
    }

    private static string FormatUtc(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}