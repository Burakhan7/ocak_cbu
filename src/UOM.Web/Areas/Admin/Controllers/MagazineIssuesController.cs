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
[Route("yonetim/dergiler")]
public sealed class MagazineIssuesController(MagazineIssueAdminService service, MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await service.ListAsync(cancellationToken));

    [HttpGet("yeni")]
    public IActionResult Create() => View("Edit", new MagazineIssue { IsActive = true, IssueDate = DateOnly.FromDateTime(DateTime.UtcNow) });

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, item.CoverImageId, cancellationToken);
        return View(item);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        MagazineIssue model,
        IFormFile? coverImageUpload,
        string? coverImageAlt,
        bool removeCoverImage,
        CancellationToken cancellationToken)
    {
        if (!await AdminMediaFormBinding.TryBindCoverImageAsync(
                mediaAdmin, ModelState, model.CoverImageId, removeCoverImage, coverImageUpload, coverImageAlt,
                $"Dergi: {model.Title}", id => model.CoverImageId = id, cancellationToken))
        {
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        var result = await service.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            return View("Edit", model);
        }

        TempData["Success"] = "Dergi kaydedildi.";
        return RedirectToAction(nameof(Index));
    }
}