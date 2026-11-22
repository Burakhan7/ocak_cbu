namespace UOM.Application.Privacy;

public static class KvkkRetentionCalendar
{
    public static DateTime ContactDeleteAfterUtc(DateTime anchorUtc, int retentionDays) =>
        DateTime.SpecifyKind(anchorUtc, DateTimeKind.Utc).AddDays(retentionDays);

    public static DateTime EventEndUtc(DateTime startsAtUtc, DateTime? endsAtUtc) =>
        endsAtUtc ?? startsAtUtc;

    public static DateTime EventInterestDeleteAfterUtc(DateTime eventEndUtc, int daysAfterEvent) =>
        DateTime.SpecifyKind(eventEndUtc, DateTimeKind.Utc).AddDays(daysAfterEvent);
}
