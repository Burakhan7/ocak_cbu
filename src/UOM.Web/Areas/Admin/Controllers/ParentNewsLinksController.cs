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
[Route("yonetim/genel-merkez-haberleri")]
public sealed class ParentNewsLinksController(
    ParentNewsLinkAdminService service,
    MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await service.ListAsync(cancellationToken));

    [HttpGet("yeni")]
    public IActionResult Create() => View("Edit", new ParentNewsLink { IsActive = true, PublishedDate = DateOnly.FromDateTime(DateTime.UtcNow) });

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, item.ImageId, cancellationToken);
        return View(item);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        ParentNewsLink model,
        IFormFile? coverImageUpload,
        string? coverImageAlt,
        bool removeCoverImage,
        CancellationToken cancellationToken)
    {
        if (!await AdminMediaFormBinding.TryBindCoverImageAsync(
                mediaAdmin, ModelState, model.ImageId, removeCoverImage, coverImageUpload, coverImageAlt,
                $"GM haber: {model.Title}", id => model.ImageId = id, cancellationToken))
        {
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.ImageId, cancellationToken);
            return View("Edit", model);
        }

        var result = await service.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.ImageId, cancellationToken);
            return View("Edit", model);
        }

        TempData["Success"] = "Kayıt güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}