using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UOM.Web.IntegrationTests;

public class PublicSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PublicSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "");
        }).CreateClient();
    }

    [Fact]
    public async Task Home_returns_ok_with_merhaba()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Merhaba");
    }

    [Fact]
    public async Task Health_returns_ok_without_database()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}