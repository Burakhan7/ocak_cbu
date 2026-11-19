using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Areas.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanManageSiteSettings)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/site-ayarlari")]
public sealed class SiteSettingsController(SiteSettingsAdminService settingsService, MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetSingleAsync(cancellationToken);
        await AdminMediaFormBinding.SetLogoImagePreviewAsync(mediaAdmin, ViewData, settings.LogoImageId, cancellationToken);
        return View(settings);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        SiteSettings model,
        IFormFile? logoImageUpload,
        string? logoImageAlt,
        bool removeLogoImage,
        CancellationToken cancellationToken)
    {
        if (!await AdminMediaFormBinding.TryBindLogoImageAsync(
                mediaAdmin,
                ModelState,
                model.LogoImageId,
                removeLogoImage,
                logoImageUpload,
                logoImageAlt,
                $"Logo: {model.SiteName ?? "Ülkü Ocakları Manisa"}",
                id => model.LogoImageId = id,
                cancellationToken))
        {
            await AdminMediaFormBinding.SetLogoImagePreviewAsync(mediaAdmin, ViewData, model.LogoImageId, cancellationToken);
            return View(model);
        }

        var result = await settingsService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetLogoImagePreviewAsync(mediaAdmin, ViewData, model.LogoImageId, cancellationToken);
            return View(model);
        }

        TempData["Success"] = "Site ayarları güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}