using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

[Route("videolar")]
public sealed class VideoController(IPublicVideoService videos) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var list = await videos.ListAsync(cancellationToken);
        return View(list);
    }
}