using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Areas.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteCampusContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/duyurular")]
public sealed class AnnouncementsController(AnnouncementAdminService announcementService, MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await announcementService.ListAsync(cancellationToken));

    [HttpGet("yeni")]
    public IActionResult Create() => View("Edit", new Announcement { ExpiresAtUtc = DateTime.UtcNow.AddMonths(1) });

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var announcement = await announcementService.GetAsync(id, cancellationToken);
        if (announcement is null)
        {
            return NotFound();
        }

        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, announcement.CoverImageId, cancellationToken);
        return View(announcement);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        Announcement model,
        IFormFile? coverImageUpload,
        string? coverImageAlt,
        bool removeCoverImage,
        CancellationToken cancellationToken)
    {
        if (!await AdminMediaFormBinding.TryBindCoverImageAsync(
                mediaAdmin,
                ModelState,
                model.CoverImageId,
                removeCoverImage,
                coverImageUpload,
                coverImageAlt,
                $"Duyuru: {model.Title}",
                id => model.CoverImageId = id,
                cancellationToken))
        {
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        var result = await announcementService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        TempData["Success"] = "Duyuru kaydedildi.";
        if (model.Id == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Edit), new { id = result.Value!.Id });
    }

    [HttpPost("{id:int}/onaya-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        var result = await announcementService.SubmitForApprovalAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Onaya gönderildi." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/yayinla")]
    [Authorize(Policy = PolicyNames.CanPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await announcementService.PublishAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Yayınlandı." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await announcementService.DeleteAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Kayıt silindi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}