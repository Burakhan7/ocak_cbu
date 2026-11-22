using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UOM.Application.Features.Public;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Web.IntegrationTests;

public class GoogleMapsAndParentFillTests
{
    [Fact]
    public async Task District_map_uses_google_maps_or_a_turkish_notice()
    {
        using var missingKey = CreateFactory(Guid.NewGuid().ToString());
        await SeedDistrictAsync(missingKey.Services);
        var withoutKey = await missingKey.CreateClient().GetStringAsync("/teskilat");
        withoutKey.Should().Contain("Google Haritalar anahtarı tanımlı değil. Harita gösterilemiyor.");
        withoutKey.Should().NotContain("leaflet");
        withoutKey.Should().NotContain("maps/api/js");

        using var withKey = CreateFactory(Guid.NewGuid().ToString(), "test-key");
        await SeedDistrictAsync(withKey.Services);
        var client = withKey.CreateClient();
        var html = await client.GetStringAsync("/teskilat");
        html.Should().Contain("data-uom-map");
        html.Should().Contain("data-api-key=\"test-key\"");
        html.Should().Contain("/js/google-maps.js");
        html.Should().NotContain("unsafe-eval");
        html.Should().NotContain("Google Haritalar anahtarı tanımlı değil");
        html.Should().NotContain("leaflet");

        var loader = await client.GetStringAsync("/js/google-maps.js");
        loader.Should().Contain("maps.googleapis.com/maps/api/js");
        loader.Should().Contain("loading=async");
        loader.Should().Contain("v=weekly");
        loader.Should().NotContain("eval(");
    }

    [Fact]
    public async Task Homepage_fills_empty_slots_and_genel_merkez_stays_parent_only()
    {
        var items = Enumerable.Range(1, 6)
            .Select(i => new ParentSiteItem(
                $"GM Haber {i}",
                "kısa özet",
                null,
                $"https://ulkuocaklari.org.tr/blog/i/haber-{i}",
                new DateOnly(2026, 11, i),
                ParentSiteItemKind.News))
            .Append(new ParentSiteItem(
                "Ayni Link",
                "kopya",
                null,
                "https://ulkuocaklari.org.tr/blog/i/elle",
                new DateOnly(2026, 11, 2),
                ParentSiteItemKind.News))
            .Append(new ParentSiteItem(
                "GM Turnuva",
                "final",
                null,
                "https://ulkuocaklari.org.tr/blog/i/turnuva",
                new DateOnly(2026, 11, 18),
                ParentSiteItemKind.Event))
            .Append(new ParentSiteItem(
                "GM Seminer",
                "seminer",
                null,
                "https://ulkuocaklari.org.tr/blog/i/seminer",
                new DateOnly(2026, 11, 19),
                ParentSiteItemKind.Event))
            .ToList();

        using var factory = CreateFactory(Guid.NewGuid().ToString(), feed: new StubParentSiteFeed(items));
        await SeedMixedContentAsync(factory.Services);

        var client = factory.CreateClient();
        var home = await client.GetStringAsync("/");
        home.Should().Contain("Manisa Yerel Haber");
        home.Should().Contain("GM Haber 1");
        home.Should().Contain("Ana sitede oku");
        home.Should().Contain("Manisa Yerel Etkinlik");
        home.Should().Contain("GM Turnuva");
        home.Should().NotContain("GM Haber 5");
        home.Should().NotContain("Ayni Link");

        var hub = await client.GetStringAsync("/genel-merkez");
        hub.Should().Contain("Elle Eklenen GM");
        hub.Should().Contain("GM Haber 1");
        hub.Should().Contain("GM Haber 6");
        hub.Should().Contain("GM Turnuva");
        hub.Should().Contain("Ana sitede oku");
        hub.Should().Contain("Ocak TV Ornek");
        hub.IndexOf("Elle Eklenen GM", StringComparison.Ordinal)
            .Should().BeLessThan(hub.IndexOf("GM Haber 1", StringComparison.Ordinal));
        hub.Should().NotContain("Ayni Link");
        hub.Should().NotContain("Manisa Yerel Haber");
        hub.Should().NotContain("Manisa Yerel Etkinlik");
        hub.Should().NotContain("Manisa Il Videosu");
    }

