using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim")]
public sealed class DashboardController(DashboardAdminService dashboardService) : Controller
{
    [HttpGet("")]
    [HttpGet("panel")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await dashboardService.LoadAsync(cancellationToken);
        return View(model);
    }
}