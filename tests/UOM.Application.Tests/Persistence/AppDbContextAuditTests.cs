using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Domain.Entities;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Time;

namespace UOM.Application.Tests.Persistence;

public class AppDbContextAuditTests
{
  [Fact]
    public async Task SaveChanges_sets_created_audit_fields()
    {
        var fixedNow = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDbContext(new FixedClock(fixedNow));

        db.Districts.Add(new District
        {
            Name = "Test",
            Slug = "test",
        });

        await db.SaveChangesAsync();

        var saved = await db.Districts.SingleAsync();
        saved.CreatedAtUtc.Should().Be(fixedNow);
        saved.IsDeleted.Should().BeFalse();
        saved.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public async Task Soft_deleted_entities_are_filtered_by_default()
    {
        await using var db = CreateDbContext(new SystemClock());

        db.Districts.Add(new District { Name = "Görünür", Slug = "gorunur" });
        var deleted = new District { Name = "Silinmiş", Slug = "silinmis" };
        db.Districts.Add(deleted);
        await db.SaveChangesAsync();

        deleted.IsDeleted = true;
        await db.SaveChangesAsync();

        var visible = await db.Districts.ToListAsync();
        visible.Should().ContainSingle(d => d.Slug == "gorunur");
    }

    private static AppDbContext CreateDbContext(IClock clock)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, clock, new NullAuditUserProvider());
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}