    [Fact]
    public async Task Empty_local_homepage_is_filled_from_the_parent_feed()
    {
        var items = new List<ParentSiteItem>
        {
            new("GM Haber 1", "özet", null, "https://ulkuocaklari.org.tr/blog/i/haber-1", new DateOnly(2026, 11, 1), ParentSiteItemKind.News),
            new("GM Turnuva", "final", null, "https://ulkuocaklari.org.tr/blog/i/turnuva", new DateOnly(2026, 11, 2), ParentSiteItemKind.Event),
        };

        using var factory = CreateFactory(Guid.NewGuid().ToString(), feed: new StubParentSiteFeed(items));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.SiteSettings.Add(new SiteSettings { SiteName = "Test" });
            await db.SaveChangesAsync();
        }

        var home = await factory.CreateClient().GetStringAsync("/");
        home.Should().Contain("GM Haber 1");
        home.Should().Contain("GM Turnuva");
        home.Should().Contain("Ana sitede oku");
        home.Should().NotContain("Yayınlanmış haber bulunmuyor.");
        home.Should().NotContain("Henüz yayınlanmış yaklaşan etkinlik yok.");
    }

    private static WebApplicationFactory<Program> CreateFactory(string dbName, string? mapsKey = null, IParentSiteFeed? feed = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "tests");
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", dbName);
            if (!string.IsNullOrWhiteSpace(mapsKey))
            {
                builder.UseSetting("GoogleMaps:ApiKey", mapsKey);
            }

            if (feed is not null)
            {
                builder.ConfigureTestServices(services => services.AddSingleton(feed));
            }
        });

    private static async Task SeedDistrictAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Districts.Add(new District
        {
            Name = "Yunusemre",
            Slug = "yunusemre",
            IsActive = true,
            Latitude = 38.62,
            Longitude = 27.42,
            Address = "Test adres",
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedMixedContentAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.SiteSettings.Add(new SiteSettings { SiteName = "Test" });
        db.NewsPosts.Add(new NewsPost
        {
            Title = "Manisa Yerel Haber",
            Slug = "manisa-yerel-haber",
            BodyHtml = "<p>yerel</p>",
            Summary = "yerel özet",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-2),
            Scope = NewsScope.Province,
        });
        db.Events.Add(new Event
        {
            Title = "Manisa Yerel Etkinlik",
            Slug = "manisa-yerel-etkinlik",
            BodyHtml = "<p>etkinlik</p>",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-2),
            StartsAtUtc = DateTime.UtcNow.AddDays(2),
        });
        db.ParentNewsLinks.Add(new ParentNewsLink
        {
            Title = "Elle Eklenen GM",
            Summary = "panel",
            ExternalUrl = "https://ulkuocaklari.org.tr/blog/i/elle",
            PublishedDate = new DateOnly(2026, 11, 1),
            IsActive = true,
            Order = 0,
        });
        db.Videos.Add(new Video
        {
            Title = "Ocak TV Ornek",
            YouTubeVideoId = "abcdefghijk",
            Source = VideoSource.OcakTV,
            PublishedDate = new DateOnly(2026, 11, 1),
        });
        db.Videos.Add(new Video
        {
            Title = "Manisa Il Videosu",
            YouTubeVideoId = "zzzzzzzzzzz",
            Source = VideoSource.Province,
            PublishedDate = new DateOnly(2026, 11, 1),
        });
        await db.SaveChangesAsync();
    }

    private sealed class StubParentSiteFeed(IReadOnlyList<ParentSiteItem> items) : IParentSiteFeed
    {
        public Task<ParentSiteSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ParentSiteSnapshot(items, "https://ulkuocaklari.org.tr/blog"));
    }
}
