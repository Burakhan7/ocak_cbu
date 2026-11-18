using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Authorization;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Admin;
using UOM.Infrastructure.Persistence;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteProvinceContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/etkinlikler")]
public sealed class EventsController(
    EventAdminService eventService,
    AppDbContext db,
    IPreviewTokenService previewTokenService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await eventService.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet("yeni")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadVenuesAsync(cancellationToken);
        return View("Edit", new Event { StartsAtUtc = DateTime.UtcNow.AddDays(7) });
    }

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var entity = await eventService.GetAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        await LoadVenuesAsync(cancellationToken);
        return View(entity);
    }

    [HttpPost("kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Event model, CancellationToken cancellationToken)
    {
        var result = await eventService.SaveAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            await LoadVenuesAsync(cancellationToken);
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt başarısız.");
            return View("Edit", model);
        }

        TempData["Success"] = "Etkinlik kaydedildi.";
        return RedirectToAction(nameof(Edit), new { id = result.Value!.Id });
    }

    [HttpPost("{id:int}/onaya-gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.SubmitForApprovalAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Etkinlik onaya gönderildi."
            : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/yayinla")]
    [Authorize(Policy = PolicyNames.CanPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.PublishAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Etkinlik yayınlandı."
            : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/reddet")]
    [Authorize(Policy = PolicyNames.CanPublish)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string rejectionNote, CancellationToken cancellationToken)
    {
        var result = await eventService.RejectAsync(id, rejectionNote, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Etkinlik reddedildi."
            : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet("{id:int}/onizleme-linki")]
    public IActionResult PreviewLink(int id)
    {
        var token = previewTokenService.CreateToken("etkinlik", id, TimeSpan.FromHours(24));
        TempData["PreviewUrl"] = Url.Action("Event", "Preview", new { area = "", id, token }, Request.Scheme);
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task LoadVenuesAsync(CancellationToken cancellationToken)
    {
        var venues = await db.Venues.OrderBy(v => v.Name).ToListAsync(cancellationToken);
        ViewBag.Venues = new SelectList(venues, "Id", "Name");
    }
}