using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;
using UOM.Domain.Enums;

namespace UOM.Web.Controllers;

[Route("duyurular")]
public sealed class AnnouncementController(IPublicAnnouncementService announcements) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(AnnouncementType? tur, CancellationToken cancellationToken)
    {
        var items = await announcements.ListListableAsync(tur, cancellationToken);
        ViewBag.Type = tur;
        return View(items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var item = await announcements.GetBySlugAsync(slug, cancellationToken);
        return item is null ? NotFound() : View(item);
    }
}