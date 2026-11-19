using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UOM.Application.Caching;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

[Route("videolar")]
[OutputCache(Duration = 300, Tags = [CacheTags.Videos], VaryByQueryKeys = ["*"])]
public sealed class VideoController(IPublicVideoService videos) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var list = await videos.ListAsync(cancellationToken);
        return View(list);
    }
}