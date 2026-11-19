using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UOM.Application.Caching;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

[Route("genel-merkez")]
[OutputCache(Duration = 300, Tags = [CacheTags.Parent], VaryByQueryKeys = ["*"])]
public sealed class ParentController(IPublicParentHubService parentHub) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int sayfa = 1, CancellationToken cancellationToken = default)
    {
        var page = await parentHub.GetHubPageAsync(sayfa, 12, cancellationToken);
        return View(page);
    }
}