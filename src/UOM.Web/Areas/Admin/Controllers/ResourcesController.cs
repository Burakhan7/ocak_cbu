using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Domain.Enums;
using UOM.Web.Areas.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteCampusContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/kaynaklar")]
public sealed class ResourcesController(
    ResourceAdminService resourceService,
    CampusAdminService campusService,
    MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await resourceService.ListAsync(cancellationToken));

    [HttpGet("yeni")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadFacultiesAsync(cancellationToken);
        return View("Edit", new Resource());
    }

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var resource = await resourceService.GetAsync(id, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        await LoadFacultiesAsync(cancellationToken);
        await AdminMediaFormBinding.SetResourceFilePreviewAsync(mediaAdmin, ViewData, resource.FileId, cancellationToken);
        return View(resource);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        Resource model,
        IFormFile? resourceFileUpload,
        bool removeResourceFile,
        CancellationToken cancellationToken)
    {
        if (model.Type == ResourceType.File
            && !await AdminMediaFormBinding.TryBindResourceFileAsync(
                mediaAdmin,
                ModelState,
                model.FileId,
                removeResourceFile,
                resourceFileUpload,
                model.Title,
                id => model.FileId = id,
                cancellationToken))
        {
            await LoadFacultiesAsync(cancellationToken);
            await AdminMediaFormBinding.SetResourceFilePreviewAsync(mediaAdmin, ViewData, model.FileId, cancellationToken);
            return View("Edit", model);
        }

        var result = await resourceService.SaveAsync(model, cancellationToken);
        await LoadFacultiesAsync(cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetResourceFilePreviewAsync(mediaAdmin, ViewData, model.FileId, cancellationToken);
            return View("Edit", model);
        }

        TempData["Success"] = "Kaynak kaydedildi.";
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
        var result = await resourceService.SubmitForApprovalAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Onaya gönderildi." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/yayinla")]
    [Authorize(Policy = PolicyNames.CanPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await resourceService.PublishAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Yayınlandı." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task LoadFacultiesAsync(CancellationToken cancellationToken) =>
        ViewBag.Faculties = await campusService.ListFacultiesAsync(cancellationToken);
}