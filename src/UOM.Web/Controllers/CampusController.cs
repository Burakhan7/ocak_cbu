using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

[Route("cbu")]
public sealed class CampusController(IPublicCampusHubService hub) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await hub.GetHubAsync(cancellationToken);
        return View(model);
    }
}