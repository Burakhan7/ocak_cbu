using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Abstractions;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Areas.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteProvinceContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/haberler")]
public sealed class NewsPostsController(
    NewsPostAdminService newsService,
    MediaAdminService mediaAdmin,
    IPreviewTokenService previewTokenService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await newsService.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet("yeni")]
  public IActionResult Create() => View("Edit", new NewsPost());

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var post = await newsService.GetAsync(id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, post.CoverImageId, cancellationToken);
        return View(post);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        NewsPost model,
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

        var result = await newsService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        TempData["Success"] = "Haber kaydedildi.";
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
        var result = await newsService.SubmitForApprovalAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Haber onaya gönderildi."
            : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/yayinla")]
    [Authorize(Policy = PolicyNames.CanPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await newsService.PublishAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Haber yayınlandı."
            : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/reddet")]
    [Authorize(Policy = PolicyNames.CanPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string rejectionNote, CancellationToken cancellationToken)
    {
        var result = await newsService.RejectAsync(id, rejectionNote, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Haber reddedildi."
            : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet("{id:int}/onizleme-linki")]
    public async Task<IActionResult> PreviewLink(int id, CancellationToken cancellationToken)
    {
        var post = await newsService.GetAsync(id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var token = previewTokenService.CreateToken("haber", id, TimeSpan.FromHours(24));
        var url = Url.Action("News", "Preview", new { area = "", id, token }, Request.Scheme);
        TempData["PreviewUrl"] = url;
        return RedirectToAction(nameof(Edit), new { id });
    }
}