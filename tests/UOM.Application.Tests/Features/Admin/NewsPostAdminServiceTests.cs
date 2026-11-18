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

public class NewsPostAdminServiceTests
{
    [Fact]
    public async Task SaveAsync_creates_news_and_writes_audit_log()
    {
        await using var db = CreateDbContext();
        var service = new NewsPostAdminService(db, new SlugService(), new SystemClock(), new AuditLogger(db, new NullAuditUserProvider(), new SystemClock()));

        var result = await service.SaveAsync(new NewsPost
        {
            Title = "Test Haber",
            BodyHtml = "<p>İçerik</p>",
            Scope = NewsScope.Province,
        });

        result.Succeeded.Should().BeTrue();
        result.Value!.Id.Should().BeGreaterThan(0);
        result.Value.Slug.Should().Be("test-haber");

        var audit = await db.AuditLogs.SingleAsync();
        audit.Action.Should().Be("Create");
        audit.EntityName.Should().Be(nameof(NewsPost));
        audit.EntityId.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task SaveAsync_persists_when_body_html_not_in_form()
    {
        await using var db = CreateDbContext();
        var service = new NewsPostAdminService(db, new SlugService(), new SystemClock(), new AuditLogger(db, new NullAuditUserProvider(), new SystemClock()));

        var model = new NewsPost { Title = "Sadece başlık", Scope = NewsScope.Province };
        model.BodyHtml = null!;

        var result = await service.SaveAsync(model);
        result.Succeeded.Should().BeTrue();
        var saved = await db.NewsPosts.SingleAsync();
        saved.BodyHtml.Should().BeEmpty();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new SystemClock(), new NullAuditUserProvider());
    }
}