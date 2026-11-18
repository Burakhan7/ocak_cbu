using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteProvinceContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/mekanlar")]
public sealed class VenuesController(VenueAdminService venueService) : Controller
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
        return venue is null ? NotFound() : View(venue);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Venue model, CancellationToken cancellationToken)
    {
        var result = await venueService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            return View("Edit", model);
        }

        TempData["Success"] = "Mekan kaydedildi.";
        return RedirectToAction(nameof(Index));
    }
}