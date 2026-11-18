using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanManageSiteSettings)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/site-ayarlari")]
public sealed class SiteSettingsController(SiteSettingsAdminService settingsService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetSingleAsync(cancellationToken);
        return View(settings);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettings model, CancellationToken cancellationToken)
    {
        var result = await settingsService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            return View(model);
        }

        TempData["Success"] = "Site ayarları güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}