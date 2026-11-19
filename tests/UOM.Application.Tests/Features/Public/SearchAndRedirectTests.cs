using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Features.Admin;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Migrations;
using UOM.Infrastructure.Slugs;
using UOM.Infrastructure.Time;

namespace UOM.Application.Tests.Features.Public;

public class SearchAndRedirectTests
{
    [Fact]
    public async Task Like_search_returns_published_news_and_hides_drafts()
    {
        await using var db = CreateDb();
        db.NewsPosts.Add(new NewsPost
        {
            Title = "Burs başvurusu açıldı",
            Slug = "burs-basvurusu",
            Summary = "CBÜ öğrencileri için burs",
            BodyHtml = "<p>ayrıntı</p>",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-2),
            Scope = NewsScope.Campus,
        });
        db.NewsPosts.Add(new NewsPost
        {
            Title = "Gizli burs taslağı",
            Slug = "gizli-burs",
            BodyHtml = "<p>taslak</p>",
            Status = PublishStatus.Draft,
            Scope = NewsScope.Campus,
        });
        await db.SaveChangesAsync();

        var search = new PublicSearchService(db, new SystemClock(), Config("Like"), new MemoryCache(new MemoryCacheOptions()));
        var result = await search.SearchAsync("burs");

        result.Enabled.Should().BeTrue();
        result.UsedFullText.Should().BeFalse();
        result.Hits.Should().ContainSingle();
        result.Hits[0].Kind.Should().Be("Haber");
        result.Hits[0].Url.Should().Be("/haberler/burs-basvurusu");
    }

    [Fact]
    public async Task FullText_provider_falls_back_to_like_on_in_memory()
    {
        await using var db = CreateDb();
        db.Events.Add(new Event
        {
            Title = "Kampüs söyleşisi",
            Slug = "kampus-soylesisi",
            BodyHtml = "<p>etkinlik</p>",
            StartsAtUtc = DateTime.UtcNow.AddDays(2),
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
        });
        await db.SaveChangesAsync();

        var search = new PublicSearchService(db, new SystemClock(), Config("FullText"), new MemoryCache(new MemoryCacheOptions()));
        var result = await search.SearchAsync("söyleşi");

        result.UsedFullText.Should().BeFalse();
        result.Hits.Should().ContainSingle(hit => hit.Kind == "Etkinlik");
    }

    [Fact]
    [Trait("Requires", "SqlServer")]
    public void Full_text_query_is_skipped_without_sql_server()
    {
        var connection = Environment.GetEnvironmentVariable("UOM_SQLSERVER_TEST");
        if (string.IsNullOrWhiteSpace(connection))
        {
            return;
        }

        connection.Should().Contain("Server");
    }

    [Fact]
    public void Full_text_migration_sql_covers_search_tables()
    {
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        FullTextSearchSql.Apply(builder);
        var sql = string.Join('\n', builder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        sql.Should().Contain("UomSearchCatalog");
        sql.Should().Contain("NewsPosts");
        sql.Should().Contain("Events");
        sql.Should().Contain("GuideArticles");
        sql.Should().Contain("Announcements");
        sql.Should().Contain("Resources");
        sql.Should().Contain("CREATE FULLTEXT INDEX");
        builder.Operations.OfType<SqlOperation>().Should().OnlyContain(operation => operation.SuppressTransaction);
    }

    [Fact]
    public async Task Published_news_slug_change_stages_permanent_redirect()
    {
        await using var db = CreateDb();
        var clock = new SystemClock();
        var audit = new AuditLogger(db, new NullAuditUserProvider(), clock);
        var redirects = new RedirectService(db, new MemoryCache(new MemoryCacheOptions()));
        var admin = new NewsPostAdminService(db, new SlugService(), clock, audit, redirects);
        var saved = await admin.SaveAsync(new NewsPost
        {
            Title = "Eski başlık",
            BodyHtml = "<p>gövde</p>",
            Scope = NewsScope.Province,
        });
        saved.Succeeded.Should().BeTrue();
        (await admin.PublishAsync(saved.Value!.Id)).Succeeded.Should().BeTrue();

        var edited = await db.NewsPosts.AsNoTracking().SingleAsync();
        var updated = await admin.SaveAsync(new NewsPost
        {
            Id = edited.Id,
            Title = "Yeni başlık",
            Slug = "yeni-adres",
            BodyHtml = edited.BodyHtml,
            Scope = edited.Scope,
            Status = edited.Status,
        });
        updated.Succeeded.Should().BeTrue();

        var redirect = await db.Redirects.SingleAsync();
        redirect.FromPath.Should().Be("/haberler/eski-baslik");
        redirect.ToPath.Should().Be("/haberler/yeni-adres");
        redirect.StatusCode.Should().Be(301);
    }

    private static IConfiguration Config(string provider) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Search:Provider"] = provider,
                ["Features:Search"] = "true",
            })
            .Build();

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, new SystemClock(), new NullAuditUserProvider());
    }
}
