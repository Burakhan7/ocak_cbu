using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UOM.Application.Caching;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

[Route("cbu")]
[OutputCache(Duration = 300, Tags = [CacheTags.Campus], VaryByQueryKeys = ["*"])]
public sealed class CampusController(IPublicCampusHubService hub) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await hub.GetHubAsync(cancellationToken);
        return View(model);
    }
}