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
[Route("yonetim/rehber")]
public sealed class GuideArticlesController(GuideArticleAdminService guideService, MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await guideService.ListAsync(cancellationToken));

    [HttpGet("yeni")]
    public IActionResult Create() => View("Edit", new GuideArticle());

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var article = await guideService.GetAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, article.CoverImageId, cancellationToken);
        return View(article);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        GuideArticle model,
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
                $"Kapak: {model.Title}",
                id => model.CoverImageId = id,
                cancellationToken))
        {
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        var result = await guideService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        TempData["Success"] = "Rehber kaydedildi.";
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
        var result = await guideService.SubmitForApprovalAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Onaya gönderildi." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/yayinla")]
    [Authorize(Policy = PolicyNames.CanPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await guideService.PublishAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Yayınlandı." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await guideService.DeleteAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Kayıt silindi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}