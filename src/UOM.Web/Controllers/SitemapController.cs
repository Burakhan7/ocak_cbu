using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using UOM.Application.Caching;
using UOM.Application.Features.Public;
using UOM.Web.Seo;

namespace UOM.Web.Controllers;

public sealed class SitemapController(ISitemapService sitemap, IConfiguration configuration) : Controller
{
    [HttpGet("/sitemap.xml")]
    [OutputCache(Duration = 3600, Tags = [CacheTags.Sitemap])]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var xml = await sitemap.BuildXmlAsync(SiteUrl.Base(Request, configuration), cancellationToken);
        return Content(xml, "application/xml", Encoding.UTF8);
    }
}
