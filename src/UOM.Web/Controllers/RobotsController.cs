using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using UOM.Application.Caching;
using UOM.Web.Seo;

namespace UOM.Web.Controllers;

public sealed class RobotsController(IConfiguration configuration) : Controller
{
    [HttpGet("/robots.txt")]
    [OutputCache(Duration = 3600, Tags = [CacheTags.Pages])]
    public IActionResult Index()
    {
        var sitemap = SiteUrl.Absolute(Request, configuration, "/sitemap.xml");
        var body = $"""
            User-agent: *
            Disallow: /yonetim
            Disallow: /onizleme

            Sitemap: {sitemap}

            """;
        return Content(body, "text/plain", Encoding.UTF8);
    }
}
