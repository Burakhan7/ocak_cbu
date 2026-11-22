using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Privacy;

namespace UOM.Application.Tests.Privacy;

public class DataSubjectRequestServiceTests
{
    [Fact]
    public async Task Erase_removes_matching_rows_without_writing_the_email_to_the_audit_log()
    {
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb(now);
        var ev = new Event
        {
            Title = "Buluşma",
            Slug = "bulusma",
            BodyHtml = "<p>x</p>",
            StartsAtUtc = now,
            Status = PublishStatus.Published,
        };
        db.Events.Add(ev);
        db.ContactMessages.Add(new ContactMessage
        {
            FullName = "Ada",
            Email = "Ada@Example.com",
            Message = "Gizli mesaj metni",
            Subject = ContactSubject.General,
            KvkkConsentAtUtc = now,
            DeleteAfterUtc = now.AddDays(180),
        });
        await db.SaveChangesAsync();
        db.EventInterests.Add(new EventInterest
        {
            EventId = ev.Id,
            FullName = "Ada",
            Email = "ada@example.com",
            KvkkConsentAtUtc = now,
        });
        await db.SaveChangesAsync();

        var service = new DataSubjectRequestService(db, new AuditLogger(db, new NullAuditUser(), new FixedClock(now)));
        var found = await service.SearchAsync("ADA@example.com");
        found.Succeeded.Should().BeTrue();
        found.Value!.Contacts.Should().ContainSingle();
        found.Value.Interests.Should().ContainSingle();
        found.Value.Contacts[0].Subject.Should().Be("Genel");

        var erased = await service.EraseAsync("ada@example.com");
        erased.Succeeded.Should().BeTrue();
        erased.Value!.Total.Should().Be(2);

        (await db.ContactMessages.IgnoreQueryFilters().CountAsync()).Should().Be(0);
        (await db.EventInterests.IgnoreQueryFilters().CountAsync()).Should().Be(0);

        var audit = await db.AuditLogs.SingleAsync();
        audit.Action.Should().Be("KvkkDataSubjectErase");
        audit.ChangesJson.Should().NotContain("example.com");
        audit.ChangesJson.Should().NotContain("Ada");
        audit.ChangesJson.Should().Contain("contactMessages");
    }

    [Fact]
    public async Task Search_rejects_a_blank_email()
    {
        await using var db = CreateDb(DateTime.UtcNow);
        var service = new DataSubjectRequestService(db, new AuditLogger(db, new NullAuditUser(), new FixedClock(DateTime.UtcNow)));
        var result = await service.SearchAsync("  ");
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("e-posta");
    }

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
