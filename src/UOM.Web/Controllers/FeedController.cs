using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using UOM.Application.Caching;
using UOM.Application.Features.Public;
using UOM.Web.Seo;

namespace UOM.Web.Controllers;

public sealed class FeedController(IFeedService feed, IConfiguration configuration) : Controller
{
    [HttpGet("/feed.xml")]
    [OutputCache(Duration = 300, Tags = [CacheTags.Feed])]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var xml = await feed.BuildRssAsync(SiteUrl.Base(Request, configuration), cancellationToken);
        return Content(xml, "application/rss+xml", Encoding.UTF8);
    }
}
