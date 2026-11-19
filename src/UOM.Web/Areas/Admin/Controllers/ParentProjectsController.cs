using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Areas.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanManageParentLayer)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/projeler")]
public sealed class ParentProjectsController(ParentProjectAdminService service, MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await service.ListAsync(cancellationToken));

    [HttpGet("yeni")]
    public IActionResult Create() => View("Edit", new ParentProject { IsActive = true });

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, item.LogoImageId, cancellationToken);
        return View(item);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        ParentProject model,
        IFormFile? coverImageUpload,
        string? coverImageAlt,
        bool removeCoverImage,
        CancellationToken cancellationToken)
    {
        if (!await AdminMediaFormBinding.TryBindCoverImageAsync(
                mediaAdmin, ModelState, model.LogoImageId, removeCoverImage, coverImageUpload, coverImageAlt,
                $"Proje: {model.Name}", id => model.LogoImageId = id, cancellationToken))
        {
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.LogoImageId, cancellationToken);
            return View("Edit", model);
        }

        var result = await service.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            return View("Edit", model);
        }

        TempData["Success"] = "Proje kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Kayıt silindi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}