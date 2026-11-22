using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using UOM.Application.Abstractions;
using UOM.Application.Privacy;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Privacy;

namespace UOM.Application.Tests.Privacy;

public class KvkkRetentionCalendarTests
{
    private static readonly DateTime Anchor = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Contact_delete_after_adds_configured_days()
    {
        KvkkRetentionCalendar.ContactDeleteAfterUtc(Anchor, 180)
            .Should().Be(Anchor.AddDays(180));
    }

    [Fact]
    public void Event_interest_uses_end_then_falls_back_to_start()
    {
        var start = Anchor;
        var end = Anchor.AddHours(3);
        KvkkRetentionCalendar.EventEndUtc(start, end).Should().Be(end);
        KvkkRetentionCalendar.EventEndUtc(start, null).Should().Be(start);
        KvkkRetentionCalendar.EventInterestDeleteAfterUtc(end, 30).Should().Be(end.AddDays(30));
    }
}

public class KvkkRetentionScheduleTests
{
    [Fact]
    public void Next_run_is_03_00_in_the_turkey_offset()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Turkey-Fixed", TimeSpan.FromHours(3), "Turkey", "Turkey");
        var atThreeLocal = new DateTime(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);
        KvkkRetentionSchedule.DelayUntilNextRun(atThreeLocal, zone).Should().Be(TimeSpan.FromHours(24));

        var oneHourBefore = new DateTime(2026, 11, 21, 23, 0, 0, DateTimeKind.Utc);
        KvkkRetentionSchedule.DelayUntilNextRun(oneHourBefore, zone).Should().Be(TimeSpan.FromHours(1));
    }
}

public class KvkkRetentionServiceTests
{
    [Fact]
    public async Task Purge_hard_deletes_expired_rows_and_keeps_the_rest()
    {
        var now = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb(now);
        var expiredEvent = new Event
        {
            Title = "Eski söyleşi",
            Slug = "eski-soylesi",
            BodyHtml = "<p>bitti</p>",
            StartsAtUtc = now.AddDays(-40),
            EndsAtUtc = now.AddDays(-40),
            Status = PublishStatus.Published,
        };
        var upcoming = new Event
        {
            Title = "Yeni söyleşi",
            Slug = "yeni-soylesi",
            BodyHtml = "<p>gelecek</p>",
            StartsAtUtc = now.AddDays(2),
            EndsAtUtc = now.AddDays(2).AddHours(2),
            Status = PublishStatus.Published,
        };
        db.Events.AddRange(expiredEvent, upcoming);
        db.ContactMessages.AddRange(
            Contact("eski@example.com", now.AddDays(-1)),
            Contact("yeni@example.com", now.AddDays(20)));
        await db.SaveChangesAsync();

        db.EventInterests.AddRange(
            new EventInterest
            {
                EventId = expiredEvent.Id,
                FullName = "Eski",
                Email = "eski@example.com",
                KvkkConsentAtUtc = now.AddDays(-40),
            },
            new EventInterest
            {
                EventId = upcoming.Id,
                FullName = "Yeni",
                Email = "yeni@example.com",
                KvkkConsentAtUtc = now,
            });
        await db.SaveChangesAsync();

        var result = await new KvkkRetentionService(db, new FixedClock(now), Config(), NullLogger<KvkkRetentionService>.Instance)
            .PurgeExpiredAsync();

        result.ContactMessages.Should().Be(1);
        result.EventInterests.Should().Be(1);

        var contacts = await db.ContactMessages.IgnoreQueryFilters().ToListAsync();
        contacts.Should().ContainSingle(c => c.Email == "yeni@example.com");
        var interests = await db.EventInterests.IgnoreQueryFilters().Include(i => i.Event).ToListAsync();
        interests.Should().ContainSingle();
        interests[0].Email.Should().Be("yeni@example.com");
        interests[0].DeleteAfterUtc.Should().Be(upcoming.EndsAtUtc!.Value.AddDays(30));
    }

    private static ContactMessage Contact(string email, DateTime? deleteAfter) => new()
    {
        FullName = "Ada",
        Email = email,
        Message = "Merhaba",
        Subject = ContactSubject.General,
        KvkkConsentAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DeleteAfterUtc = deleteAfter,
    };

    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Kvkk:ContactRetentionDays"] = "180",
            ["Kvkk:EventInterestRetentionDaysAfterEvent"] = "30",
        })
        .Build();

    private static AppDbContext CreateDb(DateTime now)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, new FixedClock(now), new NullAuditUser());
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }

    private sealed class NullAuditUser : IAuditUserProvider
    {
        public string? GetCurrentUserId() => null;
    }
}
