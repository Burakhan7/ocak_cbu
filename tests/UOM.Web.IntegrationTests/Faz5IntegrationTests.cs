using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Web.IntegrationTests;

public class Faz5IntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory(string dbName) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "tests");
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", dbName);
        });

    [Fact]
    public async Task Genel_merkez_and_teskilat_routes_return_ok()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        var client = factory.CreateClient();
        (await client.GetAsync("/genel-merkez")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/teskilat")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/videolar")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Quote_band_renders_on_home_and_youtube_lite_has_no_iframe_initially()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        await SeedParentContentAsync(factory.Services);

        var client = factory.CreateClient();
        var homeHtml = await client.GetStringAsync("/");
        homeHtml.Should().Contain("data-quote-band");
        homeHtml.Should().Contain("Birlik");

        var parentHtml = await client.GetStringAsync("/genel-merkez");
        parentHtml.Should().Contain("data-youtube-lite");
        parentHtml.Should().NotContain("youtube-nocookie.com/embed/");
    }

    [Fact]
    public async Task Board_member_without_consent_hides_photo_url()
    {
        using var factory = CreateFactory(Guid.NewGuid().ToString());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        await db.Database.EnsureCreatedAsync();

        const string photoPath = "images/board/photo.webp";
        var media = new MediaFile
        {
            StoragePath = photoPath,
            OriginalFileName = "photo.webp",
            ContentType = "image/webp",
            SizeBytes = 4,
            AltText = "Üye",
        };
        db.MediaFiles.Add(media);
        db.BoardMembers.Add(new BoardMember
        {
            FullName = "Gizli Foto",
            Title = "Üye",
            IsActive = true,
            ConsentOnFile = false,
            PhotoImageId = media.Id,
        });
        await db.SaveChangesAsync();

        var diskPath = Path.Combine(env.WebRootPath, "uploads", photoPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(diskPath)!);
        await File.WriteAllBytesAsync(diskPath, [0x52, 0x49, 0x46, 0x46]);

        var html = await factory.CreateClient().GetStringAsync("/teskilat");
        html.Should().Contain("Gizli Foto");
        html.Should().NotContain("/uploads/images/board/photo.webp");
        html.Should().Contain("Fotoğraf yok");
    }

    private static async Task SeedParentContentAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.Quotes.AddRange(
            new Quote { Text = "Birlik", Author = QuoteAuthor.Ataturk, IsActive = true, Order = 0 },
            new Quote { Text = "İkinci", Author = QuoteAuthor.Lider, IsActive = true, Order = 1 });
        db.Videos.Add(new Video
        {
            Title = "Test video",
            YouTubeVideoId = "dQw4w9WgXcQ",
            Source = VideoSource.OcakTV,
            PublishedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsFeatured = true,
        });
        await db.SaveChangesAsync();
    }
}