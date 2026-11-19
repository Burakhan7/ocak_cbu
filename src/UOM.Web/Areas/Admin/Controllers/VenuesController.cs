using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Areas.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteProvinceContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/mekanlar")]
public sealed class VenuesController(VenueAdminService venueService, MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await venueService.ListAsync(cancellationToken));

    [HttpGet("yeni")]
    public IActionResult Create() => View("Edit", new Venue());

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var venue = await venueService.GetAsync(id, cancellationToken);
        if (venue is null)
        {
            return NotFound();
        }

        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, venue.CoverImageId, cancellationToken);
        return View(venue);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        Venue model,
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
                $"Mekan: {model.Name}",
                id => model.CoverImageId = id,
                cancellationToken))
        {
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        var result = await venueService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        TempData["Success"] = "Mekan kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await venueService.DeleteAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Kayıt silindi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}