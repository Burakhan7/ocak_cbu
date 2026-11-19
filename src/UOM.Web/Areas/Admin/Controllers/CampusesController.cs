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
[Route("yonetim/kampusler")]
public sealed class CampusesController(CampusAdminService campusService, MediaAdminService mediaAdmin) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await campusService.ListCampusesAsync(cancellationToken));

    [HttpGet("yeni")]
    public IActionResult Create() => View("Edit", new Campus());

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var campus = await campusService.GetCampusAsync(id, cancellationToken);
        if (campus is null)
        {
            return NotFound();
        }

        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, campus.CoverImageId, cancellationToken);
        return View(campus);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        Campus model,
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
                $"Kampüs: {model.Name}",
                id => model.CoverImageId = id,
                cancellationToken))
        {
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        var result = await campusService.SaveCampusAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("Edit", model);
        }

        TempData["Success"] = "Kampüs kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("fakulteler")]
    public async Task<IActionResult> Faculties(CancellationToken cancellationToken) =>
        View(await campusService.ListFacultiesAsync(cancellationToken));

    [HttpGet("fakulteler/yeni")]
    public async Task<IActionResult> CreateFaculty(CancellationToken cancellationToken)
    {
        ViewBag.Campuses = await campusService.ListCampusesAsync(cancellationToken);
        return View("EditFaculty", new Faculty());
    }

    [HttpGet("fakulteler/{id:int}/duzenle")]
    public async Task<IActionResult> EditFaculty(int id, CancellationToken cancellationToken)
    {
        var faculty = await campusService.GetFacultyAsync(id, cancellationToken);
        if (faculty is null)
        {
            return NotFound();
        }

        ViewBag.Campuses = await campusService.ListCampusesAsync(cancellationToken);
        await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, faculty.CoverImageId, cancellationToken);
        return View(faculty);
    }

    [HttpPost("fakulteler/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFaculty(
        Faculty model,
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
                $"Fakülte: {model.Name}",
                id => model.CoverImageId = id,
                cancellationToken))
        {
            ViewBag.Campuses = await campusService.ListCampusesAsync(cancellationToken);
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("EditFaculty", model);
        }

        var result = await campusService.SaveFacultyAsync(model, cancellationToken);
        ViewBag.Campuses = await campusService.ListCampusesAsync(cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            await AdminMediaFormBinding.SetCoverImagePreviewAsync(mediaAdmin, ViewData, model.CoverImageId, cancellationToken);
            return View("EditFaculty", model);
        }

        TempData["Success"] = "Fakülte kaydedildi.";
        return RedirectToAction(nameof(Faculties));
    }
}