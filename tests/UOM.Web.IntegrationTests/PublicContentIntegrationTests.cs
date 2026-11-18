using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
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
}