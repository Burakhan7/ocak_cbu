using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UOM.Application.Abstractions;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Web.IntegrationTests;

public class CampusLayerIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory(string dbName) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "tests");
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", dbName);
        });

    [Fact]
    public async Task Expired_announcement_not_listed_on_public_duyurular()
    {
        var dbName = Guid.NewGuid().ToString();
        using var factory = CreateFactory(dbName);
        await SeedAnnouncementAsync(factory.Services, expired: true);
        var client = factory.CreateClient();
        var response = await client.GetAsync("/duyurular");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Test Burs");
    }

    [Fact]
    public async Task Resource_download_increments_counter_for_link_type()
    {
        var dbName = Guid.NewGuid().ToString();
        using var factory = CreateFactory(dbName);
        var resourceId = await SeedLinkResourceAsync(factory.Services);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync($"/cbu/kaynaklar/{resourceId}/indir");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Resources.Where(r => r.Id == resourceId).Select(r => r.DownloadCount).SingleAsync();
        count.Should().Be(1);
    }

    private static async Task SeedAnnouncementAsync(IServiceProvider services, bool expired)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        await db.Database.EnsureCreatedAsync();
        db.Announcements.Add(new Announcement
        {
            Title = "Test Burs",
            Slug = "test-burs",
            Body = "Örnek",
            Type = AnnouncementType.Scholarship,
            Status = PublishStatus.Published,
            PublishedAtUtc = clock.UtcNow.AddDays(-1),
            ExpiresAtUtc = expired ? clock.UtcNow.AddHours(-1) : clock.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedLinkResourceAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        await db.Database.EnsureCreatedAsync();
        var resource = new Resource
        {
            Title = "Örnek Link",
            Slug = "ornek-link",
            Type = ResourceType.Link,
            ExternalUrl = "https://example.com/resource",
            Status = PublishStatus.Published,
            PublishedAtUtc = clock.UtcNow.AddDays(-1),
        };
        db.Resources.Add(resource);
        await db.SaveChangesAsync();
        return resource.Id;
    }
}