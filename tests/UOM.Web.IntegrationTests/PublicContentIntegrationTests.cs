using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Features.Admin;
using UOM.Infrastructure.Persistence;

namespace UOM.Web.IntegrationTests;

public class PublicContentIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory(string dbName) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "tests");
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", dbName);
        });

    private static async Task SeedNewsAsync(IServiceProvider services)
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
            Title = "Taslak Haber",
            Slug = "taslak-haber",
            BodyHtml = "<p>x</p>",
            Status = PublishStatus.Draft,
            Scope = NewsScope.Province,
        });
        db.NewsPosts.Add(new NewsPost
        {
            Title = "Yayın Haber",
            Slug = "yayin-haber",
            BodyHtml = "<p>y</p>",
            Status = PublishStatus.Published,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            Scope = NewsScope.Province,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Published_news_returns_ok_draft_returns_not_found()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedNewsAsync(factory.Services);
        (await client.GetAsync("/haberler/yayin-haber")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/haberler/taslak-haber")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Published_news_appears_on_home_and_news_index()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedNewsAsync(factory.Services);

        var homeHtml = await client.GetStringAsync("/");
        var listResponse = await client.GetAsync("/haberler");
        var listHtml = await listResponse.Content.ReadAsStringAsync();

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        homeHtml.Should().Contain("yayin-haber");
        listHtml.Should().Contain("yayin-haber");
        listHtml.Should().NotContain("taslak-haber");
        homeHtml.Should().NotContain("taslak-haber");
    }

    [Fact]
    public async Task Publish_flow_makes_news_visible_on_home_and_news_index()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var admin = scope.ServiceProvider.GetRequiredService<NewsPostAdminService>();
        var saved = await admin.SaveAsync(new NewsPost
        {
            Title = "Yayın akışı testi",
            BodyHtml = "<p>akış</p>",
            Scope = NewsScope.Province,
        });
        saved.Succeeded.Should().BeTrue();
        var publish = await admin.PublishAsync(saved.Value!.Id);
        publish.Succeeded.Should().BeTrue();

        var slug = saved.Value.Slug;
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var homeHtml = await client.GetStringAsync("/");
        var listHtml = await client.GetStringAsync("/haberler");
        var detailStatus = (await client.GetAsync($"/haberler/{slug}")).StatusCode;

        homeHtml.Should().Contain(slug);
        listHtml.Should().Contain(slug);
        detailStatus.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Honeypot_contact_field_accepts_silent_success()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var get = await client.GetAsync("/iletisim");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await get.Content.ReadAsStringAsync();
        var token = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        token.Should().NotBeNullOrEmpty();

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FullName"] = "Bot",
            ["Email"] = "bot@example.com",
            ["Subject"] = ContactSubject.General.ToString(),
            ["Message"] = "spam",
            ["KvkkConsent"] = "true",
            ["Website"] = "http://spam.example",
        };
        var response = await client.PostAsync("/iletisim", new FormUrlEncodedContent(form));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Mesajınız alındı");
    }

    [Fact]
    public async Task Published_news_cover_image_url_is_served_from_uploads()
    {
        const string originalPath = "images/test/cover.webp";
        const string largePath = "images/test/cover-large.webp";
        var variantsJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["large"] = largePath });

        using var factory = CreateFactory(Guid.NewGuid().ToString());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        await db.Database.EnsureCreatedAsync();

        var media = new MediaFile
        {
            StoragePath = originalPath,
            OriginalFileName = "cover.webp",
            ContentType = "image/webp",
            SizeBytes = 4,
            AltText = "Kapak",
            Variants = variantsJson,
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

        foreach (var relative in new[] { originalPath, largePath })
        {
            var diskPath = Path.Combine(env.WebRootPath, "uploads", relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(diskPath)!);
            await File.WriteAllBytesAsync(diskPath, [0x52, 0x49, 0x46, 0x46]);
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var detailHtml = await client.GetStringAsync("/haberler/kapakli-haber");
        var imageUrl = "/uploads/" + largePath;

        detailHtml.Should().Contain(imageUrl);
        (await client.GetAsync(imageUrl)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}