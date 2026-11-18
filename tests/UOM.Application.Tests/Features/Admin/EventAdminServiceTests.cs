using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Features.Admin;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Slugs;
using UOM.Infrastructure.Time;

namespace UOM.Application.Tests.Features.Admin;

public class EventAdminServiceTests
{
    [Fact]
    public async Task SaveAsync_creates_event_and_writes_audit_log()
    {
        await using var db = CreateDbContext();
        var service = new EventAdminService(db, new SlugService(), new SystemClock(), new AuditLogger(db, new NullAuditUserProvider(), new SystemClock()));

        var result = await service.SaveAsync(new Event
        {
            Title = "Test Etkinlik",
            BodyHtml = "<p>İçerik</p>",
            StartsAtUtc = DateTime.UtcNow.AddDays(3),
            Type = EventType.Seminar,
        });

        result.Succeeded.Should().BeTrue();
        result.Value!.Id.Should().BeGreaterThan(0);

        var audit = await db.AuditLogs.SingleAsync();
        audit.Action.Should().Be("Create");
        audit.EntityName.Should().Be(nameof(Event));
        audit.EntityId.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task SaveAsync_persists_when_body_html_is_null()
    {
        await using var db = CreateDbContext();
        var service = new EventAdminService(db, new SlugService(), new SystemClock(), new AuditLogger(db, new NullAuditUserProvider(), new SystemClock()));

        var model = new Event
        {
            Title = "Etkinlik",
            StartsAtUtc = DateTime.UtcNow.AddDays(1),
            Type = EventType.Seminar,
        };
        model.BodyHtml = null!;

        var result = await service.SaveAsync(model);
        result.Succeeded.Should().BeTrue();
        (await db.Events.SingleAsync()).BodyHtml.Should().BeEmpty();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new SystemClock(), new NullAuditUserProvider());
    }
}