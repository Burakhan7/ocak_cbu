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

public class HomePageIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory(string dbName) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "tests");
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", dbName);
        });

    [Fact]
    public async Task Home_with_database_and_published_news_returns_ok()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        await SeedAsync(factory.Services);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("yayin-haber");
        html.Should().NotContain("Beklenmeyen bir hata oluştu");
    }

    [Fact]
    public async Task Home_upcoming_events_render_cover_from_uploads()
    {
        const string mediumPath = "images/events/kapak-medium.webp";
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        await SeedEventCoverAsync(factory.Services, mediumPath);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var html = await client.GetStringAsync("/");

        var eventsAt = html.IndexOf("<h2 class=\"h4 mb-0\">Yaklaşan etkinlikler</h2>", StringComparison.Ordinal);
        var imageAt = html.IndexOf("src=\"/uploads/" + mediumPath + "\"", eventsAt, StringComparison.Ordinal);
        var newsAt = html.IndexOf("<h2 class=\"h4 mb-0\">Haberler</h2>", StringComparison.Ordinal);
        var showcaseAt = html.IndexOf("home-showcase", StringComparison.Ordinal);

        eventsAt.Should().BeGreaterThanOrEqualTo(0);
        imageAt.Should().BeGreaterThan(eventsAt);
        newsAt.Should().BeGreaterThan(imageAt);
        html.Should().Contain("/etkinlikler/kapakli-etkinlik");
        html.Should().NotContain("görsel yok");

        var heroAt = html.IndexOf("/images/hero-banner.jpg", StringComparison.Ordinal);
        heroAt.Should().BeGreaterThanOrEqualTo(0);
        html.Should().Contain("Alparslan Türkeş, Mustafa Kemal Atatürk ve Devlet Bahçeli.");
        showcaseAt.Should().BeGreaterThan(heroAt);
        eventsAt.Should().BeGreaterThan(showcaseAt);
        html[showcaseAt..eventsAt].Should().Contain("/etkinlikler/kapakli-etkinlik");
        html[showcaseAt..eventsAt].Should().Contain("Etkinlik");
        html[showcaseAt..eventsAt].Should().NotContain("Önceki slayt");
        html.Should().NotContain("Ülkü Ocakları Manisa ile tanış");
        html.Should().NotContain("home-hero-event");
        html.Should().NotContain("Yaklaşan etkinlik yakında burada.");
        html.Should().NotContain("/uploads/images/events/kapak-large.webp");
        html.Should().NotContain("müsün?");
        html.Should().NotContain("cbu-home");
        html.Should().NotContain("Öğrenci rehberi");
        html.Should().NotContain("Kaynak arşivi");
        html.Should().NotContain("/cbu/rehber");
        html.Should().NotContain("/cbu/kaynaklar");
    }

    [Fact]
    public async Task Home_cbu_hub_uses_settings_cover_and_parent_news_is_two_columns()
    {
        const string coverPath = "images/cbu/hub-medium.webp";
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        await SeedHubAndParentNewsAsync(factory.Services, coverPath);

        var client = factory.CreateClient();
        var home = await client.GetStringAsync("/");
        var hub = await client.GetStringAsync("/genel-merkez");

        home.Should().NotContain("src=\"/uploads/" + coverPath + "\"");
        home.Should().NotContain("cbu-home");
        home.Should().NotContain("müsün?");
        home.Should().Contain("col-12 col-lg-6");
        home.Should().Contain("parent-news-grid");
        home.Should().Contain("Panel ozeti");
        home.Should().Contain("media-cover");
        home.Should().NotContain("parent-news-thumb");
        home.Should().NotContain("col-6 col-lg-3");

        hub.Should().Contain("parent-news-grid");
        hub.Should().Contain("col-12 col-lg-6");
        hub.Should().Contain("Panel ozeti");
        hub.Should().Contain("media-cover");
        hub.Should().NotContain("parent-news-thumb");
    }

  private static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.SiteSettings.Add(new SiteSettings { SiteName = "Test Site" });
        db.NewsPosts.Add(new NewsPost
        {
            Title = "Yayın Haber",
            Slug = "yayin-haber",
            BodyHtml = "<p>y</p>",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            Scope = NewsScope.Province,
        });
        db.Events.Add(new Event
        {
            Title = "Yaklaşan",
            Slug = "yaklasan",
            BodyHtml = "<p>e</p>",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            StartsAtUtc = DateTime.UtcNow.AddDays(3),
            EndsAtUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedEventCoverAsync(IServiceProvider services, string mediumPath)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.SiteSettings.Add(new SiteSettings { SiteName = "Test Site" });
        var media = new MediaFile
        {
            StoragePath = "images/events/kapak.webp",
            OriginalFileName = "kapak.webp",
            ContentType = "image/webp",
            SizeBytes = 8,
            AltText = "Etkinlik kapağı",
            Variants = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["thumb"] = "images/events/kapak-thumb.webp",
                ["medium"] = mediumPath,
                ["large"] = "images/events/kapak-large.webp",
            }),
        };
        db.MediaFiles.Add(media);
        await db.SaveChangesAsync();

        db.Events.Add(new Event
        {
            Title = "Kapaklı etkinlik",
            Slug = "kapakli-etkinlik",
            BodyHtml = "<p>e</p>",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            StartsAtUtc = DateTime.UtcNow.AddDays(2),
            EndsAtUtc = DateTime.UtcNow.AddDays(2).AddHours(2),
            CoverImageId = media.Id,
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedHubAndParentNewsAsync(IServiceProvider services, string coverPath)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var media = new MediaFile
        {
            StoragePath = "images/cbu/hub.webp",
            OriginalFileName = "hub.webp",
            ContentType = "image/webp",
            SizeBytes = 8,
            AltText = "CBÜ kampüsü",
            Variants = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["thumb"] = "images/cbu/hub-thumb.webp",
                ["medium"] = coverPath,
                ["large"] = "images/cbu/hub-large.webp",
            }),
        };
        db.MediaFiles.Add(media);
        await db.SaveChangesAsync();

        db.SiteSettings.Add(new SiteSettings
        {
            SiteName = "Test Site",
            CbuHubCoverImageId = media.Id,
        });
        db.ParentNewsLinks.Add(new ParentNewsLink
        {
            Title = "GM kart basligi",
            Summary = "Panel ozeti",
            ImageUrl = "https://ulkuocaklari.org.tr/ornek.jpg",
            ExternalUrl = "https://ulkuocaklari.org.tr/blog/i/ornek",
            PublishedDate = new DateOnly(2026, 11, 2),
            IsActive = true,
        });
        await db.SaveChangesAsync();
    }
}