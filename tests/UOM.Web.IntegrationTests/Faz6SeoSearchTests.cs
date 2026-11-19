using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Web.IntegrationTests;

public class Faz6SeoSearchTests
{
    private static WebApplicationFactory<Program> CreateFactory(string dbName) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "tests");
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", dbName);
            builder.UseSetting("Search:Provider", "Like");
            builder.UseSetting("Features:Search", "true");
        });

    [Fact]
    public async Task Sitemap_feed_and_robots_expose_published_news_only()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        await SeedAsync(factory.Services);
        var client = factory.CreateClient();

        var sitemap = await client.GetStringAsync("/sitemap.xml");
        sitemap.Should().Contain("/haberler/yayin-haber");
        sitemap.Should().NotContain("taslak-haber");
        sitemap.Should().Contain("/etkinlikler/kampus-soylesisi");

        var feed = await client.GetStringAsync("/feed.xml");
        feed.Should().Contain("yayin-haber");
        feed.Should().NotContain("taslak-haber");
        feed.Should().Contain("<rss");

        var robots = await client.GetStringAsync("/robots.txt");
        robots.Should().Contain("Disallow: /yonetim");
        robots.Should().Contain("Disallow: /onizleme");
        robots.Should().Contain("Sitemap:");
    }

    [Fact]
    public async Task Search_page_finds_published_content()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        await SeedAsync(factory.Services);
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/ara?q=burs");
        html.Should().Contain("/haberler/yayin-haber");
        html.Should().NotContain("taslak-haber");
        html.Should().Contain("Burs duyurusu");
    }

    [Fact]
    public async Task Trailing_slash_redirects_permanently()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/haberler/");
        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.OriginalString.Should().Be("/haberler");
    }

    [Fact]
    public async Task Stored_redirect_returns_301_and_counts_hit()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Redirects.Add(new Redirect
            {
                FromPath = "/eski-sayfa",
                ToPath = "/haberler",
                StatusCode = 301,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/eski-sayfa");
        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.OriginalString.Should().Be("/haberler");

        using var verify = factory.Services.CreateScope();
        var saved = await verify.ServiceProvider.GetRequiredService<AppDbContext>().Redirects.SingleAsync();
        saved.HitCount.Should().Be(1);
    }

    [Fact]
    public async Task Development_responses_send_noindex_and_news_json_ld()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        await SeedAsync(factory.Services);
        var client = factory.CreateClient();

        var home = await client.GetAsync("/");
        home.Headers.Should().ContainKey("X-Robots-Tag");
        home.Headers.GetValues("X-Robots-Tag").Should().Contain("noindex");
        var homeHtml = await home.Content.ReadAsStringAsync();
        homeHtml.Should().Contain("rel=\"canonical\"");
        homeHtml.Should().Contain("og:title");
        homeHtml.Should().Contain("application/ld+json");
        homeHtml.Should().Contain("Organization");

        var detail = await client.GetStringAsync("/haberler/yayin-haber");
        detail.Should().Contain("NewsArticle");
        detail.Should().Contain("BreadcrumbList");

        var evt = await client.GetStringAsync("/etkinlikler/kampus-soylesisi");
        evt.Should().Contain("\"@type\":\"Event\"");
    }

    [Fact]
    public async Task Published_news_cover_renders_responsive_picture()
    {
        const string largePath = "images/test/cover-large.webp";
        var variants = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["thumb"] = "images/test/cover-thumb.webp",
            ["medium"] = "images/test/cover-medium.webp",
            ["large"] = largePath,
        });

        using var factory = CreateFactory(Guid.NewGuid().ToString());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            var media = new MediaFile
            {
                StoragePath = "images/test/cover.webp",
                OriginalFileName = "cover.webp",
                ContentType = "image/webp",
                SizeBytes = 4,
                Width = 1200,
                Height = 800,
                AltText = "Kapak",
                Variants = variants,
            };
            db.MediaFiles.Add(media);
            await db.SaveChangesAsync();
            db.NewsPosts.Add(new NewsPost
            {
                Title = "Kapaklı haber",
                Slug = "kapakli-haber",
                BodyHtml = "<p>gövde</p>",
                Status = PublishStatus.Published,
                PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
                Scope = NewsScope.Province,
                CoverImageId = media.Id,
            });
            await db.SaveChangesAsync();
        }

        var html = await factory.CreateClient().GetStringAsync("/haberler/kapakli-haber");
        html.Should().Contain("<picture>");
        html.Should().Contain("srcset=");
        html.Should().Contain("/uploads/" + largePath);
        html.Should().Contain("400w");
    }

    [Fact]
    public async Task Versioned_static_asset_is_immutable()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        var client = factory.CreateClient();
        var response = await client.GetAsync("/css/tokens.css?v=test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.ToString().Should().Contain("immutable");
    }

    private static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (await db.NewsPosts.AnyAsync())
        {
            return;
        }

        db.NewsPosts.Add(new NewsPost
        {
            Title = "Gizli taslak",
            Slug = "taslak-haber",
            BodyHtml = "<p>taslak</p>",
            Status = PublishStatus.Draft,
            Scope = NewsScope.Province,
        });
        db.NewsPosts.Add(new NewsPost
        {
            Title = "Burs ilanı",
            Slug = "yayin-haber",
            Summary = "Burs duyurusu",
            BodyHtml = "<p>yayın</p>",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            Scope = NewsScope.Province,
        });
        db.Events.Add(new Event
        {
            Title = "Kampüs söyleşisi",
            Slug = "kampus-soylesisi",
            BodyHtml = "<p>etkinlik</p>",
            StartsAtUtc = DateTime.UtcNow.AddDays(3),
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
        });
        await db.SaveChangesAsync();
    }
}
