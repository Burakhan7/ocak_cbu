using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteCampusContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/kampusler")]
public sealed class CampusesController(CampusAdminService campusService) : Controller
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
        return campus is null ? NotFound() : View(campus);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Campus model, CancellationToken cancellationToken)
    {
        var result = await campusService.SaveCampusAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
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
        return View(faculty);
    }

    [HttpPost("fakulteler/kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFaculty(Faculty model, CancellationToken cancellationToken)
    {
        var result = await campusService.SaveFacultyAsync(model, cancellationToken);
        ViewBag.Campuses = await campusService.ListCampusesAsync(cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            return View("EditFaculty", model);
        }

        TempData["Success"] = "Fakülte kaydedildi.";
        return RedirectToAction(nameof(Faculties));
    }
}