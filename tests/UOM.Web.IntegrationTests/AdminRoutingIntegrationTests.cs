using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UOM.Web.IntegrationTests;

public class AdminRoutingIntegrationTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "tests");
            builder.UseSetting("Database:UseInMemory", "true");
            builder.UseSetting("Database:InMemoryName", Guid.NewGuid().ToString());
        });

    [Fact]
    public async Task Get_yonetim_redirects_to_login_not_public_not_found()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/yonetim");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("/yonetim/hesap/giris");
    }

    [Fact]
    public async Task Reserved_slug_yonetim_is_not_handled_as_static_page()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/yonetim");
        var body = response.Headers.Location is null
            ? await response.Content.ReadAsStringAsync()
            : string.Empty;
        body.Should().NotContain("bulunamadı", because: "reserved paths must not hit PageController → 404 re-execute");
    }
}