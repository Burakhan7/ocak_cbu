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
        var imageAt = html.IndexOf("src=\"/uploads/" + mediumPath + "\"", StringComparison.Ordinal);
        var newsAt = html.IndexOf("<h2 class=\"h4 mb-0\">Haberler</h2>", StringComparison.Ordinal);

        eventsAt.Should().BeGreaterThanOrEqualTo(0);
        imageAt.Should().BeGreaterThan(eventsAt);
        newsAt.Should().BeGreaterThan(imageAt);
        html.Should().Contain("/etkinlikler/kapakli-etkinlik");
        html.Should().NotContain("görsel yok");
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
}