using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using UOM.Web.Hosting;
using UOM.Web.Security;

namespace UOM.Web.IntegrationTests;

public class Faz7SecurityTests
{
    [Fact]
    public async Task Home_sends_security_headers_and_a_nonce_that_matches_the_document()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "");
        });
        var client = factory.CreateClient();

        await AssertDocument(await client.GetAsync("/"));
        await AssertDocument(await client.GetAsync("/"));
    }

    [Fact]
    public async Task Production_https_response_includes_hsts()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "");
            builder.UseEnvironment("Production");
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Strict-Transport-Security").Single()
            .Should().Contain("max-age=31536000")
            .And.Contain("includeSubDomains");
    }

    [Fact]
    public async Task Contact_posts_are_rate_limited_per_client()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "");
            builder.UseSetting("RateLimit:FormsPerMinutePerIp", "2");
        });
        var client = factory.CreateClient();

        async Task<HttpStatusCode> PostAsync()
        {
            var response = await client.PostAsync("/iletisim", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["FullName"] = "Ada",
            }));
            return response.StatusCode;
        }

        (await PostAsync()).Should().NotBe(HttpStatusCode.TooManyRequests);
        (await PostAsync()).Should().NotBe(HttpStatusCode.TooManyRequests);
        (await PostAsync()).Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Uploads_do_not_serve_scripts()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "");
        });
        using var scope = factory.Services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var folder = Path.Combine(env.WebRootPath, "uploads", "faz7-security");
        Directory.CreateDirectory(folder);
        var scriptPath = Path.Combine(folder, "evil.js");
        var imagePath = Path.Combine(folder, "pixel.webp");
        await File.WriteAllTextAsync(scriptPath, "alert(1)");
        await File.WriteAllBytesAsync(imagePath, "RIFF"u8.ToArray());

        try
        {
            var client = factory.CreateClient();
            var blocked = await client.GetAsync("/uploads/faz7-security/evil.js");
            blocked.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var allowed = await client.GetAsync("/uploads/faz7-security/pixel.webp");
            allowed.StatusCode.Should().Be(HttpStatusCode.OK);
            allowed.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
            allowed.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'none'");
        }
        finally
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }

            if (File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Fact]
    public void Migrate_flag_is_recognized_case_insensitively()
    {
        MigrateCommand.IsRequested(["--migrate"]).Should().BeTrue();
        MigrateCommand.IsRequested(["--Migrate"]).Should().BeTrue();
        MigrateCommand.IsRequested(["--", "run"]).Should().BeFalse();
    }

    [Fact]
    public void Health_check_allows_loopback_or_the_shared_header_only()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Health:AccessKey"] = "secret" })
            .Build();
        var production = new StubEnvironment("Production");

        var remote = new DefaultHttpContext();
        remote.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.8");
        HealthCheckAccess.IsAllowed(remote, production, config).Should().BeFalse();

        remote.Request.Headers[HealthCheckAccess.HeaderName] = "secret";
        HealthCheckAccess.IsAllowed(remote, production, config).Should().BeTrue();

        remote.Request.Headers.Remove(HealthCheckAccess.HeaderName);
        remote.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        HealthCheckAccess.IsAllowed(remote, production, config).Should().BeTrue();

        var localDev = new DefaultHttpContext();
        HealthCheckAccess.IsAllowed(localDev, new StubEnvironment("Development"), config).Should().BeTrue();
        HealthCheckAccess.IsAllowed(localDev, production, config).Should().BeFalse();
    }

    private static async Task AssertDocument(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "Referrer-Policy").Should().Be("strict-origin-when-cross-origin");
        Header(response, "Permissions-Policy").Should().Contain("camera=()").And.Contain("microphone=()").And.Contain("geolocation=()");
        Header(response, "X-Frame-Options").Should().Be("DENY");

        var csp = Header(response, "Content-Security-Policy");
        var scriptSrc = Regex.Match(csp, @"script-src ([^;]+)").Groups[1].Value;
        scriptSrc.Should().NotContain("unsafe-inline");
        scriptSrc.Should().NotContain("unsafe-eval");
        scriptSrc.Should().Contain("https://challenges.cloudflare.com");
        csp.Should().Contain("https://www.youtube-nocookie.com");
        csp.Should().Contain("https://*.tile.openstreetmap.org");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().NotContain(CspNonce.Placeholder);
        html.Should().NotContain("unpkg.com");
        var nonce = Regex.Match(html, @"nonce=""([a-f0-9]{32})""").Groups[1].Value;
        nonce.Should().NotBeNullOrEmpty();
        csp.Should().Contain($"'nonce-{nonce}'");
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values.Single()
            : throw new InvalidOperationException($"Missing header {name}.");

    private sealed class StubEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
