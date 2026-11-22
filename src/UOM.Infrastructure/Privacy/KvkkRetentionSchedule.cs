namespace UOM.Infrastructure.Privacy;

public static class KvkkRetentionSchedule
{
    public static TimeSpan DelayUntilNextRun(DateTime utcNow, TimeZoneInfo zone)
    {
        var normalized = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(normalized, zone);
        var nextLocal = local.Date.AddHours(3);
        if (local >= nextLocal)
        {
            nextLocal = nextLocal.AddDays(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), zone);
        var delay = nextUtc - normalized;
        return delay < TimeSpan.FromMinutes(1) ? TimeSpan.FromHours(24) : delay;
    }

    public static TimeZoneInfo IstanbulOrFixedOffset()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Turkey-Fixed", TimeSpan.FromHours(3), "Turkey", "Turkey");
    }
}
