using FluentAssertions;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using AppHtmlSanitizer = UOM.Application.Abstractions.IHtmlSanitizer;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Html;
using UOM.Infrastructure.Identity;
using UOM.Application.Features.Public.Models;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Time;

namespace UOM.Application.Tests.Features.Public;

public class PublicNewsServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetFeaturedForHomeAsync_returns_only_published_items()
    {
        await using var db = CreateDbContext();
        db.NewsPosts.AddRange(
            new NewsPost
            {
                Title = "Taslak",
                Slug = "taslak",
                BodyHtml = "<p>x</p>",
                Status = PublishStatus.Draft,
                Scope = NewsScope.Province,
            },
            new NewsPost
            {
                Title = "Yayında",
                Slug = "yayinda",
                BodyHtml = "<p>y</p>",
                Status = PublishStatus.Published,
                PublishedAtUtc = Now.AddHours(-1),
                Scope = NewsScope.Province,
                IsFeatured = true,
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var items = await service.GetFeaturedForHomeAsync(1, 4);

        items.Should().ContainSingle();
        items[0].Title.Should().Be("Yayında");
    }

    [Fact]
    public async Task GetFeaturedForHomeAsync_excludes_featured_from_regular_slot()
    {
        await using var db = CreateDbContext();
        db.NewsPosts.AddRange(
            new NewsPost
            {
                Title = "Öne çıkan",
                Slug = "one-cikan",
                BodyHtml = "<p>a</p>",
                Status = PublishStatus.Published,
                PublishedAtUtc = Now.AddHours(-1),
                Scope = NewsScope.Province,
                IsFeatured = true,
            },
            new NewsPost
            {
                Title = "Diğer",
                Slug = "diger",
                BodyHtml = "<p>b</p>",
                Status = PublishStatus.Published,
                PublishedAtUtc = Now.AddHours(-2),
                Scope = NewsScope.Province,
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var items = await service.GetFeaturedForHomeAsync(1, 2);

        items.Should().HaveCount(2);
        items.Select(i => i.Slug).Should().Equal("one-cikan", "diger");
    }

    [Fact]
    public async Task ListAsync_returns_published_items_using_same_visibility_as_home()
    {
        await using var db = CreateDbContext();
        db.NewsPosts.AddRange(
            new NewsPost
            {
                Title = "Liste ve ana sayfa",
                Slug = "listede",
                BodyHtml = "<p>c</p>",
                Status = PublishStatus.Published,
                PublishedAtUtc = Now.AddHours(-1),
                Scope = NewsScope.Province,
            },
            new NewsPost
            {
                Title = "Gizli taslak",
                Slug = "taslak-liste",
                BodyHtml = "<p>d</p>",
                Status = PublishStatus.Draft,
                Scope = NewsScope.Province,
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var page = await service.ListAsync(new NewsListQuery(null, null, 1, 12));
        var home = await service.GetFeaturedForHomeAsync(1, 4);

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(i => i.Slug == "listede");
        home.Should().Contain(i => i.Slug == "listede");
        home.Should().NotContain(i => i.Slug == "taslak-liste");
    }

    private static PublicNewsService CreateService(AppDbContext db)
    {
        AppHtmlSanitizer sanitizer = new GanssHtmlSanitizer();
        IClock clock = new FixedClock(Now);
        return new PublicNewsService(db, clock, sanitizer);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new SystemClock(), new NullAuditUserProvider());
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }
}